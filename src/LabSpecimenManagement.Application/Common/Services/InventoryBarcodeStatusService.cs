using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LabSpecimenManagement.Products;
using LabSpecimenManagement.Testing;
using LabSpecimenManagement.WarehouseImports;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
namespace LabSpecimenManagement.Common;

public class InventoryBarcodeStatusService : ITransientDependency
{
    private readonly IRepository<InventoryBarcode, Guid> _barcodeRepository;
    private readonly IRepository<WarehouseImportReceiptItem, Guid> _importItemRepository;
    private readonly IRepository<WarehouseImportReceipt, Guid> _importReceiptRepository;
    private readonly IRepository<IndicatorConsumption, Guid> _indicatorConsumptionRepository;
    private readonly IRepository<TestBatchConsumption, Guid> _testBatchConsumptionRepository;
    private readonly IRepository<TestBatch, Guid> _testBatchRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<ProductSku, Guid> _productSkuRepository;
    private readonly IRepository<ProductUnitConversion, Guid> _conversionRepository;
    private readonly IAsyncQueryableExecuter AsyncExecuter;

    public InventoryBarcodeStatusService(
        IRepository<InventoryBarcode, Guid> barcodeRepository,
        IRepository<WarehouseImportReceiptItem, Guid> importItemRepository,
        IRepository<WarehouseImportReceipt, Guid> importReceiptRepository,
        IRepository<IndicatorConsumption, Guid> indicatorConsumptionRepository,
        IRepository<TestBatchConsumption, Guid> testBatchConsumptionRepository,
        IRepository<TestBatch, Guid> testBatchRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<ProductSku, Guid> productSkuRepository,
        IRepository<ProductUnitConversion, Guid> conversionRepository,
        IAsyncQueryableExecuter asyncQueryableExecuter)
    {
        _barcodeRepository = barcodeRepository;
        _importItemRepository = importItemRepository;
        _importReceiptRepository = importReceiptRepository;
        _indicatorConsumptionRepository = indicatorConsumptionRepository;
        _testBatchConsumptionRepository = testBatchConsumptionRepository;
        _testBatchRepository = testBatchRepository;
        _productRepository = productRepository;
        _productSkuRepository = productSkuRepository;
        _conversionRepository = conversionRepository;
        AsyncExecuter = asyncQueryableExecuter;
    }

