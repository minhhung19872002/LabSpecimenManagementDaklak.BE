using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Products;
using LabSpecimenManagement.StockAdjustments;
using LabSpecimenManagement.WarehouseImports;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.Testing;

// TODO: EXTERNAL-CONSUMPTION — remove this entire file after BA confirms
// UAT-only receiver: phụ thuộc entity phase 2 (IndicatorConsumption, MaterialConsumptionItem, barcode...).
// KHÔNG cherry-pick file này sang main/PROD.
public partial class TestingAppService
{
    private static string NormalizeName(string? name) =>
        (name ?? string.Empty).Trim().Normalize(NormalizationForm.FormC);

    public async Task ProcessExternalConsumptionAsync(ExternalConsumptionRequestDto input)
    {
        if (input.Indicators == null || !input.Indicators.Any()) return;

        var labQ = await _laboratoryRepository.GetQueryableAsync();
        var lab = await AsyncExecuter.FirstOrDefaultAsync(
            labQ.Where(l => l.Code == input.LaboratoryCode && !l.IsDeleted));
        if (lab == null) return;

        // Match theo tên đã normalize (NFC + trim): tên tiếng Việt trong DB có thể lưu dạng
        // tổ hợp (NFD) hoặc dựng sẵn (NFC) tùy thời điểm nhập → so sánh exact sẽ miss.
        // Master data nhỏ (vài trăm row) nên load hết rồi match in-memory.
        var methodQ = await _testMethodRepository.GetQueryableAsync();
        var methods = await AsyncExecuter.ToListAsync(
            methodQ.Where(m => !m.IsDeleted).Select(m => new { m.Id, m.Name }));
        var methodByName = methods
            .GroupBy(m => NormalizeName(m.Name))
            .ToDictionary(g => g.Key, g => g.First().Id);

        var indicatorQ = await _indicatorRepository.GetQueryableAsync();
        var indicators = await AsyncExecuter.ToListAsync(
            indicatorQ.Where(i => !i.IsDeleted).Select(i => new { i.Id, i.Name }));
        var indicatorByName = indicators
            .GroupBy(i => NormalizeName(i.Name))
            .ToDictionary(g => g.Key, g => g.First().Id);

        var stQ = await _specimenTypeRepository.GetQueryableAsync();
        var specimenTypes = await AsyncExecuter.ToListAsync(
            stQ.Where(s => !s.IsDeleted).Select(s => new { s.Id, s.Name }));
        var stByName = specimenTypes
            .GroupBy(s => NormalizeName(s.Name))
            .ToDictionary(g => g.Key, g => g.First().Id);

        var tmiQuery = await _testMethodIndicatorRepository.GetQueryableAsync();
        var consumptionQuery = await _materialConsumptionRepository.GetQueryableAsync();

        var consumptionByTmi = new Dictionary<Guid, List<MaterialConsumptionItem>>();
        var consumptionRecords = new List<(ExternalConsumptionIndicatorDto Input, MaterialConsumptionItem Item)>();

        foreach (var ind in input.Indicators)
        {
            if (!methodByName.TryGetValue(NormalizeName(ind.MethodName), out var methodId)) continue;
            if (!indicatorByName.TryGetValue(NormalizeName(ind.IndicatorName), out var indicatorId)) continue;
            if (!stByName.TryGetValue(NormalizeName(ind.SpecimenTypeName), out var specimenTypeId)) continue;

            var tmi = await AsyncExecuter.FirstOrDefaultAsync(
                tmiQuery.Where(x =>
                    x.TestMethodId == methodId &&
                    x.IndicatorId == indicatorId &&
                    x.SpecimenTypeId == specimenTypeId));

            if (tmi == null) continue;

            if (!consumptionByTmi.TryGetValue(tmi.Id, out var consumptionItems))
            {
                consumptionItems = await AsyncExecuter.ToListAsync(
                    consumptionQuery.Where(c => c.TestMethodIndicatorId == tmi.Id));
                consumptionByTmi[tmi.Id] = consumptionItems;
            }

            foreach (var item in consumptionItems)
            {
                if (item.AvgConsumptionPerSample == null || item.AvgConsumptionPerSample.Value == 0) continue;
                consumptionRecords.Add((ind, item));
            }
        }

        if (!consumptionRecords.Any()) return;

        var lotProductIds = consumptionRecords
            .Where(r => r.Item.ProductId.HasValue)
            .Select(r => r.Item.ProductId!.Value)
            .Distinct().ToList();

        var convProducts = lotProductIds.Any()
            ? await _productRepository.GetListAsync(p => lotProductIds.Contains(p.Id))
            : new List<Product>();
        var convProductMap = convProducts.ToDictionary(p => p.Id);
        var convSkus = lotProductIds.Any()
            ? await _productSkuRepository.GetListAsync(s => lotProductIds.Contains(s.ProductId))
            : new List<ProductSku>();
        var convSkuByProduct = convSkus.GroupBy(s => s.ProductId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var lotTracker = new Dictionary<Guid, List<(string? LotNumber, Guid? WarehouseId, Guid ReceiptItemId, decimal Remaining)>>();
        var stockBeforeByWarehouse = new Dictionary<(Guid, Guid?), decimal>();

        if (lotProductIds.Any())
        {
            var lotItemQ = await _importReceiptItemRepository.GetQueryableAsync();
            var lotReceiptQ = await _warehouseImportReceiptRepository.GetQueryableAsync();

            var whQ = await _warehouseRepository.GetQueryableAsync();
            var excludedWhIds = whQ
                .Where(w => w.Level == 0 || w.IsDefault)
                .Select(w => w.Id)
                .ToHashSet();

            var labWarehouseIds = whQ
                .Where(w => w.LaboratoryId == lab.Id)
                .Select(w => w.Id)
                .ToHashSet();

            var rawLots = (from ii in lotItemQ
                           join ir in lotReceiptQ on ii.WarehouseImportReceiptId equals ir.Id
                           where lotProductIds.Contains(ii.ProductId)
                                 && ir.Status == WarehouseImportReceiptStatus.Completed
                                 && ii.Status == WarehouseImportLineStatus.Checked
                           orderby ii.LotExpiryDate == null ? 1 : 0,
                                   ii.LotExpiryDate,
                                   ir.ActualImportDate
                           select new
                           {
                               ii.ProductId,
                               ReceiptItemId = ii.Id,
                               EffWh = ii.StorageWarehouseId ?? ir.WarehouseId,
                               ii.LotNumber,
                               ii.LotExpiryDate,
                               BaseReceived = ii.ReceivedBaseQuantity,
                               BaseConsumed = ii.ConsumedBaseQuantity
                           }).ToList()
                           .Where(l => !l.EffWh.HasValue || !excludedWhIds.Contains(l.EffWh.Value))
                           .ToList();

            if (labWarehouseIds.Count > 0)
            {
                var labFilteredLots = rawLots.Where(l => l.EffWh.HasValue && labWarehouseIds.Contains(l.EffWh.Value)).ToList();
                if (labFilteredLots.Any())
                    rawLots = labFilteredLots;
            }

            var pwiQ = await _productWarehouseInventoryRepository.GetQueryableAsync();
            var lastInventoryMap = pwiQ
                .Where(pwi => lotProductIds.Contains(pwi.ProductId))
                .Select(pwi => new { pwi.ProductId, pwi.WarehouseId, pwi.LastInventoryDate })
                .ToList()
                .GroupBy(x => x.ProductId)
                .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.WarehouseId, x => x.LastInventoryDate));

            var icQ = await _indicatorConsumptionRepository.GetQueryableAsync();
            var ktvConsumedByLot = icQ
                .Where(c => lotProductIds.Contains(c.ProductId ?? Guid.Empty)
                    && c.PreparedSolutionBatchId == null
                    && c.LotNumber != null
                    && c.IsDeducted)
                .Select(c => new { c.ProductId, c.LotNumber, c.WarehouseId, c.ActualQuantity, c.ConsumptionDate, c.SubmitDate })
                .ToList()
                .Where(c =>
                {
                    if (c.ProductId == null || c.WarehouseId == null) return true;
                    if (!lastInventoryMap.TryGetValue(c.ProductId.Value, out var wMap)) return true;
                    if (!wMap.TryGetValue(c.WarehouseId.Value, out var lastDate)) return true;
                    var date = c.ConsumptionDate ?? c.SubmitDate;
                    return date.HasValue && date.Value > lastDate;
                })
                .ToList();

            foreach (var g in rawLots.GroupBy(l => l.ProductId))
            {
                var pid2 = g.Key;
                decimal? vpu2 = null;
                if (convProductMap.TryGetValue(pid2, out var prod2))
                {
                    if (prod2.VolumePerUnit.HasValue && prod2.VolumePerUnit.Value > 0)
                        vpu2 = (decimal)prod2.VolumePerUnit.Value;
                    else if (convSkuByProduct.TryGetValue(pid2, out var skus2))
                    {
                        var sk = skus2.FirstOrDefault(s => s.VolumePerUnit.HasValue && s.VolumePerUnit > 0);
                        if (sk != null) vpu2 = (decimal)sk.VolumePerUnit!.Value;
                    }
                }

                var lots = g.Select(l =>
                {
                    var ktvUsedDisplay = ktvConsumedByLot
                        .Where(c => c.ProductId == l.ProductId
                            && c.LotNumber == l.LotNumber
                            && c.WarehouseId == l.EffWh)
                        .Sum(c => c.ActualQuantity);
                    var ktvUsedBase = vpu2.HasValue && vpu2.Value > 0
                        ? ktvUsedDisplay / vpu2.Value
                        : ktvUsedDisplay;
                    var remaining = l.BaseReceived - l.BaseConsumed - ktvUsedBase;
                    return (l.LotNumber, WarehouseId: l.EffWh, ReceiptItemId: l.ReceiptItemId, Remaining: remaining);
                }).ToList();

                lotTracker[pid2] = lots;

                foreach (var byWh in lots.GroupBy(l => l.WarehouseId))
                {
                    var totalBase = byWh.Sum(l => l.Remaining);
                    var totalDisplay = vpu2.HasValue && vpu2.Value > 0 ? totalBase * vpu2.Value : totalBase;
                    stockBeforeByWarehouse[(pid2, byWh.Key)] = Math.Max(0, totalDisplay);
                }
            }
        }

