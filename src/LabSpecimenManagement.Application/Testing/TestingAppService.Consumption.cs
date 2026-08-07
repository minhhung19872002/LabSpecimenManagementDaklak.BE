using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Equipments;
using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Products;
using LabSpecimenManagement.TestBatches;
using LabSpecimenManagement.TestRequests;
using LabSpecimenManagement.WarehouseImports;
using Volo.Abp;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.Testing;

public partial class TestingAppService
{
    // -------------------------------------------------------------------------
    // Repositories needed for indicator consumption — injected via property
    // (ABP supports property injection for partial class members)
    // We use lazy-init via LazyServiceProvider to avoid bloating the constructor.
    // -------------------------------------------------------------------------

    private IRepository<IndicatorConsumption, Guid> _indicatorConsumptionRepository =>
        LazyServiceProvider.LazyGetRequiredService<IRepository<IndicatorConsumption, Guid>>();

    private IRepository<Unit, Guid> _unitRepository =>
        LazyServiceProvider.LazyGetRequiredService<IRepository<Unit, Guid>>();

    // -------------------------------------------------------------------------
    // Public methods
    // -------------------------------------------------------------------------

    public async Task<ConsumptionPreviewDto> GetIndicatorConsumptionPreviewAsync(Guid laboratoryTaskId, Guid indicatorId)
    {
        var indicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicatorId);

        var existingConsumptions = await _indicatorConsumptionRepository
            .GetListAsync(x => x.LaboratoryTaskIndicatorId == indicatorId);
        var hasDeducted = existingConsumptions.Any(c => c.IsDeducted);

        var lines = new List<ConsumptionPreviewLineDto>();

        // Draft lines (not yet deducted) → pre-fill for next round
        var draftConsumptions = existingConsumptions.Where(c => !c.IsDeducted).ToList();
        if (draftConsumptions.Any())
        {
            foreach (var c in draftConsumptions)
            {
                lines.Add(new ConsumptionPreviewLineDto
                {
                    ConsumptionId = c.Id,
                    ProductId = c.ProductId,
                    EquipmentId = c.EquipmentId,
                    UnitId = c.UnitId,
                    RecommendedQuantity = c.RecommendedQuantity,
                    ActualQuantity = c.ActualQuantity,
                    IsDeducted = false,
                });
            }
        }
        else if (indicator.MethodId.HasValue)
        {
            // Fall back to templates from TestMethodIndicator
            var sampleIndicator = await _sampleIndicatorRepository.FindAsync(indicator.TestRequestSampleIndicatorId);
            if (sampleIndicator != null)
            {
                var tmiQuery = await _testMethodIndicatorRepository.GetQueryableAsync();
                var tmi = await AsyncExecuter.FirstOrDefaultAsync(
                    tmiQuery.Where(x =>
                        x.TestMethodId == indicator.MethodId.Value &&
                        x.IndicatorId == sampleIndicator.IndicatorId &&
                        x.SpecimenTypeId == sampleIndicator.SpecimenTypeId));

                if (tmi != null)
                {
                    var consumptionQuery = await _materialConsumptionRepository.GetQueryableAsync();
                    var templates = await AsyncExecuter.ToListAsync(
                        consumptionQuery.Where(c => c.TestMethodIndicatorId == tmi.Id));

                    foreach (var template in templates)
                    {
                        lines.Add(new ConsumptionPreviewLineDto
                        {
                            ProductId = template.ProductId,
                            EquipmentId = template.EquipmentId,
                            UnitId = template.UnitId,
                            RecommendedQuantity = template.Quantity,
                            ActualQuantity = template.Quantity,
                            IsDeducted = false,
                        });
                    }
                }
            }
        }

        await EnrichConsumptionLinesAsync(lines);

