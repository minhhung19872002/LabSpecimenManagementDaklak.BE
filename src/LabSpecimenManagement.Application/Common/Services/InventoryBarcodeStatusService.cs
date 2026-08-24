using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LabSpecimenManagement.Products;
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
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<ProductSku, Guid> _productSkuRepository;
    private readonly IAsyncQueryableExecuter AsyncExecuter;

    public InventoryBarcodeStatusService(
        IRepository<InventoryBarcode, Guid> barcodeRepository,
        IRepository<WarehouseImportReceiptItem, Guid> importItemRepository,
        IRepository<WarehouseImportReceipt, Guid> importReceiptRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<ProductSku, Guid> productSkuRepository,
        IAsyncQueryableExecuter asyncQueryableExecuter)
    {
        _barcodeRepository = barcodeRepository;
        _importItemRepository = importItemRepository;
        _importReceiptRepository = importReceiptRepository;
        _productRepository = productRepository;
        _productSkuRepository = productSkuRepository;
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
                item.ConsumedBaseQuantity,
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

        var updatedBarcodes = new List<InventoryBarcode>();

        foreach (var item in warehouseItems)
        {
            var itemBarcodes = DeserializeBarcodes(item.BarcodeValues);
            if (itemBarcodes.Count == 0) continue;

            var capacityPerBarcode = useVolumeBased
                ? GetVolumePerBarcode(item.SkuId, skuById, product)
                : 1m;

            var isExpired = item.LotExpiryDate.HasValue && item.LotExpiryDate.Value.Date < today;

            var consumedInVolume = useVolumeBased
                ? item.ConsumedBaseQuantity * capacityPerBarcode
                : item.ConsumedBaseQuantity;
            var lotRemaining = consumedInVolume;

            foreach (var code in itemBarcodes)
            {
                if (!barcodeByCode.TryGetValue(code, out var barcode)) continue;

                InventoryBarcodeStatus newStatus;
                if (capacityPerBarcode > 0 && lotRemaining >= capacityPerBarcode)
                {
                    newStatus = InventoryBarcodeStatus.UsedUp;
                    lotRemaining -= capacityPerBarcode;
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
