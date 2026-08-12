using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Products;
using LabSpecimenManagement.Testing;
using LabSpecimenManagement.WarehouseImports;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.Common;

public interface IConsumableStockCalculator
{
    Task<Dictionary<Guid, decimal>> ComputeAsync(
        Dictionary<Guid, Guid?> productWarehouseMap);

    Task FreezeAsync(IEnumerable<Guid> productIds);
}

public class ConsumableStockCalculator : IConsumableStockCalculator, ITransientDependency
{
    private readonly IRepository<WarehouseImportReceiptItem, Guid> _receiptItemRepository;
    private readonly IRepository<WarehouseImportReceipt, Guid> _receiptRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<ProductSku, Guid> _skuRepository;
    private readonly IRepository<IndicatorConsumption, Guid> _indicatorConsumptionRepository;
    private readonly IRepository<ProductUnitConversion, Guid> _conversionRepository;

    public ConsumableStockCalculator(
        IRepository<WarehouseImportReceiptItem, Guid> receiptItemRepository,
        IRepository<WarehouseImportReceipt, Guid> receiptRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<ProductSku, Guid> skuRepository,
        IRepository<IndicatorConsumption, Guid> indicatorConsumptionRepository,
        IRepository<ProductUnitConversion, Guid> conversionRepository)
    {
        _receiptItemRepository = receiptItemRepository;
        _receiptRepository = receiptRepository;
        _productRepository = productRepository;
        _skuRepository = skuRepository;
        _indicatorConsumptionRepository = indicatorConsumptionRepository;
        _conversionRepository = conversionRepository;
    }

    public async Task<Dictionary<Guid, decimal>> ComputeAsync(
        Dictionary<Guid, Guid?> productWarehouseMap)
    {
        var pidList = productWarehouseMap.Keys.ToList();
        if (pidList.Count == 0) return new Dictionary<Guid, decimal>();

        var itemQ = await _receiptItemRepository.GetQueryableAsync();
        var receiptQ = await _receiptRepository.GetQueryableAsync();

        var lotItems = (from item in itemQ
                        join receipt in receiptQ on item.WarehouseImportReceiptId equals receipt.Id
                        where pidList.Contains(item.ProductId)
                              && receipt.Status == WarehouseImportReceiptStatus.Completed
                              && item.Status == WarehouseImportLineStatus.Checked
                        select new
                        {
                            item.ProductId,
                            item.ReceivedBaseQuantity,
                            item.ConsumedBaseQuantity,
                            item.SkuId,
                            item.StorageWarehouseId,
                            receipt.WarehouseId
                        }).ToList();

        var products = await _productRepository.GetListAsync(p => pidList.Contains(p.Id));
        var productMap = products.ToDictionary(p => p.Id);

        var skus = (await _skuRepository.GetQueryableAsync())
            .Where(s => pidList.Contains(s.ProductId)).ToList();
        var skuById = skus.ToDictionary(s => s.Id);

        var result = new Dictionary<Guid, decimal>();

        foreach (var group in lotItems.GroupBy(l => l.ProductId))
        {
            var productId = group.Key;
            if (!productMap.TryGetValue(productId, out var product)) continue;

            var whId = productWarehouseMap.GetValueOrDefault(productId);

            decimal consumable = 0;
            foreach (var lot in group)
            {
                if (whId.HasValue)
                {
                    var lotWh = lot.StorageWarehouseId
                                ?? (lot.WarehouseId.HasValue ? lot.WarehouseId : null);
                    if (!lotWh.HasValue || lotWh.Value != whId.Value) continue;
                }

                var remaining = lot.ReceivedBaseQuantity - lot.ConsumedBaseQuantity;
                if (remaining <= 0) continue;

                int? vpu = null;
                if (lot.SkuId.HasValue && skuById.TryGetValue(lot.SkuId.Value, out var sku))
                {
                    if (sku.VolumePerUnit > 0) vpu = sku.VolumePerUnit;
                }
                if (!vpu.HasValue && product.VolumePerUnit > 0)
                    vpu = product.VolumePerUnit;

                if (vpu.HasValue && vpu.Value > 0)
                    consumable += remaining * vpu.Value;
            }

            result[productId] = consumable;
        }

        foreach (var pid in pidList)
        {
            if (!result.ContainsKey(pid))
                result[pid] = 0;
        }

        return result;
    }