    public async Task RecalculateAsync(Guid productId, Guid warehouseId)
    {
        var today = DateTime.UtcNow.AddHours(7).Date;

        var importItemQuery = await _importItemRepository.GetQueryableAsync();
        var importReceiptQuery = await _importReceiptRepository.GetQueryableAsync();

        var warehouseItems = await AsyncExecuter.ToListAsync(
            from item in importItemQuery
            join receipt in importReceiptQuery on item.WarehouseImportReceiptId equals receipt.Id
            where item.ProductId == productId
                && item.BarcodeValues != null
                && (item.StorageWarehouseId == warehouseId
                    || (item.StorageWarehouseId == null && receipt.WarehouseId == warehouseId))
            select new
            {
                item.Id,
                item.LotNumber,
                item.LotExpiryDate,
                item.ReceivedBaseQuantity,
                item.BarcodeValues,
                item.SkuId,
                receipt.ActualImportDate,
            });

        if (warehouseItems.Count == 0) return;

        var barcodeQuery = await _barcodeRepository.GetQueryableAsync();
        var barcodes = await AsyncExecuter.ToListAsync(
            barcodeQuery.Where(b => b.ProductId == productId && b.CurrentWarehouseId == warehouseId)
                .OrderBy(b => b.CreationTime));

        if (barcodes.Count == 0) return;

        var barcodeByCode = barcodes.ToDictionary(b => b.Code);

        var product = await _productRepository.GetAsync(productId);

        var skuQuery = await _productSkuRepository.GetQueryableAsync();
        var allSkus = await AsyncExecuter.ToListAsync(skuQuery.Where(s => s.ProductId == productId));
        var skuById = allSkus.ToDictionary(s => s.Id);

        var useVolumeBased = product.VolumePerUnit > 0 || allSkus.Any(s => s.VolumePerUnit > 0);

        var lotInfos = new List<LotAllocationInfo>();
        foreach (var item in warehouseItems)
        {
            var itemBarcodes = DeserializeBarcodes(item.BarcodeValues);
            if (itemBarcodes.Count == 0) continue;
            var capacityPerBarcode = useVolumeBased
                ? GetVolumePerBarcode(item.SkuId, skuById, product)
                : 1m;
            lotInfos.Add(new LotAllocationInfo
            {
                ItemId = item.Id,
                LotExpiryDate = item.LotExpiryDate?.Date,
                ImportDate = item.ActualImportDate,
                TotalCapacity = itemBarcodes.Count * capacityPerBarcode,
                CapacityPerBarcode = capacityPerBarcode,
                Barcodes = itemBarcodes,
            });
        }

        if (lotInfos.Count == 0) return;

        var consumed = await AllocateConsumptionToLotsAsync(
            productId, warehouseId, product, allSkus, useVolumeBased, lotInfos);

        var updatedBarcodes = new List<InventoryBarcode>();

        foreach (var lot in lotInfos)
        {
            var isExpired = lot.LotExpiryDate.HasValue && lot.LotExpiryDate.Value < today;
            var lotRemaining = consumed.GetValueOrDefault(lot.ItemId);

            foreach (var code in lot.Barcodes)
            {
                if (!barcodeByCode.TryGetValue(code, out var barcode)) continue;

                InventoryBarcodeStatus newStatus;
                if (lot.CapacityPerBarcode > 0 && lotRemaining >= lot.CapacityPerBarcode)
                {
                    newStatus = InventoryBarcodeStatus.UsedUp;
                    lotRemaining -= lot.CapacityPerBarcode;
                }
                else if (lotRemaining > 0)
                {
                    newStatus = isExpired
                        ? InventoryBarcodeStatus.Expired
                        : InventoryBarcodeStatus.InUse;
                    lotRemaining = 0;
                }
                else
                {
                    newStatus = isExpired
                        ? InventoryBarcodeStatus.Expired
                        : InventoryBarcodeStatus.NotUsed;
                }

                if (barcode.Status != newStatus)
                {
                    barcode.Status = newStatus;
                    updatedBarcodes.Add(barcode);
                }
            }
        }

        if (updatedBarcodes.Count > 0)
        {
            await _barcodeRepository.UpdateManyAsync(updatedBarcodes, autoSave: true);
        }
    }

    public async Task RecalculateForProductsAsync(
        IEnumerable<(Guid ProductId, Guid WarehouseId)> targets)
    {
        var distinct = targets
            .Where(t => t.WarehouseId != Guid.Empty)
            .Distinct()
            .ToList();

        foreach (var (productId, warehouseId) in distinct)
        {
            await RecalculateAsync(productId, warehouseId);
        }
    }

    // --- Chronological FEFO allocation ---

