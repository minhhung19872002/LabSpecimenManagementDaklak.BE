using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.MasterData.ServiceCatalog;
using LabSpecimenManagement.Subcontractors;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// L2 — Dịch vụ & Bảng giá. 1 dịch vụ = 1 dòng TestMethodIndicator (Phương pháp + Chỉ tiêu + Nền mẫu).
/// </summary>
[RemoteService(IsEnabled = false)]
public class ServiceCatalogAppService : ApplicationService, IServiceCatalogAppService
{
    private readonly IRepository<TestMethodIndicator, Guid> _serviceRepository;
    private readonly IRepository<TestMethod, Guid> _testMethodRepository;
    private readonly IRepository<Indicator, Guid> _indicatorRepository;
    private readonly IRepository<IndicatorGroup, Guid> _indicatorGroupRepository;
    private readonly IRepository<SpecimenType, Guid> _specimenTypeRepository;
    private readonly IRepository<ServicePriceVersion, Guid> _versionRepository;
    private readonly IRepository<Subcontractor, Guid> _subcontractorRepository;
    private readonly IRepository<ServiceGroup, Guid> _serviceGroupRepository;
    private readonly ServicePriceManager _priceManager;
    private readonly IUserNameProvider _userNameProvider;

    public ServiceCatalogAppService(
        IRepository<TestMethodIndicator, Guid> serviceRepository,
        IRepository<TestMethod, Guid> testMethodRepository,
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<IndicatorGroup, Guid> indicatorGroupRepository,
        IRepository<SpecimenType, Guid> specimenTypeRepository,
        IRepository<ServicePriceVersion, Guid> versionRepository,
        IRepository<Subcontractor, Guid> subcontractorRepository,
        IRepository<ServiceGroup, Guid> serviceGroupRepository,
        ServicePriceManager priceManager,
        IUserNameProvider userNameProvider)
    {
        _serviceGroupRepository = serviceGroupRepository;
        _serviceRepository = serviceRepository;
        _testMethodRepository = testMethodRepository;
        _indicatorRepository = indicatorRepository;
        _indicatorGroupRepository = indicatorGroupRepository;
        _specimenTypeRepository = specimenTypeRepository;
        _versionRepository = versionRepository;
        _subcontractorRepository = subcontractorRepository;
        _priceManager = priceManager;
        _userNameProvider = userNameProvider;
    }

    public async Task<ListResultDto<ServiceCatalogItemDto>> GetListAsync(GetServiceCatalogInput input)
    {
        // Version đến hạn nhưng worker chưa chạy → đồng bộ trước khi trả giá hôm nay
        await _priceManager.ApplyDuePricesAsync();

        var query = await _serviceRepository.GetQueryableAsync();
        if (input.TestMethodId.HasValue)
        {
            query = query.Where(s => s.TestMethodId == input.TestMethodId.Value);
        }
        if (input.IsActive.HasValue)
        {
            query = query.Where(s => s.IsActive == input.IsActive.Value);
        }
        if (input.IndicatorGroupId.HasValue)
        {
            var indicatorQuery = await _indicatorRepository.GetQueryableAsync();
            var groupId = input.IndicatorGroupId.Value;
            query = query.Where(s => indicatorQuery.Any(i => i.Id == s.IndicatorId && i.IndicatorGroupId == groupId));
        }

        var services = await AsyncExecuter.ToListAsync(query);
        var dtos = await BuildDtosAsync(services, input.PriceDate ?? Clock.Now);

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            var keyword = input.Keyword.Trim();
            dtos = dtos.Where(d =>
                    Contains(d.ServiceCode, keyword)
                    || Contains(d.ServiceGroupCode, keyword) || Contains(d.ServiceGroupName, keyword)
                    || Contains(d.IndicatorCode, keyword) || Contains(d.IndicatorName, keyword)
                    || Contains(d.TestMethodCode, keyword) || Contains(d.TestMethodName, keyword)
                    || Contains(d.SpecimenTypeName, keyword))
                .ToList();
        }