        var allReceiptItemIds = lotTracker.Values.SelectMany(lots => lots.Select(l => l.ReceiptItemId)).Distinct().ToList();
        var barcodeMap = new Dictionary<Guid, string?>();
        if (lotProductIds.Any())
        {
            var allProductReceiptItemIds = (await _importReceiptItemRepository.GetQueryableAsync())
                .Where(ri => lotProductIds.Contains(ri.ProductId))
                .Select(ri => new { ri.Id, ri.ProductId, ri.LotNumber })
                .ToList();
            var allProductRiIds = allProductReceiptItemIds.Select(ri => ri.Id).Distinct().ToList();

            var barcodes = await _inventoryBarcodeRepository.GetListAsync(
                b => allProductRiIds.Contains(b.OriginWarehouseImportReceiptItemId));

            var riToProductLot = allProductReceiptItemIds.ToDictionary(ri => ri.Id, ri => (ri.ProductId, ri.LotNumber));
            var barcodesByProductLot = new Dictionary<(Guid, string?), List<InventoryBarcode>>();
            foreach (var b in barcodes)
            {
                if (!riToProductLot.TryGetValue(b.OriginWarehouseImportReceiptItemId, out var key)) continue;
                if (!barcodesByProductLot.TryGetValue(key, out var list))
                {
                    list = new List<InventoryBarcode>();
                    barcodesByProductLot[key] = list;
                }
                list.Add(b);
            }

            foreach (var riId in allReceiptItemIds)
            {
                if (!riToProductLot.TryGetValue(riId, out var plKey)) continue;
                if (!barcodesByProductLot.TryGetValue(plKey, out var lotBarcodes)) continue;
                var active = lotBarcodes
                    .OrderBy(b => b.Status == InventoryBarcodeStatus.InUse ? 0 :
                                  b.Status == InventoryBarcodeStatus.NotUsed ? 1 : 2)
                    .FirstOrDefault(b => b.Status == InventoryBarcodeStatus.InUse
                                      || b.Status == InventoryBarcodeStatus.NotUsed);
                barcodeMap[riId] = active?.Code;
            }
        }