    private async Task<Dictionary<Guid, decimal>> AllocateConsumptionToLotsAsync(
        Guid productId, Guid warehouseId,
        Product product, List<ProductSku> allSkus,
        bool useVolumeBased,
        List<LotAllocationInfo> lots)
    {
        var consumed = lots.ToDictionary(l => l.ItemId, _ => 0m);

        var records = await GetConsumptionRecordsAsync(productId, warehouseId);
        if (records.Count == 0) return consumed;

        var conversionMap = await GetConversionMapAsync(productId);

        records.Sort((a, b) => Nullable.Compare(a.SubmitDate, b.SubmitDate));

        foreach (var record in records)
        {
            decimal qty = useVolumeBased
                ? ConvertToVolume(record.ActualQuantity, record.UnitId, product, allSkus, conversionMap)
                : ConvertToBaseUnits(record.ActualQuantity, record.UnitId, product, allSkus, conversionMap);

            if (qty <= 0) continue;

            var candidateLots = lots
                .Where(l => !record.SubmitDate.HasValue || l.ImportDate <= record.SubmitDate.Value)
                .OrderBy(l => l.LotExpiryDate == null ? 1 : 0)
                .ThenBy(l => l.LotExpiryDate)
                .ThenBy(l => l.ImportDate);

            var remaining = qty;
            foreach (var lot in candidateLots)
            {
                if (remaining <= 0) break;
                var available = lot.TotalCapacity - consumed[lot.ItemId];
                if (available <= 0) continue;
                var deduct = Math.Min(available, remaining);
                consumed[lot.ItemId] += deduct;
                remaining -= deduct;
            }

            if (remaining > 0)
            {
                foreach (var lot in lots
                    .OrderBy(l => l.LotExpiryDate == null ? 1 : 0)
                    .ThenBy(l => l.LotExpiryDate)
                    .ThenBy(l => l.ImportDate))
                {
                    if (remaining <= 0) break;
                    var available = lot.TotalCapacity - consumed[lot.ItemId];
                    if (available <= 0) continue;
                    var deduct = Math.Min(available, remaining);
                    consumed[lot.ItemId] += deduct;
                    remaining -= deduct;
                }
            }
        }

        return consumed;
    }

    // --- Volume-based conversion helpers ---

    private static decimal ConvertToVolume(
        decimal quantity,
        Guid? unitId,
        Product product,
        List<ProductSku> skus,
        Dictionary<Guid, ProductUnitConversion> conversionMap)
    {
        if (quantity == 0) return 0;
        if (!unitId.HasValue) return quantity;
        var uid = unitId.Value;

        if (uid == product.VolumeUnitId)
            return quantity;

        if (uid == product.PackagingUnitId && product.VolumePerUnit > 0)
            return quantity * product.VolumePerUnit.Value;

        if (uid == product.BaseUnitId && product.PackagingQuantity > 0 && product.VolumePerUnit > 0)
            return quantity * product.PackagingQuantity.Value * product.VolumePerUnit.Value;

        foreach (var sku in skus)
        {
            if (uid == sku.VolumeUnitId) return quantity;
            if (uid == sku.PackagingUnitId && sku.VolumePerUnit > 0)
                return quantity * sku.VolumePerUnit.Value;
            if (uid == sku.BaseUnitId && sku.PackagingQuantity > 0 && sku.VolumePerUnit > 0)
                return quantity * sku.PackagingQuantity.Value * sku.VolumePerUnit.Value;
        }

        if (conversionMap.TryGetValue(uid, out var conv) && conv.ConvertedQuantity > 0)
        {
            var ratio = conv.Quantity / conv.ConvertedQuantity;
            var vpu = product.VolumePerUnit
                ?? skus.FirstOrDefault(s => s.VolumePerUnit > 0)?.VolumePerUnit
                ?? 0;
            return vpu > 0 ? quantity * ratio * vpu : quantity * ratio;
        }

        return quantity;
    }

    private static decimal GetVolumePerBarcode(
        Guid? skuId,
        Dictionary<Guid, ProductSku> skuById,
        Product product)
    {
        if (skuId.HasValue && skuById.TryGetValue(skuId.Value, out var sku) && sku.VolumePerUnit > 0)
            return sku.VolumePerUnit.Value;

        if (product.VolumePerUnit > 0)
            return product.VolumePerUnit.Value;

        return 1;
    }

    // --- Base-unit conversion helper ---

