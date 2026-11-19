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
/// L3 — Định mức theo mẻ: mỗi dịch vụ (Nền mẫu × Chỉ tiêu × Phương pháp) có 1 định mức
/// (<see cref="ServiceBatchNorm"/>) gồm danh sách vật tư + tổng tiêu hao cho 1 mẻ chuẩn (<see cref="MethodBatchNorm"/>).
/// </summary>
[RemoteService(IsEnabled = false)]
public partial class MethodBatchNormAppService : ApplicationService, IMethodBatchNormAppService
{
    private const string NoMethodName = "-";

    private readonly IRepository<ServiceBatchNorm, Guid> _serviceNormRepository;
    private readonly IRepository<MethodBatchNorm, Guid> _normRepository;
    private readonly IRepository<TestMethod, Guid> _testMethodRepository;
    private readonly IRepository<TestMethodIndicator, Guid> _serviceRepository;
    private readonly IRepository<Indicator, Guid> _indicatorRepository;
    private readonly IRepository<SpecimenType, Guid> _specimenTypeRepository;
    private readonly IRepository<SampleTypeCategory, Guid> _sampleTypeCategoryRepository;
    private readonly IRepository<ServiceGroup, Guid> _serviceGroupRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<Unit, Guid> _unitRepository;
    private readonly IRepository<IndicatorConsumption, Guid> _consumptionRepository;
    private readonly IRepository<ServiceBatchNormEquipment, Guid> _equipmentRepository;
    private readonly IRepository<ProductUnitConversion, Guid> _unitConversionRepository;

    public MethodBatchNormAppService(
        IRepository<ServiceBatchNorm, Guid> serviceNormRepository,
        IRepository<MethodBatchNorm, Guid> normRepository,
        IRepository<TestMethod, Guid> testMethodRepository,
        IRepository<TestMethodIndicator, Guid> serviceRepository,
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<SpecimenType, Guid> specimenTypeRepository,
        IRepository<SampleTypeCategory, Guid> sampleTypeCategoryRepository,
        IRepository<ServiceGroup, Guid> serviceGroupRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<Unit, Guid> unitRepository,
        IRepository<IndicatorConsumption, Guid> consumptionRepository,
        IRepository<ServiceBatchNormEquipment, Guid> equipmentRepository,
        IRepository<ProductUnitConversion, Guid> unitConversionRepository)
    {
        _serviceNormRepository = serviceNormRepository;
        _normRepository = normRepository;
        _testMethodRepository = testMethodRepository;
        _serviceRepository = serviceRepository;
        _indicatorRepository = indicatorRepository;
        _specimenTypeRepository = specimenTypeRepository;
        _sampleTypeCategoryRepository = sampleTypeCategoryRepository;
        _serviceGroupRepository = serviceGroupRepository;
        _productRepository = productRepository;
        _unitRepository = unitRepository;
        _consumptionRepository = consumptionRepository;
        _equipmentRepository = equipmentRepository;
        _unitConversionRepository = unitConversionRepository;
    }

    // ───────────────────────── Định mức theo dịch vụ ─────────────────────────

    public async Task<BatchNormOverviewDto> GetOverviewAsync()
    {
        var headers = await _serviceNormRepository.GetListAsync();
        var norms = await MapServiceNormsAsync(headers);

        // PP đã khai báo trong dịch vụ nhưng chưa thêm: chỉ cho cặp (nền mẫu, chỉ tiêu) đã có định mức
        var pairs = norms.Select(n => (n.SpecimenTypeId, n.IndicatorId)).ToHashSet();
        var usedServiceIds = headers.Select(h => h.TestMethodIndicatorId).ToHashSet();
        var indicatorIds = norms.Select(n => n.IndicatorId).Distinct().ToList();

        var candidates = new List<BatchNormCandidateDto>();
        if (indicatorIds.Count > 0)
        {
            var services = (await _serviceRepository.GetListAsync(s => s.IsActive && indicatorIds.Contains(s.IndicatorId)))
                .Where(s => !usedServiceIds.Contains(s.Id) && pairs.Contains((s.SpecimenTypeId, s.IndicatorId)))
                .ToList();
            var ctx = await LoadServiceContextAsync(services);
            candidates = services
                .Where(s => ctx.Methods.TryGetValue(s.TestMethodId, out var m) && m.Name != NoMethodName)
                .Select(s =>
                {
                    var method = ctx.Methods[s.TestMethodId];
                    return new BatchNormCandidateDto
                    {
                        TestMethodIndicatorId = s.Id,
                        ServiceCode = s.ServiceCode,
                        ServiceGroupName = s.ServiceGroupId.HasValue ? ctx.Groups.GetValueOrDefault(s.ServiceGroupId.Value)?.Name : null,
                        TestMethodId = s.TestMethodId,
                        TestMethodCode = method.Code,
                        TestMethodName = method.Name,
                        IndicatorId = s.IndicatorId,
                        SpecimenTypeId = s.SpecimenTypeId,
                    };
                })
                .OrderBy(c => c.TestMethodName)
                .ToList();
        }

        return new BatchNormOverviewDto { Norms = norms, Candidates = candidates };
    }

