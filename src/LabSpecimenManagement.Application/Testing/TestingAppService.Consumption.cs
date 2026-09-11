using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.Equipments;
using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Products;
using LabSpecimenManagement.TestBatches;
using LabSpecimenManagement.TestRequests;
using LabSpecimenManagement.StockAdjustments;
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

    private IConsumableStockCalculator _consumableStockCalculator =>
        LazyServiceProvider.LazyGetRequiredService<IConsumableStockCalculator>();

    private IRepository<Unit, Guid> _unitRepository =>
        LazyServiceProvider.LazyGetRequiredService<IRepository<Unit, Guid>>();

    private IRepository<WarehouseImportReceipt, Guid> _warehouseImportReceiptRepository =>
        LazyServiceProvider.LazyGetRequiredService<IRepository<WarehouseImportReceipt, Guid>>();

    private IRepository<Warehouse, Guid> _warehouseRepository =>
        LazyServiceProvider.LazyGetRequiredService<IRepository<Warehouse, Guid>>();

    private IRepository<ProductWarehouseInventory, Guid> _productWarehouseInventoryRepository =>
        LazyServiceProvider.LazyGetRequiredService<IRepository<ProductWarehouseInventory, Guid>>();

    private IRepository<InventoryBarcode, Guid> _inventoryBarcodeRepository =>
        LazyServiceProvider.LazyGetRequiredService<IRepository<InventoryBarcode, Guid>>();

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

        var previewTask = await _laboratoryTaskRepository.GetAsync(laboratoryTaskId);
        await EnrichConsumptionLinesAsync(lines, previewTask.LaboratoryId);

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

        // Fetch task early — needed for lab filter and status update
        var task = await _laboratoryTaskRepository.GetAsync(laboratoryTaskId);

        // Handle consumption lines for this round
        if (input.ConsumptionLines != null && input.ConsumptionLines.Any())
        {
            var productIdsForWh = input.ConsumptionLines
                .Where(l => l.ProductId.HasValue).Select(l => l.ProductId!.Value).Distinct().ToList();
            var allLotsMap = await ResolveAllLotsForProductsAsync(productIdsForWh, task.LaboratoryId);

            // Preload unit conversion data for lot splitting
            var products = productIdsForWh.Any()
                ? await _productRepository.GetListAsync(p => productIdsForWh.Contains(p.Id))
                : new List<Product>();
            var productMap = products.ToDictionary(p => p.Id);
            var skus = productIdsForWh.Any()
                ? await _productSkuRepository.GetListAsync(s => productIdsForWh.Contains(s.ProductId))
                : new List<ProductSku>();
            var skuByProduct = skus.GroupBy(s => s.ProductId).ToDictionary(g => g.Key, g => g.ToList());
            var conversions = productIdsForWh.Any()
                ? await _productUnitConversionRepository.GetListAsync(c => productIdsForWh.Contains(c.ProductId))
                : new List<ProductUnitConversion>();
            var conversionMap = conversions.ToDictionary(c => (c.ProductId, c.ConvertedUnitId));

            // Resolve barcode codes for all lots
            var allLots = allLotsMap.Values.SelectMany(l => l).ToList();
            var barcodeMap = await ResolveBarcodeCodesForLotsAsync(allLots);

            // Compute consumable stock per warehouse
            var consumableProductWhMap = productIdsForWh
                .ToDictionary(pid => pid, pid =>
                {
                    var first = allLotsMap.GetValueOrDefault(pid)?.FirstOrDefault();
                    return first?.WarehouseId;
                });
            var consumableStocks = await _consumableStockCalculator.ComputeAsync(consumableProductWhMap);

            var newConsumptions = new List<IndicatorConsumption>();
            foreach (var line in input.ConsumptionLines)
            {
                if (line.EquipmentId.HasValue || !line.ProductId.HasValue)
                {
                    var consumption = new IndicatorConsumption(
                        GuidGenerator.Create(), input.IndicatorId,
                        line.ProductId, line.EquipmentId, line.UnitId,
                        line.RecommendedQuantity);
                    consumption.ActualQuantity = line.ActualQuantity;
                    consumption.SubmitDate = Clock.Now;
                    await _indicatorConsumptionRepository.InsertAsync(consumption, autoSave: false);
                    newConsumptions.Add(consumption);
                    continue;
                }

                var productLots = allLotsMap.GetValueOrDefault(line.ProductId.Value);
                if (productLots == null || !productLots.Any()
                    || !productMap.TryGetValue(line.ProductId.Value, out var product))
                {
                    var consumption = new IndicatorConsumption(
                        GuidGenerator.Create(), input.IndicatorId,
                        line.ProductId, null, line.UnitId, line.RecommendedQuantity);
                    consumption.ActualQuantity = line.ActualQuantity;
                    consumption.SubmitDate = Clock.Now;
                    consumption.ConsumableStockBefore = consumableStocks.GetValueOrDefault(line.ProductId.Value);
                    await _indicatorConsumptionRepository.InsertAsync(consumption, autoSave: false);
                    newConsumptions.Add(consumption);
                    continue;
                }

                var unitsPerStorage = ResolveUnitsPerStorage(line.UnitId, product, skuByProduct, conversionMap);
                if (unitsPerStorage == null || unitsPerStorage.Value == 0)
                {
                    var consumption = new IndicatorConsumption(
                        GuidGenerator.Create(), input.IndicatorId,
                        line.ProductId, null, line.UnitId, line.RecommendedQuantity);
                    consumption.ActualQuantity = line.ActualQuantity;
                    consumption.SubmitDate = Clock.Now;
                    consumption.ConsumableStockBefore = consumableStocks.GetValueOrDefault(line.ProductId.Value);
                    await _indicatorConsumptionRepository.InsertAsync(consumption, autoSave: false);
                    newConsumptions.Add(consumption);
                    continue;
                }

                var remainingQty = line.ActualQuantity;
                bool isFirst = true;
                foreach (var lot in productLots)
                {
                    if (remainingQty <= 0) break;

                    var lotAvailableInConsumptionUnit = lot.RemainingBaseQuantity * unitsPerStorage.Value;
                    if (lotAvailableInConsumptionUnit <= 0) continue;

                    var consumeFromLot = Math.Min(remainingQty, lotAvailableInConsumptionUnit);

                    var consumption = new IndicatorConsumption(
                        GuidGenerator.Create(), input.IndicatorId,
                        line.ProductId, null, line.UnitId,
                        isFirst ? line.RecommendedQuantity : 0);
                    consumption.ActualQuantity = consumeFromLot;
                    consumption.SubmitDate = Clock.Now;
                    consumption.WarehouseId = lot.WarehouseId;
                    consumption.LotNumber = lot.LotNumber;
                    consumption.WarehouseImportReceiptItemId = lot.WarehouseImportReceiptItemId;
                    consumption.BarcodeCode = barcodeMap.GetValueOrDefault(lot.WarehouseImportReceiptItemId);
                    consumption.ConsumableStockBefore = consumableStocks.GetValueOrDefault(line.ProductId.Value);
                    await _indicatorConsumptionRepository.InsertAsync(consumption, autoSave: false);
                    newConsumptions.Add(consumption);

                    remainingQty -= consumeFromLot;
                    isFirst = false;
                }

                // Overflow: remaining quantity exceeds all lots — assign to last lot
                if (remainingQty > 0)
                {
                    var lastLot = productLots.Last();
                    var consumption = new IndicatorConsumption(
                        GuidGenerator.Create(), input.IndicatorId,
                        line.ProductId, null, line.UnitId, 0);
                    consumption.ActualQuantity = remainingQty;
                    consumption.SubmitDate = Clock.Now;
                    consumption.WarehouseId = lastLot.WarehouseId;
                    consumption.LotNumber = lastLot.LotNumber;
                    consumption.WarehouseImportReceiptItemId = lastLot.WarehouseImportReceiptItemId;
                    consumption.BarcodeCode = barcodeMap.GetValueOrDefault(lastLot.WarehouseImportReceiptItemId);
                    consumption.ConsumableStockBefore = consumableStocks.GetValueOrDefault(line.ProductId.Value);
                    await _indicatorConsumptionRepository.InsertAsync(consumption, autoSave: false);
                    newConsumptions.Add(consumption);
                }
            }

            await CurrentUnitOfWork.SaveChangesAsync();

            await DeductStockFromIndicatorConsumptionsAsync(newConsumptions, laboratoryTaskId);

            foreach (var c in newConsumptions)
            {
                c.IsDeducted = true;
                await _indicatorConsumptionRepository.UpdateAsync(c, autoSave: false);
            }

            await CurrentUnitOfWork.SaveChangesAsync();

            var barcodeTargets = newConsumptions
                .Where(c => c.ProductId.HasValue && c.WarehouseId.HasValue)
                .Select(c => (c.ProductId!.Value, c.WarehouseId!.Value));
            await _barcodeStatusService.RecalculateForProductsAsync(barcodeTargets);
        }

        // Update task status
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
            var draftTask = await _laboratoryTaskRepository.GetAsync(laboratoryTaskId);
            var productIdsForWh = input.ConsumptionLines
                .Where(l => l.ProductId.HasValue).Select(l => l.ProductId!.Value).Distinct();
            var warehouseMap = await ResolveWarehouseForProductsAsync(productIdsForWh, draftTask.LaboratoryId);

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
                consumption.SubmitDate = Clock.Now;
                if (line.ProductId.HasValue)
                    consumption.WarehouseId = warehouseMap.GetValueOrDefault(line.ProductId.Value);
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

        var bulkTask = await _laboratoryTaskRepository.GetAsync(laboratoryTaskId);
        await EnrichConsumptionLinesAsync(aggregated, bulkTask.LaboratoryId);

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
        {
            var productIdsForWh = input.ConsumptionLines
                .Where(l => l.ProductId.HasValue).Select(l => l.ProductId!.Value).Distinct().ToList();
            var allLotsMap = await ResolveAllLotsForProductsAsync(productIdsForWh, task.LaboratoryId);

            var products = productIdsForWh.Any()
                ? await _productRepository.GetListAsync(p => productIdsForWh.Contains(p.Id))
                : new List<Product>();
            var productMap = products.ToDictionary(p => p.Id);
            var skus = productIdsForWh.Any()
                ? await _productSkuRepository.GetListAsync(s => productIdsForWh.Contains(s.ProductId))
                : new List<ProductSku>();
            var skuByProduct = skus.GroupBy(s => s.ProductId).ToDictionary(g => g.Key, g => g.ToList());
            var conversions = productIdsForWh.Any()
                ? await _productUnitConversionRepository.GetListAsync(c => productIdsForWh.Contains(c.ProductId))
                : new List<ProductUnitConversion>();
            var conversionMap = conversions.ToDictionary(c => (c.ProductId, c.ConvertedUnitId));

            var allLots = allLotsMap.Values.SelectMany(l => l).ToList();
            var barcodeMap = await ResolveBarcodeCodesForLotsAsync(allLots);

            var consumableProductWhMap = productIdsForWh
                .ToDictionary(pid => pid, pid =>
                {
                    var first = allLotsMap.GetValueOrDefault(pid)?.FirstOrDefault();
                    return first?.WarehouseId;
                });
            var consumableStocks = await _consumableStockCalculator.ComputeAsync(consumableProductWhMap);

            var indicatorCount = submittedIndicators.Count;

            // Track cumulative consumption from each lot across all indicators (shared pool)
            var consumedFromLot = new Dictionary<Guid, decimal>();
            foreach (var lot in allLots)
                consumedFromLot.TryAdd(lot.WarehouseImportReceiptItemId, 0m);

            var allNewConsumptions = new List<IndicatorConsumption>();

            foreach (var ind in submittedIndicators)
            {
                foreach (var line in input.ConsumptionLines)
                {
                    var perIndicatorQty = line.ActualQuantity / indicatorCount;
                    var perIndicatorRecommended = line.RecommendedQuantity / indicatorCount;

                    if (line.EquipmentId.HasValue || !line.ProductId.HasValue)
                    {
                        var consumption = new IndicatorConsumption(
                            GuidGenerator.Create(), ind.Id,
                            line.ProductId, line.EquipmentId, line.UnitId,
                            perIndicatorRecommended);
                        consumption.ActualQuantity = perIndicatorQty;
                        consumption.IsDeducted = true;
                        consumption.SubmitDate = Clock.Now;
                        await _indicatorConsumptionRepository.InsertAsync(consumption, autoSave: false);
                        allNewConsumptions.Add(consumption);
                        continue;
                    }

                    var productLots = allLotsMap.GetValueOrDefault(line.ProductId.Value);
                    if (productLots == null || !productLots.Any()
                        || !productMap.TryGetValue(line.ProductId.Value, out var product))
                    {
                        var consumption = new IndicatorConsumption(
                            GuidGenerator.Create(), ind.Id,
                            line.ProductId, null, line.UnitId, perIndicatorRecommended);
                        consumption.ActualQuantity = perIndicatorQty;
                        consumption.IsDeducted = true;
                        consumption.SubmitDate = Clock.Now;
                        consumption.ConsumableStockBefore = consumableStocks.GetValueOrDefault(line.ProductId.Value);
                        await _indicatorConsumptionRepository.InsertAsync(consumption, autoSave: false);
                        allNewConsumptions.Add(consumption);
                        continue;
                    }

                    var unitsPerStorage = ResolveUnitsPerStorage(line.UnitId, product, skuByProduct, conversionMap);
                    if (unitsPerStorage == null || unitsPerStorage.Value == 0)
                    {
                        var consumption = new IndicatorConsumption(
                            GuidGenerator.Create(), ind.Id,
                            line.ProductId, null, line.UnitId, perIndicatorRecommended);
                        consumption.ActualQuantity = perIndicatorQty;
                        consumption.IsDeducted = true;
                        consumption.SubmitDate = Clock.Now;
                        consumption.ConsumableStockBefore = consumableStocks.GetValueOrDefault(line.ProductId.Value);
                        await _indicatorConsumptionRepository.InsertAsync(consumption, autoSave: false);
                        allNewConsumptions.Add(consumption);
                        continue;
                    }

                    var remainingQty = perIndicatorQty;
                    bool isFirst = true;
                    foreach (var lot in productLots)
                    {
                        if (remainingQty <= 0) break;

                        var lotTotalInConsumptionUnit = lot.RemainingBaseQuantity * unitsPerStorage.Value;
                        var alreadyConsumed = consumedFromLot.GetValueOrDefault(lot.WarehouseImportReceiptItemId) * unitsPerStorage.Value;
                        var lotAvailable = lotTotalInConsumptionUnit - alreadyConsumed;
                        if (lotAvailable <= 0) continue;

                        var consumeFromLot = Math.Min(remainingQty, lotAvailable);

                        var consumption = new IndicatorConsumption(
                            GuidGenerator.Create(), ind.Id,
                            line.ProductId, null, line.UnitId,
                            isFirst ? perIndicatorRecommended : 0);
                        consumption.ActualQuantity = consumeFromLot;
                        consumption.IsDeducted = true;
                        consumption.SubmitDate = Clock.Now;
                        consumption.WarehouseId = lot.WarehouseId;
                        consumption.LotNumber = lot.LotNumber;
                        consumption.WarehouseImportReceiptItemId = lot.WarehouseImportReceiptItemId;
                        consumption.BarcodeCode = barcodeMap.GetValueOrDefault(lot.WarehouseImportReceiptItemId);
                        consumption.ConsumableStockBefore = consumableStocks.GetValueOrDefault(line.ProductId.Value);
                        await _indicatorConsumptionRepository.InsertAsync(consumption, autoSave: false);
                        allNewConsumptions.Add(consumption);

                        consumedFromLot[lot.WarehouseImportReceiptItemId] += consumeFromLot / unitsPerStorage.Value;
                        remainingQty -= consumeFromLot;
                        isFirst = false;
                    }

                    if (remainingQty > 0)
                    {
                        var lastLot = productLots.Last();
                        var consumption = new IndicatorConsumption(
                            GuidGenerator.Create(), ind.Id,
                            line.ProductId, null, line.UnitId, 0);
                        consumption.ActualQuantity = remainingQty;
                        consumption.IsDeducted = true;
                        consumption.SubmitDate = Clock.Now;
                        consumption.WarehouseId = lastLot.WarehouseId;
                        consumption.LotNumber = lastLot.LotNumber;
                        consumption.WarehouseImportReceiptItemId = lastLot.WarehouseImportReceiptItemId;
                        consumption.BarcodeCode = barcodeMap.GetValueOrDefault(lastLot.WarehouseImportReceiptItemId);
                        consumption.ConsumableStockBefore = consumableStocks.GetValueOrDefault(line.ProductId.Value);
                        await _indicatorConsumptionRepository.InsertAsync(consumption, autoSave: false);
                        allNewConsumptions.Add(consumption);
                    }
                }
            }

            await CurrentUnitOfWork.SaveChangesAsync();

            await DeductStockFromConsumptionLineInputsAsync(input.ConsumptionLines, laboratoryTaskId);

            var barcodeTargets = allNewConsumptions
                .Where(c => c.ProductId.HasValue && c.WarehouseId.HasValue)
                .Select(c => (c.ProductId!.Value, c.WarehouseId!.Value));
            await _barcodeStatusService.RecalculateForProductsAsync(barcodeTargets);
        }

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

    private async Task EnrichConsumptionLinesAsync(List<ConsumptionPreviewLineDto> lines, Guid? laboratoryId = null)
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

        var warehouseMap = await ResolveWarehouseForProductsAsync(productIds, laboratoryId);
        var perWhAvailable = new Dictionary<Guid, decimal>();
        if (productIds.Any())
        {
            var itemQ = await _importReceiptItemRepository.GetQueryableAsync();
            var receiptQ = await _warehouseImportReceiptRepository.GetQueryableAsync();
            perWhAvailable = (from item in itemQ
                join receipt in receiptQ on item.WarehouseImportReceiptId equals receipt.Id
                where productIds.Contains(item.ProductId)
                      && receipt.Status == WarehouseImportReceiptStatus.Completed
                      && item.Status == WarehouseImportLineStatus.Checked
                      && (item.ReceivedBaseQuantity - item.ConsumedBaseQuantity) > 0
                select new
                {
                    item.ProductId,
                    EffWh = item.StorageWarehouseId ?? receipt.WarehouseId,
                    Qty = item.ReceivedBaseQuantity - item.ConsumedBaseQuantity
                })
                .ToList()
                .Where(x => warehouseMap.TryGetValue(x.ProductId, out var wh) && wh.HasValue && x.EffWh == wh.Value)
                .GroupBy(x => x.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));
        }

        foreach (var line in lines)
        {
            if (line.ProductId.HasValue && productMap.TryGetValue(line.ProductId.Value, out var product))
            {
                line.ProductName = product.Name;
                line.ProductCode = product.Code;
                line.ProductType = (int)product.ProductType;
                line.StorageAvailableQuantity = perWhAvailable.GetValueOrDefault(product.Id, product.AvailableQuantity);
                var storageUnitId = product.PackagingUnitId ?? product.BaseUnitId;
                if (storageUnitId.HasValue && unitMap.TryGetValue(storageUnitId.Value, out var suName))
                    line.StorageUnitName = suName;

                var whAvailable = perWhAvailable.GetValueOrDefault(product.Id, product.AvailableQuantity);
                var availableInUnit = whAvailable;
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
                        availableInUnit = whAvailable * factor.Value;
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

    private async Task<Dictionary<Guid, Guid?>> ResolveWarehouseForProductsAsync(
        IEnumerable<Guid> productIds, Guid? laboratoryId = null)
    {
        var resolved = await ResolveWarehouseAndLotForProductsAsync(productIds, laboratoryId);
        return resolved.ToDictionary(kv => kv.Key, kv => kv.Value.WarehouseId);
    }

    private async Task<Dictionary<Guid, (Guid? WarehouseId, string? LotNumber)>> ResolveWarehouseAndLotForProductsAsync(
        IEnumerable<Guid> productIds, Guid? laboratoryId = null)
    {
        var allLots = await ResolveAllLotsForProductsAsync(productIds, laboratoryId);
        return allLots.ToDictionary(
            kv => kv.Key,
            kv =>
            {
                var first = kv.Value.FirstOrDefault();
                return (first?.WarehouseId, first?.LotNumber);
            });
    }

    private async Task<Dictionary<Guid, List<LotAllocation>>> ResolveAllLotsForProductsAsync(
        IEnumerable<Guid> productIds, Guid? laboratoryId = null)
    {
        var ids = productIds.Distinct().ToList();
        if (!ids.Any()) return new Dictionary<Guid, List<LotAllocation>>();

        var warehouseQuery = await _warehouseRepository.GetQueryableAsync();
        var excludedWarehouseIds = warehouseQuery
            .Where(w => w.Level == 0 || w.IsDefault)
            .Select(w => w.Id)
            .ToHashSet();

        HashSet<Guid>? labWarehouseIds = null;
        if (laboratoryId.HasValue)
        {
            labWarehouseIds = warehouseQuery
                .Where(w => w.LaboratoryId == laboratoryId.Value)
                .Select(w => w.Id)
                .ToHashSet();
        }

        var itemQuery = await _importReceiptItemRepository.GetQueryableAsync();
        var receiptQuery = await _warehouseImportReceiptRepository.GetQueryableAsync();

        var lots = (from item in itemQuery
                    join receipt in receiptQuery on item.WarehouseImportReceiptId equals receipt.Id
                    where ids.Contains(item.ProductId)
                          && receipt.Status == WarehouseImportReceiptStatus.Completed
                          && item.Status == WarehouseImportLineStatus.Checked
                          && (item.ReceivedBaseQuantity - item.ConsumedBaseQuantity) > 0
                    orderby item.LotExpiryDate == null ? 1 : 0,
                            item.LotExpiryDate,
                            receipt.ActualImportDate
                    select new LotAllocation
                    {
                        WarehouseImportReceiptItemId = item.Id,
                        ProductId = item.ProductId,
                        WarehouseId = item.StorageWarehouseId ?? receipt.WarehouseId,
                        LotNumber = item.LotNumber,
                        RemainingBaseQuantity = item.ReceivedBaseQuantity - item.ConsumedBaseQuantity,
                        LotExpiryDate = item.LotExpiryDate,
                        BarcodeValues = item.BarcodeValues,
                        SkuId = item.SkuId,
                    })
                    .ToList()
                    .Where(l => l.WarehouseId.HasValue
                                && !excludedWarehouseIds.Contains(l.WarehouseId.Value)
                                && (labWarehouseIds == null || labWarehouseIds.Contains(l.WarehouseId.Value)))
                    .ToList();

        // Fallback: if lab filter returned empty but lots exist without lab filter, use all warehouses
        if (labWarehouseIds != null && !lots.Any())
        {
            lots = (from item in itemQuery
                        join receipt in receiptQuery on item.WarehouseImportReceiptId equals receipt.Id
                        where ids.Contains(item.ProductId)
                              && receipt.Status == WarehouseImportReceiptStatus.Completed
                              && item.Status == WarehouseImportLineStatus.Checked
                              && (item.ReceivedBaseQuantity - item.ConsumedBaseQuantity) > 0
                        orderby item.LotExpiryDate == null ? 1 : 0,
                                item.LotExpiryDate,
                                receipt.ActualImportDate
                        select new LotAllocation
                        {
                            WarehouseImportReceiptItemId = item.Id,
                            ProductId = item.ProductId,
                            WarehouseId = item.StorageWarehouseId ?? receipt.WarehouseId,
                            LotNumber = item.LotNumber,
                            RemainingBaseQuantity = item.ReceivedBaseQuantity - item.ConsumedBaseQuantity,
                            LotExpiryDate = item.LotExpiryDate,
                            BarcodeValues = item.BarcodeValues,
                            SkuId = item.SkuId,
                        })
                        .ToList()
                        .Where(l => l.WarehouseId.HasValue
                                    && !excludedWarehouseIds.Contains(l.WarehouseId.Value))
                        .ToList();
        }

        return ids.ToDictionary(
            id => id,
            id => lots.Where(l => l.ProductId == id).ToList());
    }

    private async Task<Dictionary<Guid, string?>> ResolveBarcodeCodesForLotsAsync(
        List<LotAllocation> lots)
    {
        var receiptItemIds = lots
            .Select(l => l.WarehouseImportReceiptItemId)
            .Distinct()
            .ToList();
        if (!receiptItemIds.Any())
            return new Dictionary<Guid, string?>();

        var barcodes = await _inventoryBarcodeRepository.GetListAsync(
            b => receiptItemIds.Contains(b.OriginWarehouseImportReceiptItemId));

        var barcodesByReceiptItem = barcodes
            .GroupBy(b => b.OriginWarehouseImportReceiptItemId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(b => b.Status == InventoryBarcodeStatus.InUse ? 0 :
                                    b.Status == InventoryBarcodeStatus.NotUsed ? 1 : 2)
                      .ToList());

        return receiptItemIds.ToDictionary(
            riId => riId,
            riId =>
            {
                if (!barcodesByReceiptItem.TryGetValue(riId, out var lotBarcodes))
                    return (string?)null;
                var active = lotBarcodes.FirstOrDefault(b =>
                    b.Status == InventoryBarcodeStatus.InUse
                    || b.Status == InventoryBarcodeStatus.NotUsed);
                return active?.Code;
            });
    }

    private decimal? ResolveUnitsPerStorage(
        Guid? consumeUnitId,
        Product product,
        Dictionary<Guid, List<ProductSku>> skuByProduct,
        Dictionary<(Guid, Guid), ProductUnitConversion> conversionMap)
    {
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

        return unitsPerStorage;
    }

    private class LotAllocation
    {
        public Guid WarehouseImportReceiptItemId { get; set; }
        public Guid ProductId { get; set; }
        public Guid? WarehouseId { get; set; }
        public string? LotNumber { get; set; }
        public decimal RemainingBaseQuantity { get; set; }
        public DateTime? LotExpiryDate { get; set; }
        public string? BarcodeValues { get; set; }
        public Guid? SkuId { get; set; }
    }
}