    private static decimal ConvertToBaseUnits(
        decimal quantity,
        Guid? unitId,
        Product product,
        List<ProductSku> skus,
        Dictionary<Guid, ProductUnitConversion> conversionMap)
    {
        if (quantity == 0) return 0;
        if (!unitId.HasValue)
        {
            var fallbackVpu = product.VolumePerUnit
                ?? skus.FirstOrDefault(s => s.VolumePerUnit > 0)?.VolumePerUnit
                ?? 0;
            return fallbackVpu > 0 ? quantity / fallbackVpu : quantity;
        }
        var uid = unitId.Value;

        if (uid == product.PackagingUnitId)
            return quantity;

        if (uid == product.VolumeUnitId && product.VolumePerUnit > 0)
            return quantity / product.VolumePerUnit.Value;

        if (uid == product.BaseUnitId && product.PackagingQuantity > 0)
            return quantity * product.PackagingQuantity.Value;

        foreach (var sku in skus)
        {
            if (uid == sku.PackagingUnitId) return quantity;
            if (uid == sku.VolumeUnitId && sku.VolumePerUnit > 0)
                return quantity / sku.VolumePerUnit.Value;
            if (uid == sku.BaseUnitId && sku.PackagingQuantity > 0)
                return quantity * sku.PackagingQuantity.Value;
        }

        if (conversionMap.TryGetValue(uid, out var conv) && conv.ConvertedQuantity > 0)
            return quantity / (conv.ConvertedQuantity / conv.Quantity);

        var volumePerUnit = product.VolumePerUnit
            ?? skus.FirstOrDefault(s => s.VolumePerUnit > 0)?.VolumePerUnit
            ?? 0;
        return volumePerUnit > 0 ? quantity / volumePerUnit : quantity;
    }

    // --- Shared helpers ---

    private async Task<List<ConsumptionRecord>> GetConsumptionRecordsAsync(Guid productId, Guid warehouseId)
    {
        var indicatorQuery = await _indicatorConsumptionRepository.GetQueryableAsync();
        var batchQuery = await _testBatchConsumptionRepository.GetQueryableAsync();

        var indicatorRecords = await AsyncExecuter.ToListAsync(
            indicatorQuery
                .Where(c => c.ProductId == productId && c.WarehouseId == warehouseId && c.IsDeducted)
                .Select(c => new ConsumptionRecord { ActualQuantity = c.ActualQuantity, UnitId = c.UnitId, SubmitDate = c.SubmitDate }));

        var testBatchQuery = await _testBatchRepository.GetQueryableAsync();
        var batchRecords = await AsyncExecuter.ToListAsync(
            from c in batchQuery
            join batch in testBatchQuery on c.TestBatchId equals batch.Id
            where c.ProductId == productId && c.WarehouseId == warehouseId && c.IsDeducted
            select new ConsumptionRecord
            {
                ActualQuantity = c.ActualQuantity,
                UnitId = c.UnitId,
                SubmitDate = (DateTime?)(batch.ConsumptionConfirmedDate ?? batch.CompletedDate ?? batch.CreationTime),
            });

        indicatorRecords.AddRange(batchRecords);
        return indicatorRecords;
    }

    private async Task<Dictionary<Guid, ProductUnitConversion>> GetConversionMapAsync(Guid productId)
    {
        var conversionQuery = await _conversionRepository.GetQueryableAsync();
        var conversions = await AsyncExecuter.ToListAsync(
            conversionQuery.Where(c => c.ProductId == productId));
        return conversions
            .GroupBy(c => c.ConvertedUnitId)
            .ToDictionary(g => g.Key, g => g.First());
    }

    private static List<string> DeserializeBarcodes(string? barcodeValues)
    {
        if (string.IsNullOrWhiteSpace(barcodeValues)) return new List<string>();
        try
        {
            return JsonSerializer.Deserialize<List<string>>(barcodeValues) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private class LotAllocationInfo
    {
        public Guid ItemId { get; init; }
        public DateTime? LotExpiryDate { get; init; }
        public DateTime ImportDate { get; init; }
        public decimal TotalCapacity { get; init; }
        public decimal CapacityPerBarcode { get; init; }
        public List<string> Barcodes { get; init; } = new();
    }

    private class ConsumptionRecord
    {
        public decimal ActualQuantity { get; init; }
        public Guid? UnitId { get; init; }
        public DateTime? SubmitDate { get; init; }
    }
}