    public async Task FreezeAsync(IEnumerable<Guid> productIds)
    {
        var pidList = productIds.Distinct().ToList();
        if (pidList.Count == 0) return;

        var icQuery = await _indicatorConsumptionRepository.GetQueryableAsync();
        var unfrozen = icQuery
            .Where(c => c.ProductId.HasValue
                        && pidList.Contains(c.ProductId.Value)
                        && c.IsDeducted
                        && c.ConsumableStockBefore == null)
            .ToList();

        if (unfrozen.Count == 0) return;

        var affectedProductIds = unfrozen.Select(c => c.ProductId!.Value).Distinct().ToList();

        var products = await _productRepository.GetListAsync(p => affectedProductIds.Contains(p.Id));
        var productMap = products.ToDictionary(p => p.Id);

        var skus = (await _skuRepository.GetQueryableAsync())
            .Where(s => affectedProductIds.Contains(s.ProductId)).ToList();
        var skusByProduct = skus.GroupBy(s => s.ProductId).ToDictionary(g => g.Key, g => g.ToList());

        var conversions = await _conversionRepository.GetListAsync(c => affectedProductIds.Contains(c.ProductId));
        var conversionMap = conversions.ToDictionary(c => (c.ProductId, c.ConvertedUnitId));

        var warehouseGroups = unfrozen
            .GroupBy(c => (c.ProductId!.Value, c.WarehouseId))
            .Select(g => new { ProductId = g.Key.Value, WarehouseId = g.Key.WarehouseId })
            .ToList();

        var stockCache = new Dictionary<(Guid, Guid?), decimal>();
        foreach (var wg in warehouseGroups)
        {
            var key = (wg.ProductId, wg.WarehouseId);
            if (!stockCache.ContainsKey(key))
            {
                var map = new Dictionary<Guid, Guid?> { { wg.ProductId, wg.WarehouseId } };
                var result = await ComputeAsync(map);
                stockCache[key] = result.GetValueOrDefault(wg.ProductId);
            }
        }

        foreach (var wg in warehouseGroups)
        {
            var productId = wg.ProductId;
            if (!productMap.TryGetValue(productId, out var product)) continue;

            var whId = wg.WarehouseId;
            var currentConsumable = stockCache.GetValueOrDefault((productId, whId));

            var productSkus = skusByProduct.GetValueOrDefault(productId) ?? new List<ProductSku>();
            int defaultVpu = productSkus.FirstOrDefault(s => s.VolumePerUnit > 0)?.VolumePerUnit
                             ?? product.VolumePerUnit ?? 0;

            var allDeducted = icQuery
                .Where(c => c.ProductId == productId && c.IsDeducted
                            && c.WarehouseId == whId)
                .OrderByDescending(c => c.SubmitDate)
                .ThenByDescending(c => c.Id)
                .ToList();

            var consumeVolumes = new decimal[allDeducted.Count];
            for (int i = 0; i < allDeducted.Count; i++)
            {
                if (allDeducted[i].ActualQuantity > 0)
                {
                    consumeVolumes[i] = ConvertConsumptionToVolume(
                        allDeducted[i].ActualQuantity,
                        allDeducted[i].UnitId,
                        product, productSkus, conversionMap, defaultVpu);
                }
            }

            decimal totalConsumed = 0;
            for (int j = 0; j < consumeVolumes.Length; j++)
                totalConsumed += consumeVolumes[j];

            decimal running = currentConsumable + totalConsumed;
            for (int i = allDeducted.Count - 1; i >= 0; i--)
            {
                if (allDeducted[i].ConsumableStockBefore == null)
                {
                    allDeducted[i].ConsumableStockBefore = running;
                    await _indicatorConsumptionRepository.UpdateAsync(allDeducted[i], autoSave: false);
                }
                running -= consumeVolumes[i];
            }
        }
    }

    private static decimal ConvertConsumptionToVolume(
        decimal amount,
        Guid? consumeUnitId,
        Product product,
        List<ProductSku> skus,
        Dictionary<(Guid ProductId, Guid ConvertedUnitId), ProductUnitConversion> conversionMap,
        int defaultVpu)
    {
        if (!consumeUnitId.HasValue || defaultVpu <= 0)
            return amount;

        decimal? unitsPerStorage = null;

        if (consumeUnitId == product.PackagingUnitId)
            unitsPerStorage = 1;
        else if (consumeUnitId == product.VolumeUnitId && product.VolumePerUnit > 0)
            unitsPerStorage = product.VolumePerUnit.Value;
        else if (consumeUnitId == product.BaseUnitId && product.PackagingQuantity > 0)
            unitsPerStorage = 1m / product.PackagingQuantity.Value;

        if (unitsPerStorage == null)
        {
            foreach (var sku in skus)
            {
                if (consumeUnitId == sku.PackagingUnitId) { unitsPerStorage = 1; break; }
                if (consumeUnitId == sku.VolumeUnitId && sku.VolumePerUnit > 0) { unitsPerStorage = sku.VolumePerUnit.Value; break; }
                if (consumeUnitId == sku.BaseUnitId && sku.PackagingQuantity > 0) { unitsPerStorage = 1m / sku.PackagingQuantity.Value; break; }
            }
        }

        if (unitsPerStorage == null
            && conversionMap.TryGetValue((product.Id, consumeUnitId.Value), out var conv)
            && conv.ConvertedQuantity > 0)
        {
            unitsPerStorage = conv.ConvertedQuantity / conv.Quantity;
        }

        if (unitsPerStorage == null || unitsPerStorage.Value == 0)
            return amount;

        return (amount / unitsPerStorage.Value) * defaultVpu;
    }
}
