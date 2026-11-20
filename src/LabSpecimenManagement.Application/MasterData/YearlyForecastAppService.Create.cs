using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.MasterData.BatchNorms;
using LabSpecimenManagement.MasterData.YearlyForecasts;
using LabSpecimenManagement.Products;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Phiếu tạo / sửa dự toán mẫu năm: nhiều bộ (nền mẫu × chỉ tiêu × PP) cùng lúc, mỗi bộ nhập số mẫu riêng,
/// có thể bỏ bớt / bổ sung vật tư, thiết bị hoặc "áp dụng tiêu hao" theo bộ khác.
/// Phần vật tư / thiết bị chỉ để hiển thị & ước tính — tiêu hao / mẫu khi trừ kho vẫn theo định mức (ConsumptionNormResolver).
/// </summary>
public partial class YearlyForecastAppService
{
    private const string NoMethodName = "-";

    private IRepository<ServiceYearlyForecastItem, Guid> ItemRepository =>
        LazyServiceProvider.LazyGetRequiredService<IRepository<ServiceYearlyForecastItem, Guid>>();

    private IRepository<ServiceBatchNormEquipment, Guid> NormEquipmentRepository =>
        LazyServiceProvider.LazyGetRequiredService<IRepository<ServiceBatchNormEquipment, Guid>>();

    private NormProductOptionProvider ProductOptions =>
        LazyServiceProvider.LazyGetRequiredService<NormProductOptionProvider>();

    public async Task<ForecastCreateOptionsDto> GetCreateOptionsAsync(GetForecastCreateOptionsInput input)
    {
        var forecasts = (await _forecastRepository.GetListAsync(f => f.Year == input.Year))
            .Where(f => f.EstimatedSamples > 0)
            .ToList();
        var forecastByService = forecasts.ToDictionary(f => f.TestMethodIndicatorId);
        var serviceIdByForecast = forecasts.ToDictionary(f => f.Id, f => f.TestMethodIndicatorId);
        var services = await GetSelectableServicesAsync(forecastByService.Keys.ToList());

        // Định mức theo mẻ đang sử dụng (mỗi dịch vụ tối đa 1)
        var headers = (await _serviceNormRepository.GetListAsync(h => h.IsActive))
            .GroupBy(h => h.TestMethodIndicatorId)
            .ToDictionary(g => g.Key, g => g.First());
        var headerIds = headers.Values.Select(h => h.Id).ToList();
        var normLines = headerIds.Count == 0
            ? new List<MethodBatchNorm>()
            : (await _normRepository.GetListAsync(n => n.IsActive && n.ServiceBatchNormId != null && headerIds.Contains(n.ServiceBatchNormId.Value)))
                .Where(n => n.IsComplete)
                .ToList();
        var linesByHeader = normLines.ToLookup(n => n.ServiceBatchNormId!.Value);
        var equipmentsByHeader = (headerIds.Count == 0
                ? new List<ServiceBatchNormEquipment>()
                : await NormEquipmentRepository.GetListAsync(e => headerIds.Contains(e.ServiceBatchNormId)))
            .ToLookup(e => e.ServiceBatchNormId);

        var unitIds = normLines.Where(n => n.UnitId.HasValue).Select(n => n.UnitId!.Value).Distinct().ToList();
        var units = unitIds.Count == 0
            ? new Dictionary<Guid, Unit>()
            : (await _unitRepository.GetListAsync(u => unitIds.Contains(u.Id))).ToDictionary(u => u.Id);

        var forecastIds = forecasts.Select(f => f.Id).ToList();
        var itemsByForecast = (forecastIds.Count == 0
                ? new List<ServiceYearlyForecastItem>()
                : await ItemRepository.GetListAsync(i => forecastIds.Contains(i.ForecastId)))
            .ToLookup(i => i.ForecastId);

        var ctx = await LoadComboContextAsync(services);
        var combos = new List<ForecastComboDto>();
        foreach (var service in services)
        {
            var combo = MapCombo(service, ctx);
            if (combo == null)
            {
                continue;
            }

            if (headers.TryGetValue(service.Id, out var header))
            {
                combo.ExistingNormCode = header.Code;
                combo.NormMaterials = linesByHeader[header.Id]
                    .GroupBy(n => n.ProductId!.Value)
                    .Select(g => g.First())
                    .Select(n => new ForecastNormMaterialDto
                    {
                        ProductId = n.ProductId!.Value,
                        UnitId = n.UnitId,
                        UnitName = n.UnitId.HasValue ? units.GetValueOrDefault(n.UnitId.Value)?.Name : null,
                        Quantity = n.BatchQuantity!.Value,
                    })
                    .ToList();
                combo.NormEquipmentIds = equipmentsByHeader[header.Id].Select(e => e.ProductId).Distinct().ToList();
            }

            if (forecastByService.TryGetValue(service.Id, out var forecast))
            {
                combo.Forecast = MapConfig(forecast, itemsByForecast[forecast.Id], serviceIdByForecast);
            }

            combos.Add(combo);
        }

        return new ForecastCreateOptionsDto
        {
            Year = input.Year,
            Combos = combos,
            Materials = await ProductOptions.LoadProductOptionsAsync(NormProductOptionProvider.MaterialTypes),
            Equipments = await ProductOptions.LoadProductOptionsAsync(new[] { ProductType.Equipment }),
        };
    }