    public async Task<ListResultDto<ServiceBatchNormDto>> AddServicesAsync(AddServiceBatchNormsInput input)
    {
        var ids = input.TestMethodIndicatorIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            throw new UserFriendlyException("Chọn ít nhất một phương pháp");
        }

        var services = await _serviceRepository.GetListAsync(s => ids.Contains(s.Id));
        if (services.Count != ids.Count)
        {
            throw new UserFriendlyException("Dịch vụ không tồn tại hoặc đã bị xóa");
        }

        var existed = await _serviceNormRepository.AnyAsync(h => ids.Contains(h.TestMethodIndicatorId));
        if (existed)
        {
            throw new UserFriendlyException("Có phương pháp đã có trong định mức. Vui lòng tải lại danh sách.");
        }

        var nextCode = await CreateCodeGeneratorAsync();
        var headers = ids.Select(id => new ServiceBatchNorm(GuidGenerator.Create(), nextCode(), id)).ToList();
        await _serviceNormRepository.InsertManyAsync(headers);
        await SaveNormsAsync();
        return new ListResultDto<ServiceBatchNormDto>(await MapServiceNormsAsync(headers));
    }

    public async Task<ServiceBatchNormDto> ToggleServiceActiveAsync(Guid id)
    {
        var header = await _serviceNormRepository.GetAsync(id);
        header.IsActive = !header.IsActive;
        await _serviceNormRepository.UpdateAsync(header, autoSave: true);
        return (await MapServiceNormsAsync(new List<ServiceBatchNorm> { header })).First();
    }

    public async Task DeleteServiceAsync(Guid id)
    {
        var header = await _serviceNormRepository.GetAsync(id);
        var lineIds = (await _normRepository.GetListAsync(l => l.ServiceBatchNormId == header.Id)).Select(l => l.Id).ToList();
        if (lineIds.Count > 0
            && await _consumptionRepository.AnyAsync(c => c.MethodBatchNormId != null && lineIds.Contains(c.MethodBatchNormId.Value)))
        {
            throw new UserFriendlyException("Định mức đã phát sinh tiêu hao, không thể xóa. Hãy tắt Trạng thái sử dụng.");
        }

        // Giữ nguyên các dòng vật tư (gắn với định mức đã xóa mềm) để Hoàn tác khôi phục đủ
        await _serviceNormRepository.DeleteAsync(header, autoSave: true);
    }

    public async Task<ServiceBatchNormDto> RestoreServiceAsync(Guid id)
    {
        ServiceBatchNorm header;
        using (DataFilter.Disable<ISoftDelete>())
        {
            header = await _serviceNormRepository.GetAsync(id);
        }

        if (header.IsDeleted)
        {
            var conflict = await _serviceNormRepository.AnyAsync(h => h.TestMethodIndicatorId == header.TestMethodIndicatorId);
            if (conflict)
            {
                throw new UserFriendlyException("Phương pháp đã được thêm lại vào định mức, không thể hoàn tác.");
            }

            header.IsDeleted = false;
            header.DeleterId = null;
            header.DeletionTime = null;
            await _serviceNormRepository.UpdateAsync(header, autoSave: true);
        }

        return (await MapServiceNormsAsync(new List<ServiceBatchNorm> { header })).First();
    }

    // ───────────────────────── Dòng vật tư ─────────────────────────

    public async Task<ListResultDto<MethodBatchNormDto>> GetByServiceAsync(Guid testMethodIndicatorId)
    {
        var header = await _serviceNormRepository.FirstOrDefaultAsync(h => h.TestMethodIndicatorId == testMethodIndicatorId);
        if (header == null)
        {
            return new ListResultDto<MethodBatchNormDto>(new List<MethodBatchNormDto>());
        }

        var norms = await _normRepository.GetListAsync(n => n.ServiceBatchNormId == header.Id);
        var dtos = await MapAsync(norms);
        return new ListResultDto<MethodBatchNormDto>(dtos
            .OrderByDescending(d => d.IsActive)
            .ThenBy(d => d.Code)
            .ToList());
    }

    public async Task<MethodBatchNormDto> CreateAsync(CreateUpdateMethodBatchNormInput input)
    {
        var service = await _serviceRepository.FindAsync(input.TestMethodIndicatorId)
                      ?? throw new UserFriendlyException("Dịch vụ không tồn tại hoặc đã bị xóa");

        var header = await _serviceNormRepository.FirstOrDefaultAsync(h => h.TestMethodIndicatorId == service.Id);
        if (header == null)
        {
            var nextCode = await CreateCodeGeneratorAsync();
            header = new ServiceBatchNorm(GuidGenerator.Create(), nextCode(), service.Id);
            await _serviceNormRepository.InsertAsync(header);
            await SaveNormsAsync();
        }

        var unitId = await ValidateAsync(null, header.Id, input);

        var norm = new MethodBatchNorm(
            GuidGenerator.Create(),
            null!, // DB sinh mã DMxxxx (sequence) và EF đọc lại sau insert
            header.Id,
            service.TestMethodId,
            input.ProductId,
            unitId,
            input.BatchQuantity);

        await _normRepository.InsertAsync(norm, autoSave: true);
        return (await MapAsync(new List<MethodBatchNorm> { norm })).First();
    }

    public async Task<MethodBatchNormDto> UpdateAsync(Guid id, CreateUpdateMethodBatchNormInput input)
    {
        var norm = await _normRepository.GetAsync(id);
        var header = norm.ServiceBatchNormId.HasValue
            ? await _serviceNormRepository.FindAsync(norm.ServiceBatchNormId.Value)
            : null;
        if (header == null || header.TestMethodIndicatorId != input.TestMethodIndicatorId)
        {
            throw new UserFriendlyException("Không được đổi dịch vụ của định mức. Hãy tạo định mức mới.");
        }

        norm.UnitId = await ValidateAsync(norm.Id, header.Id, input);
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
    /// BR-05: (Dịch vụ + Vật tư) là duy nhất. Trả về đơn vị tiêu hao lấy theo vật tư
    /// (đơn vị thể tích nếu có, không thì đơn vị cơ bản — giống cách trừ kho quy đổi).
    /// </summary>
    private async Task<Guid?> ValidateAsync(Guid? normId, Guid headerId, CreateUpdateMethodBatchNormInput input)
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
            n.ServiceBatchNormId == headerId && n.ProductId == input.ProductId && (normId == null || n.Id != normId));
        if (duplicated)
        {
            throw new UserFriendlyException($"Định mức đã có vật tư {product.Code} - {product.Name}");
        }

        return product.VolumeUnitId ?? product.BaseUnitId;
    }

    // ───────────────────────── Map ─────────────────────────

    private sealed class ServiceContext
    {
        public Dictionary<Guid, TestMethod> Methods { get; init; } = new();
        public Dictionary<Guid, Indicator> Indicators { get; init; } = new();
        public Dictionary<Guid, SpecimenType> Specimens { get; init; } = new();
        public Dictionary<Guid, SampleTypeCategory> Categories { get; init; } = new();
        public Dictionary<Guid, ServiceGroup> Groups { get; init; } = new();
    }

    private async Task<ServiceContext> LoadServiceContextAsync(List<TestMethodIndicator> services)
    {
        var methodIds = services.Select(s => s.TestMethodId).Distinct().ToList();
        var indicatorIds = services.Select(s => s.IndicatorId).Distinct().ToList();
        var specimenIds = services.Where(s => s.SpecimenTypeId.HasValue).Select(s => s.SpecimenTypeId!.Value).Distinct().ToList();
        var groupIds = services.Where(s => s.ServiceGroupId.HasValue).Select(s => s.ServiceGroupId!.Value).Distinct().ToList();

        var specimens = specimenIds.Count == 0
            ? new Dictionary<Guid, SpecimenType>()
            : (await _specimenTypeRepository.GetListAsync(x => specimenIds.Contains(x.Id))).ToDictionary(x => x.Id);
        var categoryIds = specimens.Values.Where(x => x.SampleTypeCategoryId.HasValue)
            .Select(x => x.SampleTypeCategoryId!.Value).Distinct().ToList();

        return new ServiceContext
        {
            Methods = methodIds.Count == 0
                ? new()
                : (await _testMethodRepository.GetListAsync(x => methodIds.Contains(x.Id))).ToDictionary(x => x.Id),
            Indicators = indicatorIds.Count == 0
                ? new()
                : (await _indicatorRepository.GetListAsync(x => indicatorIds.Contains(x.Id))).ToDictionary(x => x.Id),
            Specimens = specimens,
            Categories = categoryIds.Count == 0
                ? new()
                : (await _sampleTypeCategoryRepository.GetListAsync(x => categoryIds.Contains(x.Id))).ToDictionary(x => x.Id),
            Groups = groupIds.Count == 0
                ? new()
                : (await _serviceGroupRepository.GetListAsync(x => groupIds.Contains(x.Id))).ToDictionary(x => x.Id),
        };
    }

    /// <summary>Bỏ qua định mức có dịch vụ / chỉ tiêu / phương pháp đã bị xóa</summary>
    private async Task<List<ServiceBatchNormDto>> MapServiceNormsAsync(List<ServiceBatchNorm> headers)
    {
        if (headers.Count == 0)
        {
            return new List<ServiceBatchNormDto>();
        }

        var serviceIds = headers.Select(h => h.TestMethodIndicatorId).Distinct().ToList();
        var services = (await _serviceRepository.GetListAsync(s => serviceIds.Contains(s.Id))).ToDictionary(s => s.Id);
        var ctx = await LoadServiceContextAsync(services.Values.ToList());

        var headerIds = headers.Select(h => h.Id).ToList();
        var lines = await _normRepository.GetListAsync(n => n.ServiceBatchNormId != null && headerIds.Contains(n.ServiceBatchNormId.Value));
        var materialCounts = lines.Where(l => l.IsActive && l.ProductId.HasValue)
            .GroupBy(l => l.ServiceBatchNormId!.Value)
            .ToDictionary(g => g.Key, g => g.Count());

        var lineIds = lines.Select(l => l.Id).ToList();
        var usedHeaderIds = new HashSet<Guid>();
        if (lineIds.Count > 0)
        {
            var consumptionQuery = await _consumptionRepository.GetQueryableAsync();
            var usedLineIds = (await AsyncExecuter.ToListAsync(consumptionQuery
                    .Where(c => c.MethodBatchNormId != null && lineIds.Contains(c.MethodBatchNormId.Value))
                    .Select(c => c.MethodBatchNormId!.Value)
                    .Distinct()))
                .ToHashSet();
            usedHeaderIds = lines.Where(l => usedLineIds.Contains(l.Id)).Select(l => l.ServiceBatchNormId!.Value).ToHashSet();
        }

        var equipmentQuery = await _equipmentRepository.GetQueryableAsync();
        var equipmentCounts = (await AsyncExecuter.ToListAsync(equipmentQuery
                .Where(e => headerIds.Contains(e.ServiceBatchNormId))
                .GroupBy(e => e.ServiceBatchNormId)
                .Select(g => new { g.Key, Count = g.Count() })))
            .ToDictionary(x => x.Key, x => x.Count);

        var result = new List<ServiceBatchNormDto>();
        foreach (var h in headers)
        {
            if (!services.TryGetValue(h.TestMethodIndicatorId, out var s)
                || !ctx.Methods.TryGetValue(s.TestMethodId, out var method)
                || !ctx.Indicators.TryGetValue(s.IndicatorId, out var indicator))
            {
                continue;
            }

            var specimen = s.SpecimenTypeId.HasValue ? ctx.Specimens.GetValueOrDefault(s.SpecimenTypeId.Value) : null;
            var category = specimen?.SampleTypeCategoryId is { } catId ? ctx.Categories.GetValueOrDefault(catId) : null;
            result.Add(new ServiceBatchNormDto
            {
                Id = h.Id,
                Code = h.Code,
                TestMethodIndicatorId = s.Id,
                ServiceCode = s.ServiceCode,
                ServiceGroupName = s.ServiceGroupId.HasValue ? ctx.Groups.GetValueOrDefault(s.ServiceGroupId.Value)?.Name : null,
                TestMethodId = method.Id,
                TestMethodCode = method.Code,
                TestMethodName = method.Name,
                TestMethodFormattedName = method.FormattedName,
                IndicatorId = indicator.Id,
                IndicatorCode = indicator.Code,
                IndicatorName = indicator.Name,
                IndicatorFormattedName = indicator.FormattedName,
                SpecimenTypeId = s.SpecimenTypeId,
                SpecimenTypeName = specimen?.Name,
                SpecimenTypeSortOrder = specimen?.SortOrder ?? 0,
                SampleTypeCategoryId = category?.Id,
                SampleTypeCategoryName = category?.Name,
                IsActive = h.IsActive,
                MaterialCount = materialCounts.GetValueOrDefault(h.Id),
                EquipmentCount = equipmentCounts.GetValueOrDefault(h.Id),
                IsUsed = usedHeaderIds.Contains(h.Id),
                CreationTime = h.CreationTime,
            });
        }

        return result;
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
                ServiceBatchNormId = n.ServiceBatchNormId,
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
