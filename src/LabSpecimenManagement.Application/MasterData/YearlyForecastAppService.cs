using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.MasterData.YearlyForecasts;
using LabSpecimenManagement.Products;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// L4 — Dự toán số mẫu năm theo dịch vụ. Danh sách tự sinh từ các phương pháp đã có định mức (L3).
/// Tiêu hao / mẫu = Tổng tiêu hao 1 mẻ (L3) ÷ Số mẫu dự toán năm (L4).
/// </summary>
[RemoteService(IsEnabled = false)]
public partial class YearlyForecastAppService : ApplicationService, IYearlyForecastAppService
{
    private readonly IRepository<ServiceYearlyForecast, Guid> _forecastRepository;
    private readonly IRepository<TestMethodIndicator, Guid> _serviceRepository;
    private readonly IRepository<ServiceBatchNorm, Guid> _serviceNormRepository;
    private readonly IRepository<MethodBatchNorm, Guid> _normRepository;
    private readonly IRepository<TestMethod, Guid> _testMethodRepository;
    private readonly IRepository<Indicator, Guid> _indicatorRepository;
    private readonly IRepository<SpecimenType, Guid> _specimenTypeRepository;
    private readonly IRepository<SampleTypeCategory, Guid> _sampleTypeCategoryRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<Unit, Guid> _unitRepository;

    public YearlyForecastAppService(
        IRepository<ServiceYearlyForecast, Guid> forecastRepository,
        IRepository<TestMethodIndicator, Guid> serviceRepository,
        IRepository<ServiceBatchNorm, Guid> serviceNormRepository,
        IRepository<MethodBatchNorm, Guid> normRepository,
        IRepository<TestMethod, Guid> testMethodRepository,
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<SpecimenType, Guid> specimenTypeRepository,
        IRepository<SampleTypeCategory, Guid> sampleTypeCategoryRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<Unit, Guid> unitRepository)
    {
        _forecastRepository = forecastRepository;
        _serviceRepository = serviceRepository;
        _serviceNormRepository = serviceNormRepository;
        _normRepository = normRepository;
        _testMethodRepository = testMethodRepository;
        _indicatorRepository = indicatorRepository;
        _specimenTypeRepository = specimenTypeRepository;
        _sampleTypeCategoryRepository = sampleTypeCategoryRepository;
        _productRepository = productRepository;
        _unitRepository = unitRepository;
    }

    private int CurrentYear => Clock.Now.Year;