    public async Task<SaveForecastDetailsResultDto> SaveDetailsAsync(SaveForecastDetailsInput input)
    {
        EnsureEditableYear(input.Year);

        var items = input.Items ?? new List<SaveForecastDetailItemInput>();
        if (items.Count == 0)
        {
            throw new UserFriendlyException("Chưa chọn bộ nền mẫu · chỉ tiêu · phương pháp nào");
        }

        if (items.GroupBy(i => i.TestMethodIndicatorId).Any(g => g.Count() > 1))
        {
            throw new UserFriendlyException("Có bộ nền mẫu · chỉ tiêu · phương pháp bị chọn trùng");
        }

        var ids = items.Select(i => i.TestMethodIndicatorId).ToList();
        var allForecasts = await _forecastRepository.GetListAsync(f => f.Year == input.Year);
        var forecastByService = allForecasts.ToDictionary(f => f.TestMethodIndicatorId);
        var liveForecasts = allForecasts.Where(f => f.EstimatedSamples > 0).ToList();

        var services = (await GetSelectableServicesAsync(liveForecasts.Select(f => f.TestMethodIndicatorId).ToList()))
            .Where(s => ids.Contains(s.Id))
            .ToDictionary(s => s.Id);
        if (services.Count != ids.Count)
        {
            throw new UserFriendlyException("Có bộ nền mẫu · chỉ tiêu · phương pháp không tồn tại hoặc đã ngừng. Vui lòng tải lại.");
        }

        var ctx = await LoadComboContextAsync(services.Values.ToList());
        string Label(Guid serviceId)
        {
            var c = MapCombo(services[serviceId], ctx);
            return c == null ? "?" : $"{c.SpecimenTypeName ?? "Mọi nền mẫu"} › {c.IndicatorName} › {c.TestMethodName}";
        }

        foreach (var item in items)
        {
            if (item.EstimatedSamples is not > 0)
            {
                throw new UserFriendlyException($"Chưa nhập số mẫu / năm cho bộ {Label(item.TestMethodIndicatorId)}");
            }
        }

        if (input.IsCreate)
        {
            var existed = items
                .Where(i => forecastByService.GetValueOrDefault(i.TestMethodIndicatorId)?.EstimatedSamples > 0)
                .Select(i => Label(i.TestMethodIndicatorId))
                .ToList();
            if (existed.Count > 0)
            {
                throw new UserFriendlyException(
                    $"Đã có dự toán năm {input.Year}: {string.Join("; ", existed)}. Vui lòng tải lại và bỏ chọn các bộ này.");
            }
        }

        ValidateLinks(items, liveForecasts, Label);

        var ownItems = items.Where(i => i.SourceTestMethodIndicatorId == null).ToList();
        var configs = await ValidateConfigsAsync(ownItems, Label);

        // 1. Số mẫu (tạo dòng dự toán nếu chưa có)
        var forecastOf = new Dictionary<Guid, ServiceYearlyForecast>();
        var created = new List<ServiceYearlyForecast>();
        foreach (var item in items)
        {
            var forecast = forecastByService.GetValueOrDefault(item.TestMethodIndicatorId);
            if (forecast == null)
            {
                forecast = new ServiceYearlyForecast(GuidGenerator.Create(), item.TestMethodIndicatorId, input.Year, item.EstimatedSamples);
                created.Add(forecast);
            }
            else
            {
                forecast.EstimatedSamples = item.EstimatedSamples;
            }

            forecastOf[item.TestMethodIndicatorId] = forecast;
        }

        // 2. Áp dụng tiêu hao: nguồn có thể là bộ trong phiếu hoặc dự toán đã lưu
        foreach (var item in items)
        {
            forecastOf[item.TestMethodIndicatorId].SourceForecastId = item.SourceTestMethodIndicatorId is { } sourceId
                ? (forecastOf.GetValueOrDefault(sourceId) ?? forecastByService[sourceId]).Id
                : null;
        }

        if (created.Count > 0)
        {
            await _forecastRepository.InsertManyAsync(created);
        }

        var updated = forecastOf.Values.Except(created).ToList();
        if (updated.Count > 0)
        {
            await _forecastRepository.UpdateManyAsync(updated);
        }

        // 3. Thay toàn bộ cấu hình vật tư / thiết bị (bộ đang áp dụng tiêu hao thì không giữ cấu hình riêng)
        var forecastIds = forecastOf.Values.Select(f => f.Id).ToList();
        // autoSave: xóa trước khi thêm lại để không đụng unique (ForecastId, Kind, ProductId)
        await ItemRepository.DeleteAsync(i => forecastIds.Contains(i.ForecastId), autoSave: true);
        var rows = ownItems
            .SelectMany(item => BuildItems(forecastOf[item.TestMethodIndicatorId].Id, configs[item.TestMethodIndicatorId]))
            .ToList();
        if (rows.Count > 0)
        {
            await ItemRepository.InsertManyAsync(rows);
        }

        try
        {
            await CurrentUnitOfWork!.SaveChangesAsync();
        }
        catch (Exception ex) when (IsUniqueViolation(ex))
        {
            throw new UserFriendlyException("Dự toán vừa được người khác lưu cùng lúc. Vui lòng tải lại và thử lại.");
        }

        return new SaveForecastDetailsResultDto
        {
            Count = items.Count,
            TotalSamples = items.Sum(i => i.EstimatedSamples ?? 0),
        };
    }