        var fakeIndicatorId = GuidGenerator.Create();
        var now = Clock.Now;

        foreach (var (ind, item) in consumptionRecords)
        {
            var consumeQty = item.AvgConsumptionPerSample!.Value;

            if (!item.ProductId.HasValue)
            {
                var c = new IndicatorConsumption(
                    GuidGenerator.Create(), fakeIndicatorId, null, item.EquipmentId,
                    item.UnitId, consumeQty);
                c.IsDeducted = true;
                c.SubmitDate = now;
                c.ConsumptionDate = now;
                c.TechnicianName = ind.TechnicianName;
                await _indicatorConsumptionRepository.InsertAsync(c, autoSave: false);
                continue;
            }

            var prodId = item.ProductId.Value;

            if (lotTracker.TryGetValue(prodId, out var lots) && lots.Count > 0)
            {
                var baseQty = ConvertConsumptionToBaseUnits(
                    prodId, item.UnitId, consumeQty, convProductMap, convSkuByProduct);
                if (baseQty.HasValue && baseQty.Value > 0)
                {
                    var remainingBase = baseQty.Value;
                    var ratio = consumeQty / baseQty.Value;

                    var splits = new List<(string? LotNumber, Guid? WarehouseId, Guid ReceiptItemId, decimal TakeBase)>();
                    for (int i = 0; i < lots.Count && remainingBase > 0; i++)
                    {
                        if (lots[i].Remaining <= 0) continue;
                        var take = Math.Min(lots[i].Remaining, remainingBase);
                        var lotNo = lots[i].LotNumber;
                        var lotWh = lots[i].WarehouseId;
                        var lotRiId = lots[i].ReceiptItemId;
                        lots[i] = (lotNo, lotWh, lotRiId, lots[i].Remaining - take);
                        remainingBase -= take;
                        splits.Add((lotNo, lotWh, lotRiId, take));
                    }

                    if (remainingBase > 0)
                    {
                        if (splits.Count > 0)
                        {
                            var last = splits[^1];
                            splits[^1] = (last.LotNumber, last.WarehouseId, last.ReceiptItemId, last.TakeBase + remainingBase);
                        }
                        else
                        {
                            var lastLot = lots[^1];
                            splits.Add((lastLot.LotNumber, lastLot.WarehouseId, lastLot.ReceiptItemId, remainingBase));
                        }
                    }

                    foreach (var split in splits)
                    {
                        var portionQty = split.TakeBase * ratio;
                        var stockBefore = stockBeforeByWarehouse.GetValueOrDefault((prodId, split.WarehouseId));
                        var c = new IndicatorConsumption(
                            GuidGenerator.Create(), fakeIndicatorId, item.ProductId, item.EquipmentId,
                            item.UnitId, portionQty);
                        c.IsDeducted = true;
                        c.SubmitDate = now;
                        c.ConsumptionDate = now;
                        c.WarehouseId = split.WarehouseId;
                        c.ConsumableStockBefore = stockBefore;
                        c.LotNumber = split.LotNumber;
                        c.WarehouseImportReceiptItemId = split.ReceiptItemId;
                        c.BarcodeCode = barcodeMap.GetValueOrDefault(split.ReceiptItemId);
                        c.TechnicianName = ind.TechnicianName;
                        await _indicatorConsumptionRepository.InsertAsync(c, autoSave: false);
                    }
                }
                else
                {
                    var resolvedLot = lots.FirstOrDefault(l => l.Remaining > 0);
                    if (resolvedLot == default) resolvedLot = lots[^1];
                    var stockBefore = stockBeforeByWarehouse.GetValueOrDefault((prodId, resolvedLot.WarehouseId));
                    var c = new IndicatorConsumption(
                        GuidGenerator.Create(), fakeIndicatorId, item.ProductId, item.EquipmentId,
                        item.UnitId, consumeQty);
                    c.IsDeducted = true;
                    c.SubmitDate = now;
                    c.ConsumptionDate = now;
                    c.WarehouseId = resolvedLot.WarehouseId;
                    c.ConsumableStockBefore = stockBefore;
                    c.LotNumber = resolvedLot.LotNumber;
                    c.WarehouseImportReceiptItemId = resolvedLot.ReceiptItemId;
                    c.BarcodeCode = barcodeMap.GetValueOrDefault(resolvedLot.ReceiptItemId);
                    c.TechnicianName = ind.TechnicianName;
                    await _indicatorConsumptionRepository.InsertAsync(c, autoSave: false);
                }
            }
            else
            {
                var c = new IndicatorConsumption(
                    GuidGenerator.Create(), fakeIndicatorId, item.ProductId, item.EquipmentId,
                    item.UnitId, consumeQty);
                c.IsDeducted = true;
                c.SubmitDate = now;
                c.ConsumptionDate = now;
                c.TechnicianName = ind.TechnicianName;
                await _indicatorConsumptionRepository.InsertAsync(c, autoSave: false);
            }
        }

        var equipmentItems = consumptionByTmi.Values.SelectMany(x => x).Where(c => c.EquipmentId.HasValue).ToList();
        if (equipmentItems.Any())
        {
            var usageByEquipment = equipmentItems
                .GroupBy(c => c.EquipmentId!.Value)
                .ToDictionary(g => g.Key, g => (int)g.Sum(c => c.Quantity));

            var equipments = await _equipmentRepository.GetListAsync(e => usageByEquipment.Keys.Contains(e.Id));

            foreach (var equipment in equipments)
            {
                equipment.UsageCount += usageByEquipment[equipment.Id];
                await _equipmentRepository.UpdateAsync(equipment, autoSave: true);
            }
        }

        await CurrentUnitOfWork.SaveChangesAsync();

        var barcodeTargets = stockBeforeByWarehouse.Keys
            .Where(k => k.Item2.HasValue)
            .Select(k => (k.Item1, k.Item2!.Value));
        await _barcodeStatusService.RecalculateForProductsAsync(barcodeTargets);
    }
}
