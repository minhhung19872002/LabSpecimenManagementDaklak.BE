using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.GoodsReceipts;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.Suppliers;
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
        ICodeGeneratorService codeGenerator,
        IDataFilter dataFilter,
        IUnitConversionResolver unitConversionResolver)
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
        _codeGenerator = codeGenerator;
        _dataFilter = dataFilter;
        _unitConversionResolver = unitConversionResolver;
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

        // Filter by Warehouse (includes child warehouses)
        var importItemQuery = await _warehouseImportReceiptItemRepository.GetQueryableAsync();
        var importReceiptQuery = await _warehouseImportReceiptRepository.GetQueryableAsync();

        if (input.WarehouseId.HasValue)
        {
            var warehouseIds = await GetWarehouseSubtreeIdsAsync(input.WarehouseId.Value);

            var receiptIdsInWarehouse = importReceiptQuery
                .Where(r => r.WarehouseId.HasValue && warehouseIds.Contains(r.WarehouseId.Value))
                .Select(r => r.Id);

            var productIdsInWarehouse = importItemQuery
                .Where(i =>
                    (i.StorageWarehouseId.HasValue && warehouseIds.Contains(i.StorageWarehouseId.Value)) ||
                    receiptIdsInWarehouse.Contains(i.WarehouseImportReceiptId))
                .Select(i => i.ProductId)
                .Distinct();

            query = query.Where(x => productIdsInWarehouse.Contains(x.Id));
        }

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
        // 1. BFS warehouse hierarchy
        var warehouseQuery = await _warehouseRepository.GetQueryableAsync();
        var allWarehouses = warehouseQuery.Where(w => w.IsActive)
            .Select(w => new { w.Id, w.ParentWarehouseId, w.Name }).ToList();

        var warehouseIds = new HashSet<Guid> { input.WarehouseId!.Value };
        var queue = new Queue<Guid>();
        queue.Enqueue(input.WarehouseId.Value);
        while (queue.Count > 0)
        {
            var parentId = queue.Dequeue();
            foreach (var child in allWarehouses.Where(w => w.ParentWarehouseId == parentId))
            {
                if (warehouseIds.Add(child.Id))
                    queue.Enqueue(child.Id);
            }
        }

        var warehouseNameMap = allWarehouses
            .Where(w => warehouseIds.Contains(w.Id))
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

        // 5. Convert each import item to base units + volume units, grouped by (ProductId, WarehouseId)
        var entries = new List<WarehouseStockEntry>();

        foreach (var item in filteredImportItems)
        {
            var product = productMap[item.ProductId];
            decimal baseQty;
            try
            {
                if (item.SkuId.HasValue && skuLookup.TryGetValue(item.SkuId.Value, out var sku))
                    baseQty = ResolveSkuQuantity(sku, item.UnitId, item.ActualReceivedQuantity);
                else
                    baseQty = await _unitConversionResolver.ResolveBaseQuantityAsync(
                        product, item.UnitId, item.ActualReceivedQuantity, item.ProductName ?? product.Name);
            }
            catch
            {
                baseQty = item.ActualReceivedQuantity;
            }

            decimal volumeQty = ComputeVolumeQty(
                item.SkuId.HasValue && skuLookup.TryGetValue(item.SkuId.Value, out var s2) ? s2 : null,
                product, item.UnitId, item.ActualReceivedQuantity);

            var whName = warehouseNameMap.GetValueOrDefault(
                item.EffectiveWarehouseId!.Value,
                item.StorageWarehouseName ?? item.ReceiptWarehouseName ?? "");

            entries.Add(new WarehouseStockEntry
            {
                ProductId = item.ProductId,
                WarehouseId = item.EffectiveWarehouseId!.Value,
                WarehouseName = whName,
                BaseQty = baseQty,
                VolumeQty = volumeQty
            });
        }

        // 6. Group by ProductId only — same product in multiple child warehouses → one row, quantities summed
        var grouped = entries
            .GroupBy(x => x.ProductId)
            .Select(g =>
            {
                var warehouseNames = g.Select(x => x.WarehouseName).Distinct().ToList();
                return new WarehouseStockEntry
                {
                    ProductId = g.Key,
                    WarehouseId = g.First().WarehouseId,
                    WarehouseName = string.Join(", ", warehouseNames),
                    BaseQty = g.Sum(x => x.BaseQty),
                    VolumeQty = g.Sum(x => x.VolumeQty)
                };
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

        for (int i = 0; i < dtos.Count; i++)
        {
            var entry = pagedItems[i];
            var product = productsForPage[i];
            dtos[i].AvailableQuantity = entry.BaseQty;
            dtos[i].StorageAvailableQuantity = product.BaseUnitId.HasValue && product.PackagingQuantity > 0
                ? entry.BaseQty * product.PackagingQuantity.Value
                : entry.BaseQty;
            dtos[i].ConsumableImportedQuantity = entry.VolumeQty;
            dtos[i].WarehouseName = entry.WarehouseName;
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

        var unitQuery = await _unitRepository.GetQueryableAsync();
        var unitIds = new[] { product.BaseUnitId, product.PackagingUnitId }
            .Where(uid => uid.HasValue).Select(uid => uid!.Value).Distinct().ToList();
        var unitMap = unitQuery.Where(u => unitIds.Contains(u.Id))
            .ToDictionary(u => u.Id, u => u.Name);

        decimal warehouseQty = product.AvailableQuantity;

        if (warehouseId.HasValue)
        {
            var warehouseIds = await GetWarehouseSubtreeIdsAsync(warehouseId.Value);

            var importItemQuery = await _warehouseImportReceiptItemRepository.GetQueryableAsync();
            var importReceiptQuery = await _warehouseImportReceiptRepository.GetQueryableAsync();

            var importItems = (from item in importItemQuery
                               join receipt in importReceiptQuery on item.WarehouseImportReceiptId equals receipt.Id
                               where item.ProductId == id
                                     && receipt.Status == WarehouseImportReceiptStatus.Completed
                                     && item.Status == WarehouseImportLineStatus.Checked
                               where (item.StorageWarehouseId.HasValue && warehouseIds.Contains(item.StorageWarehouseId.Value))
                                     || (!item.StorageWarehouseId.HasValue && receipt.WarehouseId.HasValue && warehouseIds.Contains(receipt.WarehouseId.Value))
                               select new { item.ActualReceivedQuantity, item.UnitId, item.SkuId })
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
                    try
                    {
                        if (item.SkuId.HasValue && skuLookup.TryGetValue(item.SkuId.Value, out var sku))
                            warehouseQty += ResolveSkuQuantity(sku, item.UnitId, item.ActualReceivedQuantity);
                        else
                            warehouseQty += await _unitConversionResolver.ResolveBaseQuantityAsync(
                                product, item.UnitId, item.ActualReceivedQuantity, product.Name);
                    }
                    catch
                    {
                        warehouseQty += item.ActualReceivedQuantity;
                    }
                }
            }
            else
            {
                warehouseQty = 0;
            }
        }

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
            PackagingUnitName = product.PackagingUnitId.HasValue ? unitMap.GetValueOrDefault(product.PackagingUnitId.Value) : null,
            PackagingQuantity = product.PackagingQuantity,
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
            var warehouseIds = await GetWarehouseSubtreeIdsAsync(input.WarehouseId.Value);
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

        var goodsReceiptItemIds = rawItems.Select(x => x.GoodsReceiptItemId).Distinct().ToList();
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
            if (grItemLookup.TryGetValue(x.GoodsReceiptItemId, out var grItem))
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

    #region Private Methods

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
        var earliestBatchMap = allImportItems
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.ActualImportDate).First());

        // Usage aggregation: sum consumed quantity per product
        var movementQuery = await _stockMovementLogRepository.GetQueryableAsync();
        var usageByProduct = movementQuery
            .Where(l => productIds.Contains(l.ProductId) && l.MovementType == StockMovementType.MaterialConsumption)
            .GroupBy(l => l.ProductId)
            .Select(g => new { ProductId = g.Key, Total = g.Sum(l => Math.Abs(l.ChangeQuantity)) })
            .ToList()
            .ToDictionary(x => x.ProductId, x => x.Total);

        // Consumable quantity from completed imports (per-SKU accurate)
        var consumableImportItems = (from item in importItemQuery
                                     join receipt in importReceiptQuery on item.WarehouseImportReceiptId equals receipt.Id
                                     where productIds.Contains(item.ProductId)
                                           && receipt.Status == WarehouseImportReceiptStatus.Completed
                                           && item.Status == WarehouseImportLineStatus.Checked
                                     select new
                                     {
                                         item.ProductId,
                                         item.SkuId,
                                         item.UnitId,
                                         item.ActualReceivedQuantity
                                     })
                                     .ToList();
        var consumableByProduct = new Dictionary<Guid, decimal>();
        foreach (var ci in consumableImportItems)
        {
            decimal cQty = 0;
            ProductSku? sku = null;
            if (ci.SkuId.HasValue && skuMap.TryGetValue(ci.ProductId, out var pSkus))
                sku = pSkus.FirstOrDefault(s => s.Id == ci.SkuId.Value);

            if (sku?.VolumePerUnit > 0)
            {
                if (ci.UnitId == sku.VolumeUnitId)
                    cQty = ci.ActualReceivedQuantity;
                else if (ci.UnitId == sku.PackagingUnitId)
                    cQty = ci.ActualReceivedQuantity * sku.VolumePerUnit.Value;
                else if (ci.UnitId == sku.BaseUnitId && sku.BaseUnitId != sku.PackagingUnitId)
                    cQty = ci.ActualReceivedQuantity * (sku.PackagingQuantity ?? 1) * sku.VolumePerUnit.Value;
                else
                    cQty = ci.ActualReceivedQuantity * sku.VolumePerUnit.Value;
            }
            else
            {
                var prod = products.FirstOrDefault(p => p.Id == ci.ProductId);
                if (prod?.VolumePerUnit > 0)
                {
                    if (ci.UnitId == prod.VolumeUnitId)
                        cQty = ci.ActualReceivedQuantity;
                    else if (ci.UnitId == prod.PackagingUnitId)
                        cQty = ci.ActualReceivedQuantity * prod.VolumePerUnit.Value;
                    else if (ci.UnitId == prod.BaseUnitId && prod.BaseUnitId != prod.PackagingUnitId)
                        cQty = ci.ActualReceivedQuantity * (prod.PackagingQuantity ?? 1) * prod.VolumePerUnit.Value;
                    else
                        cQty = ci.ActualReceivedQuantity * prod.VolumePerUnit.Value;
                }
            }

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

        return products.Select(product =>
        {
            var dto = ObjectMapper.Map<Product, ProductDto>(product);
            dto.BaseUnitName = product.BaseUnitId.HasValue ? unitMap.GetValueOrDefault(product.BaseUnitId.Value) : null;
            dto.PackagingUnitName = product.PackagingUnitId.HasValue ? unitMap.GetValueOrDefault(product.PackagingUnitId.Value) : null;
            dto.VolumeUnitName = product.VolumeUnitId.HasValue ? unitMap.GetValueOrDefault(product.VolumeUnitId.Value) : null;
            dto.StorageAvailableQuantity = product.BaseUnitId.HasValue && product.PackagingQuantity > 0
                ? product.AvailableQuantity * product.PackagingQuantity.Value
                : product.AvailableQuantity;
            dto.ConsumableImportedQuantity = consumableByProduct.GetValueOrDefault(product.Id);
            dto.WarehouseName = productWarehouseMap.GetValueOrDefault(product.Id);

            // FIFO batch data
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
}
