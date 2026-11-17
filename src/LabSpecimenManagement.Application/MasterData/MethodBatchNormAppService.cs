using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.MasterData.BatchNorms;
using LabSpecimenManagement.Products;
using LabSpecimenManagement.Testing;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// L3 — Định mức tiêu hao theo mẻ: Phương pháp → vật tư + tổng tiêu hao cho 1 mẻ chuẩn.
/// </summary>
[RemoteService(IsEnabled = false)]
public class MethodBatchNormAppService : ApplicationService, IMethodBatchNormAppService
{
    private readonly IRepository<MethodBatchNorm, Guid> _normRepository;
    private readonly IRepository<TestMethod, Guid> _testMethodRepository;
    private readonly IRepository<TestMethodIndicator, Guid> _serviceRepository;
    private readonly IRepository<Indicator, Guid> _indicatorRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<Unit, Guid> _unitRepository;
    private readonly IRepository<IndicatorConsumption, Guid> _consumptionRepository;

    public MethodBatchNormAppService(
        IRepository<MethodBatchNorm, Guid> normRepository,
        IRepository<TestMethod, Guid> testMethodRepository,
        IRepository<TestMethodIndicator, Guid> serviceRepository,
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<Unit, Guid> unitRepository,
        IRepository<IndicatorConsumption, Guid> consumptionRepository)
    {
        _normRepository = normRepository;
        _testMethodRepository = testMethodRepository;
        _serviceRepository = serviceRepository;
        _indicatorRepository = indicatorRepository;
        _productRepository = productRepository;
        _unitRepository = unitRepository;
        _consumptionRepository = consumptionRepository;
    }

    public async Task<ListResultDto<BatchNormMethodDto>> GetMethodsAsync(GetBatchNormMethodsInput input)
    {
        var methods = (await _testMethodRepository.GetListAsync(m => m.Name != "-"))
            .OrderBy(m => m.SortOrder).ThenBy(m => m.Name)
            .ToList();

        var serviceQuery = await _serviceRepository.GetQueryableAsync();
        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();
        var serviceCounts = (await AsyncExecuter.ToListAsync(serviceQuery
                .Where(s => indicatorQuery.Any(i => i.Id == s.IndicatorId))
                .GroupBy(s => s.TestMethodId)
                .Select(g => new { g.Key, Count = g.Count() })))
            .ToDictionary(x => x.Key, x => x.Count);

        var normsByMethod = (await _normRepository.GetListAsync(n => n.IsActive))
            .GroupBy(n => n.TestMethodId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var dtos = methods.Select(m =>
        {
            var norms = normsByMethod.GetValueOrDefault(m.Id) ?? new List<MethodBatchNorm>();
            return new BatchNormMethodDto
            {
                TestMethodId = m.Id,
                Code = m.Code,
                Name = m.Name,
                FormattedName = m.FormattedName,
                IsActive = m.IsActive,
                ServiceCount = serviceCounts.GetValueOrDefault(m.Id),
                NormCount = norms.Count,
                HasCompleteNorm = norms.Any(n => n.IsComplete),
            };
        });

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            var keyword = input.Keyword.Trim();
            dtos = dtos.Where(d => d.Code.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                                   || d.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase));
        }

        if (input.HasNorm.HasValue)
        {
            dtos = dtos.Where(d => d.HasCompleteNorm == input.HasNorm.Value);
        }