        return new ListResultDto<ServiceCatalogItemDto>(dtos);
    }

    public async Task<BulkCreateServicesResultDto> BulkCreateAsync(BulkCreateServicesInput input)
    {
        var testMethod = await _testMethodRepository.GetAsync(input.TestMethodId);
        if (input.Items.Count == 0)
        {
            throw new UserFriendlyException("Chưa chọn chỉ tiêu");
        }

        var indicatorIds = input.Items.Select(r => r.IndicatorId).Distinct().ToList();
        var existingIndicators = (await _indicatorRepository.GetListAsync(i => indicatorIds.Contains(i.Id)))
            .ToDictionary(i => i.Id);
        var missingIndicator = indicatorIds.FirstOrDefault(id => !existingIndicators.ContainsKey(id));
        if (missingIndicator != Guid.Empty)
        {
            throw new UserFriendlyException("Chỉ tiêu không tồn tại hoặc đã bị xóa");
        }

        var existing = await _serviceRepository.GetListAsync(s => s.TestMethodId == input.TestMethodId);
        var existingKeys = existing.Select(s => (s.IndicatorId, s.SpecimenTypeId)).ToHashSet();
        var sortOrder = existing.Count > 0 ? existing.Max(s => s.SortOrder) : 0;

        var subcontractorIds = input.Items
            .Where(i => i.IsSubcontract && i.SubcontractorId.HasValue)
            .Select(i => i.SubcontractorId!.Value)
            .Distinct()
            .ToList();
        if (subcontractorIds.Count > 0)
        {
            var found = await _subcontractorRepository.CountAsync(s => subcontractorIds.Contains(s.Id));
            if (found != subcontractorIds.Count)
            {
                throw new UserFriendlyException("Đơn vị nhận thầu không tồn tại hoặc đã bị xóa");
            }
        }

        var newItems = new List<BulkCreateServiceItemInput>();
        var skipped = 0;
        foreach (var item in input.Items)
        {
            // SpecimenTypeId null → dịch vụ áp dụng mọi nền mẫu.
            // BA: 1 dịch vụ = chỉ tiêu × nền mẫu × phương pháp → tổ hợp đã có (dù thuộc dịch vụ nào) thì bỏ qua
            if (!existingKeys.Add((item.IndicatorId, item.SpecimenTypeId)))
            {
                skipped++;
                continue;
            }
            if (string.IsNullOrWhiteSpace(item.Unit) && string.IsNullOrWhiteSpace(testMethod.Unit))
            {
                throw new UserFriendlyException("Chưa nhập ĐVT (tiêu hao) cho chỉ tiêu "
                                                + existingIndicators[item.IndicatorId].Name);
            }
            newItems.Add(item);
        }

        if (newItems.Count == 0)
        {
            return new BulkCreateServicesResultDto { SkippedCount = skipped };
        }

        var serviceGroup = await ResolveServiceGroupAsync(input.ServiceGroupId, input.ServiceGroupName);

        var created = newItems.Select(item =>
        {
            var unit = string.IsNullOrWhiteSpace(item.Unit) ? testMethod.Unit : item.Unit.Trim();
            // ĐVT giữ như phương pháp → dùng bản có định dạng của phương pháp; sửa tay → text thường
            var formattedUnit = unit == testMethod.Unit ? testMethod.FormattedUnit : System.Net.WebUtility.HtmlEncode(unit);
            return new TestMethodIndicator(
                GuidGenerator.Create(),
                input.TestMethodId,
                item.IndicatorId,
                item.UnitPrice,
                item.SpecimenTypeId,
                unit,
                formattedUnit)
            {
                SortOrder = ++sortOrder,
                ServiceGroupId = serviceGroup.Id,
                IsSubcontract = item.IsSubcontract,
                SubcontractorId = item.IsSubcontract ? item.SubcontractorId : null,
            };
        }).ToList();

        await _serviceRepository.InsertManyAsync(created, autoSave: true);

        // Giá có hiệu lực ngay (BA: không có ngày áp dụng / TAT trên phiếu khai báo) — vẫn ghi log phiên bản giá.
        // Ô để trống = Chờ định giá → chưa có phiên bản, đặt giá lần đầu trên Ma trận sẽ tạo.
        var today = _priceManager.Today;
        await _versionRepository.InsertManyAsync(created
            .Where(s => s.UnitPrice.HasValue)
            .Select(s => new ServicePriceVersion(GuidGenerator.Create(), s.Id, s.UnitPrice!.Value, null, today, "Tạo dịch vụ")), autoSave: true);

        // Chỉ tiêu đã có phương pháp → bỏ cờ "cần thiết lập phương pháp"
        if (testMethod.Name != "-")
        {
            var createdIndicatorIds = created.Select(s => s.IndicatorId).ToHashSet();
            foreach (var indicator in existingIndicators.Values.Where(i => i.NeedsMethodSetup && createdIndicatorIds.Contains(i.Id)))
            {
                indicator.NeedsMethodSetup = false;
                await _indicatorRepository.UpdateAsync(indicator);
            }
        }

        return new BulkCreateServicesResultDto
        {
            ServiceGroupId = serviceGroup.Id,
            CreatedCount = created.Count,
            SkippedCount = skipped,
        };
    }

    public async Task<ListResultDto<ServiceGroupDto>> GetServiceGroupsAsync()
    {
        var groups = await _serviceGroupRepository.GetListAsync();
        return new ListResultDto<ServiceGroupDto>(groups
            .OrderBy(g => g.Name)
            .Select(MapServiceGroup)
            .ToList());
    }

    public async Task<ServiceGroupDto> CreateServiceGroupAsync(CreateServiceGroupInput input)
    {
        var name = input.Name?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            throw new UserFriendlyException("Chưa nhập tên dịch vụ");
        }
        if (await FindServiceGroupByNameAsync(name) != null)
        {
            throw new UserFriendlyException($"Dịch vụ \"{name}\" đã tồn tại");
        }

        var group = await _serviceGroupRepository.InsertAsync(new ServiceGroup(GuidGenerator.Create(), null, name), autoSave: true);
        return MapServiceGroup(group);
    }

    public async Task<ServiceGroupDto> UpdateServiceGroupAsync(Guid id, CreateServiceGroupInput input)
    {
        var group = await _serviceGroupRepository.GetAsync(id);
        var name = input.Name?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            throw new UserFriendlyException("Chưa nhập tên dịch vụ");
        }
        var existing = await FindServiceGroupByNameAsync(name);
        if (existing != null && existing.Id != id)
        {
            throw new UserFriendlyException($"Dịch vụ \"{name}\" đã tồn tại");
        }

        group.Name = name;
        await _serviceGroupRepository.UpdateAsync(group, autoSave: true);
        return MapServiceGroup(group);
    }

    public async Task<int> DeleteServiceGroupAsync(Guid id)
    {
        var group = await _serviceGroupRepository.GetAsync(id);
        // Xóa mềm nhóm → FK SetNull không chạy, phải tự gỡ nhóm khỏi các dòng
        var services = await _serviceRepository.GetListAsync(s => s.ServiceGroupId == id);
        foreach (var service in services)
        {
            service.ServiceGroupId = null;
        }
        await _serviceRepository.UpdateManyAsync(services);
        await _serviceGroupRepository.DeleteAsync(group, autoSave: true);
        return services.Count;
    }

    public async Task<int> AssignServicesAsync(AssignServicesInput input)
    {
        var ids = input.ServiceIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            throw new UserFriendlyException("Chưa chọn dịch vụ");
        }
        if (input.ChangeServiceGroup && input.ServiceGroupId.HasValue
            && !await _serviceGroupRepository.AnyAsync(g => g.Id == input.ServiceGroupId.Value))
        {
            throw new UserFriendlyException("Tên dịch vụ không tồn tại hoặc đã bị xóa");
        }

        var services = await _serviceRepository.GetListAsync(s => ids.Contains(s.Id));
        var changed = 0;
        foreach (var service in services)
        {
            var dirty = false;
            if (input.ChangeServiceGroup && service.ServiceGroupId != input.ServiceGroupId)
            {
                service.ServiceGroupId = input.ServiceGroupId;
                dirty = true;
            }
            if (input.IsSubcontract.HasValue && service.IsSubcontract != input.IsSubcontract.Value)
            {
                service.IsSubcontract = input.IsSubcontract.Value;
                if (!service.IsSubcontract)
                {
                    service.SubcontractorId = null;
                }
                dirty = true;
            }
            if (dirty)
            {
                changed++;
            }
        }
        await _serviceRepository.UpdateManyAsync(services, autoSave: true);
        return changed;
    }

    /// <summary>Dịch vụ có sẵn theo Id, hoặc theo tên (trùng tên → dùng lại, chưa có → tạo mới, DB sinh mã NDV)</summary>
    private async Task<ServiceGroup> ResolveServiceGroupAsync(Guid? serviceGroupId, string? serviceGroupName)
    {
        if (serviceGroupId.HasValue)
        {
            return await _serviceGroupRepository.GetAsync(serviceGroupId.Value);
        }

        var name = serviceGroupName?.Trim();
        if (string.IsNullOrEmpty(name))
        {
            throw new UserFriendlyException("Chưa nhập tên dịch vụ");
        }

        return await FindServiceGroupByNameAsync(name)
               ?? await _serviceGroupRepository.InsertAsync(new ServiceGroup(GuidGenerator.Create(), null, name), autoSave: true);
    }

    private async Task<ServiceGroup?> FindServiceGroupByNameAsync(string name)
    {
        var lower = name.ToLower();
        return await _serviceGroupRepository.FirstOrDefaultAsync(g => g.Name.ToLower() == lower);
    }

    private static ServiceGroupDto MapServiceGroup(ServiceGroup g) => new()
    {
        Id = g.Id,
        Code = g.Code,
        Name = g.Name,
        IsActive = g.IsActive,
    };

    public async Task<ServiceCatalogItemDto> UpdateAsync(Guid id, UpdateServiceInput input)
    {
        var service = await _serviceRepository.GetAsync(id);
        service.Unit = input.Unit;
        service.FormattedUnit = input.FormattedUnit;
        service.IsSubcontract = input.IsSubcontract;
        service.SubcontractorId = input.IsSubcontract ? input.SubcontractorId : null;
        await _serviceRepository.UpdateAsync(service, autoSave: true);

        return (await BuildDtosAsync(new List<TestMethodIndicator> { service }, Clock.Now)).First();
    }

    public async Task<ListResultDto<ServicePriceVersionDto>> GetPriceVersionsAsync(Guid id)
    {
        await _serviceRepository.GetAsync(id);
        var versions = await _priceManager.GetVersionsAsync(id);
        var today = _priceManager.Today;
        var current = ServicePriceManager.ResolveAt(versions, today);

        var dtos = versions
            .OrderByDescending(v => v.EffectiveDate ?? DateTime.MinValue)
            .ThenByDescending(v => v.CreationTime)
            .Select(v => new ServicePriceVersionDto
            {
                Id = v.Id,
                TestMethodIndicatorId = v.TestMethodIndicatorId,
                UnitPrice = v.UnitPrice,
                TurnaroundDays = v.TurnaroundDays,
                EffectiveDate = v.EffectiveDate,
                Note = v.Note,
                CreationTime = v.CreationTime,
                Status = v.Id == current?.Id
                    ? ServicePriceVersionStatus.Current
                    : v.EffectiveDate > today
                        ? ServicePriceVersionStatus.Upcoming
                        : ServicePriceVersionStatus.Replaced,
            })
            .ToList();

        var creatorIds = versions.ToDictionary(v => v.Id, v => v.CreatorId);
        await _userNameProvider.EnrichWithUserNamesAsync(dtos, d => creatorIds[d.Id], (d, name) => d.CreatorName = name);

        return new ListResultDto<ServicePriceVersionDto>(dtos);
    }

    public async Task<ServiceCatalogItemDto> SetPriceAsync(Guid id, SetServicePriceInput input)
    {
        var service = await _serviceRepository.GetAsync(id);
        var versions = await _priceManager.GetVersionsAsync(id);
        var date = (input.EffectiveDate ?? Clock.Now).Date;

        // TAT bỏ trống → giữ TAT đang áp dụng tại ngày đó
        var turnaroundDays = input.TurnaroundDays
                             ?? ServicePriceManager.ResolveAt(versions, date)?.TurnaroundDays
                             ?? service.TurnaroundDays;

        await _priceManager.SetPriceAsync(service, input.UnitPrice, turnaroundDays, date, input.Note, versions);

        return (await BuildDtosAsync(new List<TestMethodIndicator> { service }, Clock.Now)).First();
    }

    public async Task DeletePriceVersionAsync(Guid versionId)
    {
        var version = await _versionRepository.GetAsync(versionId);
        if (version.EffectiveDate is null || version.EffectiveDate <= _priceManager.Today)
        {
            throw new UserFriendlyException("Chỉ xóa được phiên bản giá sắp áp dụng. Giá đã áp dụng được giữ lại để tra cứu lịch sử.");
        }

        await _versionRepository.DeleteAsync(version, autoSave: true);
    }

    public async Task<BulkUpdatePriceResultDto> BulkUpdatePriceAsync(BulkUpdatePriceInput input)
    {
        var ids = input.ServiceIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            throw new UserFriendlyException("Chưa chọn dịch vụ");
        }
        if (input.Mode == BulkPriceAdjustMode.Percent && !input.Increase && input.Value > 100)
        {
            throw new UserFriendlyException("Giảm giá không được quá 100%");
        }

        var services = await _serviceRepository.GetListAsync(s => ids.Contains(s.Id));
        var versionsByService = await _priceManager.GetVersionsAsync(ids);
        var date = input.EffectiveDate.Date;
        var note = input.Mode == BulkPriceAdjustMode.Percent
            ? $"Cập nhật hàng loạt {(input.Increase ? "+" : "-")}{input.Value:0.##}%"
            : $"Cập nhật hàng loạt {(input.Increase ? "+" : "-")}{input.Value:#,0} đ";

        var updated = 0;
        foreach (var service in services)
        {
            var versions = versionsByService.GetValueOrDefault(service.Id) ?? new List<ServicePriceVersion>();
            var basis = ServicePriceManager.ResolveAt(versions, date);
            var oldPrice = basis?.UnitPrice ?? service.UnitPrice;
            // Chờ định giá: chưa có giá gốc để tăng/giảm → bỏ qua (không biến thành 0 đ)
            if (oldPrice == null)
            {
                continue;
            }
            var newPrice = ComputeAdjustedPrice(oldPrice.Value, input.Mode, input.Increase, input.Value);
            var turnaroundDays = input.TurnaroundDays ?? basis?.TurnaroundDays ?? service.TurnaroundDays;

            await _priceManager.SetPriceAsync(service, newPrice, turnaroundDays, date, note, versions);
            updated++;
        }

        return new BulkUpdatePriceResultDto { UpdatedCount = updated };
    }

    public static decimal ComputeAdjustedPrice(decimal oldPrice, BulkPriceAdjustMode mode, bool increase, decimal value)
    {
        var delta = mode == BulkPriceAdjustMode.Percent ? oldPrice * value / 100m : value;
        var newPrice = increase ? oldPrice + delta : oldPrice - delta;
        return Math.Max(0, Math.Round(newPrice, 0, MidpointRounding.AwayFromZero));
    }

    public async Task<ImportServicePricesResultDto> ImportPricesAsync(ImportServicePricesInput input)
    {
        var codes = input.Rows
            .Select(r => r.ServiceCode?.Trim())
            .Where(c => !string.IsNullOrEmpty(c))
            .Select(c => c!.ToUpperInvariant())
            .Distinct()
            .ToList();

        var services = await _serviceRepository.GetListAsync(s => s.ServiceCode != null && codes.Contains(s.ServiceCode.ToUpper()));
        var serviceByCode = services.ToDictionary(s => s.ServiceCode!.ToUpperInvariant());
        var versionsByService = await _priceManager.GetVersionsAsync(services.Select(s => s.Id).ToList());
        var names = (await BuildDtosAsync(services, Clock.Now)).ToDictionary(d => d.Id, ServiceDisplayName);

        var result = new ImportServicePricesResultDto();
        var seenKeys = new HashSet<(string, DateTime)>();
        var today = _priceManager.Today;

        foreach (var row in input.Rows.OrderBy(r => r.Line))
        {
            var code = row.ServiceCode?.Trim().ToUpperInvariant();
            var date = (row.EffectiveDate ?? today).Date;
            var dto = new ImportServicePriceRowDto
            {
                Line = row.Line,
                ServiceCode = row.ServiceCode?.Trim(),
                NewPrice = row.UnitPrice,
                NewTurnaroundDays = row.TurnaroundDays,
                EffectiveDate = date,
            };
            result.Rows.Add(dto);

            if (string.IsNullOrEmpty(code))
            {
                SetError(dto, "Thiếu Service_ID");
                continue;
            }
            if (!serviceByCode.TryGetValue(code, out var service))
            {
                SetError(dto, "Service_ID không tồn tại trong hệ thống");
                continue;
            }

            dto.ServiceName = names.GetValueOrDefault(service.Id);
            var versions = versionsByService.GetValueOrDefault(service.Id) ?? new List<ServicePriceVersion>();
            var basis = ServicePriceManager.ResolveAt(versions, date);
            dto.OldPrice = basis?.UnitPrice ?? service.UnitPrice;
            dto.OldTurnaroundDays = basis?.TurnaroundDays ?? service.TurnaroundDays;

            if (row.UnitPrice is null or < 0)
            {
                SetError(dto, "Đơn giá trống hoặc âm");
                continue;
            }
            if (row.TurnaroundDays is < 0)
            {
                SetError(dto, "TAT không được âm");
                continue;
            }
            if (!seenKeys.Add((code, date)))
            {
                SetError(dto, "Trùng Service_ID + ngày hiệu lực với dòng phía trên");
                continue;
            }

            dto.NewTurnaroundDays = row.TurnaroundDays ?? dto.OldTurnaroundDays;
            if (dto.OldPrice == row.UnitPrice && dto.OldTurnaroundDays == dto.NewTurnaroundDays)
            {
                dto.Result = ImportServicePriceRowResult.Same;
                result.SameCount++;
                continue;
            }

            dto.Result = ImportServicePriceRowResult.Update;
            result.UpdateCount++;

            if (!input.DryRun)
            {
                await _priceManager.SetPriceAsync(service, row.UnitPrice.Value, dto.NewTurnaroundDays, date, "Import Excel", versions);
            }
        }

        result.Applied = !input.DryRun;
        return result;
    }

    public async Task ToggleActiveAsync(Guid id)
    {
        var service = await _serviceRepository.GetAsync(id);
        service.IsActive = !service.IsActive;
        await _serviceRepository.UpdateAsync(service);
    }

    /// <summary>BR-06: xóa mềm — phiếu cũ giữ snapshot tên / giá</summary>
    public async Task DeleteAsync(Guid id)
    {
        await _serviceRepository.DeleteAsync(id);
    }

    public async Task<int> DeleteManyAsync(List<Guid> ids)
    {
        var services = await _serviceRepository.GetListAsync(s => ids.Contains(s.Id));
        await _serviceRepository.DeleteManyAsync(services);
        return services.Count;
    }

    public Task<int> ApplyDuePricesAsync()
    {
        return _priceManager.ApplyDuePricesAsync();
    }

    private async Task<List<ServiceCatalogItemDto>> BuildDtosAsync(List<TestMethodIndicator> services, DateTime priceDate)
    {
        if (services.Count == 0)
        {
            return new List<ServiceCatalogItemDto>();
        }

        var methodIds = services.Select(s => s.TestMethodId).Distinct().ToList();
        var indicatorIds = services.Select(s => s.IndicatorId).Distinct().ToList();
        var specimenTypeIds = services.Where(s => s.SpecimenTypeId.HasValue).Select(s => s.SpecimenTypeId!.Value).Distinct().ToList();

        var methods = (await _testMethodRepository.GetListAsync(m => methodIds.Contains(m.Id))).ToDictionary(m => m.Id);
        var indicators = (await _indicatorRepository.GetListAsync(i => indicatorIds.Contains(i.Id))).ToDictionary(i => i.Id);
        var groupIds = indicators.Values.Select(i => i.IndicatorGroupId).Distinct().ToList();
        var groups = (await _indicatorGroupRepository.GetListAsync(g => groupIds.Contains(g.Id))).ToDictionary(g => g.Id);
        var specimenTypes = specimenTypeIds.Count == 0
            ? new Dictionary<Guid, SpecimenType>()
            : (await _specimenTypeRepository.GetListAsync(st => specimenTypeIds.Contains(st.Id))).ToDictionary(st => st.Id);
        var versionsByService = await _priceManager.GetVersionsAsync(services.Select(s => s.Id).ToList());
        var subcontractorIds = services.Where(s => s.SubcontractorId.HasValue).Select(s => s.SubcontractorId!.Value).Distinct().ToList();
        var subcontractorNames = subcontractorIds.Count == 0
            ? new Dictionary<Guid, string>()
            : (await _subcontractorRepository.GetListAsync(s => subcontractorIds.Contains(s.Id))).ToDictionary(s => s.Id, s => s.Name);
        var serviceGroupIds = services.Where(s => s.ServiceGroupId.HasValue).Select(s => s.ServiceGroupId!.Value).Distinct().ToList();
        var serviceGroups = serviceGroupIds.Count == 0
            ? new Dictionary<Guid, ServiceGroup>()
            : (await _serviceGroupRepository.GetListAsync(g => serviceGroupIds.Contains(g.Id))).ToDictionary(g => g.Id);

        var result = new List<ServiceCatalogItemDto>();
        foreach (var service in services)
        {
            // Chỉ tiêu đã xóa (mềm) → dịch vụ không còn hiển thị
            if (!indicators.TryGetValue(service.IndicatorId, out var indicator)
                || !methods.TryGetValue(service.TestMethodId, out var method))
            {
                continue;
            }

            var specimenType = service.SpecimenTypeId.HasValue ? specimenTypes.GetValueOrDefault(service.SpecimenTypeId.Value) : null;
            var versions = versionsByService.GetValueOrDefault(service.Id) ?? new List<ServicePriceVersion>();
            var atDate = ServicePriceManager.ResolveAt(versions, priceDate);
            var upcoming = ServicePriceManager.NextAfter(versions, priceDate);
            var serviceGroup = service.ServiceGroupId.HasValue ? serviceGroups.GetValueOrDefault(service.ServiceGroupId.Value) : null;

            result.Add(new ServiceCatalogItemDto
            {
                Id = service.Id,
                ServiceCode = service.ServiceCode,
                TestMethodId = method.Id,
                TestMethodCode = method.Code,
                TestMethodName = method.Name,
                TestMethodFormattedName = method.FormattedName,
                IndicatorId = indicator.Id,
                IndicatorCode = indicator.Code,
                IndicatorName = indicator.Name,
                IndicatorFormattedName = indicator.FormattedName,
                ParentIndicatorId = indicator.ParentIndicatorId,
                IndicatorGroupId = indicator.IndicatorGroupId,
                IndicatorGroupName = (indicator.IndicatorGroupId is Guid groupId ? groups.GetValueOrDefault(groupId)?.Name : null),
                // Dịch vụ đã xóa mềm → coi như chưa thuộc dịch vụ
                ServiceGroupId = serviceGroup?.Id,
                ServiceGroupCode = serviceGroup?.Code,
                ServiceGroupName = serviceGroup?.Name,
                SpecimenTypeId = service.SpecimenTypeId,
                SpecimenTypeCode = specimenType?.Code,
                SpecimenTypeName = specimenType?.Name,
                Unit = service.Unit,
                FormattedUnit = service.FormattedUnit,
                IsActive = service.IsActive,
                IsSubcontract = service.IsSubcontract,
                SubcontractorId = service.SubcontractorId,
                SubcontractorName = service.SubcontractorId.HasValue ? subcontractorNames.GetValueOrDefault(service.SubcontractorId.Value) : null,
                UnitPrice = service.UnitPrice,
                TurnaroundDays = service.TurnaroundDays,
                PriceAtDate = atDate?.UnitPrice ?? service.UnitPrice,
                TurnaroundDaysAtDate = atDate?.TurnaroundDays ?? service.TurnaroundDays,
                EffectiveDateAtDate = atDate?.EffectiveDate,
                UpcomingPrice = upcoming?.UnitPrice,
                UpcomingEffectiveDate = upcoming?.EffectiveDate,
                VersionCount = versions.Count,
            });
        }

        return result
            .OrderBy(d => d.TestMethodName)
            .ThenBy(d => d.IndicatorName)
            .ThenBy(d => d.SpecimenTypeName)
            .ToList();
    }

    private static string ServiceDisplayName(ServiceCatalogItemDto d)
    {
        var name = $"{d.IndicatorName} — {d.TestMethodName}";
        return string.IsNullOrEmpty(d.SpecimenTypeName) ? name : $"{name} ({d.SpecimenTypeName})";
    }

    private static bool Contains(string? source, string keyword)
    {
        return source != null && source.Contains(keyword, StringComparison.OrdinalIgnoreCase);
    }

    private static void SetError(ImportServicePriceRowDto dto, string message)
    {
        dto.Result = ImportServicePriceRowResult.Error;
        dto.Message = message;
    }
}