    public async Task<YearlyForecastListDto> GetListAsync(GetYearlyForecastsInput input)
    {
        var year = input.Year;
        var isLocked = year < CurrentYear;

        // Định mức gắn theo dịch vụ (nền mẫu × chỉ tiêu × PP), chỉ lấy định mức đang sử dụng
        var headers = (await _serviceNormRepository.GetListAsync(h => h.IsActive)).ToDictionary(h => h.Id, h => h.TestMethodIndicatorId);
        var normServiceIds = headers.Values.Distinct().ToList();
        var headerIds = headers.Keys.ToList();
        var norms = headerIds.Count == 0
            ? new List<MethodBatchNorm>()
            : await _normRepository.GetListAsync(n => n.IsActive && n.ServiceBatchNormId != null && headerIds.Contains(n.ServiceBatchNormId.Value));

        var forecasts = await _forecastRepository.GetListAsync(f => f.Year == year || f.Year == year - 1);
        var current = forecasts.Where(f => f.Year == year).ToDictionary(f => f.TestMethodIndicatorId);
        var previous = forecasts.Where(f => f.Year == year - 1).ToDictionary(f => f.TestMethodIndicatorId);

        // Dịch vụ có định mức (bỏ thầu phụ) + dịch vụ đã có dự toán năm này (giữ lịch sử khi định mức bị ngừng)
        var forecastServiceIds = current.Keys.ToList();
        var services = await _serviceRepository.GetListAsync(s =>
            (s.IsActive && !s.IsSubcontract && normServiceIds.Contains(s.Id)) || forecastServiceIds.Contains(s.Id));

        var methodIds = services.Select(s => s.TestMethodId).Distinct().ToList();
        var indicatorIds = services.Select(s => s.IndicatorId).Distinct().ToList();
        var specimenTypeIds = services.Where(s => s.SpecimenTypeId.HasValue).Select(s => s.SpecimenTypeId!.Value).Distinct().ToList();

        var methods = (await _testMethodRepository.GetListAsync(m => methodIds.Contains(m.Id))).ToDictionary(m => m.Id);
        var indicators = (await _indicatorRepository.GetListAsync(i => indicatorIds.Contains(i.Id))).ToDictionary(i => i.Id);
        var specimenTypes = specimenTypeIds.Count == 0
            ? new Dictionary<Guid, SpecimenType>()
            : (await _specimenTypeRepository.GetListAsync(st => specimenTypeIds.Contains(st.Id))).ToDictionary(st => st.Id);
        var categoryIds = specimenTypes.Values
            .Where(st => st.SampleTypeCategoryId.HasValue)
            .Select(st => st.SampleTypeCategoryId!.Value)
            .Distinct()
            .ToList();
        var categories = categoryIds.Count == 0
            ? new Dictionary<Guid, SampleTypeCategory>()
            : (await _sampleTypeCategoryRepository.GetListAsync(c => categoryIds.Contains(c.Id))).ToDictionary(c => c.Id);

        var completeNorms = norms.Where(n => n.IsComplete).ToList();
        var productIds = completeNorms.Select(n => n.ProductId!.Value).Distinct().ToList();
        var unitIds = completeNorms.Where(n => n.UnitId.HasValue).Select(n => n.UnitId!.Value).Distinct().ToList();
        var products = productIds.Count == 0
            ? new Dictionary<Guid, Product>()
            : (await _productRepository.GetListAsync(p => productIds.Contains(p.Id))).ToDictionary(p => p.Id);
        var units = unitIds.Count == 0
            ? new Dictionary<Guid, Unit>()
            : (await _unitRepository.GetListAsync(u => unitIds.Contains(u.Id))).ToDictionary(u => u.Id);
        var normsByService = completeNorms.GroupBy(n => headers[n.ServiceBatchNormId!.Value]).ToDictionary(g => g.Key, g => g.ToList());

        var rows = new List<YearlyForecastRowDto>();
        foreach (var service in services)
        {
            if (!methods.TryGetValue(service.TestMethodId, out var method)
                || !indicators.TryGetValue(service.IndicatorId, out var indicator))
            {
                continue;
            }

            var estimated = current.GetValueOrDefault(service.Id)?.EstimatedSamples;
            var specimenType = service.SpecimenTypeId.HasValue ? specimenTypes.GetValueOrDefault(service.SpecimenTypeId.Value) : null;
            var category = specimenType?.SampleTypeCategoryId is { } categoryId ? categories.GetValueOrDefault(categoryId) : null;

            rows.Add(new YearlyForecastRowDto
            {
                TestMethodIndicatorId = service.Id,
                ServiceCode = service.ServiceCode,
                TestMethodId = method.Id,
                TestMethodCode = method.Code,
                TestMethodName = method.Name,
                IndicatorId = indicator.Id,
                IndicatorCode = indicator.Code,
                IndicatorName = indicator.Name,
                IndicatorFormattedName = indicator.FormattedName,
                SpecimenTypeId = service.SpecimenTypeId,
                SpecimenTypeName = specimenType?.Name,
                SpecimenTypeSortOrder = specimenType?.SortOrder ?? 0,
                SampleTypeCategoryId = category?.Id,
                SampleTypeCategoryName = category?.Name,
                EstimatedSamples = estimated,
                PreviousYearSamples = previous.GetValueOrDefault(service.Id)?.EstimatedSamples,
                NormLines = (normsByService.GetValueOrDefault(service.Id) ?? new List<MethodBatchNorm>())
                    .Select(n =>
                    {
                        var product = products.GetValueOrDefault(n.ProductId!.Value);
                        return new ForecastNormLineDto
                        {
                            MethodBatchNormId = n.Id,
                            ProductCode = product?.Code,
                            ProductName = product?.Name,
                            UnitName = n.UnitId.HasValue ? units.GetValueOrDefault(n.UnitId.Value)?.Name : null,
                            BatchQuantity = n.BatchQuantity!.Value,
                            PerSample = ConsumptionNormResolver.ComputePerSample(n.BatchQuantity, estimated),
                        };
                    })
                    .ToList(),
            });
        }

        rows = rows
            .OrderBy(r => r.TestMethodName)
            .ThenBy(r => r.IndicatorName)
            .ThenBy(r => r.SpecimenTypeName)
            .ToList();

        var result = new YearlyForecastListDto
        {
            Year = year,
            IsLocked = isLocked,
            CanClone = !isLocked && previous.Values.Any(f => f.EstimatedSamples > 0),
            TotalCount = rows.Count,
            MissingCount = rows.Count(r => r.EstimatedSamples is not > 0),
            PreviousYearTotal = previous.Values.Sum(f => f.EstimatedSamples is > 0 ? f.EstimatedSamples.Value : 0),
        };

        IEnumerable<YearlyForecastRowDto> filtered = rows;
        if (input.TestMethodId.HasValue)
        {
            filtered = filtered.Where(r => r.TestMethodId == input.TestMethodId.Value);
        }
        if (input.OnlyMissing)
        {
            filtered = filtered.Where(r => r.EstimatedSamples is not > 0);
        }
        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            var keyword = input.Keyword.Trim();
            filtered = filtered.Where(r =>
                (r.ServiceCode?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)
                || r.IndicatorName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || r.IndicatorCode.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || r.TestMethodName.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                || (r.SpecimenTypeName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        result.Items = filtered.ToList();
        return result;
    }

    public async Task<int> SaveAsync(SaveYearlyForecastsInput input)
    {
        EnsureEditableYear(input.Year);

        var items = input.Items
            .GroupBy(i => i.TestMethodIndicatorId)
            .Select(g => g.Last())
            .ToList();
        if (items.Any(i => i.EstimatedSamples is < 0))
        {
            throw new UserFriendlyException("Số mẫu dự toán không được âm");
        }

        var serviceIds = items.Select(i => i.TestMethodIndicatorId).ToList();
        var existingServiceIds = (await _serviceRepository.GetListAsync(s => serviceIds.Contains(s.Id)))
            .Select(s => s.Id)
            .ToHashSet();
        var forecasts = (await _forecastRepository.GetListAsync(f => f.Year == input.Year && serviceIds.Contains(f.TestMethodIndicatorId)))
            .ToDictionary(f => f.TestMethodIndicatorId);

        var changed = 0;
        foreach (var item in items.Where(i => existingServiceIds.Contains(i.TestMethodIndicatorId)))
        {
            var value = item.EstimatedSamples is > 0 ? item.EstimatedSamples : null;
            changed += await UpsertAsync(forecasts.GetValueOrDefault(item.TestMethodIndicatorId), item.TestMethodIndicatorId, input.Year, value)
                ? 1
                : 0;
        }

        // Xóa dự toán đang là nguồn "áp dụng tiêu hao" → các bộ áp dụng theo nó quay về định mức của chính mình
        var removedIds = items
            .Where(i => i.EstimatedSamples is not > 0)
            .Select(i => forecasts.GetValueOrDefault(i.TestMethodIndicatorId)?.Id)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();
        if (removedIds.Count > 0)
        {
            // Dự toán bị xóa bỏ luôn cấu hình (vật tư/thiết bị bổ sung, ẩn, liên kết nguồn) → nhập lại số mẫu không hồi sinh cấu hình cũ
            var unlinked = forecasts.Values.Where(f => removedIds.Contains(f.Id) && f.SourceForecastId != null).ToList();
            foreach (var forecast in unlinked)
            {
                forecast.SourceForecastId = null;
            }

            if (unlinked.Count > 0)
            {
                await _forecastRepository.UpdateManyAsync(unlinked);
            }

            await ItemRepository.DeleteAsync(i => removedIds.Contains(i.ForecastId));

            var targets = await _forecastRepository.GetListAsync(f => f.SourceForecastId != null && removedIds.Contains(f.SourceForecastId.Value));
            foreach (var target in targets)
            {
                target.SourceForecastId = null;
            }

            if (targets.Count > 0)
            {
                await _forecastRepository.UpdateManyAsync(targets);
            }
        }

        return changed;
    }

    /// <summary>WF-1: copy dự toán năm trước vào các dòng năm nay còn trống (không ghi đè)</summary>
    public async Task<CloneYearlyForecastResultDto> CloneFromPreviousYearAsync(CloneYearlyForecastInput input)
    {
        EnsureEditableYear(input.Year);

        var previous = await _forecastRepository.GetListAsync(f => f.Year == input.Year - 1 && f.EstimatedSamples > 0);
        if (previous.Count == 0)
        {
            throw new UserFriendlyException($"Năm {input.Year - 1} chưa có dữ liệu dự toán để clone");
        }

        var serviceIds = previous.Select(f => f.TestMethodIndicatorId).ToList();
        var liveServiceIds = (await _serviceRepository.GetListAsync(s => serviceIds.Contains(s.Id)))
            .Select(s => s.Id)
            .ToHashSet();
        var current = (await _forecastRepository.GetListAsync(f => f.Year == input.Year && serviceIds.Contains(f.TestMethodIndicatorId)))
            .ToDictionary(f => f.TestMethodIndicatorId);

        var cloned = 0;
        foreach (var source in previous.Where(f => liveServiceIds.Contains(f.TestMethodIndicatorId)))
        {
            var target = current.GetValueOrDefault(source.TestMethodIndicatorId);
            if (target?.EstimatedSamples is > 0)
            {
                continue;
            }

            await UpsertAsync(target, source.TestMethodIndicatorId, input.Year, source.EstimatedSamples);
            cloned++;
        }

        return new CloneYearlyForecastResultDto { ClonedCount = cloned };
    }

    private async Task<bool> UpsertAsync(ServiceYearlyForecast? forecast, Guid serviceId, int year, int? estimatedSamples)
    {
        if (forecast == null)
        {
            if (estimatedSamples == null)
            {
                return false;
            }

            await _forecastRepository.InsertAsync(new ServiceYearlyForecast(GuidGenerator.Create(), serviceId, year, estimatedSamples));
            return true;
        }

        if (forecast.EstimatedSamples == estimatedSamples)
        {
            return false;
        }

        forecast.EstimatedSamples = estimatedSamples;
        await _forecastRepository.UpdateAsync(forecast);
        return true;
    }

    /// <summary>BR-07: năm đã qua chỉ xem; chỉ mở năm hiện tại và năm sau</summary>
    private void EnsureEditableYear(int year)
    {
        if (year < CurrentYear)
        {
            throw new UserFriendlyException($"Năm {year} đã khóa, chỉ được xem");
        }
        if (year > CurrentYear + 1)
        {
            throw new UserFriendlyException($"Chỉ được lập dự toán cho năm {CurrentYear} và {CurrentYear + 1}");
        }
    }
}