        return new ConsumptionPreviewDto
        {
            IsFirstSubmit = !hasDeducted,
            Lines = lines,
        };
    }

    public async Task<TestingDetailDto> SubmitIndicatorWithConsumptionAsync(
        Guid laboratoryTaskId,
        SubmitIndicatorWithConsumptionDto input)
    {
        var indicator = await _laboratoryTaskIndicatorRepository.GetAsync(input.IndicatorId);
        if (indicator.LaboratoryTaskId != laboratoryTaskId)
            throw new UserFriendlyException("Chỉ tiêu không thuộc công việc này");

        var allowedStatuses = new[]
        {
            IndicatorApprovalStatus.Pending,
            IndicatorApprovalStatus.Rejected,
            IndicatorApprovalStatus.MethodChanged,
            IndicatorApprovalStatus.AcceptedForTesting,
            IndicatorApprovalStatus.Entered,
        };
        if (!allowedStatuses.Contains(indicator.ApprovalStatus))
            throw new UserFriendlyException("Chỉ tiêu này không thể gửi kết quả");

        if (string.IsNullOrWhiteSpace(indicator.Result))
            throw new UserFriendlyException("Vui lòng nhập kết quả trước khi gửi");

        // Set TestDate and submit
        indicator.TestDate = input.TestDate;
        indicator.SubmitDate = Clock.Now;
        indicator.EntryDate ??= Clock.Now;
        indicator.EnteredById ??= CurrentUser.Id;
        indicator.ApprovalStatus = IndicatorApprovalStatus.Entered;
        await _laboratoryTaskIndicatorRepository.UpdateAsync(indicator, autoSave: false);

        // Always delete drafts when submitting (regardless of whether new lines are provided)
        var existingConsumptions = await _indicatorConsumptionRepository
            .GetListAsync(x => x.LaboratoryTaskIndicatorId == input.IndicatorId);
        var draftConsumptions = existingConsumptions.Where(c => !c.IsDeducted).ToList();
        if (draftConsumptions.Any())
            await _indicatorConsumptionRepository.DeleteManyAsync(draftConsumptions, autoSave: false);

        // Handle consumption lines for this round
        if (input.ConsumptionLines != null && input.ConsumptionLines.Any())
        {
            var newConsumptions = new List<IndicatorConsumption>();
            foreach (var line in input.ConsumptionLines)
            {
                var consumption = new IndicatorConsumption(
                    GuidGenerator.Create(),
                    input.IndicatorId,
                    line.ProductId,
                    line.EquipmentId,
                    line.UnitId,
                    line.RecommendedQuantity);
                consumption.ActualQuantity = line.ActualQuantity;
                await _indicatorConsumptionRepository.InsertAsync(consumption, autoSave: false);
                newConsumptions.Add(consumption);
            }

            await CurrentUnitOfWork.SaveChangesAsync();

            await DeductStockFromIndicatorConsumptionsAsync(newConsumptions, laboratoryTaskId);

            foreach (var c in newConsumptions)
            {
                c.IsDeducted = true;
                await _indicatorConsumptionRepository.UpdateAsync(c, autoSave: false);
            }
        }

        // Update task status
        var task = await _laboratoryTaskRepository.GetAsync(laboratoryTaskId);
        var allIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => x.LaboratoryTaskId == laboratoryTaskId);
        var newTaskStatus = CalculateTaskStatusFromIndicators(allIndicators);
        if (task.Status != newTaskStatus)
        {
            task.Status = newTaskStatus;
            await _laboratoryTaskRepository.UpdateAsync(task, autoSave: false);
        }

        await TryUpdateTestRequestToInProgressAsync(task.TestRequestId);

        if (task.TechnicalManagerId.HasValue)
            await NotifyTechnicalManagerOnSubmitAsync(task, new List<LaboratoryTaskIndicator> { indicator });

        await CurrentUnitOfWork.SaveChangesAsync();

        return await GetAsync(laboratoryTaskId);
    }

    public async Task<TestingDetailDto> SaveIndicatorConsumptionDraftAsync(
        Guid laboratoryTaskId,
        SaveIndicatorConsumptionDraftDto input)
    {
        var indicator = await _laboratoryTaskIndicatorRepository.GetAsync(input.IndicatorId);
        if (indicator.LaboratoryTaskId != laboratoryTaskId)
            throw new UserFriendlyException("Chỉ tiêu không thuộc công việc này");

        var existing = await _indicatorConsumptionRepository
            .GetListAsync(x => x.LaboratoryTaskIndicatorId == input.IndicatorId);
        var nonDeducted = existing.Where(c => !c.IsDeducted).ToList();
        if (nonDeducted.Any())
            await _indicatorConsumptionRepository.DeleteManyAsync(nonDeducted, autoSave: false);

        if (input.ConsumptionLines != null && input.ConsumptionLines.Any())
        {
            foreach (var line in input.ConsumptionLines)
            {
                var consumption = new IndicatorConsumption(
                    GuidGenerator.Create(),
                    input.IndicatorId,
                    line.ProductId,
                    line.EquipmentId,
                    line.UnitId,
                    line.RecommendedQuantity);
                consumption.ActualQuantity = line.ActualQuantity;
                await _indicatorConsumptionRepository.InsertAsync(consumption, autoSave: false);
            }
        }

        await CurrentUnitOfWork.SaveChangesAsync();

        return await GetAsync(laboratoryTaskId);
    }

    public async Task<ConsumptionPreviewDto> GetBulkIndicatorConsumptionPreviewAsync(
        Guid laboratoryTaskId,
        List<Guid> indicatorIds)
    {
        var indicators = await _laboratoryTaskIndicatorRepository.GetListAsync(
            x => indicatorIds.Contains(x.Id) && x.LaboratoryTaskId == laboratoryTaskId);

        var rawLines = new List<ConsumptionPreviewLineDto>();
        var processedTmiIds = new HashSet<Guid>();

        var tmiQuery = await _testMethodIndicatorRepository.GetQueryableAsync();
        var consumptionQuery = await _materialConsumptionRepository.GetQueryableAsync();

        foreach (var indicator in indicators.Where(i => i.MethodId.HasValue))
        {
            var sampleIndicator = await _sampleIndicatorRepository.FindAsync(indicator.TestRequestSampleIndicatorId);
            if (sampleIndicator == null) continue;

            var tmi = await AsyncExecuter.FirstOrDefaultAsync(
                tmiQuery.Where(x =>
                    x.TestMethodId == indicator.MethodId!.Value &&
                    x.IndicatorId == sampleIndicator.IndicatorId &&
                    x.SpecimenTypeId == sampleIndicator.SpecimenTypeId));

            if (tmi == null || !processedTmiIds.Add(tmi.Id)) continue;

            var templates = await AsyncExecuter.ToListAsync(
                consumptionQuery.Where(c => c.TestMethodIndicatorId == tmi.Id));

            foreach (var template in templates)
            {
                rawLines.Add(new ConsumptionPreviewLineDto
                {
                    ProductId = template.ProductId,
                    EquipmentId = template.EquipmentId,
                    UnitId = template.UnitId,
                    RecommendedQuantity = template.Quantity,
                    ActualQuantity = template.Quantity,
                    IsDeducted = false,
                });
            }
        }

        // Aggregate by (ProductId, EquipmentId, UnitId)
        var aggregated = rawLines
            .GroupBy(l => (l.ProductId, l.EquipmentId, l.UnitId))
            .Select(g => new ConsumptionPreviewLineDto
            {
                ProductId = g.Key.ProductId,
                EquipmentId = g.Key.EquipmentId,
                UnitId = g.Key.UnitId,
                RecommendedQuantity = g.Sum(l => l.RecommendedQuantity),
                ActualQuantity = g.Sum(l => l.ActualQuantity),
                IsDeducted = false,
            })
            .ToList();

        await EnrichConsumptionLinesAsync(aggregated);

        var hasDeductedHistory = false;
        if (indicatorIds.Any())
        {
            var existingRecords = await _indicatorConsumptionRepository
                .GetListAsync(x => indicatorIds.Contains(x.LaboratoryTaskIndicatorId) && x.IsDeducted);
            hasDeductedHistory = existingRecords.Any();
        }

        return new ConsumptionPreviewDto
        {
            IsFirstSubmit = !hasDeductedHistory,
            Lines = aggregated,
        };
    }

    public async Task<TestingDetailDto> SubmitBulkIndicatorsWithConsumptionAsync(
        Guid laboratoryTaskId,
        BulkSubmitIndicatorWithConsumptionDto input)
    {
        var task = await _laboratoryTaskRepository.GetAsync(laboratoryTaskId);
        var currentUserId = CurrentUser.Id;

        var allTaskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(
            x => x.LaboratoryTaskId == laboratoryTaskId);
        var indicatorMap = allTaskIndicators.ToDictionary(i => i.Id);

        var submittedIndicators = new List<LaboratoryTaskIndicator>();

        foreach (var item in input.Indicators)
        {
            if (!indicatorMap.TryGetValue(item.IndicatorId, out var indicator)) continue;
            if (!await CanEditIndicatorAsync(indicator, task, currentUserId)) continue;

            var allowedStatuses = new[]
            {
                IndicatorApprovalStatus.Pending,
                IndicatorApprovalStatus.Rejected,
                IndicatorApprovalStatus.MethodChanged,
                IndicatorApprovalStatus.AcceptedForTesting,
                IndicatorApprovalStatus.Entered,
            };
            if (!allowedStatuses.Contains(indicator.ApprovalStatus)) continue;
            if (string.IsNullOrWhiteSpace(indicator.Result)) continue;

            indicator.TestDate = item.TestDate;
            indicator.SubmitDate = Clock.Now;
            indicator.EntryDate ??= Clock.Now;
            indicator.EnteredById ??= currentUserId;
            indicator.ApprovalStatus = IndicatorApprovalStatus.Entered;

            await _laboratoryTaskIndicatorRepository.UpdateAsync(indicator, autoSave: false);
            submittedIndicators.Add(indicator);
        }

        // Delete stale drafts for all submitted indicators (prevent double-deduction on future single submit)
        if (submittedIndicators.Any())
        {
            var submittedIds = submittedIndicators.Select(i => i.Id).ToList();
            var staleDrafts = await _indicatorConsumptionRepository
                .GetListAsync(x => submittedIds.Contains(x.LaboratoryTaskIndicatorId) && !x.IsDeducted);
            if (staleDrafts.Any())
                await _indicatorConsumptionRepository.DeleteManyAsync(staleDrafts, autoSave: false);
        }

        // Flush all pending changes before stock deduction and batch item updates
        await CurrentUnitOfWork.SaveChangesAsync();

        if (submittedIndicators.Any() && input.ConsumptionLines.Any())
            await DeductStockFromConsumptionLineInputsAsync(input.ConsumptionLines, laboratoryTaskId);

        await MarkBatchItemsAsExternallyCompletedAsync(submittedIndicators);

        var newTaskStatus = CalculateTaskStatusFromIndicators(allTaskIndicators);
        if (task.Status != newTaskStatus)
        {
            task.Status = newTaskStatus;
            await _laboratoryTaskRepository.UpdateAsync(task, autoSave: false);
        }

        await TryUpdateTestRequestToInProgressAsync(task.TestRequestId);

        if (task.TechnicalManagerId.HasValue && submittedIndicators.Any())
            await NotifyTechnicalManagerOnSubmitAsync(task, submittedIndicators);

        await CurrentUnitOfWork.SaveChangesAsync();

        return await GetAsync(laboratoryTaskId);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private async Task DeductStockFromConsumptionLineInputsAsync(
        List<ConsumptionLineInputDto> lines,
        Guid laboratoryTaskId)
    {
        var productLines = lines.Where(l => l.ProductId.HasValue).ToList();
        var equipmentLines = lines.Where(l => l.EquipmentId.HasValue).ToList();

        if (productLines.Any())
        {
            var productIds = productLines.Select(l => l.ProductId!.Value).Distinct().ToList();
            var products = await _productRepository.GetListAsync(p => productIds.Contains(p.Id));
            var productMap = products.ToDictionary(p => p.Id);

            var skus = await _productSkuRepository.GetListAsync(s => productIds.Contains(s.ProductId));
            var skuByProduct = skus.GroupBy(s => s.ProductId).ToDictionary(g => g.Key, g => g.ToList());

            var conversions = await _productUnitConversionRepository
                .GetListAsync(c => productIds.Contains(c.ProductId));
            var conversionMap = conversions.ToDictionary(c => (c.ProductId, c.ConvertedUnitId));

            foreach (var line in productLines)
            {
                if (!productMap.TryGetValue(line.ProductId!.Value, out var product)) continue;

                var consumeUnitId = line.UnitId;
                decimal? unitsPerStorage = null;

                if (consumeUnitId == product.PackagingUnitId)
                    unitsPerStorage = 1;
                else if (consumeUnitId == product.VolumeUnitId && product.VolumePerUnit > 0)
                    unitsPerStorage = product.VolumePerUnit.Value;
                else if (consumeUnitId == product.BaseUnitId && product.PackagingQuantity > 0)
                    unitsPerStorage = 1m / product.PackagingQuantity.Value;

                if (unitsPerStorage == null && skuByProduct.TryGetValue(product.Id, out var pSkus))
                {
                    foreach (var sku in pSkus)
                    {
                        if (consumeUnitId == sku.PackagingUnitId) { unitsPerStorage = 1; break; }
                        if (consumeUnitId == sku.VolumeUnitId && sku.VolumePerUnit > 0) { unitsPerStorage = sku.VolumePerUnit.Value; break; }
                        if (consumeUnitId == sku.BaseUnitId && sku.PackagingQuantity > 0) { unitsPerStorage = 1m / sku.PackagingQuantity.Value; break; }
                    }
                }

                if (unitsPerStorage == null && consumeUnitId.HasValue
                    && conversionMap.TryGetValue((product.Id, consumeUnitId.Value), out var conv)
                    && conv.ConvertedQuantity > 0)
                {
                    unitsPerStorage = conv.ConvertedQuantity / conv.Quantity;
                }

                if (unitsPerStorage == null || unitsPerStorage.Value == 0) continue;

                var storageQty = line.ActualQuantity / unitsPerStorage.Value;

                // BA: tạm thời comment out auto-deduction, chỉ hiện số nhập thực tế
                // var deductions = await _fifoStockService.DeductAsync(
                //     product, storageQty,
                //     StockMovementType.MaterialConsumption,
                //     laboratoryTaskId, null,
                //     "Tiêu hao thử nghiệm (bulk)");
                //
                // if (product.ProductType == ProductType.PreparedSolution && deductions.Any())
                // {
                //     var productSkus = skuByProduct.GetValueOrDefault(product.Id);
                //     await UpdatePreparedSolutionRemainingVolumeAsync(product, productSkus, deductions);
                // }
            }
        }

        if (equipmentLines.Any())
        {
            var usageByEquipment = equipmentLines
                .GroupBy(l => l.EquipmentId!.Value)
                .ToDictionary(g => g.Key, g => (int)g.Sum(l => l.ActualQuantity));

            var equipments = await _equipmentRepository
                .GetListAsync(e => usageByEquipment.Keys.Contains(e.Id));

            foreach (var equipment in equipments)
            {
                equipment.UsageCount += usageByEquipment[equipment.Id];
                await _equipmentRepository.UpdateAsync(equipment, autoSave: true);
            }
        }
    }

    private async Task DeductStockFromIndicatorConsumptionsAsync(
        List<IndicatorConsumption> consumptions,
        Guid laboratoryTaskId)
    {
        var productConsumptions = consumptions.Where(c => c.ProductId.HasValue).ToList();
        var equipmentConsumptions = consumptions.Where(c => c.EquipmentId.HasValue).ToList();

        if (productConsumptions.Any())
        {
            var productIds = productConsumptions.Select(c => c.ProductId!.Value).Distinct().ToList();
            var products = await _productRepository.GetListAsync(p => productIds.Contains(p.Id));
            var productMap = products.ToDictionary(p => p.Id);

            var skus = await _productSkuRepository.GetListAsync(s => productIds.Contains(s.ProductId));
            var skuByProduct = skus.GroupBy(s => s.ProductId).ToDictionary(g => g.Key, g => g.ToList());

            var conversions = await _productUnitConversionRepository
                .GetListAsync(c => productIds.Contains(c.ProductId));
            var conversionMap = conversions.ToDictionary(c => (c.ProductId, c.ConvertedUnitId));

            foreach (var consumption in productConsumptions)
            {
                if (!productMap.TryGetValue(consumption.ProductId!.Value, out var product))
                    continue;

                var consumeUnitId = consumption.UnitId;
                decimal? unitsPerStorage = null;

                if (consumeUnitId == product.PackagingUnitId)
                    unitsPerStorage = 1;
                else if (consumeUnitId == product.VolumeUnitId && product.VolumePerUnit > 0)
                    unitsPerStorage = product.VolumePerUnit.Value;
                else if (consumeUnitId == product.BaseUnitId && product.PackagingQuantity > 0)
                    unitsPerStorage = 1m / product.PackagingQuantity.Value;

                if (unitsPerStorage == null && skuByProduct.TryGetValue(product.Id, out var pSkus))
                {
                    foreach (var sku in pSkus)
                    {
                        if (consumeUnitId == sku.PackagingUnitId) { unitsPerStorage = 1; break; }
                        if (consumeUnitId == sku.VolumeUnitId && sku.VolumePerUnit > 0) { unitsPerStorage = sku.VolumePerUnit.Value; break; }
                        if (consumeUnitId == sku.BaseUnitId && sku.PackagingQuantity > 0) { unitsPerStorage = 1m / sku.PackagingQuantity.Value; break; }
                    }
                }

                if (unitsPerStorage == null && consumeUnitId.HasValue
                    && conversionMap.TryGetValue((product.Id, consumeUnitId.Value), out var conv)
                    && conv.ConvertedQuantity > 0)
                {
                    unitsPerStorage = conv.ConvertedQuantity / conv.Quantity;
                }

                if (unitsPerStorage == null || unitsPerStorage.Value == 0)
                    continue;

                var storageQty = consumption.ActualQuantity / unitsPerStorage.Value;

                // BA: tạm thời comment out auto-deduction, chỉ hiện số nhập thực tế
                // var deductions = await _fifoStockService.DeductAsync(
                //     product, storageQty,
                //     StockMovementType.MaterialConsumption,
                //     laboratoryTaskId, null,
                //     "Tiêu hao thử nghiệm");
                //
                // if (product.ProductType == ProductType.PreparedSolution && deductions.Any())
                // {
                //     var productSkus = skuByProduct.GetValueOrDefault(product.Id);
                //     await UpdatePreparedSolutionRemainingVolumeAsync(product, productSkus, deductions);
                // }
            }
        }

        if (equipmentConsumptions.Any())
        {
            var usageByEquipment = equipmentConsumptions
                .GroupBy(c => c.EquipmentId!.Value)
                .ToDictionary(g => g.Key, g => (int)g.Sum(c => c.ActualQuantity));

            var equipments = await _equipmentRepository
                .GetListAsync(e => usageByEquipment.Keys.Contains(e.Id));

            foreach (var equipment in equipments)
            {
                equipment.UsageCount += usageByEquipment[equipment.Id];
                await _equipmentRepository.UpdateAsync(equipment, autoSave: true);
            }
        }
    }

    private async Task EnrichConsumptionLinesAsync(List<ConsumptionPreviewLineDto> lines)
    {
        var productIds = lines.Where(l => l.ProductId.HasValue).Select(l => l.ProductId!.Value).Distinct().ToList();
        var equipmentIds = lines.Where(l => l.EquipmentId.HasValue).Select(l => l.EquipmentId!.Value).Distinct().ToList();
        var unitIds = lines.Where(l => l.UnitId.HasValue).Select(l => l.UnitId!.Value).Distinct().ToList();

        var products = productIds.Any() ? await _productRepository.GetListAsync(p => productIds.Contains(p.Id)) : new List<Product>();
        var equipments = equipmentIds.Any() ? await _equipmentRepository.GetListAsync(e => equipmentIds.Contains(e.Id)) : new List<Equipment>();
        var skus = productIds.Any() ? await _productSkuRepository.GetListAsync(s => productIds.Contains(s.ProductId)) : new List<ProductSku>();
        var skuByProduct = skus.GroupBy(s => s.ProductId).ToDictionary(g => g.Key, g => g.ToList());

        var productUnitIds = products
            .SelectMany(p => new[] { p.PackagingUnitId, p.BaseUnitId })
            .Where(id => id.HasValue).Select(id => id!.Value);
        unitIds = unitIds.Union(productUnitIds).Distinct().ToList();

        var units = unitIds.Any() ? await _unitRepository.GetListAsync(u => unitIds.Contains(u.Id)) : new List<Unit>();
        var conversions = productIds.Any()
            ? await _productUnitConversionRepository.GetListAsync(c => productIds.Contains(c.ProductId))
            : new List<ProductUnitConversion>();

        var productMap = products.ToDictionary(p => p.Id);
        var equipmentMap = equipments.ToDictionary(e => e.Id);
        var unitMap = units.ToDictionary(u => u.Id, u => u.Name);
        var conversionMap = conversions.ToDictionary(c => (c.ProductId, c.ConvertedUnitId));

        foreach (var line in lines)
        {
            if (line.ProductId.HasValue && productMap.TryGetValue(line.ProductId.Value, out var product))
            {
                line.ProductName = product.Name;
                line.ProductCode = product.Code;
                line.ProductType = (int)product.ProductType;
                line.StorageAvailableQuantity = product.AvailableQuantity;
                var storageUnitId = product.PackagingUnitId ?? product.BaseUnitId;
                if (storageUnitId.HasValue && unitMap.TryGetValue(storageUnitId.Value, out var suName))
                    line.StorageUnitName = suName;

                var availableInUnit = product.AvailableQuantity;
                if (line.UnitId.HasValue)
                {
                    var consumeUnitId = line.UnitId.Value;
                    decimal? factor = null;

                    if (consumeUnitId == product.PackagingUnitId)
                        factor = 1;
                    else if (consumeUnitId == product.VolumeUnitId && product.VolumePerUnit > 0)
                        factor = product.VolumePerUnit.Value;
                    else if (consumeUnitId == product.BaseUnitId && product.PackagingQuantity > 0)
                        factor = 1m / product.PackagingQuantity.Value;

                    if (factor == null && skuByProduct.TryGetValue(product.Id, out var pSkus))
                    {
                        foreach (var sku in pSkus)
                        {
                            if (consumeUnitId == sku.PackagingUnitId) { factor = 1; break; }
                            if (consumeUnitId == sku.VolumeUnitId && sku.VolumePerUnit > 0) { factor = sku.VolumePerUnit.Value; break; }
                            if (consumeUnitId == sku.BaseUnitId && sku.PackagingQuantity > 0) { factor = 1m / sku.PackagingQuantity.Value; break; }
                        }
                    }

                    if (factor == null && conversionMap.TryGetValue((product.Id, consumeUnitId), out var conv) && conv.ConvertedQuantity > 0)
                        factor = conv.ConvertedQuantity / conv.Quantity;

                    if (factor.HasValue)
                        availableInUnit = product.AvailableQuantity * factor.Value;
                }
                line.AvailableQuantity = availableInUnit;
                line.ExceedsStock = line.ActualQuantity > availableInUnit;
            }
            if (line.EquipmentId.HasValue && equipmentMap.TryGetValue(line.EquipmentId.Value, out var equipment))
                line.EquipmentName = equipment.Name;
            if (line.UnitId.HasValue && unitMap.TryGetValue(line.UnitId.Value, out var unitName))
                line.UnitName = unitName;
        }
    }
}