    // ───────────────────────── Kiểm tra ─────────────────────────

    /// <summary>
    /// Áp dụng tiêu hao chỉ 1 cấp: nguồn ≠ chính nó, nguồn không áp dụng theo bộ khác,
    /// bộ đang là nguồn của bộ khác thì không được áp dụng theo bộ khác.
    /// </summary>
    private static void ValidateLinks(
        List<SaveForecastDetailItemInput> items, List<ServiceYearlyForecast> liveForecasts, Func<Guid, string> label)
    {
        var inRequest = items.ToDictionary(i => i.TestMethodIndicatorId);
        var liveByService = liveForecasts.ToDictionary(f => f.TestMethodIndicatorId);
        var serviceByForecast = liveForecasts.ToDictionary(f => f.Id, f => f.TestMethodIndicatorId);

        // Nguồn sau khi lưu của mọi bộ: bộ trong phiếu theo phiếu, bộ ngoài phiếu theo dữ liệu đã lưu
        Guid? SourceAfterSave(Guid serviceId)
        {
            if (inRequest.TryGetValue(serviceId, out var item))
            {
                return item.SourceTestMethodIndicatorId;
            }

            return liveByService.GetValueOrDefault(serviceId)?.SourceForecastId is { } sourceForecastId
                ? serviceByForecast.GetValueOrDefault(sourceForecastId)
                : null;
        }

        foreach (var item in items)
        {
            if (item.SourceTestMethodIndicatorId is not { } sourceId)
            {
                continue;
            }

            var name = label(item.TestMethodIndicatorId);
            if (sourceId == item.TestMethodIndicatorId)
            {
                throw new UserFriendlyException($"Bộ {name} không thể áp dụng tiêu hao theo chính nó");
            }

            if (!inRequest.ContainsKey(sourceId) && !liveByService.ContainsKey(sourceId))
            {
                throw new UserFriendlyException($"Bộ nguồn áp dụng tiêu hao cho {name} chưa có dự toán năm này. Vui lòng tải lại.");
            }

            if (SourceAfterSave(sourceId) != null)
            {
                throw new UserFriendlyException($"Bộ nguồn của {name} đang áp dụng tiêu hao theo bộ khác (chỉ áp dụng 1 cấp)");
            }

            var targets = liveForecasts.Count(f => !inRequest.ContainsKey(f.TestMethodIndicatorId) && SourceAfterSave(f.TestMethodIndicatorId) == item.TestMethodIndicatorId)
                + items.Count(i => i.SourceTestMethodIndicatorId == item.TestMethodIndicatorId);
            if (targets > 0)
            {
                throw new UserFriendlyException($"Bộ {name} đang là nguồn áp dụng tiêu hao cho {targets} bộ khác nên không thể áp dụng theo bộ khác");
            }
        }
    }

