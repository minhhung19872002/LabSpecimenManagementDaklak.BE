using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.MasterData.BatchNorms;
using LabSpecimenManagement.Products;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Phiếu khai báo định mức theo mẻ: tạo nhiều định mức (mỗi bộ Nền mẫu × Chỉ tiêu × PP 1 định mức)
/// dùng chung danh sách vật tư + thiết bị.
/// </summary>
public partial class MethodBatchNormAppService
{
    private const string CodePrefix = "DM-";

    /// <summary>Đơn vị công suất sử dụng của thiết bị</summary>
    public static readonly IReadOnlyList<string> CapacityUnits =
        new[] { "phút/mẫu", "giờ/mẫu", "lần chạy/mẫu", "kWh/mẫu", "% công suất" };

    private static readonly ProductType[] MaterialTypes = NormProductOptionProvider.MaterialTypes;

    public async Task<BatchNormCreateOptionsDto> GetCreateOptionsAsync(GetBatchNormCreateOptionsInput input)
    {
        var services = await GetSelectableServicesAsync();
        var normCodes = (await _serviceNormRepository.GetListAsync())
            .ToDictionary(h => h.TestMethodIndicatorId, h => h.Code);

        var result = new BatchNormCreateOptionsDto
        {
            ServiceGroups = await BuildServiceGroupOptionsAsync(services, normCodes),
            NextCode = (await CreateCodeGeneratorAsync())(),
            Materials = await LoadProductOptionsAsync(MaterialTypes),
            Equipments = await LoadProductOptionsAsync(new[] { ProductType.Equipment }),
            CapacityUnits = CapacityUnits.ToList(),
        };

        if (input.ServiceGroupId.HasValue || input.Ungrouped)
        {
            var groupServices = services
                .Where(s => input.Ungrouped ? s.ServiceGroupId == null : s.ServiceGroupId == input.ServiceGroupId)
                .ToList();
            result.Combos = await MapCombosAsync(groupServices, normCodes);
        }

        return result;
    }

    public async Task<ListResultDto<ServiceBatchNormDto>> BulkCreateAsync(BulkCreateBatchNormsInput input)
    {
        var ids = input.TestMethodIndicatorIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            throw new UserFriendlyException("Chưa chọn bộ nền mẫu · chỉ tiêu · phương pháp áp dụng");
        }

        var services = (await GetSelectableServicesAsync()).Where(s => ids.Contains(s.Id)).ToDictionary(s => s.Id);
        if (services.Count != ids.Count)
        {
            throw new UserFriendlyException("Có bộ nền mẫu · chỉ tiêu · phương pháp không tồn tại hoặc đã ngừng. Vui lòng tải lại.");
        }

        var existing = await _serviceNormRepository.GetListAsync(h => ids.Contains(h.TestMethodIndicatorId));
        if (existing.Count > 0)
        {
            var combos = await MapCombosAsync(existing.Select(h => services[h.TestMethodIndicatorId]).ToList(),
                existing.ToDictionary(h => h.TestMethodIndicatorId, h => h.Code));
            var names = combos.Select(c =>
                $"{c.IndicatorName} – {c.TestMethodName} ({c.SpecimenTypeName ?? "Mọi nền mẫu"}, {c.ExistingNormCode})");
            throw new UserFriendlyException($"Đã có định mức: {string.Join("; ", names)}. Vui lòng tải lại và bỏ chọn các bộ này.");
        }

        var materials = await ValidateMaterialsAsync(input.Materials);
        var equipments = await ValidateEquipmentsAsync(input.Equipments ?? new());