        return new ListResultDto<BatchNormMethodDto>(dtos.ToList());
    }

    public async Task<ListResultDto<MethodBatchNormDto>> GetByMethodAsync(Guid testMethodId)
    {
        var norms = await _normRepository.GetListAsync(n => n.TestMethodId == testMethodId);
        var dtos = await MapAsync(norms);
        return new ListResultDto<MethodBatchNormDto>(dtos
            .OrderByDescending(d => d.IsActive)
            .ThenBy(d => d.Code)
            .ToList());
    }

    public async Task<MethodBatchNormDto> CreateAsync(CreateUpdateMethodBatchNormInput input)
    {
        await _testMethodRepository.GetAsync(input.TestMethodId);
        var unitId = await ValidateAsync(null, input);

        var norm = new MethodBatchNorm(
            GuidGenerator.Create(),
            null!, // DB sinh mã DMxxxx (sequence) và EF đọc lại sau insert
            input.TestMethodId,
            input.ProductId,
            unitId,
            input.BatchQuantity);

        await _normRepository.InsertAsync(norm, autoSave: true);
        return (await MapAsync(new List<MethodBatchNorm> { norm })).First();
    }

    public async Task<MethodBatchNormDto> UpdateAsync(Guid id, CreateUpdateMethodBatchNormInput input)
    {
        var norm = await _normRepository.GetAsync(id);
        if (norm.TestMethodId != input.TestMethodId)
        {
            throw new UserFriendlyException("Không được đổi phương pháp của định mức. Hãy tạo định mức mới.");
        }

        norm.UnitId = await ValidateAsync(norm.Id, input);
        norm.ProductId = input.ProductId;
        norm.BatchQuantity = input.BatchQuantity;

        await _normRepository.UpdateAsync(norm, autoSave: true);
        return (await MapAsync(new List<MethodBatchNorm> { norm })).First();
    }

    public async Task<MethodBatchNormDto> ToggleActiveAsync(Guid id)
    {
        var norm = await _normRepository.GetAsync(id);
        norm.IsActive = !norm.IsActive;
        await _normRepository.UpdateAsync(norm, autoSave: true);
        return (await MapAsync(new List<MethodBatchNorm> { norm })).First();
    }

    public async Task DeleteAsync(Guid id)
    {
        var used = await _consumptionRepository.AnyAsync(c => c.MethodBatchNormId == id);
        if (used)
        {
            throw new UserFriendlyException("Định mức đã phát sinh tiêu hao, không thể xóa. Hãy chuyển sang Ngừng hoạt động.");
        }

        await _normRepository.DeleteAsync(id);
    }

    /// <summary>
    /// BR-05: (Phương pháp + Vật tư) là duy nhất. Trả về đơn vị tiêu hao lấy theo vật tư
    /// (đơn vị thể tích nếu có, không thì đơn vị cơ bản — giống cách trừ kho quy đổi).
    /// </summary>
    private async Task<Guid?> ValidateAsync(Guid? normId, CreateUpdateMethodBatchNormInput input)
    {
        if (input.BatchQuantity is < 0)
        {
            throw new UserFriendlyException("Tổng tiêu hao không được âm");
        }

        if (!input.ProductId.HasValue)
        {
            return null;
        }

        var product = await _productRepository.FindAsync(input.ProductId.Value)
                      ?? throw new UserFriendlyException("Vật tư không tồn tại hoặc đã bị xóa");

        var duplicated = await _normRepository.AnyAsync(n =>
            n.TestMethodId == input.TestMethodId && n.ProductId == input.ProductId && (normId == null || n.Id != normId));
        if (duplicated)
        {
            throw new UserFriendlyException($"Phương pháp đã có định mức cho vật tư {product.Code} - {product.Name}");
        }

        return product.VolumeUnitId ?? product.BaseUnitId;
    }

    private async Task<List<MethodBatchNormDto>> MapAsync(List<MethodBatchNorm> norms)
    {
        if (norms.Count == 0)
        {
            return new List<MethodBatchNormDto>();
        }

        var methodIds = norms.Select(n => n.TestMethodId).Distinct().ToList();
        var productIds = norms.Where(n => n.ProductId.HasValue).Select(n => n.ProductId!.Value).Distinct().ToList();
        var unitIds = norms.Where(n => n.UnitId.HasValue).Select(n => n.UnitId!.Value).Distinct().ToList();
        var normIds = norms.Select(n => n.Id).ToList();

        var methods = (await _testMethodRepository.GetListAsync(m => methodIds.Contains(m.Id))).ToDictionary(m => m.Id);
        var products = productIds.Count == 0
            ? new Dictionary<Guid, Product>()
            : (await _productRepository.GetListAsync(p => productIds.Contains(p.Id))).ToDictionary(p => p.Id);
        var units = unitIds.Count == 0
            ? new Dictionary<Guid, Unit>()
            : (await _unitRepository.GetListAsync(u => unitIds.Contains(u.Id))).ToDictionary(u => u.Id);

        var consumptionQuery = await _consumptionRepository.GetQueryableAsync();
        var usedIds = (await AsyncExecuter.ToListAsync(consumptionQuery
                .Where(c => c.MethodBatchNormId != null && normIds.Contains(c.MethodBatchNormId.Value))
                .Select(c => c.MethodBatchNormId!.Value)
                .Distinct()))
            .ToHashSet();

        return norms.Select(n =>
        {
            var method = methods.GetValueOrDefault(n.TestMethodId);
            var product = n.ProductId.HasValue ? products.GetValueOrDefault(n.ProductId.Value) : null;
            var unit = n.UnitId.HasValue ? units.GetValueOrDefault(n.UnitId.Value) : null;
            return new MethodBatchNormDto
            {
                Id = n.Id,
                Code = n.Code,
                TestMethodId = n.TestMethodId,
                TestMethodCode = method?.Code,
                TestMethodName = method?.Name,
                ProductId = n.ProductId,
                ProductCode = product?.Code,
                ProductName = product?.Name,
                ProductFormattedName = product?.FormattedName,
                UnitId = n.UnitId,
                UnitName = unit?.Name,
                BatchQuantity = n.BatchQuantity,
                IsActive = n.IsActive,
                IsComplete = n.IsComplete,
                IsUsed = usedIds.Contains(n.Id),
                CreationTime = n.CreationTime,
                LastModificationTime = n.LastModificationTime,
            };
        }).ToList();
    }
}
