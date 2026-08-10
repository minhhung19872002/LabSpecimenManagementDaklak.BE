using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.GoodsReceipts;
using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.StockAdjustments;
using LabSpecimenManagement.Suppliers;
using LabSpecimenManagement.Testing;
using LabSpecimenManagement.TestRequests;
using LabSpecimenManagement.WarehouseImports;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace LabSpecimenManagement.Products;

[RemoteService(IsEnabled = false)]
[Authorize(LabSpecimenManagementPermissions.Products.Default)]
public class ProductAppService : ApplicationService, IProductAppService
{
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<ProductAttachment, Guid> _attachmentRepository;
    private readonly IRepository<ProductUnitConversion, Guid> _conversionRepository;
    private readonly IRepository<ProductSku, Guid> _skuRepository;
    private readonly IRepository<Unit, Guid> _unitRepository;
    private readonly IRepository<GoodsReceiptItem, Guid> _goodsReceiptItemRepository;
    private readonly IRepository<GoodsReceipt, Guid> _goodsReceiptRepository;
    private readonly IRepository<Supplier, Guid> _supplierRepository;
    private readonly IRepository<Manufacturer, Guid> _manufacturerRepository;
    private readonly IRepository<WarehouseImportReceiptItem, Guid> _warehouseImportReceiptItemRepository;
    private readonly IRepository<WarehouseImportReceipt, Guid> _warehouseImportReceiptRepository;
    private readonly IRepository<Warehouse, Guid> _warehouseRepository;
    private readonly IRepository<StockMovementLog, Guid> _stockMovementLogRepository;
    private readonly IRepository<TestBatchConsumption, Guid> _testBatchConsumptionRepository;
    private readonly IRepository<IndicatorConsumption, Guid> _indicatorConsumptionRepository;
    private readonly IRepository<TestBatch, Guid> _testBatchRepository;
    private readonly IRepository<TestBatchItem, Guid> _testBatchItemRepository;
    private readonly IRepository<LaboratoryTaskIndicator, Guid> _ltiRepository;
    private readonly IRepository<TestRequestSampleIndicator, Guid> _trsiRepository;
    private readonly IRepository<TestRequestSample, Guid> _sampleRepository;
    private readonly IRepository<Indicator, Guid> _indicatorRepository;
    private readonly IRepository<TestMethod, Guid> _testMethodRepository;
    private readonly IRepository<SpecimenType, Guid> _specimenTypeRepository;
    private readonly IRepository<ProductWarehouseInventory, Guid> _productWarehouseInventoryRepository;
    private readonly IUserNameProvider _userNameProvider;
    private readonly ICodeGeneratorService _codeGenerator;
    private readonly IDataFilter _dataFilter;
    private readonly IUnitConversionResolver _unitConversionResolver;

    public ProductAppService(
        IRepository<Product, Guid> productRepository,
        IRepository<ProductAttachment, Guid> attachmentRepository,
        IRepository<ProductUnitConversion, Guid> conversionRepository,
        IRepository<ProductSku, Guid> skuRepository,
        IRepository<Unit, Guid> unitRepository,
        IRepository<GoodsReceiptItem, Guid> goodsReceiptItemRepository,
        IRepository<GoodsReceipt, Guid> goodsReceiptRepository,
        IRepository<Supplier, Guid> supplierRepository,
        IRepository<Manufacturer, Guid> manufacturerRepository,
        IRepository<WarehouseImportReceiptItem, Guid> warehouseImportReceiptItemRepository,
        IRepository<WarehouseImportReceipt, Guid> warehouseImportReceiptRepository,
        IRepository<Warehouse, Guid> warehouseRepository,
        IRepository<StockMovementLog, Guid> stockMovementLogRepository,
        IRepository<TestBatchConsumption, Guid> testBatchConsumptionRepository,
        IRepository<IndicatorConsumption, Guid> indicatorConsumptionRepository,
        IRepository<TestBatch, Guid> testBatchRepository,
        IRepository<TestBatchItem, Guid> testBatchItemRepository,
        IRepository<LaboratoryTaskIndicator, Guid> ltiRepository,
        IRepository<TestRequestSampleIndicator, Guid> trsiRepository,
        IRepository<TestRequestSample, Guid> sampleRepository,
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<TestMethod, Guid> testMethodRepository,
        IRepository<SpecimenType, Guid> specimenTypeRepository,
        IUserNameProvider userNameProvider,
        ICodeGeneratorService codeGenerator,
        IDataFilter dataFilter,
        IUnitConversionResolver unitConversionResolver,
        IRepository<ProductWarehouseInventory, Guid> productWarehouseInventoryRepository)
    {
        _productRepository = productRepository;
        _attachmentRepository = attachmentRepository;
        _conversionRepository = conversionRepository;
        _skuRepository = skuRepository;
        _unitRepository = unitRepository;
        _goodsReceiptItemRepository = goodsReceiptItemRepository;
        _goodsReceiptRepository = goodsReceiptRepository;
        _supplierRepository = supplierRepository;
        _manufacturerRepository = manufacturerRepository;
        _warehouseImportReceiptItemRepository = warehouseImportReceiptItemRepository;
        _warehouseImportReceiptRepository = warehouseImportReceiptRepository;
        _warehouseRepository = warehouseRepository;
        _stockMovementLogRepository = stockMovementLogRepository;
        _testBatchConsumptionRepository = testBatchConsumptionRepository;
        _indicatorConsumptionRepository = indicatorConsumptionRepository;
        _testBatchRepository = testBatchRepository;
        _testBatchItemRepository = testBatchItemRepository;
        _ltiRepository = ltiRepository;
        _trsiRepository = trsiRepository;
        _sampleRepository = sampleRepository;
        _indicatorRepository = indicatorRepository;
        _testMethodRepository = testMethodRepository;
        _specimenTypeRepository = specimenTypeRepository;
        _userNameProvider = userNameProvider;
        _codeGenerator = codeGenerator;
        _dataFilter = dataFilter;
        _unitConversionResolver = unitConversionResolver;
        _productWarehouseInventoryRepository = productWarehouseInventoryRepository;
    }