    private record ForecastItemConfig(
        List<ForecastExtraMaterialDto> ExtraMaterials,
        List<Guid> ExtraEquipmentIds,
        List<Guid> HiddenMaterialIds,
        List<Guid> HiddenEquipmentIds);

    private async Task<Dictionary<Guid, ForecastItemConfig>> ValidateConfigsAsync(
        List<SaveForecastDetailItemInput> items, Func<Guid, string> label)
    {
        var materialIds = items.SelectMany(i => i.ExtraMaterials ?? new()).Select(m => m.ProductId).Distinct().ToList();
        var equipmentIds = items.SelectMany(i => i.ExtraEquipmentIds ?? new()).Distinct().ToList();
        var productIds = materialIds.Concat(equipmentIds).Distinct().ToList();
        var products = productIds.Count == 0
            ? new Dictionary<Guid, Product>()
            : (await _productRepository.GetListAsync(p => productIds.Contains(p.Id))).ToDictionary(p => p.Id);
        var allowedUnits = await ProductOptions.LoadAllowedUnitsAsync(
            products.Values.Where(p => materialIds.Contains(p.Id)).ToList());

        var result = new Dictionary<Guid, ForecastItemConfig>();
        foreach (var item in items)
        {
            var name = label(item.TestMethodIndicatorId);
            var extras = item.ExtraMaterials ?? new();
            var extraEquipments = (item.ExtraEquipmentIds ?? new()).Distinct().ToList();
            if (extras.GroupBy(m => m.ProductId).Any(g => g.Count() > 1))
            {
                throw new UserFriendlyException($"Vật tư bổ sung của bộ {name} bị chọn trùng");
            }

            foreach (var m in extras)
            {
                var product = products.GetValueOrDefault(m.ProductId);
                if (product == null || !product.IsActive || product.IsDraft
                    || !NormProductOptionProvider.MaterialTypes.Contains(product.ProductType))
                {
                    throw new UserFriendlyException($"Vật tư bổ sung của bộ {name} không tồn tại hoặc đã ngừng sử dụng. Vui lòng tải lại.");
                }

                if (m.Quantity is < 0)
                {
                    throw new UserFriendlyException($"Lượng tiêu hao của {ProductLabel(product)} (bộ {name}) không được âm");
                }

                if (!allowedUnits[product.Id].Contains(m.UnitId))
                {
                    throw new UserFriendlyException($"Đơn vị tiêu hao không thuộc vật tư {ProductLabel(product)}");
                }
            }

            foreach (var id in extraEquipments)
            {
                var product = products.GetValueOrDefault(id);
                if (product == null || !product.IsActive || product.IsDraft || product.ProductType != ProductType.Equipment)
                {
                    throw new UserFriendlyException($"Thiết bị bổ sung của bộ {name} không tồn tại hoặc đã ngừng sử dụng. Vui lòng tải lại.");
                }
            }

            result[item.TestMethodIndicatorId] = new ForecastItemConfig(
                extras,
                extraEquipments,
                (item.HiddenMaterialIds ?? new()).Distinct().Where(id => extras.All(m => m.ProductId != id)).ToList(),
                (item.HiddenEquipmentIds ?? new()).Distinct().Where(id => !extraEquipments.Contains(id)).ToList());
        }

        return result;
    }

    private IEnumerable<ServiceYearlyForecastItem> BuildItems(Guid forecastId, ForecastItemConfig config)
    {
        foreach (var m in config.ExtraMaterials)
        {
            yield return new ServiceYearlyForecastItem(GuidGenerator.Create(), forecastId, m.ProductId, ForecastItemKind.Material,
                false, m.UnitId, m.Quantity, m.Mode);
        }

        foreach (var id in config.ExtraEquipmentIds)
        {
            yield return new ServiceYearlyForecastItem(GuidGenerator.Create(), forecastId, id, ForecastItemKind.Equipment, false);
        }

        foreach (var id in config.HiddenMaterialIds)
        {
            yield return new ServiceYearlyForecastItem(GuidGenerator.Create(), forecastId, id, ForecastItemKind.Material, true);
        }

        foreach (var id in config.HiddenEquipmentIds)
        {
            yield return new ServiceYearlyForecastItem(GuidGenerator.Create(), forecastId, id, ForecastItemKind.Equipment, true);
        }
    }

    // ───────────────────────── Dữ liệu ─────────────────────────

