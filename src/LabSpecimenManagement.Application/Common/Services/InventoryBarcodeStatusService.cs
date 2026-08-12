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
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<ProductSku, Guid> _productSkuRepository;
    private readonly IAsyncQueryableExecuter AsyncExecuter;

    public InventoryBarcodeStatusService(
        IRepository<InventoryBarcode, Guid> barcodeRepository,
        IRepository<WarehouseImportReceiptItem, Guid> importItemRepository,
        IRepository<WarehouseImportReceipt, Guid> importReceiptRepository,
        IRepository<IndicatorConsumption, Guid> indicatorConsumptionRepository,
        IRepository<TestBatchConsumption, Guid> testBatchConsumptionRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<ProductSku, Guid> productSkuRepository,
        IAsyncQueryableExecuter asyncQueryableExecuter)
    {
        _barcodeRepository = barcodeRepository;
        _importItemRepository = importItemRepository;
        _importReceiptRepository = importReceiptRepository;
        _indicatorConsumptionRepository = indicatorConsumptionRepository;
        _testBatchConsumptionRepository = testBatchConsumptionRepository;
        _productRepository = productRepository;
        _productSkuRepository = productSkuRepository;
        AsyncExecuter = asyncQueryableExecuter;
    }

    public async Task RecalculateAsync(Guid productId, Guid warehouseId)
    {
        var totalBaseConsumed = await GetTotalBaseConsumedAsync(productId, warehouseId);
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
                receipt.ActualImportDate,
            });

        if (warehouseItems.Count == 0) return;

        var barcodeQuery = await _barcodeRepository.GetQueryableAsync();
        var barcodes = await AsyncExecuter.ToListAsync(
            barcodeQuery.Where(b => b.ProductId == productId && b.CurrentWarehouseId == warehouseId)
                .OrderBy(b => b.CreationTime));

        if (barcodes.Count == 0) return;

        var barcodeByCode = barcodes.ToDictionary(b => b.Code);

        var mergedGroups = warehouseItems
            .GroupBy(x => (x.LotNumber, LotExpiryDate: x.LotExpiryDate?.Date))
            .OrderBy(g => g.Key.LotExpiryDate == null ? 1 : 0)
            .ThenBy(g => g.Key.LotExpiryDate)
            .ToList();

        var remainingConsumed = totalBaseConsumed;
        var updatedBarcodes = new List<InventoryBarcode>();

        foreach (var g in mergedGroups)
        {
            var orderedItems = g.OrderBy(x => x.ActualImportDate).ToList();
            var fullBarcodeList = orderedItems
                .SelectMany(x => DeserializeBarcodes(x.BarcodeValues))
                .OrderBy(c => c)
                .ToList();

            var received = orderedItems.Sum(x => x.ReceivedBaseQuantity);
            var consumed = Math.Min(remainingConsumed, received);
            remainingConsumed -= consumed;

            var isExpired = g.Key.LotExpiryDate.HasValue && g.Key.LotExpiryDate.Value < today;
            var consumedFloor = (int)Math.Floor(consumed);

            for (var i = 0; i < fullBarcodeList.Count; i++)
            {
                var code = fullBarcodeList[i];
                if (!barcodeByCode.TryGetValue(code, out var barcode)) continue;

                InventoryBarcodeStatus newStatus;
                if (i < consumedFloor)
                {
                    newStatus = InventoryBarcodeStatus.UsedUp;
                }
                else if (i == consumedFloor && consumed > consumedFloor)
                {
                    newStatus = isExpired ? InventoryBarcodeStatus.Expired : InventoryBarcodeStatus.InUse;
                }
                else
                {
                    newStatus = isExpired ? InventoryBarcodeStatus.Expired : InventoryBarcodeStatus.NotUsed;
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

    private async Task<decimal> GetTotalBaseConsumedAsync(Guid productId, Guid warehouseId)
    {
        var indicatorQuery = await _indicatorConsumptionRepository.GetQueryableAsync();
        var batchQuery = await _testBatchConsumptionRepository.GetQueryableAsync();

        var indicatorAmounts = await AsyncExecuter.ToListAsync(
            indicatorQuery
                .Where(c => c.ProductId == productId && c.WarehouseId == warehouseId && c.IsDeducted)
                .Select(c => c.ActualQuantity));

        var batchAmounts = await AsyncExecuter.ToListAsync(
            batchQuery
                .Where(c => c.ProductId == productId && c.WarehouseId == warehouseId && c.IsDeducted)
                .Select(c => c.ActualQuantity));

        var totalConsumable = indicatorAmounts.Sum() + batchAmounts.Sum();
        if (totalConsumable == 0) return 0;

        var product = await _productRepository.GetAsync(productId);
        var volumePerUnit = product.VolumePerUnit ?? 0;

        if (volumePerUnit <= 0)
        {
            var skuQuery = await _productSkuRepository.GetQueryableAsync();
            volumePerUnit = await AsyncExecuter.FirstOrDefaultAsync(
                skuQuery
                    .Where(s => s.ProductId == productId && s.VolumePerUnit != null && s.VolumePerUnit > 0)
                    .Select(s => s.VolumePerUnit!.Value));
        }

        return volumePerUnit > 0
            ? totalConsumable / volumePerUnit
            : totalConsumable;
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
}
