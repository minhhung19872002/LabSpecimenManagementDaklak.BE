using System;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Products;
using LabSpecimenManagement.WarehouseImports;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.Data;

public class BackfillLotBaseQuantityDataSeedContributor : IDataSeedContributor, ITransientDependency
{
    private readonly IRepository<WarehouseImportReceiptItem, Guid> _itemRepository;
    private readonly IRepository<WarehouseImportReceipt, Guid> _receiptRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<ProductSku, Guid> _skuRepository;
    private readonly IRepository<ProductUnitConversion, Guid> _conversionRepository;

    public BackfillLotBaseQuantityDataSeedContributor(
        IRepository<WarehouseImportReceiptItem, Guid> itemRepository,
        IRepository<WarehouseImportReceipt, Guid> receiptRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<ProductSku, Guid> skuRepository,
        IRepository<ProductUnitConversion, Guid> conversionRepository)
    {
        _itemRepository = itemRepository;
        _receiptRepository = receiptRepository;
        _productRepository = productRepository;
        _skuRepository = skuRepository;
        _conversionRepository = conversionRepository;
    }

    public async Task SeedAsync(DataSeedContext context)
    {
        var itemQuery = await _itemRepository.GetQueryableAsync();
        var receiptQuery = await _receiptRepository.GetQueryableAsync();

        var itemsToBackfill = (from item in itemQuery
                               join receipt in receiptQuery on item.WarehouseImportReceiptId equals receipt.Id
                               where receipt.Status == WarehouseImportReceiptStatus.Completed
                                     && item.Status == WarehouseImportLineStatus.Checked
                                     && item.ReceivedBaseQuantity == 0
                                     && item.ActualReceivedQuantity > 0
                               select item).ToList();

        if (!itemsToBackfill.Any()) return;

        var productIds = itemsToBackfill.Select(i => i.ProductId).Distinct().ToList();
        var products = (await _productRepository.GetListAsync(p => productIds.Contains(p.Id)))
            .ToDictionary(p => p.Id);

        var skuIds = itemsToBackfill.Where(i => i.SkuId.HasValue).Select(i => i.SkuId!.Value).Distinct().ToList();
        var skus = skuIds.Any()
            ? (await _skuRepository.GetListAsync(s => skuIds.Contains(s.Id))).ToDictionary(s => s.Id)
            : new System.Collections.Generic.Dictionary<Guid, ProductSku>();

        var conversionQuery = await _conversionRepository.GetQueryableAsync();

        foreach (var item in itemsToBackfill)
        {
            if (!products.TryGetValue(item.ProductId, out var product))
                continue;

            decimal baseQty;
            if (item.SkuId.HasValue && skus.TryGetValue(item.SkuId.Value, out var sku))
            {
                baseQty = ResolveSkuQuantity(sku, item.UnitId, item.ActualReceivedQuantity);
            }
            else
            {
                baseQty = ResolveBaseQuantity(product, item.UnitId, item.ActualReceivedQuantity, conversionQuery);
            }

            item.ReceivedBaseQuantity = baseQty;
            await _itemRepository.UpdateAsync(item, autoSave: true);
        }
    }

    private static decimal ResolveSkuQuantity(ProductSku sku, Guid? selectedUnitId, decimal quantity)
    {
        if (selectedUnitId == sku.PackagingUnitId || sku.PackagingUnitId == null)
            return quantity;

        if (selectedUnitId == sku.BaseUnitId)
        {
            if (sku.BaseUnitId == sku.PackagingUnitId)
                return quantity;
            return quantity * (sku.PackagingQuantity ?? 1);
        }

        if (selectedUnitId == sku.VolumeUnitId && sku.VolumePerUnit > 0)
        {
            var ratio = quantity / sku.VolumePerUnit.Value;
            return Math.Round(Math.Ceiling(ratio * 100m) / 100m, 2);
        }

        return quantity;
    }

    private static decimal ResolveBaseQuantity(
        Product product, Guid? unitId, decimal quantity,
        IQueryable<ProductUnitConversion> conversionQuery)
    {
        if (product.BaseUnitId == null || unitId == product.BaseUnitId)
            return quantity;

        if (unitId == product.PackagingUnitId && product.PackagingQuantity > 0)
            return quantity / product.PackagingQuantity.Value;

        var conversion = conversionQuery.FirstOrDefault(c =>
            c.ProductId == product.Id && c.ConvertedUnitId == unitId);

        if (conversion == null || conversion.ConvertedQuantity == 0)
            return quantity;

        decimal ratio = conversion.Quantity / conversion.ConvertedQuantity;
        return quantity * ratio;
    }
}