    private static ForecastConfigDto MapConfig(
        ServiceYearlyForecast forecast, IEnumerable<ServiceYearlyForecastItem> items, Dictionary<Guid, Guid> serviceIdByForecast)
    {
        var list = items.ToList();
        return new ForecastConfigDto
        {
            EstimatedSamples = forecast.EstimatedSamples,
            SourceTestMethodIndicatorId = forecast.SourceForecastId is { } sourceId ? serviceIdByForecast.GetValueOrDefault(sourceId) : null,
            ExtraMaterials = list
                .Where(i => i.Kind == ForecastItemKind.Material && !i.IsHidden && i.UnitId.HasValue)
                .Select(i => new ForecastExtraMaterialDto
                {
                    ProductId = i.ProductId,
                    UnitId = i.UnitId!.Value,
                    Quantity = i.Quantity,
                    Mode = i.Mode ?? ForecastQuantityMode.PerSample,
                })
                .ToList(),
            ExtraEquipmentIds = list.Where(i => i.Kind == ForecastItemKind.Equipment && !i.IsHidden).Select(i => i.ProductId).ToList(),
            HiddenMaterialIds = list.Where(i => i.Kind == ForecastItemKind.Material && i.IsHidden).Select(i => i.ProductId).ToList(),
            HiddenEquipmentIds = list.Where(i => i.Kind == ForecastItemKind.Equipment && i.IsHidden).Select(i => i.ProductId).ToList(),
        };
    }

    /// <summary>
    /// Dịch vụ đang hoạt động có phương pháp thật (bỏ PP "-") và chỉ tiêu chưa xóa,
    /// cộng các dịch vụ đã có dự toán năm này (để vẫn sửa được khi dịch vụ đã ngừng).
    /// </summary>
    private async Task<List<TestMethodIndicator>> GetSelectableServicesAsync(List<Guid> forecastServiceIds)
    {
        var services = await _serviceRepository.GetListAsync(s => s.IsActive || forecastServiceIds.Contains(s.Id));
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

    private record ComboContext(
        Dictionary<Guid, TestMethod> Methods,
        Dictionary<Guid, Indicator> Indicators,
        Dictionary<Guid, SpecimenType> Specimens,
        Dictionary<Guid, SampleTypeCategory> Categories);

    private async Task<ComboContext> LoadComboContextAsync(List<TestMethodIndicator> services)
    {
        var methodIds = services.Select(s => s.TestMethodId).Distinct().ToList();
        var indicatorIds = services.Select(s => s.IndicatorId).Distinct().ToList();
        var specimenIds = services.Where(s => s.SpecimenTypeId.HasValue).Select(s => s.SpecimenTypeId!.Value).Distinct().ToList();

        var methods = (await _testMethodRepository.GetListAsync(m => methodIds.Contains(m.Id))).ToDictionary(m => m.Id);
        var indicators = (await _indicatorRepository.GetListAsync(i => indicatorIds.Contains(i.Id))).ToDictionary(i => i.Id);
        var specimens = specimenIds.Count == 0
            ? new Dictionary<Guid, SpecimenType>()
            : (await _specimenTypeRepository.GetListAsync(s => specimenIds.Contains(s.Id))).ToDictionary(s => s.Id);
        var categoryIds = specimens.Values
            .Where(s => s.SampleTypeCategoryId.HasValue)
            .Select(s => s.SampleTypeCategoryId!.Value)
            .Distinct()
            .ToList();
        var categories = categoryIds.Count == 0
            ? new Dictionary<Guid, SampleTypeCategory>()
            : (await _sampleTypeCategoryRepository.GetListAsync(c => categoryIds.Contains(c.Id))).ToDictionary(c => c.Id);

        return new ComboContext(methods, indicators, specimens, categories);
    }

    private static ForecastComboDto? MapCombo(TestMethodIndicator s, ComboContext ctx)
    {
        if (!ctx.Methods.TryGetValue(s.TestMethodId, out var method) || !ctx.Indicators.TryGetValue(s.IndicatorId, out var indicator))
        {
            return null;
        }

        var specimen = s.SpecimenTypeId.HasValue ? ctx.Specimens.GetValueOrDefault(s.SpecimenTypeId.Value) : null;
        var category = specimen?.SampleTypeCategoryId is { } catId ? ctx.Categories.GetValueOrDefault(catId) : null;
        return new ForecastComboDto
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
        };
    }

    private static string ProductLabel(Product product)
        => string.IsNullOrWhiteSpace(product.Name) ? product.Code : $"{product.Code} - {product.Name}";

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
}