    public async Task<PagedResultDto<ProductDto>> GetListAsync(ProductFilterDto input)
    {
        if (input.WarehouseId.HasValue)
        {
            return await GetListByWarehouseAsync(input);
        }

        var query = await _productRepository.GetQueryableAsync();

        // Filter by ProductType
        if (input.ProductType.HasValue)
        {
            query = query.Where(x => x.ProductType == input.ProductType.Value);
        }

        // Search by Code or Name
        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var search = input.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Code.ToLower().Contains(search) ||
                x.Name.ToLower().Contains(search));
        }

        // Filter by IsDraft
        if (input.IsDraft.HasValue)
        {
            query = query.Where(x => x.IsDraft == input.IsDraft.Value);
        }

        var importItemQuery = await _warehouseImportReceiptItemRepository.GetQueryableAsync();
        var importReceiptQuery = await _warehouseImportReceiptRepository.GetQueryableAsync();

        var totalCount = query.Count();

        IQueryable<Product> sortedQuery;
        if (!string.IsNullOrWhiteSpace(input.Sorting))
        {
            var sorting = input.Sorting.Trim().ToLower();
            sortedQuery = sorting switch
            {
                "name asc" => query.OrderBy(x => x.Name),
                "name desc" => query.OrderByDescending(x => x.Name),
                "code asc" => query.OrderBy(x => x.Code),
                "code desc" => query.OrderByDescending(x => x.Code),
                "producttype asc" => query.OrderBy(x => x.ProductType),
                "producttype desc" => query.OrderByDescending(x => x.ProductType),
                "availablequantity asc" => query.OrderBy(x => x.AvailableQuantity),
                "availablequantity desc" => query.OrderByDescending(x => x.AvailableQuantity),
                "storageavailablequantity asc" => query.OrderBy(x => x.BaseUnitId.HasValue && x.PackagingQuantity > 0 ? x.AvailableQuantity * x.PackagingQuantity.Value : x.AvailableQuantity),
                "storageavailablequantity desc" => query.OrderByDescending(x => x.BaseUnitId.HasValue && x.PackagingQuantity > 0 ? x.AvailableQuantity * x.PackagingQuantity.Value : x.AvailableQuantity),
                "creationtime asc" => query.OrderBy(x => x.CreationTime),
                "warehousename asc" => query.OrderBy(x =>
                    importReceiptQuery
                        .Where(r => r.Id == importItemQuery
                            .Where(i => i.ProductId == x.Id)
                            .OrderByDescending(i => i.Id)
                            .Select(i => i.WarehouseImportReceiptId)
                            .FirstOrDefault())
                        .Select(r => r.WarehouseName)
                        .FirstOrDefault()),
                "warehousename desc" => query.OrderByDescending(x =>
                    importReceiptQuery
                        .Where(r => r.Id == importItemQuery
                            .Where(i => i.ProductId == x.Id)
                            .OrderByDescending(i => i.Id)
                            .Select(i => i.WarehouseImportReceiptId)
                            .FirstOrDefault())
                        .Select(r => r.WarehouseName)
                        .FirstOrDefault()),
                _ => query.OrderByDescending(x => x.CreationTime),
            };
        }
        else
        {
            sortedQuery = query.OrderByDescending(x => x.CreationTime);
        }

        var items = sortedQuery
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        var dtos = await MapToProductDtosAsync(items);

        return new PagedResultDto<ProductDto>(totalCount, dtos);
    }

    private async Task<PagedResultDto<ProductDto>> GetListByWarehouseAsync(ProductFilterDto input)
    {
        // 1. Level-aware warehouse resolution: Lv0 = only self, Lv1+ = BFS subtree
        var warehouseIds = await GetWarehouseIdSetAsync(input.WarehouseId!.Value);

        var warehouseQuery = await _warehouseRepository.GetQueryableAsync();
        var warehouseNameMap = warehouseQuery.Where(w => w.IsActive && warehouseIds.Contains(w.Id))
            .Select(w => new { w.Id, w.Name })
            .ToDictionary(w => w.Id, w => w.Name);

        // 2. Query completed+checked import items in warehouse scope
        var importItemQuery = await _warehouseImportReceiptItemRepository.GetQueryableAsync();
        var importReceiptQuery = await _warehouseImportReceiptRepository.GetQueryableAsync();

        var importItemsRaw = (from item in importItemQuery
                              join receipt in importReceiptQuery on item.WarehouseImportReceiptId equals receipt.Id
                              where receipt.Status == WarehouseImportReceiptStatus.Completed
                                    && item.Status == WarehouseImportLineStatus.Checked
                              where (item.StorageWarehouseId.HasValue && warehouseIds.Contains(item.StorageWarehouseId.Value))
                                    || (!item.StorageWarehouseId.HasValue && receipt.WarehouseId.HasValue && warehouseIds.Contains(receipt.WarehouseId.Value))
                              select new
                              {
                                  item.ProductId,
                                  item.ProductName,
                                  EffectiveWarehouseId = item.StorageWarehouseId ?? receipt.WarehouseId,
                                  item.StorageWarehouseName,
                                  ReceiptWarehouseName = receipt.WarehouseName,
                                  item.ActualReceivedQuantity,
                                  item.ReceivedBaseQuantity,
                                  item.ConsumedBaseQuantity,
                                  item.UnitId,
                                  item.SkuId
                              })
                              .ToList();

        var importItems = importItemsRaw.Where(x => x.EffectiveWarehouseId.HasValue).ToList();
        if (!importItems.Any())
            return new PagedResultDto<ProductDto>(0, new List<ProductDto>());

        // 3. Load products with filters
        var productIdsInScope = importItems.Select(x => x.ProductId).Distinct().ToList();
        var productQuery = await _productRepository.GetQueryableAsync();
        productQuery = productQuery.Where(x => productIdsInScope.Contains(x.Id));

        if (input.ProductType.HasValue)
            productQuery = productQuery.Where(x => x.ProductType == input.ProductType.Value);
        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var search = input.Search.Trim().ToLower();
            productQuery = productQuery.Where(x =>
                x.Code.ToLower().Contains(search) || x.Name.ToLower().Contains(search));
        }
        if (input.IsDraft.HasValue)
            productQuery = productQuery.Where(x => x.IsDraft == input.IsDraft.Value);

        var filteredProducts = productQuery.ToList();
        var filteredProductIds = filteredProducts.Select(p => p.Id).ToHashSet();
        var productMap = filteredProducts.ToDictionary(p => p.Id);

        var filteredImportItems = importItems.Where(x => filteredProductIds.Contains(x.ProductId)).ToList();
        if (!filteredImportItems.Any())
            return new PagedResultDto<ProductDto>(0, new List<ProductDto>());

        // 4. Load SKUs for unit conversion
        var skuIds = filteredImportItems.Where(x => x.SkuId.HasValue).Select(x => x.SkuId!.Value).Distinct().ToList();
        var skuQueryable = await _skuRepository.GetQueryableAsync();
        var skus = skuIds.Any() ? skuQueryable.Where(s => skuIds.Contains(s.Id)).ToList() : new List<ProductSku>();
        var skuLookup = skus.ToDictionary(s => s.Id);

        // 5. Use remaining lot quantities (ReceivedBase - Consumed), grouped by (ProductId, WarehouseId)
        var entries = new List<WarehouseStockEntry>();

        foreach (var item in filteredImportItems)
        {
            var remainingBase = item.ReceivedBaseQuantity - item.ConsumedBaseQuantity;

            var product = productMap[item.ProductId];

            int? vpu = null;
            if (item.SkuId.HasValue && skuLookup.TryGetValue(item.SkuId.Value, out var sku) && sku.VolumePerUnit > 0)
                vpu = sku.VolumePerUnit;
            else if (product.VolumePerUnit > 0)
                vpu = product.VolumePerUnit;

            decimal volumeQty = vpu.HasValue && vpu.Value > 0 ? remainingBase * vpu.Value : 0;

            var whName = warehouseNameMap.GetValueOrDefault(
                item.EffectiveWarehouseId!.Value,
                item.StorageWarehouseName ?? item.ReceiptWarehouseName ?? "");

            entries.Add(new WarehouseStockEntry
            {
                ProductId = item.ProductId,
                WarehouseId = item.EffectiveWarehouseId!.Value,
                WarehouseName = whName,
                BaseQty = remainingBase,
                VolumeQty = volumeQty
            });
        }

        // 6. Group by (ProductId, WarehouseId) — one row per product per warehouse
        var grouped = entries
            .GroupBy(x => new { x.ProductId, x.WarehouseId })
            .Select(g => new WarehouseStockEntry
            {
                ProductId = g.Key.ProductId,
                WarehouseId = g.Key.WarehouseId,
                WarehouseName = g.First().WarehouseName,
                BaseQty = g.Sum(x => x.BaseQty),
                VolumeQty = g.Sum(x => x.VolumeQty)
            })
            .ToList();

        // 7. Sort
        var sorting = input.Sorting?.Trim().ToLower() ?? "";
        IEnumerable<WarehouseStockEntry> sorted = sorting switch
        {
            "name asc" => grouped.OrderBy(x => productMap[x.ProductId].Name),
            "name desc" => grouped.OrderByDescending(x => productMap[x.ProductId].Name),
            "code asc" => grouped.OrderBy(x => productMap[x.ProductId].Code),
            "code desc" => grouped.OrderByDescending(x => productMap[x.ProductId].Code),
            "producttype asc" => grouped.OrderBy(x => productMap[x.ProductId].ProductType),
            "producttype desc" => grouped.OrderByDescending(x => productMap[x.ProductId].ProductType),
            "availablequantity asc" => grouped.OrderBy(x => x.BaseQty),
            "availablequantity desc" => grouped.OrderByDescending(x => x.BaseQty),
            "storageavailablequantity asc" => grouped.OrderBy(x =>
                productMap[x.ProductId].BaseUnitId.HasValue && productMap[x.ProductId].PackagingQuantity > 0
                    ? x.BaseQty * productMap[x.ProductId].PackagingQuantity!.Value
                    : x.BaseQty),
            "storageavailablequantity desc" => grouped.OrderByDescending(x =>
                productMap[x.ProductId].BaseUnitId.HasValue && productMap[x.ProductId].PackagingQuantity > 0
                    ? x.BaseQty * productMap[x.ProductId].PackagingQuantity!.Value
                    : x.BaseQty),
            "warehousename asc" => grouped.OrderBy(x => x.WarehouseName),
            "warehousename desc" => grouped.OrderByDescending(x => x.WarehouseName),
            "creationtime asc" => grouped.OrderBy(x => productMap[x.ProductId].CreationTime),
            _ => grouped.OrderByDescending(x => productMap[x.ProductId].CreationTime),
        };

        // 8. Paginate
        var totalCount = grouped.Count;
        var pagedItems = sorted.Skip(input.SkipCount).Take(input.MaxResultCount).ToList();

        if (!pagedItems.Any())
            return new PagedResultDto<ProductDto>(totalCount, new List<ProductDto>());

        // 9. Map to DTOs with per-warehouse overrides
        var productsForPage = pagedItems.Select(x => productMap[x.ProductId]).ToList();
        var dtos = await MapToProductDtosAsync(productsForPage);

        // 10. Per-warehouse consumption overrides (grouped by ProductId+WarehouseId to match stock rows)
        var pagedProductIds = productsForPage.Select(p => p.Id).ToList();

        // 10a. "Sử dụng" per (product, warehouse) — from StockMovementLog
        var movementQuery = await _stockMovementLogRepository.GetQueryableAsync();
        var whUsageByProductWh = movementQuery
            .Where(l => pagedProductIds.Contains(l.ProductId)
                && l.MovementType == StockMovementType.MaterialConsumption
                && l.WarehouseId.HasValue && warehouseIds.Contains(l.WarehouseId.Value))
            .GroupBy(l => new { l.ProductId, WarehouseId = l.WarehouseId!.Value })
            .Select(g => new { g.Key.ProductId, g.Key.WarehouseId, Total = g.Sum(l => Math.Abs(l.ChangeQuantity)) })
            .ToList()
            .ToDictionary(x => (x.ProductId, x.WarehouseId), x => x.Total);

        // 10b. "Tiêu hao lý thuyết" per (product, warehouse) — from IndicatorConsumption + TestBatchConsumption
        var pwiQuery = await _productWarehouseInventoryRepository.GetQueryableAsync();
        var lastInventoryByProduct = pwiQuery
            .Where(pwi => pagedProductIds.Contains(pwi.ProductId))
            .GroupBy(pwi => pwi.ProductId)
            .Select(g => new { ProductId = g.Key, LastDate = g.Max(pwi => pwi.LastInventoryDate) })
            .ToList()
            .ToDictionary(x => x.ProductId, x => x.LastDate);

        var icQuery = await _indicatorConsumptionRepository.GetQueryableAsync();
        var ltiQueryForTC = await _ltiRepository.GetQueryableAsync();
        var whTheoreticalFromIC = (from ic in icQuery
                                   join lti in ltiQueryForTC on ic.LaboratoryTaskIndicatorId equals lti.Id
                                   where ic.ProductId.HasValue
                                         && pagedProductIds.Contains(ic.ProductId!.Value)
                                         && ic.IsDeducted
                                         && ic.WarehouseId.HasValue && warehouseIds.Contains(ic.WarehouseId.Value)
                                   select new { ic.ProductId, ic.WarehouseId, ic.RecommendedQuantity, lti.SubmitDate })
                                  .ToList()
                                  .Where(x => !lastInventoryByProduct.TryGetValue(x.ProductId!.Value, out var lid)
                                               || (x.SubmitDate.HasValue && x.SubmitDate.Value > lid))
                                  .GroupBy(x => (x.ProductId!.Value, x.WarehouseId!.Value))
                                  .ToDictionary(g => g.Key, g => g.Sum(x => x.RecommendedQuantity));

        var tbcQuery = await _testBatchConsumptionRepository.GetQueryableAsync();
        var tbQuery = await _testBatchRepository.GetQueryableAsync();
        var whTheoreticalFromTBC = (from tbc in tbcQuery
                                    join tb in tbQuery on tbc.TestBatchId equals tb.Id
                                    where tbc.ProductId.HasValue
                                          && pagedProductIds.Contains(tbc.ProductId!.Value)
                                          && tbc.IsDeducted
                                          && tbc.WarehouseId.HasValue && warehouseIds.Contains(tbc.WarehouseId.Value)
                                    select new { tbc.ProductId, tbc.WarehouseId, tbc.RecommendedQuantity, tb.ConsumptionConfirmedDate })
                                   .ToList()
                                   .Where(x => !lastInventoryByProduct.TryGetValue(x.ProductId!.Value, out var lid)
                                                || (x.ConsumptionConfirmedDate.HasValue && x.ConsumptionConfirmedDate.Value > lid))
                                   .GroupBy(x => (x.ProductId!.Value, x.WarehouseId!.Value))
                                   .ToDictionary(g => g.Key, g => g.Sum(x => x.RecommendedQuantity));

        var whTheoreticalByProductWh = new Dictionary<(Guid, Guid), decimal>();
        foreach (var kvp in whTheoreticalFromIC)
            whTheoreticalByProductWh[kvp.Key] = kvp.Value;
        foreach (var kvp in whTheoreticalFromTBC)
            whTheoreticalByProductWh[kvp.Key] = whTheoreticalByProductWh.GetValueOrDefault(kvp.Key) + kvp.Value;

        for (int i = 0; i < dtos.Count; i++)
        {
            var entry = pagedItems[i];
            var product = productsForPage[i];
            dtos[i].AvailableQuantity = entry.BaseQty;
            dtos[i].StorageAvailableQuantity = product.BaseUnitId.HasValue && product.PackagingQuantity > 0
                ? entry.BaseQty * product.PackagingQuantity.Value
                : entry.BaseQty;
            dtos[i].ConsumableImportedQuantity = entry.VolumeQty;

            var key = (product.Id, entry.WarehouseId);
            var whConsumed = whUsageByProductWh.GetValueOrDefault(key);
            dtos[i].TotalConsumedQuantity = whConsumed;
            dtos[i].StorageTotalConsumedQuantity = product.BaseUnitId.HasValue && product.PackagingQuantity > 0
                ? whConsumed * product.PackagingQuantity.Value
                : whConsumed;

            dtos[i].TheoreticalConsumption = whTheoreticalByProductWh.GetValueOrDefault(key);
            dtos[i].ExpectedStockConsumable = dtos[i].ConsumableImportedQuantity - dtos[i].TheoreticalConsumption;
            dtos[i].WarehouseName = entry.WarehouseName;
            dtos[i].StockWarehouseId = entry.WarehouseId;
        }

        return new PagedResultDto<ProductDto>(totalCount, dtos);
    }

    private static decimal ComputeVolumeQty(ProductSku? sku, Product product, Guid? unitId, decimal qty)
    {
        if (sku?.VolumePerUnit > 0)
        {
            if (unitId == sku.VolumeUnitId) return qty;
            if (unitId == sku.PackagingUnitId) return qty * sku.VolumePerUnit.Value;
            if (unitId == sku.BaseUnitId && sku.BaseUnitId != sku.PackagingUnitId)
                return qty * (sku.PackagingQuantity ?? 1) * sku.VolumePerUnit.Value;
            return qty * sku.VolumePerUnit.Value;
        }
        if (product.VolumePerUnit > 0)
        {
            if (unitId == product.VolumeUnitId) return qty;
            if (unitId == product.PackagingUnitId) return qty * product.VolumePerUnit.Value;
            if (unitId == product.BaseUnitId && product.BaseUnitId != product.PackagingUnitId)
                return qty * (product.PackagingQuantity ?? 1) * product.VolumePerUnit.Value;
            return qty * product.VolumePerUnit.Value;
        }
        return 0;
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

    private class WarehouseStockEntry
    {
        public Guid ProductId { get; set; }
        public Guid WarehouseId { get; set; }
        public string WarehouseName { get; set; } = "";
        public decimal BaseQty { get; set; }
        public decimal VolumeQty { get; set; }
    }

    public async Task<ProductDto> GetAsync(Guid id)
    {
        var product = await _productRepository.GetAsync(id);
        var dtos = await MapToProductDtosAsync(new List<Product> { product });
        return dtos.First();
    }

    [Authorize(LabSpecimenManagementPermissions.Products.Create)]
    public async Task<ProductDto> CreateAsync(CreateUpdateProductDto input)
    {
        await ValidateDuplicateNameAsync(input.Name, input.ProductType);
        await ValidateDuplicateIdentificationCodeAsync(input.IdentificationCode);

        var code = await GenerateProductCodeAsync(input.ProductType);

        var product = new Product(
            GuidGenerator.Create(),
            code,
            input.Name ?? "",
            input.ProductType,
            input.BaseUnitId)
        {
            FormattedName = input.FormattedName,
            IdentificationCode = input.IdentificationCode,
            PackagingQuantity = input.PackagingQuantity,
            PackagingUnitId = input.PackagingUnitId,
            VolumePerUnit = input.VolumePerUnit,
            VolumeUnitId = input.VolumeUnitId,
            PackagingSpec = input.PackagingSpec,
            CASNumber = input.CASNumber,
            Manufacturer = input.Manufacturer,
            Origin = input.Origin,
            Concentration = input.Concentration,
            FormattedConcentration = input.FormattedConcentration,
            FormattedOrigin = input.FormattedOrigin,
            GhsCode = input.GhsCode,
            StandardCertificationType = input.StandardCertificationType.HasValue
                ? (StandardCertificationType)input.StandardCertificationType.Value
                : null,
            FormattedChemicalFormula = input.FormattedChemicalFormula,
            CertifiedValue = input.CertifiedValue,
            FormattedCertifiedValue = input.FormattedCertifiedValue,
            MeasurementUncertainty = input.MeasurementUncertainty,
            FormattedMeasurementUncertainty = input.FormattedMeasurementUncertainty,
            StandardLink = input.StandardLink,
            AnalysisGrade = input.AnalysisGrade,
            FormattedPackagingSpec = input.FormattedPackagingSpec,
            MaterialComposition = input.MaterialComposition,
            FormattedMaterialComposition = input.FormattedMaterialComposition,
            IsReusable = input.IsReusable,
            MaxReuseCount = input.MaxReuseCount,
            IsSterile = input.IsSterile,
            UsagePurpose = input.UsagePurpose,
            ShelfLifeDays = input.ShelfLifeDays,
            BaseProductId = input.BaseProductId,
            StorageConditions = input.StorageConditions,
            FormattedStorageConditions = input.FormattedStorageConditions,
            Categories = input.Categories,
            ChemicalFormula = input.ChemicalFormula,
            MinStockLevel = input.MinStockLevel,
            EquipmentModel = input.EquipmentModel,
            ManufactureYear = input.ManufactureYear,
            PowerConsumption = input.PowerConsumption,
            MainTechnicalSpecs = input.MainTechnicalSpecs,
            InstallationLocation = input.InstallationLocation,
            IsActive = input.IsActive,
            IsDraft = input.IsDraft,
        };

        await _productRepository.InsertAsync(product, autoSave: true);

        // Save SKUs
        if (input.Skus != null)
        {
            ValidateUniqueManufacturerIds(input.Skus);
            foreach (var skuInput in input.Skus)
            {
                var sku = new ProductSku(
                    GuidGenerator.Create(),
                    product.Id,
                    skuInput.ManufacturerId,
                    skuInput.BaseUnitId,
                    skuInput.PackagingQuantity,
                    skuInput.PackagingUnitId,
                    skuInput.VolumePerUnit,
                    skuInput.VolumeUnitId);
                await _skuRepository.InsertAsync(sku, autoSave: true);
            }
        }

        // Save attachments
        if (input.Attachments != null)
        {
            foreach (var att in input.Attachments)
            {
                var attachment = new ProductAttachment(
                    GuidGenerator.Create(),
                    product.Id,
                    att.FileId,
                    att.FileName,
                    $"uploads/samples/{att.FileId}",
                    att.FileSize,
                    att.FileType);
                await _attachmentRepository.InsertAsync(attachment, autoSave: true);
            }
        }

        // Save unit conversions
        if (input.UnitConversions != null)
        {
            foreach (var conv in input.UnitConversions)
            {
                var conversion = new ProductUnitConversion(
                    GuidGenerator.Create(),
                    product.Id,
                    conv.Quantity,
                    conv.BaseUnitId,
                    conv.ConvertedQuantity,
                    conv.ConvertedUnitId);
                await _conversionRepository.InsertAsync(conversion, autoSave: true);
            }
        }

        return await GetAsync(product.Id);
    }

    [Authorize(LabSpecimenManagementPermissions.Products.Edit)]
    public async Task<ProductDto> UpdateAsync(Guid id, CreateUpdateProductDto input)
    {
        await ValidateDuplicateNameAsync(input.Name, input.ProductType, id);
        await ValidateDuplicateIdentificationCodeAsync(input.IdentificationCode, id);

        var product = await _productRepository.GetAsync(id);

        product.Name = input.Name ?? "";
        product.FormattedName = input.FormattedName;
        product.IdentificationCode = input.IdentificationCode;
        product.ProductType = input.ProductType;
        product.BaseUnitId = input.BaseUnitId;
        product.PackagingQuantity = input.PackagingQuantity;
        product.PackagingUnitId = input.PackagingUnitId;
        product.VolumePerUnit = input.VolumePerUnit;
        product.VolumeUnitId = input.VolumeUnitId;
        product.PackagingSpec = input.PackagingSpec;
        product.CASNumber = input.CASNumber;
        product.Manufacturer = input.Manufacturer;
        product.Origin = input.Origin;
        product.Concentration = input.Concentration;
        product.FormattedConcentration = input.FormattedConcentration;
        product.FormattedOrigin = input.FormattedOrigin;
        product.GhsCode = input.GhsCode;
        product.StandardCertificationType = input.StandardCertificationType.HasValue
            ? (StandardCertificationType)input.StandardCertificationType.Value
            : null;
        product.FormattedChemicalFormula = input.FormattedChemicalFormula;
        product.CertifiedValue = input.CertifiedValue;
        product.FormattedCertifiedValue = input.FormattedCertifiedValue;
        product.MeasurementUncertainty = input.MeasurementUncertainty;
        product.FormattedMeasurementUncertainty = input.FormattedMeasurementUncertainty;
        product.StandardLink = input.StandardLink;
        product.AnalysisGrade = input.AnalysisGrade;
        product.FormattedPackagingSpec = input.FormattedPackagingSpec;
        product.MaterialComposition = input.MaterialComposition;
        product.FormattedMaterialComposition = input.FormattedMaterialComposition;
        product.IsReusable = input.IsReusable;
        product.MaxReuseCount = input.MaxReuseCount;
        product.IsSterile = input.IsSterile;
        product.UsagePurpose = input.UsagePurpose;
        product.ShelfLifeDays = input.ShelfLifeDays;
        product.BaseProductId = input.BaseProductId;
        product.StorageConditions = input.StorageConditions;
        product.FormattedStorageConditions = input.FormattedStorageConditions;
        product.Categories = input.Categories;
        product.ChemicalFormula = input.ChemicalFormula;
        product.MinStockLevel = input.MinStockLevel;
        product.EquipmentModel = input.EquipmentModel;
        product.ManufactureYear = input.ManufactureYear;
        product.PowerConsumption = input.PowerConsumption;
        product.MainTechnicalSpecs = input.MainTechnicalSpecs;
        product.InstallationLocation = input.InstallationLocation;
        product.IsActive = input.IsActive;
        product.IsDraft = input.IsDraft;

        await _productRepository.UpdateAsync(product, autoSave: true);

        // Update SKUs — replace all (same pattern as UnitConversions)
        if (input.Skus != null)
        {
            ValidateUniqueManufacturerIds(input.Skus);

            var existingSkus = (await _skuRepository.GetQueryableAsync())
                .Where(x => x.ProductId == id)
                .ToList();

            var inputIds = input.Skus
                .Where(x => x.Id.HasValue)
                .Select(x => x.Id!.Value)
                .ToHashSet();

            foreach (var existing in existingSkus)
            {
                if (!inputIds.Contains(existing.Id))
                {
                    await _skuRepository.DeleteAsync(existing, autoSave: true);
                }
            }

            foreach (var skuInput in input.Skus)
            {
                if (skuInput.Id.HasValue)
                {
                    var existing = existingSkus.FirstOrDefault(x => x.Id == skuInput.Id.Value)
                        ?? throw new Volo.Abp.Domain.Entities.EntityNotFoundException(typeof(ProductSku), skuInput.Id.Value);
                    existing.ManufacturerId = skuInput.ManufacturerId;
                    existing.BaseUnitId = skuInput.BaseUnitId;
                    existing.PackagingQuantity = skuInput.PackagingQuantity;
                    existing.PackagingUnitId = skuInput.PackagingUnitId;
                    existing.VolumePerUnit = skuInput.VolumePerUnit;
                    existing.VolumeUnitId = skuInput.VolumeUnitId;
                    await _skuRepository.UpdateAsync(existing, autoSave: true);
                }
                else
                {
                    var sku = new ProductSku(
                        GuidGenerator.Create(),
                        id,
                        skuInput.ManufacturerId,
                        skuInput.BaseUnitId,
                        skuInput.PackagingQuantity,
                        skuInput.PackagingUnitId,
                        skuInput.VolumePerUnit,
                        skuInput.VolumeUnitId);
                    await _skuRepository.InsertAsync(sku, autoSave: true);
                }
            }
        }

        // Update attachments — add new ones (existing ones are managed via DeleteAttachment)
        if (input.Attachments != null)
        {
            var existingAttachments = (await _attachmentRepository.GetQueryableAsync())
                .Where(x => x.ProductId == id)
                .Select(x => x.FileId)
                .ToList();

            foreach (var att in input.Attachments)
            {
                if (!existingAttachments.Contains(att.FileId))
                {
                    var attachment = new ProductAttachment(
                        GuidGenerator.Create(),
                        id,
                        att.FileId,
                        att.FileName,
                        $"uploads/samples/{att.FileId}",
                        att.FileSize,
                        att.FileType);
                    await _attachmentRepository.InsertAsync(attachment, autoSave: true);
                }
            }
        }

        // Update unit conversions — replace all
        if (input.UnitConversions != null)
        {
            var existingConversions = (await _conversionRepository.GetQueryableAsync())
                .Where(x => x.ProductId == id)
                .ToList();

            // Delete removed
            var inputIds = input.UnitConversions
                .Where(x => x.Id.HasValue)
                .Select(x => x.Id!.Value)
                .ToHashSet();

            foreach (var existing in existingConversions)
            {
                if (!inputIds.Contains(existing.Id))
                {
                    await _conversionRepository.DeleteAsync(existing, autoSave: true);
                }
            }

            // Add or update
            foreach (var conv in input.UnitConversions)
            {
                if (conv.Id.HasValue)
                {
                    var existing = existingConversions.FirstOrDefault(x => x.Id == conv.Id.Value);
                    if (existing != null)
                    {
                        existing.Quantity = conv.Quantity;
                        existing.BaseUnitId = conv.BaseUnitId;
                        existing.ConvertedQuantity = conv.ConvertedQuantity;
                        existing.ConvertedUnitId = conv.ConvertedUnitId;
                        await _conversionRepository.UpdateAsync(existing, autoSave: true);
                    }
                }
                else
                {
                    var conversion = new ProductUnitConversion(
                        GuidGenerator.Create(),
                        id,
                        conv.Quantity,
                        conv.BaseUnitId,
                        conv.ConvertedQuantity,
                        conv.ConvertedUnitId);
                    await _conversionRepository.InsertAsync(conversion, autoSave: true);
                }
            }
        }

        return await GetAsync(id);
    }

    [Authorize(LabSpecimenManagementPermissions.Products.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        await CheckProductNotReferencedAsync(id);

        // Delete child records before soft-deleting product
        var attachments = (await _attachmentRepository.GetQueryableAsync())
            .Where(x => x.ProductId == id).ToList();
        foreach (var att in attachments)
        {
            await _attachmentRepository.DeleteAsync(att, autoSave: true);
        }

        var conversions = (await _conversionRepository.GetQueryableAsync())
            .Where(x => x.ProductId == id).ToList();
        foreach (var conv in conversions)
        {
            await _conversionRepository.DeleteAsync(conv, autoSave: true);
        }

        var skus = (await _skuRepository.GetQueryableAsync())
            .Where(x => x.ProductId == id).ToList();
        foreach (var sku in skus)
        {
            await _skuRepository.DeleteAsync(sku, autoSave: true);
        }

        await _productRepository.DeleteAsync(id);
    }

    [Authorize(LabSpecimenManagementPermissions.Products.Edit)]
    public async Task ToggleActiveAsync(Guid id)
    {
        var product = await _productRepository.GetAsync(id);
        product.IsActive = !product.IsActive;
        await _productRepository.UpdateAsync(product);
    }

    public async Task<ListResultDto<ProductDto>> GetActiveProductsAsync()
    {
        var query = await _productRepository.GetQueryableAsync();
        var items = query
            .WhereActive()
            .Where(x => !x.IsDraft)
            .OrderBy(x => x.Name)
            .ToList();

        var dtos = await MapToProductDtosAsync(items);
        return new ListResultDto<ProductDto>(dtos);
    }

    [Authorize(LabSpecimenManagementPermissions.Products.Edit)]
    public async Task DeleteAttachmentAsync(Guid productId, Guid attachmentId)
    {
        var attachment = await _attachmentRepository.GetAsync(attachmentId);
        if (attachment.ProductId != productId)
        {
            throw new UserFriendlyException("Tài liệu không thuộc sản phẩm này.");
        }
        await _attachmentRepository.DeleteAsync(attachment);
    }

    [Authorize(LabSpecimenManagementPermissions.Products.Edit)]
    public async Task DeleteSkuAsync(Guid productId, Guid skuId)
    {
        var sku = await _skuRepository.GetAsync(skuId);
        if (sku.ProductId != productId)
        {
            throw new UserFriendlyException("SKU không thuộc sản phẩm này.");
        }
        await _skuRepository.DeleteAsync(sku);
    }

    public async Task<PagedResultDto<ProductSourceDto>> GetProductSourcesAsync(Guid productId, GetProductSourcesInput input)
    {
        var product = await _productRepository.GetAsync(productId);

        var itemQuery = await _goodsReceiptItemRepository.GetQueryableAsync();
        var receiptQuery = await _goodsReceiptRepository.GetQueryableAsync();
        var supplierQuery = await _supplierRepository.GetQueryableAsync();

        var unitQuery = await _unitRepository.GetQueryableAsync();
        var unitName = unitQuery.Where(u => u.Id == product.BaseUnitId).Select(u => u.Name).FirstOrDefault() ?? "";

        var sourceQuery = from item in itemQuery
                          join receipt in receiptQuery on item.GoodsReceiptId equals receipt.Id
                          join supplier in supplierQuery on receipt.SupplierId equals supplier.Id
                          where item.ProductId == productId
                          select new { item, receipt, supplier };

        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var search = input.Search.Trim().ToLower();
            sourceQuery = sourceQuery.Where(x =>
                x.supplier.Code.ToLower().Contains(search) ||
                x.supplier.Name.ToLower().Contains(search));
        }

        var grouped = sourceQuery
            .GroupBy(x => new { x.supplier.Id, x.supplier.Code, x.supplier.Name })
            .Select(g => new
            {
                SupplierId = g.Key.Id,
                SupplierCode = g.Key.Code,
                SupplierName = g.Key.Name,
                TotalImported = g.Sum(x => x.item.Quantity),
                LatestReceiptTime = g.Max(x => x.receipt.CreationTime),
            })
            .ToList();

        var totalCount = grouped.Count;

        var paged = grouped
            .OrderByDescending(x => x.LatestReceiptTime)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        var supplierIds = paged.Select(x => x.SupplierId).ToHashSet();
        var latestPrices = sourceQuery
            .Where(x => supplierIds.Contains(x.supplier.Id))
            .GroupBy(x => x.supplier.Id)
            .Select(g => new
            {
                SupplierId = g.Key,
                LatestPrice = g.OrderByDescending(x => x.receipt.CreationTime).Select(x => x.item.UnitPrice).FirstOrDefault()
            })
            .ToList()
            .ToDictionary(x => x.SupplierId, x => x.LatestPrice);

        var dtos = paged.Select(x => new ProductSourceDto
        {
            SupplierId = x.SupplierId,
            SupplierCode = x.SupplierCode,
            SupplierName = x.SupplierName,
            ProductName = product.Name,
            UnitName = unitName,
            TotalImported = x.TotalImported,
            AvailableQuantity = product.AvailableQuantity,
            LatestUnitPrice = latestPrices.GetValueOrDefault(x.SupplierId),
        }).ToList();

        return new PagedResultDto<ProductSourceDto>(totalCount, dtos);
    }

    public async Task<ProductDetailDto> GetDetailAsync(Guid id, Guid? warehouseId)
    {
        var product = await _productRepository.GetAsync(id);

        var productSkus = (await _skuRepository.GetQueryableAsync())
            .Where(s => s.ProductId == id).ToList();

        var unitQuery = await _unitRepository.GetQueryableAsync();
        var unitIds = new[] { product.BaseUnitId, product.PackagingUnitId, product.VolumeUnitId }
            .Concat(productSkus.SelectMany(s => new[] { s.PackagingUnitId, s.VolumeUnitId }))
            .Where(uid => uid.HasValue).Select(uid => uid!.Value).Distinct().ToList();
        var unitMap = unitQuery.Where(u => unitIds.Contains(u.Id))
            .ToDictionary(u => u.Id, u => u.Name);

        decimal warehouseQty = product.AvailableQuantity;
        decimal consumableQty = 0;

        var importItemQuery = await _warehouseImportReceiptItemRepository.GetQueryableAsync();
        var importReceiptQuery = await _warehouseImportReceiptRepository.GetQueryableAsync();

        if (warehouseId.HasValue)
        {
            var warehouseIds = await GetWarehouseIdSetAsync(warehouseId.Value);

            var importItems = (from item in importItemQuery
                               join receipt in importReceiptQuery on item.WarehouseImportReceiptId equals receipt.Id
                               where item.ProductId == id
                                     && receipt.Status == WarehouseImportReceiptStatus.Completed
                                     && item.Status == WarehouseImportLineStatus.Checked
                               where (item.StorageWarehouseId.HasValue && warehouseIds.Contains(item.StorageWarehouseId.Value))
                                     || (!item.StorageWarehouseId.HasValue && receipt.WarehouseId.HasValue && warehouseIds.Contains(receipt.WarehouseId.Value))
                               select new { item.ReceivedBaseQuantity, item.ConsumedBaseQuantity, item.SkuId })
                               .ToList();

            if (importItems.Any())
            {
                var skuIds = importItems.Where(x => x.SkuId.HasValue).Select(x => x.SkuId!.Value).Distinct().ToList();
                var skuLookup = skuIds.Any()
                    ? (await _skuRepository.GetQueryableAsync()).Where(s => skuIds.Contains(s.Id)).ToDictionary(s => s.Id)
                    : new Dictionary<Guid, ProductSku>();

                warehouseQty = 0;
                foreach (var item in importItems)
                {
                    var remaining = item.ReceivedBaseQuantity - item.ConsumedBaseQuantity;
                    if (remaining <= 0) continue;

                    warehouseQty += remaining;

                    ProductSku? sku = null;
                    if (item.SkuId.HasValue && skuLookup.TryGetValue(item.SkuId.Value, out var skuVal))
                        sku = skuVal;

                    int? vpu = null;
                    if (sku?.VolumePerUnit > 0)
                        vpu = sku.VolumePerUnit;
                    else if (product.VolumePerUnit > 0)
                        vpu = product.VolumePerUnit;

                    if (vpu.HasValue && vpu.Value > 0)
                        consumableQty += remaining * vpu.Value;
                }
            }
            else
            {
                warehouseQty = 0;
            }
        }
        else
        {
            var allItems = (from item in importItemQuery
                            join receipt in importReceiptQuery on item.WarehouseImportReceiptId equals receipt.Id
                            where item.ProductId == id
                                  && receipt.Status == WarehouseImportReceiptStatus.Completed
                                  && item.Status == WarehouseImportLineStatus.Checked
                            select new { item.ReceivedBaseQuantity, item.ConsumedBaseQuantity, item.SkuId })
                            .ToList();

            if (allItems.Any())
            {
                var skuIds = allItems.Where(x => x.SkuId.HasValue).Select(x => x.SkuId!.Value).Distinct().ToList();
                var skuLookup = skuIds.Any()
                    ? (await _skuRepository.GetQueryableAsync()).Where(s => skuIds.Contains(s.Id)).ToDictionary(s => s.Id)
                    : new Dictionary<Guid, ProductSku>();

                warehouseQty = 0;
                foreach (var item in allItems)
                {
                    var remaining = item.ReceivedBaseQuantity - item.ConsumedBaseQuantity;
                    if (remaining <= 0) continue;

                    warehouseQty += remaining;

                    ProductSku? sku = null;
                    if (item.SkuId.HasValue && skuLookup.TryGetValue(item.SkuId.Value, out var skuVal))
                        sku = skuVal;

                    int? vpu = null;
                    if (sku?.VolumePerUnit > 0)
                        vpu = sku.VolumePerUnit;
                    else if (product.VolumePerUnit > 0)
                        vpu = product.VolumePerUnit;

                    if (vpu.HasValue && vpu.Value > 0)
                        consumableQty += remaining * vpu.Value;
                }
            }
        }

        var detailSystemMfgIds = (await _manufacturerRepository.GetQueryableAsync())
            .Where(m => m.IsSystem).Select(m => m.Id).ToHashSet();

        return new ProductDetailDto
        {
            Id = product.Id,
            Name = product.Name,
            FormattedName = product.FormattedName,
            IdentificationCode = product.IdentificationCode,
            CASNumber = product.CASNumber,
            StorageConditions = product.StorageConditions,
            FormattedStorageConditions = product.FormattedStorageConditions,
            MainTechnicalSpecs = product.MainTechnicalSpecs,
            ProductType = product.ProductType,
            WarehouseAvailableQuantity = warehouseQty,
            BaseUnitName = product.BaseUnitId.HasValue ? unitMap.GetValueOrDefault(product.BaseUnitId.Value) : null,
            PackagingUnitName = product.PackagingUnitId.HasValue
                ? unitMap.GetValueOrDefault(product.PackagingUnitId.Value)
                : (productSkus.FirstOrDefault(s => s.PackagingUnitId.HasValue && detailSystemMfgIds.Contains(s.ManufacturerId))
                   ?? productSkus.FirstOrDefault(s => s.PackagingUnitId.HasValue))?.PackagingUnitId is { } skuPkgId
                    ? unitMap.GetValueOrDefault(skuPkgId)
                    : null,
            PackagingQuantity = product.PackagingQuantity,
            ConsumableImportedQuantity = consumableQty,
            VolumePerUnit = product.VolumePerUnit
                ?? (productSkus.FirstOrDefault(s => s.VolumePerUnit.HasValue && detailSystemMfgIds.Contains(s.ManufacturerId))
                   ?? productSkus.FirstOrDefault(s => s.VolumePerUnit.HasValue))?.VolumePerUnit,
            VolumeUnitName = product.VolumeUnitId.HasValue
                ? unitMap.GetValueOrDefault(product.VolumeUnitId.Value)
                : (productSkus.FirstOrDefault(s => s.VolumeUnitId.HasValue && detailSystemMfgIds.Contains(s.ManufacturerId))
                   ?? productSkus.FirstOrDefault(s => s.VolumeUnitId.HasValue))?.VolumeUnitId is { } skuVolId
                    ? unitMap.GetValueOrDefault(skuVolId)
                    : null,
        };
    }

    public async Task<PagedResultDto<ProductImportHistoryDto>> GetImportHistoryAsync(Guid id, GetProductImportHistoryInput input)
    {
        var importItemQuery = await _warehouseImportReceiptItemRepository.GetQueryableAsync();
        var importReceiptQuery = await _warehouseImportReceiptRepository.GetQueryableAsync();

        var query = from item in importItemQuery
                    join receipt in importReceiptQuery on item.WarehouseImportReceiptId equals receipt.Id
                    where item.ProductId == id
                          && receipt.Status == WarehouseImportReceiptStatus.Completed
                          && item.Status == WarehouseImportLineStatus.Checked
                    select new { item, receipt };

        if (input.WarehouseId.HasValue)
        {
            var warehouseIds = await GetWarehouseIdSetAsync(input.WarehouseId.Value);
            query = query.Where(x =>
                (x.item.StorageWarehouseId.HasValue && warehouseIds.Contains(x.item.StorageWarehouseId.Value))
                || (!x.item.StorageWarehouseId.HasValue && x.receipt.WarehouseId.HasValue && warehouseIds.Contains(x.receipt.WarehouseId.Value)));
        }

        if (input.FromDate.HasValue)
            query = query.Where(x => x.receipt.ActualImportDate >= input.FromDate.Value.Date);

        if (input.ToDate.HasValue)
            query = query.Where(x => x.receipt.ActualImportDate < input.ToDate.Value.Date.AddDays(1));

        var totalCount = query.Count();

        var sorted = !string.IsNullOrWhiteSpace(input.Sorting)
            ? input.Sorting.Trim().ToLower() switch
            {
                "lotexpirydate asc" => query.OrderBy(x => x.item.LotExpiryDate == null ? 1 : 0).ThenBy(x => x.item.LotExpiryDate),
                "lotexpirydate desc" => query.OrderBy(x => x.item.LotExpiryDate == null ? 1 : 0).ThenByDescending(x => x.item.LotExpiryDate),
                "actualimportdate asc" => query.OrderBy(x => x.receipt.ActualImportDate),
                "actualimportdate desc" => query.OrderByDescending(x => x.receipt.ActualImportDate),
                _ => query.OrderBy(x => x.item.LotExpiryDate == null ? 1 : 0).ThenBy(x => x.item.LotExpiryDate),
            }
            : query.OrderBy(x => x.item.LotExpiryDate == null ? 1 : 0).ThenBy(x => x.item.LotExpiryDate);

        var rawItems = sorted.Skip(input.SkipCount).Take(input.MaxResultCount)
            .Select(x => new
            {
                ReceiptId = x.receipt.Id,
                ReceiptCode = x.receipt.Code,
                x.receipt.ActualImportDate,
                x.receipt.SupplierName,
                x.item.OrderedQuantity,
                x.item.ActualReceivedQuantity,
                x.item.UnitName,
                x.item.LotNumber,
                x.item.LotExpiryDate,
                x.item.QualityDocumentFileId,
                x.item.QualityDocumentFileName,
                x.item.QualityDocumentFilePath,
                x.receipt.TotalAmount,
                x.item.GoodsReceiptItemId,
                x.item.SkuId,
            }).ToList();

        var goodsReceiptItemIds = rawItems
            .Where(x => x.GoodsReceiptItemId.HasValue)
            .Select(x => x.GoodsReceiptItemId!.Value).Distinct().ToList();
        var grItemLookup = (await _goodsReceiptItemRepository.GetQueryableAsync())
            .Where(x => goodsReceiptItemIds.Contains(x.Id))
            .Select(x => new { x.Id, x.UnitName })
            .ToDictionary(x => x.Id);

        var skuIds = rawItems.Where(x => x.SkuId.HasValue).Select(x => x.SkuId!.Value).Distinct().ToList();
        var skuMap = skuIds.Count > 0
            ? (await _skuRepository.GetQueryableAsync()).Where(x => skuIds.Contains(x.Id)).ToDictionary(x => x.Id)
            : new Dictionary<Guid, ProductSku>();
        var manufacturerIds = skuMap.Values.Select(x => x.ManufacturerId).Distinct().ToList();
        var manufacturerNames = manufacturerIds.Count > 0
            ? (await _manufacturerRepository.GetQueryableAsync()).Where(x => manufacturerIds.Contains(x.Id)).ToDictionary(x => x.Id, x => x.Name)
            : new Dictionary<Guid, string>();

        var items = rawItems.Select(x =>
        {
            var dto = new ProductImportHistoryDto
            {
                WarehouseImportReceiptId = x.ReceiptId,
                ReceiptCode = x.ReceiptCode,
                ActualImportDate = x.ActualImportDate,
                SupplierName = x.SupplierName,
                OrderedQuantity = x.OrderedQuantity,
                ActualReceivedQuantity = x.ActualReceivedQuantity,
                UnitName = x.UnitName,
                LotNumber = x.LotNumber,
                LotExpiryDate = x.LotExpiryDate,
                QualityDocumentFileId = x.QualityDocumentFileId,
                QualityDocumentFileName = x.QualityDocumentFileName,
                QualityDocumentFilePath = x.QualityDocumentFilePath,
                TotalAmount = x.TotalAmount,
            };
            if (x.GoodsReceiptItemId.HasValue && grItemLookup.TryGetValue(x.GoodsReceiptItemId.Value, out var grItem))
            {
                dto.PurchaseUnitName = grItem.UnitName;
            }
            if (x.SkuId.HasValue && skuMap.TryGetValue(x.SkuId.Value, out var sku)
                && manufacturerNames.TryGetValue(sku.ManufacturerId, out var mfName))
                dto.ManufacturerName = mfName;
            return dto;
        }).ToList();

        var receiptIds = items.Select(x => x.WarehouseImportReceiptId).Distinct().ToList();
        var deliveryDocsMap = new Dictionary<Guid, List<WarehouseImports.DeliveryDocumentFileDto>>();
        if (receiptIds.Any())
        {
            var receipts = importReceiptQuery.Where(r => receiptIds.Contains(r.Id)).ToList();
            foreach (var r in receipts)
            {
                var docs = new List<WarehouseImports.DeliveryDocumentFileDto>();
                if (!string.IsNullOrEmpty(r.DeliveryDocumentFiles))
                {
                    var parsed = System.Text.Json.JsonSerializer.Deserialize<List<WarehouseImports.DeliveryDocumentFileDto>>(r.DeliveryDocumentFiles);
                    if (parsed != null) docs.AddRange(parsed);
                }
                else if (!string.IsNullOrEmpty(r.DeliveryDocumentFileId))
                {
                    docs.Add(new WarehouseImports.DeliveryDocumentFileDto
                    {
                        FileId = r.DeliveryDocumentFileId,
                        FileName = r.DeliveryDocumentFileName ?? "",
                        FileSize = r.DeliveryDocumentFileSize,
                    });
                }
                deliveryDocsMap[r.Id] = docs;
            }
        }

        foreach (var item in items)
        {
            item.DeliveryDocumentFiles = deliveryDocsMap.GetValueOrDefault(item.WarehouseImportReceiptId);
        }

        return new PagedResultDto<ProductImportHistoryDto>(totalCount, items);
    }

    public async Task<PagedResultDto<ProductMaterialReceiptHistoryDto>> GetMaterialReceiptHistoryAsync(
        Guid id,
        GetProductMaterialReceiptHistoryInput input)
    {
        var importItemQuery = await _warehouseImportReceiptItemRepository.GetQueryableAsync();
        var importReceiptQuery = await _warehouseImportReceiptRepository.GetQueryableAsync();
        var skuQuery = await _skuRepository.GetQueryableAsync();
        var manufacturerQuery = await _manufacturerRepository.GetQueryableAsync();
        var product = await _productRepository.GetAsync(id);

        // Source for material receipts is always the central (default) warehouse
        var centralWarehouse = (await _warehouseRepository.GetQueryableAsync())
            .Where(w => w.IsDefault && w.IsActive)
            .Select(w => new { w.Id, w.Name })
            .FirstOrDefault();
        var centralId = centralWarehouse?.Id;
        var centralName = centralWarehouse?.Name;

        var query = from item in importItemQuery
                    join receipt in importReceiptQuery on item.WarehouseImportReceiptId equals receipt.Id
                    join skuCandidate in skuQuery on item.SkuId equals (Guid?)skuCandidate.Id into skuGroup
                    from sku in skuGroup.DefaultIfEmpty()
                    let resolvedManufacturerId = item.ManufacturerId ?? (sku == null ? (Guid?)null : sku.ManufacturerId)
                    join manufacturerCandidate in manufacturerQuery
                        on resolvedManufacturerId equals (Guid?)manufacturerCandidate.Id into manufacturerGroup
                    from manufacturer in manufacturerGroup.DefaultIfEmpty()
                    where item.ProductId == id
                          && receipt.ReceiptType == WarehouseImportReceiptType.MaterialReceipt
                          && receipt.MaterialReceiptId.HasValue
                          && receipt.Status == WarehouseImportReceiptStatus.Completed
                          && item.Status == WarehouseImportLineStatus.Checked
                    select new
                    {
                        MaterialReceiptId = receipt.MaterialReceiptId!.Value,
                        receipt.Code,
                        receipt.ActualImportDate,
                        receipt.CreationTime,
                        WarehouseId = item.StorageWarehouseId ?? receipt.WarehouseId ?? Guid.Empty,
                        WarehouseName = item.StorageWarehouseName ?? receipt.WarehouseName,
                        SourceWarehouseId = centralId,
                        SourceWarehouseName = centralName,
                        item.ActualReceivedQuantity,
                        item.UnitId,
                        item.UnitName,
                        ManufacturerId = resolvedManufacturerId,
                        ManufacturerName = item.ManufacturerName ?? (manufacturer == null ? null : manufacturer.Name),
                        item.LotNumber,
                        item.LotExpiryDate,
                        item.StockBeforeBaseQuantity,
                        item.StockAfterBaseQuantity,
                        item.BarcodeValues,
                        StorageUnitId = sku == null
                            ? product.PackagingUnitId ?? product.BaseUnitId
                            : sku.PackagingUnitId,
                        ConsumableUnitId = sku == null ? product.VolumeUnitId : sku.VolumeUnitId,
                        VolumePerUnit = sku == null ? product.VolumePerUnit : sku.VolumePerUnit
                    };

        if (input.WarehouseId.HasValue)
        {
            var isViewingFromCentral = centralId.HasValue && centralId.Value == input.WarehouseId.Value;
            if (isViewingFromCentral)
            {
                // From central: show all material receipts (they all originate from central)
            }
            else
            {
                // From child: show only items stored in this warehouse
                query = query.Where(x => x.WarehouseId == input.WarehouseId.Value);
            }
        }
        if (input.FromDate.HasValue)
            query = query.Where(x => x.ActualImportDate >= input.FromDate.Value.Date);
        if (input.ToDate.HasValue)
            query = query.Where(x => x.ActualImportDate < input.ToDate.Value.Date.AddDays(1));

        var groupedQuery = query
            .GroupBy(x => new
            {
                x.MaterialReceiptId,
                x.Code,
                x.ActualImportDate,
                x.CreationTime,
                x.WarehouseId,
                x.WarehouseName,
                x.SourceWarehouseId,
                x.SourceWarehouseName,
                x.UnitName,
                x.ManufacturerId,
                x.ManufacturerName,
                x.LotNumber,
                x.LotExpiryDate,
                x.StorageUnitId,
                x.ConsumableUnitId,
                x.VolumePerUnit
            })
            .Select(g => new MaterialReceiptHistoryGroupRow
            {
                MaterialReceiptId = g.Key.MaterialReceiptId,
                ReceiptCode = g.Key.Code,
                ReceiptDate = g.Key.ActualImportDate,
                CreationTime = g.Key.CreationTime,
                ReceivingWarehouseId = g.Key.WarehouseId,
                ReceivingWarehouseName = g.Key.WarehouseName,
                SourceWarehouseId = g.Key.SourceWarehouseId,
                SourceWarehouseName = g.Key.SourceWarehouseName,
                ReceivedQuantity = g.Sum(x => x.ActualReceivedQuantity),
                UnitName = g.Key.UnitName,
                ManufacturerId = g.Key.ManufacturerId,
                ManufacturerName = g.Key.ManufacturerName,
                LotNumber = g.Key.LotNumber,
                LotExpiryDate = g.Key.LotExpiryDate,
                StorageUnitId = g.Key.StorageUnitId,
                ConsumableUnitId = g.Key.ConsumableUnitId,
                VolumePerUnit = g.Key.VolumePerUnit
            });

        var totalCount = await AsyncExecuter.CountAsync(groupedQuery);
        if (totalCount == 0)
            return new PagedResultDto<ProductMaterialReceiptHistoryDto>(0, new List<ProductMaterialReceiptHistoryDto>());

        var pageRows = await AsyncExecuter.ToListAsync(groupedQuery
            .OrderByDescending(x => x.CreationTime)
            .ThenByDescending(x => x.ReceiptDate)
            .ThenBy(x => x.ManufacturerName)
            .ThenBy(x => x.LotExpiryDate)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount));

        var unitIds = pageRows
            .SelectMany(x => new[] { x.StorageUnitId, x.ConsumableUnitId })
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();
        var unitMap = unitIds.Count == 0
            ? new Dictionary<Guid, string>()
            : (await AsyncExecuter.ToListAsync(
                (await _unitRepository.GetQueryableAsync()).Where(x => unitIds.Contains(x.Id))))
                .ToDictionary(x => x.Id, x => x.Name);

        var pageReceiptIds = pageRows.Select(x => x.MaterialReceiptId).Distinct().ToList();
        var barcodeQuery = from item in importItemQuery
                           join receipt in importReceiptQuery
                               on item.WarehouseImportReceiptId equals receipt.Id
                           join skuCandidate in skuQuery
                               on item.SkuId equals (Guid?)skuCandidate.Id into skuGroup
                           from sku in skuGroup.DefaultIfEmpty()
                           where item.ProductId == id
                                 && receipt.ReceiptType == WarehouseImportReceiptType.MaterialReceipt
                                 && receipt.MaterialReceiptId.HasValue
                                 && receipt.Status == WarehouseImportReceiptStatus.Completed
                                 && item.Status == WarehouseImportLineStatus.Checked
                                 && pageReceiptIds.Contains(receipt.MaterialReceiptId.Value)
                           select new
                           {
                               MaterialReceiptId = receipt.MaterialReceiptId.Value,
                               WarehouseId = item.StorageWarehouseId ?? receipt.WarehouseId ?? Guid.Empty,
                               ManufacturerId = item.ManufacturerId ?? (sku == null ? (Guid?)null : sku.ManufacturerId),
                               item.LotNumber,
                               item.LotExpiryDate,
                               StorageUnitId = sku == null
                                   ? product.PackagingUnitId ?? product.BaseUnitId
                                   : sku.PackagingUnitId,
                               ConsumableUnitId = sku == null ? product.VolumeUnitId : sku.VolumeUnitId,
                               VolumePerUnit = sku == null ? product.VolumePerUnit : sku.VolumePerUnit,
                               item.BarcodeValues,
                               item.StockBeforeBaseQuantity,
                               item.StockAfterBaseQuantity
                           };
        var pageBarcodeRows = await AsyncExecuter.ToListAsync(barcodeQuery);

        var items = pageRows.Select(row =>
        {
            var matchingRows = pageBarcodeRows
                .Where(x =>
                    x.MaterialReceiptId == row.MaterialReceiptId &&
                    x.WarehouseId == row.ReceivingWarehouseId &&
                    x.ManufacturerId == row.ManufacturerId &&
                    x.LotNumber == row.LotNumber &&
                    x.LotExpiryDate == row.LotExpiryDate &&
                    x.StorageUnitId == row.StorageUnitId &&
                    x.ConsumableUnitId == row.ConsumableUnitId &&
                    x.VolumePerUnit == row.VolumePerUnit)
                .ToList();
            var barcodes = matchingRows
                .SelectMany(x => DeserializeBarcodes(x.BarcodeValues))
                .Distinct()
                .ToList();
            var stockBefore = matchingRows
                .Where(x => x.StockBeforeBaseQuantity.HasValue)
                .Select(x => x.StockBeforeBaseQuantity)
                .DefaultIfEmpty()
                .Min();
            var stockAfter = matchingRows
                .Where(x => x.StockAfterBaseQuantity.HasValue)
                .Select(x => x.StockAfterBaseQuantity)
                .DefaultIfEmpty()
                .Max();

            return new ProductMaterialReceiptHistoryDto
            {
                MaterialReceiptId = row.MaterialReceiptId,
                ReceiptCode = row.ReceiptCode,
                ReceiptDate = row.ReceiptDate,
                ReceivingWarehouseId = row.ReceivingWarehouseId,
                ReceivingWarehouseName = row.ReceivingWarehouseName,
                SourceWarehouseId = row.SourceWarehouseId,
                SourceWarehouseName = row.SourceWarehouseName,
                ReceivedQuantity = row.ReceivedQuantity,
                UnitName = row.UnitName,
                ManufacturerId = row.ManufacturerId,
                ManufacturerName = row.ManufacturerName,
                LotNumber = row.LotNumber,
                LotExpiryDate = row.LotExpiryDate,
                Barcodes = barcodes,
                StockBeforeBaseQuantity = stockBefore,
                StockAfterBaseQuantity = stockAfter,
                StorageUnitName = row.StorageUnitId.HasValue
                    ? unitMap.GetValueOrDefault(row.StorageUnitId.Value) : null,
                ConsumableUnitName = row.ConsumableUnitId.HasValue
                    ? unitMap.GetValueOrDefault(row.ConsumableUnitId.Value) : null,
                VolumePerUnit = row.VolumePerUnit
            };
        }).ToList();

        return new PagedResultDto<ProductMaterialReceiptHistoryDto>(totalCount, items);
    }

    #region Private Methods

    private async Task<HashSet<Guid>> GetWarehouseIdSetAsync(Guid warehouseId)
    {
        var wh = await _warehouseRepository.FindAsync(warehouseId);
        if (wh == null || wh.Level == 0)
            return new HashSet<Guid> { warehouseId };

        return await GetWarehouseSubtreeIdsAsync(warehouseId);
    }

    private async Task<HashSet<Guid>> GetWarehouseSubtreeIdsAsync(Guid rootWarehouseId)
    {
        var warehouseQuery = await _warehouseRepository.GetQueryableAsync();
        var allWarehouses = warehouseQuery.Where(w => w.IsActive)
            .Select(w => new { w.Id, w.ParentWarehouseId }).ToList();

        var warehouseIds = new HashSet<Guid> { rootWarehouseId };
        var queue = new Queue<Guid>();
        queue.Enqueue(rootWarehouseId);
        while (queue.Count > 0)
        {
            var parentId = queue.Dequeue();
            foreach (var child in allWarehouses.Where(w => w.ParentWarehouseId == parentId))
            {
                if (warehouseIds.Add(child.Id))
                    queue.Enqueue(child.Id);
            }
        }

        return warehouseIds;
    }

    private async Task CheckProductNotReferencedAsync(Guid productId)
    {
        var query = await _goodsReceiptItemRepository.GetQueryableAsync();
        var isReferenced = query.Any(x => x.ProductId == productId);
        if (isReferenced)
        {
            throw new UserFriendlyException("Không thể xóa sản phẩm này vì đang được sử dụng trong phiếu nhập hàng.");
        }
    }

    private async Task ValidateDuplicateNameAsync(string? name, ProductType productType, Guid? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(name)) return;

        var query = await _productRepository.GetQueryableAsync();
        var normalizedName = name.Trim().ToLower();

        var exists = await AsyncExecuter.AnyAsync(query.Where(x =>
            x.Name.ToLower() == normalizedName &&
            x.ProductType == productType &&
            (excludeId == null || x.Id != excludeId)));

        if (exists)
        {
            throw new UserFriendlyException("Tên sản phẩm đã tồn tại trong cùng loại sản phẩm. Vui lòng chọn tên khác.");
        }
    }

    private async Task ValidateDuplicateIdentificationCodeAsync(string? identificationCode, Guid? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(identificationCode)) return;

        var query = await _productRepository.GetQueryableAsync();
        var normalized = identificationCode.Trim().ToLower();

        var exists = await AsyncExecuter.AnyAsync(query.Where(x =>
            x.IdentificationCode != null &&
            x.IdentificationCode.ToLower() == normalized &&
            (excludeId == null || x.Id != excludeId)));

        if (exists)
        {
            throw new UserFriendlyException("Vật tư đã tồn tại trong kho");
        }
    }

    private async Task<string> GenerateProductCodeAsync(ProductType productType)
    {
        var prefix = productType switch
        {
            ProductType.Chemical => "CHE",
            ProductType.Biological => "BIO",
            ProductType.Supply => "SUP",
            ProductType.Equipment => "EQP",
            _ => "PRD"
        };

        using (_dataFilter.Disable<Volo.Abp.ISoftDelete>())
        {
            var todayPrefix = _codeGenerator.GetTodayPrefix(prefix);
            var query = await _productRepository.GetQueryableAsync();
            var count = query.Count(x => x.Code.StartsWith(todayPrefix));
            return _codeGenerator.GenerateCodeWithDate(prefix, count);
        }
    }

    private async Task<List<ProductDto>> MapToProductDtosAsync(List<Product> products)
    {
        if (!products.Any()) return new List<ProductDto>();

        var productIds = products.Select(x => x.Id).ToList();
        var unitIds = products
            .SelectMany(x => new[] { x.BaseUnitId, x.PackagingUnitId, x.VolumeUnitId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        // Load units
        var unitsQuery = await _unitRepository.GetQueryableAsync();
        var units = unitsQuery.Where(u => unitIds.Contains(u.Id)).ToList();
        var unitMap = units.ToDictionary(u => u.Id, u => u.Name);

        // Load attachments
        var attachmentsQuery = await _attachmentRepository.GetQueryableAsync();
        var attachments = attachmentsQuery.Where(a => productIds.Contains(a.ProductId)).ToList();
        var attachmentMap = attachments.GroupBy(a => a.ProductId).ToDictionary(g => g.Key, g => g.ToList());

        // Load unit conversions
        var conversionsQuery = await _conversionRepository.GetQueryableAsync();
        var conversions = conversionsQuery.Where(c => productIds.Contains(c.ProductId)).ToList();
        var conversionMap = conversions.GroupBy(c => c.ProductId).ToDictionary(g => g.Key, g => g.ToList());

        // Load SKUs
        var skuQuery = await _skuRepository.GetQueryableAsync();
        var skus = skuQuery.Where(s => productIds.Contains(s.ProductId)).ToList();
        var skuMap = skus.GroupBy(s => s.ProductId).ToDictionary(g => g.Key, g => g.ToList());

        // Load manufacturer names for SKUs
        var manufacturerIds = skus.Select(s => s.ManufacturerId).Distinct().ToList();
        var manufacturerQuery = await _manufacturerRepository.GetQueryableAsync();
        var manufacturers = manufacturerIds.Any()
            ? manufacturerQuery.Where(m => manufacturerIds.Contains(m.Id)).ToList()
            : new List<Manufacturer>();
        var manufacturerMap = manufacturers.ToDictionary(m => m.Id, m => new { m.Code, m.Name });

        // Load SKU unit names into unitMap
        var skuUnitIds = skus
            .SelectMany(s => new[] { s.BaseUnitId, s.PackagingUnitId, s.VolumeUnitId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .Where(id => !unitMap.ContainsKey(id))
            .ToList();
        if (skuUnitIds.Any())
        {
            var skuUnits = unitsQuery.Where(u => skuUnitIds.Contains(u.Id)).ToList();
            foreach (var u in skuUnits)
            {
                unitMap[u.Id] = u.Name;
            }
        }

        // Load warehouse names from latest WarehouseImportReceipt per product
        var importItemQuery = await _warehouseImportReceiptItemRepository.GetQueryableAsync();
        var importReceiptQuery = await _warehouseImportReceiptRepository.GetQueryableAsync();
        var latestReceiptIds = importItemQuery
            .Where(i => productIds.Contains(i.ProductId))
            .GroupBy(i => i.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                ReceiptId = g.OrderByDescending(i => i.Id).Select(i => i.WarehouseImportReceiptId).FirstOrDefault()
            })
            .ToList();
        var receiptIds = latestReceiptIds.Select(x => x.ReceiptId).Distinct().ToList();
        var receiptWarehouseMap = receiptIds.Any()
            ? importReceiptQuery
                .Where(r => receiptIds.Contains(r.Id))
                .Select(r => new { r.Id, r.WarehouseName })
                .ToList()
                .ToDictionary(r => r.Id, r => r.WarehouseName)
            : new Dictionary<Guid, string?>();
        var productWarehouseMap = latestReceiptIds.ToDictionary(
            x => x.ProductId,
            x => receiptWarehouseMap.GetValueOrDefault(x.ReceiptId));

        // FIFO batch data: earliest import per product (by ActualImportDate)
        var allImportItems = (from item in importItemQuery
                              join receipt in importReceiptQuery on item.WarehouseImportReceiptId equals receipt.Id
                              where productIds.Contains(item.ProductId)
                              select new
                              {
                                  item.ProductId,
                                  item.LotNumber,
                                  item.LotExpiryDate,
                                  item.Notes,
                                  receipt.ActualImportDate
                              })
                              .ToList();
        // Nearest-expiry lot data: pick the lot soonest to expire; nulls (no expiry) sorted last
        var earliestBatchMap = allImportItems
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g
                .OrderBy(x => x.LotExpiryDate ?? DateTime.MaxValue)
                .First());

        // Usage aggregation: sum consumed quantity per product
        var movementQuery = await _stockMovementLogRepository.GetQueryableAsync();
        var usageByProduct = movementQuery
            .Where(l => productIds.Contains(l.ProductId) && l.MovementType == StockMovementType.MaterialConsumption)
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, Total = g.Sum(l => Math.Abs(l.ChangeQuantity)) })
            .ToList()
            .ToDictionary(x => x.ProductId, x => x.Total);

        // Theoretical consumption: sum RecommendedQuantity from both consumption flows, filtered by lastInventoryDate
        var pwiQuery = await _productWarehouseInventoryRepository.GetQueryableAsync();
        var lastInventoryByProduct = pwiQuery
            .Where(pwi => productIds.Contains(pwi.ProductId))
            .GroupBy(pwi => pwi.ProductId)
            .Select(g => new { ProductId = g.Key, LastDate = g.Max(pwi => pwi.LastInventoryDate) })
            .ToList()
            .ToDictionary(x => x.ProductId, x => x.LastDate);

        var icQuery = await _indicatorConsumptionRepository.GetQueryableAsync();
        var ltiQueryForTC = await _ltiRepository.GetQueryableAsync();
        var theoreticalFromIC = (from ic in icQuery
                                 join lti in ltiQueryForTC on ic.LaboratoryTaskIndicatorId equals lti.Id
                                 where ic.ProductId.HasValue
                                       && productIds.Contains(ic.ProductId!.Value)
                                       && ic.IsDeducted
                                 select new { ic.ProductId, ic.RecommendedQuantity, lti.SubmitDate })
                                .ToList()
                                .Where(x => !lastInventoryByProduct.TryGetValue(x.ProductId!.Value, out var lid)
                                             || (x.SubmitDate.HasValue && x.SubmitDate.Value > lid))
                                .GroupBy(x => x.ProductId!.Value)
                                .ToDictionary(g => g.Key, g => g.Sum(x => x.RecommendedQuantity));

        var tbcQuery = await _testBatchConsumptionRepository.GetQueryableAsync();
        var tbQuery = await _testBatchRepository.GetQueryableAsync();
        var theoreticalFromTBC = (from tbc in tbcQuery
                                  join tb in tbQuery on tbc.TestBatchId equals tb.Id
                                  where tbc.ProductId.HasValue
                                        && productIds.Contains(tbc.ProductId!.Value)
                                        && tbc.IsDeducted
                                  select new { tbc.ProductId, tbc.RecommendedQuantity, tb.ConsumptionConfirmedDate })
                                 .ToList()
                                 .Where(x => !lastInventoryByProduct.TryGetValue(x.ProductId!.Value, out var lid)
                                              || (x.ConsumptionConfirmedDate.HasValue && x.ConsumptionConfirmedDate.Value > lid))
                                 .GroupBy(x => x.ProductId!.Value)
                                 .ToDictionary(g => g.Key, g => g.Sum(x => x.RecommendedQuantity));

        var theoreticalByProduct = new Dictionary<Guid, decimal>();
        foreach (var kvp in theoreticalFromIC)
            theoreticalByProduct[kvp.Key] = kvp.Value;
        foreach (var kvp in theoreticalFromTBC)
            theoreticalByProduct[kvp.Key] = theoreticalByProduct.GetValueOrDefault(kvp.Key) + kvp.Value;

        // Consumable quantity = remaining lot volume (ReceivedBase - Consumed) × VolumePerUnit
        var consumableImportItems = (from item in importItemQuery
                                     join receipt in importReceiptQuery on item.WarehouseImportReceiptId equals receipt.Id
                                     where productIds.Contains(item.ProductId)
                                           && receipt.Status == WarehouseImportReceiptStatus.Completed
                                           && item.Status == WarehouseImportLineStatus.Checked
                                     select new
                                     {
                                         item.ProductId,
                                         item.SkuId,
                                         RemainingBase = item.ReceivedBaseQuantity - item.ConsumedBaseQuantity
                                     })
                                     .ToList();
        var consumableByProduct = new Dictionary<Guid, decimal>();
        foreach (var ci in consumableImportItems)
        {
            if (ci.RemainingBase <= 0) continue;

            int? vpu = null;
            if (ci.SkuId.HasValue && skuMap.TryGetValue(ci.ProductId, out var pSkus))
            {
                var sku = pSkus.FirstOrDefault(s => s.Id == ci.SkuId.Value);
                if (sku?.VolumePerUnit > 0) vpu = sku.VolumePerUnit;
            }
            if (!vpu.HasValue)
            {
                var prod = products.FirstOrDefault(p => p.Id == ci.ProductId);
                if (prod?.VolumePerUnit > 0) vpu = prod.VolumePerUnit;
            }

            decimal cQty = vpu.HasValue && vpu.Value > 0 ? ci.RemainingBase * vpu.Value : 0;

            if (cQty > 0)
                consumableByProduct[ci.ProductId] = consumableByProduct.GetValueOrDefault(ci.ProductId) + cQty;
        }

        // Load all unit names for conversions
        var allConversionUnitIds = conversions
            .SelectMany(c => new[] { c.BaseUnitId, c.ConvertedUnitId })
            .Distinct()
            .Where(id => !unitMap.ContainsKey(id))
            .ToList();
        if (allConversionUnitIds.Any())
        {
            var extraUnits = unitsQuery.Where(u => allConversionUnitIds.Contains(u.Id)).ToList();
            foreach (var u in extraUnits)
            {
                unitMap[u.Id] = u.Name;
            }
        }

        var systemMfgIds = manufacturers.Where(m => m.IsSystem).Select(m => m.Id).ToHashSet();

        return products.Select(product =>
        {
            var dto = ObjectMapper.Map<Product, ProductDto>(product);
            dto.BaseUnitName = product.BaseUnitId.HasValue ? unitMap.GetValueOrDefault(product.BaseUnitId.Value) : null;
            dto.PackagingUnitName = product.PackagingUnitId.HasValue ? unitMap.GetValueOrDefault(product.PackagingUnitId.Value) : null;
            if (dto.PackagingUnitName == null && skuMap.TryGetValue(product.Id, out var fallbackSkus))
            {
                var sku = fallbackSkus.FirstOrDefault(s => s.PackagingUnitId.HasValue && systemMfgIds.Contains(s.ManufacturerId))
                       ?? fallbackSkus.FirstOrDefault(s => s.PackagingUnitId.HasValue);
                if (sku != null)
                    dto.PackagingUnitName = unitMap.GetValueOrDefault(sku.PackagingUnitId!.Value);
            }
            dto.VolumeUnitName = product.VolumeUnitId.HasValue ? unitMap.GetValueOrDefault(product.VolumeUnitId.Value) : null;
            if (dto.VolumeUnitName == null && skuMap.TryGetValue(product.Id, out var volFallbackSkus))
            {
                var vSku = volFallbackSkus.FirstOrDefault(s => s.VolumeUnitId.HasValue && systemMfgIds.Contains(s.ManufacturerId))
                        ?? volFallbackSkus.FirstOrDefault(s => s.VolumeUnitId.HasValue);
                if (vSku != null)
                    dto.VolumeUnitName = unitMap.GetValueOrDefault(vSku.VolumeUnitId!.Value);
            }
            if (!dto.VolumePerUnit.HasValue && skuMap.TryGetValue(product.Id, out var vpuFallbackSkus))
            {
                var vpuSku = vpuFallbackSkus.FirstOrDefault(s => s.VolumePerUnit.HasValue && systemMfgIds.Contains(s.ManufacturerId))
                          ?? vpuFallbackSkus.FirstOrDefault(s => s.VolumePerUnit.HasValue);
                if (vpuSku != null)
                    dto.VolumePerUnit = vpuSku.VolumePerUnit;
            }
            dto.StorageAvailableQuantity = product.BaseUnitId.HasValue && product.PackagingQuantity > 0
                ? product.AvailableQuantity * product.PackagingQuantity.Value
                : product.AvailableQuantity;
            dto.ConsumableImportedQuantity = consumableByProduct.GetValueOrDefault(product.Id);
            dto.WarehouseName = productWarehouseMap.GetValueOrDefault(product.Id);

            // Nearest-expiry lot data
            if (earliestBatchMap.TryGetValue(product.Id, out var batch))
            {
                dto.EarliestReceivedDate = batch.ActualImportDate;
                dto.EarliestLotNumber = batch.LotNumber;
                dto.EarliestLotExpiryDate = batch.LotExpiryDate;
                dto.EarliestLotNotes = batch.Notes;
            }

            // Usage
            var consumed = usageByProduct.GetValueOrDefault(product.Id);
            dto.TotalConsumedQuantity = consumed;
            dto.StorageTotalConsumedQuantity = product.BaseUnitId.HasValue && product.PackagingQuantity > 0
                ? consumed * product.PackagingQuantity.Value
                : consumed;

            // Theoretical consumption & expected stock
            dto.TheoreticalConsumption = theoreticalByProduct.GetValueOrDefault(product.Id);
            dto.ExpectedStockConsumable = dto.ConsumableImportedQuantity - dto.TheoreticalConsumption;

            // Map attachments
            if (attachmentMap.TryGetValue(product.Id, out var productAttachments))
            {
                dto.Attachments = productAttachments.Select(a => new ProductAttachmentDto
                {
                    Id = a.Id,
                    FileId = a.FileId,
                    FileName = a.FileName,
                    FileSize = a.FileSize,
                    FileType = a.FileType,
                    Url = $"/api/v1/files/{a.FileId}",
                    CreationTime = a.CreationTime,
                }).ToList();
            }

            // Map unit conversions
            if (conversionMap.TryGetValue(product.Id, out var productConversions))
            {
                dto.UnitConversions = productConversions.Select(c => new ProductUnitConversionDto
                {
                    Id = c.Id,
                    Quantity = c.Quantity,
                    BaseUnitId = c.BaseUnitId,
                    BaseUnitName = unitMap.GetValueOrDefault(c.BaseUnitId),
                    ConvertedQuantity = c.ConvertedQuantity,
                    ConvertedUnitId = c.ConvertedUnitId,
                    ConvertedUnitName = unitMap.GetValueOrDefault(c.ConvertedUnitId),
                }).ToList();
            }

            // Map SKUs
            if (skuMap.TryGetValue(product.Id, out var productSkus))
            {
                dto.SkuCount = productSkus.Count;
                dto.Skus = productSkus.Select(s =>
                {
                    var mfr = manufacturerMap.GetValueOrDefault(s.ManufacturerId);
                    return new ProductSkuDto
                    {
                        Id = s.Id,
                        ManufacturerId = s.ManufacturerId,
                        ManufacturerCode = mfr?.Code,
                        ManufacturerName = mfr?.Name,
                        BaseUnitId = s.BaseUnitId,
                        BaseUnitName = s.BaseUnitId.HasValue ? unitMap.GetValueOrDefault(s.BaseUnitId.Value) : null,
                        PackagingQuantity = s.PackagingQuantity,
                        PackagingUnitId = s.PackagingUnitId,
                        PackagingUnitName = s.PackagingUnitId.HasValue ? unitMap.GetValueOrDefault(s.PackagingUnitId.Value) : null,
                        VolumePerUnit = s.VolumePerUnit,
                        VolumeUnitId = s.VolumeUnitId,
                        VolumeUnitName = s.VolumeUnitId.HasValue ? unitMap.GetValueOrDefault(s.VolumeUnitId.Value) : null,
                        CreationTime = s.CreationTime,
                    };
                }).ToList();
            }

            return dto;
        }).ToList();
    }

    private static void ValidateUniqueManufacturerIds(List<CreateUpdateProductSkuDto> skus)
    {
        var duplicates = skus
            .GroupBy(s => s.ManufacturerId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Any())
        {
            throw new UserFriendlyException("Mỗi hãng sản xuất chỉ được chọn một lần cho mỗi sản phẩm.");
        }
    }

    #endregion

    public async Task<PagedResultDto<ProductUsageLogItemDto>> GetUsageLogAsync(Guid id, GetProductUsageLogInput input)
    {
        var testBatchConsumptionQuery = await _testBatchConsumptionRepository.GetQueryableAsync();
        var indicatorConsumptionQuery = await _indicatorConsumptionRepository.GetQueryableAsync();
        var batchQuery = await _testBatchRepository.GetQueryableAsync();
        var itemQuery = await _testBatchItemRepository.GetQueryableAsync();
        var ltiQuery = await _ltiRepository.GetQueryableAsync();
        var trsiQuery = await _trsiRepository.GetQueryableAsync();
        var sampleQuery = await _sampleRepository.GetQueryableAsync();
        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();
        var methodQuery = await _testMethodRepository.GetQueryableAsync();
        var specimenTypeQuery = await _specimenTypeRepository.GetQueryableAsync();

        // Consumption filter: only the selected warehouse, not subtree
        HashSet<Guid>? usageLogWarehouseIds = null;
        if (input.WarehouseId.HasValue)
            usageLogWarehouseIds = new HashSet<Guid> { input.WarehouseId.Value };

        // Path 1: TestBatchConsumption → TestBatch → TestBatchItem (batch flow)
        var tbcFiltered = testBatchConsumptionQuery.Where(c => c.ProductId == id);
        if (usageLogWarehouseIds != null)
            tbcFiltered = tbcFiltered.Where(c => c.WarehouseId.HasValue && usageLogWarehouseIds.Contains(c.WarehouseId.Value));

        var batchIds = tbcFiltered
            .Select(c => c.TestBatchId)
            .Distinct();

        var fromBatchPath = (
            from batch in batchQuery
            where batchIds.Contains(batch.Id)
               && (!input.DateFrom.HasValue || (batch.ConsumptionConfirmedDate.HasValue && batch.ConsumptionConfirmedDate >= input.DateFrom))
               && (!input.DateTo.HasValue || (batch.ConsumptionConfirmedDate.HasValue && batch.ConsumptionConfirmedDate <= input.DateTo.Value.AddDays(1)))
            join item in itemQuery on batch.Id equals item.TestBatchId
            where item.IsSubmitted
            join lti in ltiQuery on item.LaboratoryTaskIndicatorId equals lti.Id
            join trsi in trsiQuery on lti.TestRequestSampleIndicatorId equals trsi.Id
            join sample in sampleQuery on trsi.TestRequestSampleId equals sample.Id
            join ind in indicatorQuery on trsi.IndicatorId equals ind.Id
            join method in methodQuery on lti.MethodId equals method.Id into methods
            from method in methods.DefaultIfEmpty()
            join st in specimenTypeQuery on trsi.SpecimenTypeId equals st.Id into sts
            from st in sts.DefaultIfEmpty()
            select new UsageLogRawItem
            {
                LtiId = lti.Id,
                SampleCode = item.SampleCode ?? sample.SampleCode,
                SampleName = sample.SampleName,
                SampleMatrix = st != null ? st.Name : null,
                IndicatorName = ind.Name,
                TestMethodName = method != null ? method.Name : (lti.SubContractMethodName ?? ""),
                TechnicianId = lti.TechnicianId,
                Result = item.Result ?? lti.Result,
                ResultSubmissionDate = batch.ConsumptionConfirmedDate,
            }
        ).ToList();

        // Path 2: IndicatorConsumption → LaboratoryTaskIndicator (direct indicator submit flow)
        // One row per (LtiId, SubmitDate) so each re-submit shows as a separate row
        var icFiltered = indicatorConsumptionQuery.Where(c => c.ProductId == id);
        if (usageLogWarehouseIds != null)
            icFiltered = icFiltered.Where(c => c.WarehouseId.HasValue && usageLogWarehouseIds.Contains(c.WarehouseId.Value));

        var icSubmissions = icFiltered
            .Select(c => new { c.LaboratoryTaskIndicatorId, c.SubmitDate })
            .Distinct()
            .ToList();

        if (input.DateFrom.HasValue)
            icSubmissions = icSubmissions
                .Where(s => s.SubmitDate.HasValue && s.SubmitDate >= input.DateFrom.Value)
                .ToList();
        if (input.DateTo.HasValue)
            icSubmissions = icSubmissions
                .Where(s => s.SubmitDate.HasValue && s.SubmitDate <= input.DateTo.Value.AddDays(1))
                .ToList();

        var ltiIdsFromIC = icSubmissions.Select(s => s.LaboratoryTaskIndicatorId).Distinct().ToList();

        var ltiDetailMap = (
            from lti in ltiQuery
            where ltiIdsFromIC.Contains(lti.Id)
            join trsi in trsiQuery on lti.TestRequestSampleIndicatorId equals trsi.Id
            join sample in sampleQuery on trsi.TestRequestSampleId equals sample.Id
            join ind in indicatorQuery on trsi.IndicatorId equals ind.Id
            join method in methodQuery on lti.MethodId equals method.Id into methods
            from method in methods.DefaultIfEmpty()
            join st in specimenTypeQuery on trsi.SpecimenTypeId equals st.Id into sts
            from st in sts.DefaultIfEmpty()
            select new
            {
                LtiId = lti.Id,
                SampleCode = sample.SampleCode,
                SampleName = sample.SampleName,
                SampleMatrix = st != null ? st.Name : null,
                IndicatorName = ind.Name,
                TestMethodName = method != null ? method.Name : (lti.SubContractMethodName ?? ""),
                TechnicianId = lti.TechnicianId,
                Result = lti.Result,
            }
        ).ToDictionary(x => x.LtiId);

        var fromIndicatorPath = icSubmissions
            .Where(s => ltiDetailMap.ContainsKey(s.LaboratoryTaskIndicatorId))
            .Select(s =>
            {
                var d = ltiDetailMap[s.LaboratoryTaskIndicatorId];
                return new UsageLogRawItem
                {
                    LtiId = d.LtiId,
                    SubmitDate = s.SubmitDate,
                    SampleCode = d.SampleCode,
                    SampleName = d.SampleName,
                    SampleMatrix = d.SampleMatrix,
                    IndicatorName = d.IndicatorName,
                    TestMethodName = d.TestMethodName,
                    TechnicianId = d.TechnicianId,
                    Result = d.Result,
                    ResultSubmissionDate = s.SubmitDate,
                };
            })
            .ToList();

        // Merge both paths. Batch path takes priority — exclude indicator path entries whose LtiId appears in batch path
        var batchLtiIds = new HashSet<Guid>(fromBatchPath.Select(x => x.LtiId));
        var merged = fromBatchPath
            .Concat(fromIndicatorPath.Where(x => !batchLtiIds.Contains(x.LtiId)))
            .OrderByDescending(x => x.ResultSubmissionDate)
            .ToList();

        var totalCount = merged.Count;

        // Enrich consumption amounts for ALL entries (needed for running balance)
        var icAllQuery = (await _indicatorConsumptionRepository.GetQueryableAsync())
            .Where(c => c.ProductId == id);
        if (usageLogWarehouseIds != null)
            icAllQuery = icAllQuery.Where(c => c.WarehouseId.HasValue && usageLogWarehouseIds.Contains(c.WarehouseId.Value));
        var icAll = icAllQuery.ToList();

        var icByLtiSubmit = icAll
            .GroupBy(c => (c.LaboratoryTaskIndicatorId, c.SubmitDate))
            .ToDictionary(
                g => g.Key,
                g => (Quantity: g.Sum(c => c.ActualQuantity), UnitId: g.First().UnitId));

        var unitIdsNeeded = icByLtiSubmit.Values
            .Where(v => v.UnitId.HasValue)
            .Select(v => v.UnitId!.Value)
            .Distinct()
            .ToList();

        var product = await _productRepository.GetAsync(id);
        var productSkusForLog = (await _skuRepository.GetQueryableAsync())
            .Where(s => s.ProductId == id).ToList();

        var volumeUnitId = product.VolumeUnitId
            ?? productSkusForLog.FirstOrDefault(s => s.VolumeUnitId.HasValue)?.VolumeUnitId;
        if (volumeUnitId.HasValue && !unitIdsNeeded.Contains(volumeUnitId.Value))
            unitIdsNeeded.Add(volumeUnitId.Value);

        var unitNames = new Dictionary<Guid, string>();
        if (unitIdsNeeded.Any())
        {
            var uq = await _unitRepository.GetQueryableAsync();
            unitNames = uq.Where(u => unitIdsNeeded.Contains(u.Id))
                          .ToDictionary(u => u.Id, u => u.Name);
        }

        string? consumableUnitName = volumeUnitId.HasValue
            ? unitNames.GetValueOrDefault(volumeUnitId.Value)
            : null;

        foreach (var r in merged)
        {
            if (icByLtiSubmit.TryGetValue((r.LtiId, r.SubmitDate), out var ic))
            {
                r.ConsumptionAmount = ic.Quantity;
                r.ConsumptionUnitName = ic.UnitId.HasValue
                    ? unitNames.GetValueOrDefault(ic.UnitId.Value)
                    : null;
            }
            else if (r.SubmitDate == null)
            {
                // Batch path or old records without SubmitDate: sum all consumption for this LTI
                var ltiConsumptions = icAll.Where(c => c.LaboratoryTaskIndicatorId == r.LtiId).ToList();
                if (ltiConsumptions.Any())
                {
                    r.ConsumptionAmount = ltiConsumptions.Sum(c => c.ActualQuantity);
                    var unitId = ltiConsumptions.First().UnitId;
                    r.ConsumptionUnitName = unitId.HasValue
                        ? unitNames.GetValueOrDefault(unitId.Value)
                        : null;
                }
            }
        }

        // Compute current consumable stock (in volume units) for running balance
        var logImportItemQ = await _warehouseImportReceiptItemRepository.GetQueryableAsync();
        var logImportReceiptQ = await _warehouseImportReceiptRepository.GetQueryableAsync();

        var lotQuery = from item in logImportItemQ
                       join receipt in logImportReceiptQ on item.WarehouseImportReceiptId equals receipt.Id
                       where item.ProductId == id
                             && receipt.Status == WarehouseImportReceiptStatus.Completed
                             && item.Status == WarehouseImportLineStatus.Checked
                       select new { item.ReceivedBaseQuantity, item.ConsumedBaseQuantity, item.SkuId, item.StorageWarehouseId, receipt.WarehouseId };

        if (usageLogWarehouseIds != null)
        {
            lotQuery = lotQuery.Where(x =>
                (x.StorageWarehouseId.HasValue && usageLogWarehouseIds.Contains(x.StorageWarehouseId.Value))
                || (!x.StorageWarehouseId.HasValue && x.WarehouseId.HasValue && usageLogWarehouseIds.Contains(x.WarehouseId.Value)));
        }

        var lotItems = lotQuery.ToList();

        decimal currentConsumable = 0;
        foreach (var lot in lotItems)
        {
            var remaining = lot.ReceivedBaseQuantity - lot.ConsumedBaseQuantity;
            if (remaining <= 0) continue;

            int? vpu = null;
            if (lot.SkuId.HasValue)
            {
                var sku = productSkusForLog.FirstOrDefault(s => s.Id == lot.SkuId.Value);
                if (sku?.VolumePerUnit > 0) vpu = sku.VolumePerUnit;
            }
            if (!vpu.HasValue && product.VolumePerUnit > 0)
                vpu = product.VolumePerUnit;

            if (vpu.HasValue && vpu.Value > 0)
                currentConsumable += remaining * vpu.Value;
        }

        // Running balance: merged is sorted DESC (newest first), accumulate backwards
        if (currentConsumable > 0 && consumableUnitName != null)
        {
            decimal running = currentConsumable;
            for (int i = 0; i < merged.Count; i++)
            {
                merged[i].ConsumableStockBefore = running;
                merged[i].ConsumableStockUnitName = consumableUnitName;
                if (merged[i].ConsumptionAmount.HasValue && merged[i].ConsumptionAmount > 0)
                    running += merged[i].ConsumptionAmount.Value;
            }
        }

        // Paginate
        var rawResults = merged
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        // Enrich technician names (page only)
        await _userNameProvider.EnrichWithUserNamesAsync(
            rawResults,
            r => r.TechnicianId,
            (r, name) => r.TechnicianName = name);

        var items = rawResults.Select(r => new ProductUsageLogItemDto
        {
            SampleCode = r.SampleCode ?? "",
            SampleName = r.SampleName,
            SampleMatrix = r.SampleMatrix,
            IndicatorName = r.IndicatorName,
            TestMethodName = r.TestMethodName,
            TechnicianName = r.TechnicianName,
            Result = r.Result,
            ResultSubmissionDate = r.ResultSubmissionDate,
            ConsumableStockBefore = r.ConsumableStockBefore,
            ConsumableStockUnitName = r.ConsumableStockUnitName,
            ConsumptionAmount = r.ConsumptionAmount,
            ConsumptionUnitName = r.ConsumptionUnitName,
        }).ToList();

        return new PagedResultDto<ProductUsageLogItemDto>(totalCount, items);
    }

    private static List<string> DeserializeBarcodes(string? barcodeValues)
    {
        if (string.IsNullOrWhiteSpace(barcodeValues))
            return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(barcodeValues) ?? new List<string>();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }

    private class MaterialReceiptHistoryGroupRow
    {
        public Guid MaterialReceiptId { get; set; }
        public string ReceiptCode { get; set; } = string.Empty;
        public DateTime ReceiptDate { get; set; }
        public DateTime CreationTime { get; set; }
        public Guid ReceivingWarehouseId { get; set; }
        public string? ReceivingWarehouseName { get; set; }
        public Guid? SourceWarehouseId { get; set; }
        public string? SourceWarehouseName { get; set; }
        public decimal ReceivedQuantity { get; set; }
        public string? UnitName { get; set; }
        public Guid? ManufacturerId { get; set; }
        public string? ManufacturerName { get; set; }
        public string? LotNumber { get; set; }
        public DateTime? LotExpiryDate { get; set; }
        public Guid? StorageUnitId { get; set; }
        public Guid? ConsumableUnitId { get; set; }
        public int? VolumePerUnit { get; set; }
    }

    private class UsageLogRawItem
    {
        public Guid LtiId { get; set; }
        public DateTime? SubmitDate { get; set; }
        public string? SampleCode { get; set; }
        public string SampleName { get; set; } = "";
        public string? SampleMatrix { get; set; }
        public string IndicatorName { get; set; } = "";
        public string TestMethodName { get; set; } = "";
        public Guid? TechnicianId { get; set; }
        public string? TechnicianName { get; set; }
        public string? Result { get; set; }
        public DateTime? ResultSubmissionDate { get; set; }
        public decimal? ConsumableStockBefore { get; set; }
        public string? ConsumableStockUnitName { get; set; }
        public decimal? ConsumptionAmount { get; set; }
        public string? ConsumptionUnitName { get; set; }
    }
}