        var nextCode = await CreateCodeGeneratorAsync();
        var headers = new List<ServiceBatchNorm>();
        var lines = new List<MethodBatchNorm>();
        var equipmentRows = new List<ServiceBatchNormEquipment>();
        foreach (var id in ids)
        {
            var service = services[id];
            var header = new ServiceBatchNorm(GuidGenerator.Create(), nextCode(), service.Id);
            headers.Add(header);
            lines.AddRange(materials.Select(m => new MethodBatchNorm(
                GuidGenerator.Create(),
                null!, // DB sinh mã DMxxxx (sequence)
                header.Id,
                service.TestMethodId,
                m.ProductId,
                m.UnitId,
                m.Quantity)));
            equipmentRows.AddRange(equipments.Select(e => new ServiceBatchNormEquipment(
                GuidGenerator.Create(), header.Id, e.ProductId, e.Capacity, e.CapacityUnit.Trim())));
        }

        await _serviceNormRepository.InsertManyAsync(headers);
        await _normRepository.InsertManyAsync(lines);
        if (equipmentRows.Count > 0)
        {
            await _equipmentRepository.InsertManyAsync(equipmentRows);
        }

        await SaveNormsAsync();
        return new ListResultDto<ServiceBatchNormDto>(await MapServiceNormsAsync(headers));
    }

    public async Task<ListResultDto<ServiceBatchNormEquipmentDto>> GetEquipmentsByServiceAsync(Guid testMethodIndicatorId)
    {
        var header = await _serviceNormRepository.FirstOrDefaultAsync(h => h.TestMethodIndicatorId == testMethodIndicatorId);
        if (header == null)
        {
            return new ListResultDto<ServiceBatchNormEquipmentDto>(new List<ServiceBatchNormEquipmentDto>());
        }

        var rows = await _equipmentRepository.GetListAsync(e => e.ServiceBatchNormId == header.Id);
        var productIds = rows.Select(r => r.ProductId).Distinct().ToList();
        var products = productIds.Count == 0
            ? new Dictionary<Guid, Product>()
            : (await _productRepository.GetListAsync(p => productIds.Contains(p.Id))).ToDictionary(p => p.Id);

        return new ListResultDto<ServiceBatchNormEquipmentDto>(rows
            .Select(r =>
            {
                var product = products.GetValueOrDefault(r.ProductId);
                return new ServiceBatchNormEquipmentDto
                {
                    Id = r.Id,
                    ProductId = r.ProductId,
                    ProductCode = product?.Code,
                    ProductName = product?.Name,
                    ProductFormattedName = product?.FormattedName,
                    Capacity = r.Capacity,
                    CapacityUnit = r.CapacityUnit,
                };
            })
            .OrderBy(d => d.ProductName)
            .ToList());
    }

    // ───────────────────────── Mã định mức ─────────────────────────

    /// <summary>
    /// Sinh mã DM-{năm}-{0001} tăng dần theo năm; tính cả định mức đã xóa mềm để không tái sử dụng mã.
    /// Trùng mã khi tạo đồng thời được chặn bởi unique index (xem <see cref="SaveNormsAsync"/>).
    /// </summary>
    private async Task<Func<string>> CreateCodeGeneratorAsync()
    {
        var prefix = $"{CodePrefix}{Clock.Now.Year}-";
        List<string> codes;
        using (DataFilter.Disable<ISoftDelete>())
        {
            var query = await _serviceNormRepository.GetQueryableAsync();
            codes = await AsyncExecuter.ToListAsync(query.Where(h => h.Code.StartsWith(prefix)).Select(h => h.Code));
        }

        var max = codes
            .Select(c => int.TryParse(c.AsSpan(prefix.Length), out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();
        return () => $"{prefix}{++max:D4}";
    }

    private async Task SaveNormsAsync()
    {
        try
        {
            await CurrentUnitOfWork!.SaveChangesAsync();
        }
        catch (Exception ex) when (IsUniqueViolation(ex))
        {
            throw new UserFriendlyException("Định mức vừa được người khác tạo cùng lúc. Vui lòng tải lại và thử lại.");
        }
    }

    private static bool IsUniqueViolation(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException)
        {
            if (e.Message.Contains("23505") || e.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    // ───────────────────────── Dữ liệu chọn ─────────────────────────

    /// <summary>Dịch vụ đang hoạt động có phương pháp thật (bỏ PP "-") và chỉ tiêu chưa xóa</summary>
    private async Task<List<TestMethodIndicator>> GetSelectableServicesAsync()
    {
        var services = await _serviceRepository.GetListAsync(s => s.IsActive);
        var methodIds = services.Select(s => s.TestMethodId).Distinct().ToList();
        var indicatorIds = services.Select(s => s.IndicatorId).Distinct().ToList();

        var methodQuery = await _testMethodRepository.GetQueryableAsync();
        var validMethodIds = (await AsyncExecuter.ToListAsync(methodQuery
                .Where(m => methodIds.Contains(m.Id) && m.Name != NoMethodName)
                .Select(m => m.Id)))
            .ToHashSet();
        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();
        var validIndicatorIds = (await AsyncExecuter.ToListAsync(indicatorQuery
                .Where(i => indicatorIds.Contains(i.Id))
                .Select(i => i.Id)))
            .ToHashSet();

        return services
            .Where(s => validMethodIds.Contains(s.TestMethodId) && validIndicatorIds.Contains(s.IndicatorId))
            .ToList();
    }

    private async Task<List<BatchNormServiceGroupOptionDto>> BuildServiceGroupOptionsAsync(
        List<TestMethodIndicator> services, Dictionary<Guid, string> normCodes)
    {
        var groups = (await _serviceGroupRepository.GetListAsync(g => g.IsActive)).ToDictionary(g => g.Id);
        var result = services
            .GroupBy(s => s.ServiceGroupId)
            .Where(g => g.Key == null || groups.ContainsKey(g.Key.Value))
            .Select(g => new BatchNormServiceGroupOptionDto
            {
                Id = g.Key,
                Code = g.Key.HasValue ? groups[g.Key.Value].Code : null,
                Name = g.Key.HasValue ? groups[g.Key.Value].Name : "(Chưa đặt tên dịch vụ)",
                TotalCount = g.Count(),
                FreeCount = g.Count(s => !normCodes.ContainsKey(s.Id)),
            })
            .ToList();

        // Nhóm chưa đặt tên luôn ở cuối
        return result
            .OrderBy(g => g.Id == null)
            .ThenBy(g => g.Name, StringComparer.Create(new System.Globalization.CultureInfo("vi-VN"), true))
            .ToList();
    }

    private async Task<List<BatchNormComboDto>> MapCombosAsync(List<TestMethodIndicator> services, Dictionary<Guid, string> normCodes)
    {
        var ctx = await LoadServiceContextAsync(services);
        return services
            .Where(s => ctx.Methods.ContainsKey(s.TestMethodId) && ctx.Indicators.ContainsKey(s.IndicatorId))
            .Select(s =>
            {
                var method = ctx.Methods[s.TestMethodId];
                var indicator = ctx.Indicators[s.IndicatorId];
                var specimen = s.SpecimenTypeId.HasValue ? ctx.Specimens.GetValueOrDefault(s.SpecimenTypeId.Value) : null;
                var category = specimen?.SampleTypeCategoryId is { } catId ? ctx.Categories.GetValueOrDefault(catId) : null;
                return new BatchNormComboDto
                {
                    TestMethodIndicatorId = s.Id,
                    ServiceCode = s.ServiceCode,
                    SampleTypeCategoryId = category?.Id,
                    SampleTypeCategoryName = category?.Name,
                    SpecimenTypeId = s.SpecimenTypeId,
                    SpecimenTypeName = specimen?.Name,
                    SpecimenTypeSortOrder = specimen?.SortOrder ?? 0,
                    IndicatorId = indicator.Id,
                    IndicatorCode = indicator.Code,
                    IndicatorName = indicator.Name,
                    IndicatorFormattedName = indicator.FormattedName,
                    TestMethodId = method.Id,
                    TestMethodCode = method.Code,
                    TestMethodName = method.Name,
                    ExistingNormCode = normCodes.GetValueOrDefault(s.Id),
                };
            })
            .ToList();
    }

    private NormProductOptionProvider ProductOptions => LazyServiceProvider.LazyGetRequiredService<NormProductOptionProvider>();

    private Task<List<BatchNormProductOptionDto>> LoadProductOptionsAsync(ProductType[] types)
        => ProductOptions.LoadProductOptionsAsync(types);

    private Task<Dictionary<Guid, List<Guid>>> LoadAllowedUnitsAsync(List<Product> products)
        => ProductOptions.LoadAllowedUnitsAsync(products);

    private async Task<List<BulkCreateBatchNormMaterialInput>> ValidateMaterialsAsync(List<BulkCreateBatchNormMaterialInput>? materials)
    {
        if (materials == null || materials.Count == 0)
        {
            throw new UserFriendlyException("Chưa có vật tư");
        }

        var products = await LoadProductsForValidationAsync(materials.Select(m => m.ProductId).ToList(), "Vật tư");
        var allowed = await LoadAllowedUnitsAsync(products.Values.ToList());
        foreach (var m in materials)
        {
            var product = products[m.ProductId];
            var label = ProductLabel(product);
            if (!MaterialTypes.Contains(product.ProductType))
            {
                throw new UserFriendlyException($"{label} không phải vật tư (hóa chất, sinh phẩm, vật tư tiêu hao, dung môi, chất chuẩn)");
            }

            if (m.Quantity <= 0)
            {
                throw new UserFriendlyException($"Lượng tiêu hao gốc của {label} phải lớn hơn 0");
            }

            if (!allowed[product.Id].Contains(m.UnitId))
            {
                throw new UserFriendlyException($"Đơn vị tiêu hao không thuộc vật tư {label}");
            }
        }

        return materials;
    }

    private async Task<List<BulkCreateBatchNormEquipmentInput>> ValidateEquipmentsAsync(List<BulkCreateBatchNormEquipmentInput> equipments)
    {
        if (equipments.Count == 0)
        {
            return equipments;
        }

        var products = await LoadProductsForValidationAsync(equipments.Select(e => e.ProductId).ToList(), "Thiết bị");
        foreach (var e in equipments)
        {
            var product = products[e.ProductId];
            var label = ProductLabel(product);
            if (product.ProductType != ProductType.Equipment)
            {
                throw new UserFriendlyException($"{label} không phải thiết bị");
            }

            if (e.Capacity <= 0)
            {
                throw new UserFriendlyException($"Công suất sử dụng của {label} phải lớn hơn 0");
            }

            if (!CapacityUnits.Contains(e.CapacityUnit?.Trim()))
            {
                throw new UserFriendlyException($"Đơn vị công suất của {label} không hợp lệ");
            }
        }

        return equipments;
    }

    private async Task<Dictionary<Guid, Product>> LoadProductsForValidationAsync(List<Guid> productIds, string label)
    {
        var duplicated = productIds.GroupBy(id => id).FirstOrDefault(g => g.Count() > 1);
        var distinctIds = productIds.Distinct().ToList();
        var products = (await _productRepository.GetListAsync(p => distinctIds.Contains(p.Id))).ToDictionary(p => p.Id);
        if (products.Count != distinctIds.Count)
        {
            throw new UserFriendlyException($"{label} không tồn tại hoặc đã bị xóa");
        }

        if (duplicated != null)
        {
            var product = products[duplicated.Key];
            throw new UserFriendlyException($"{label} {ProductLabel(product)} bị chọn trùng");
        }

        var inactive = products.Values.FirstOrDefault(p => !p.IsActive || p.IsDraft);
        if (inactive != null)
        {
            throw new UserFriendlyException($"{label} {ProductLabel(inactive)} đã ngừng sử dụng hoặc đang là nháp");
        }

        return products;
    }

    private static string ProductLabel(Product product)
        => string.IsNullOrWhiteSpace(product.Name) ? product.Code : $"{product.Code} - {product.Name}";
}
