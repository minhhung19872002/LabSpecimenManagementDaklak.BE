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
        IDataFilter dataFilter)
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
    }

    public async Task<PagedResultDto<ProductDto>> GetListAsync(ProductFilterDto input)
    {
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
            var warehouseQuery = await _warehouseRepository.GetQueryableAsync();
            var allWarehouses = warehouseQuery.Where(w => w.IsActive)
                .Select(w => new { w.Id, w.ParentWarehouseId }).ToList();

            var warehouseIds = new HashSet<Guid> { input.WarehouseId.Value };
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

            var productIdsInWarehouse = importItemQuery
                .Where(i => importReceiptQuery
                    .Where(r => warehouseIds.Contains(r.WarehouseId!.Value))
                    .Select(r => r.Id)
                    .Contains(i.WarehouseImportReceiptId))
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
            StorageConditions = input.StorageConditions,
            FormattedStorageConditions = input.FormattedStorageConditions,
            Categories = input.Categories,
            ChemicalFormula = input.ChemicalFormula,
            MinStockLevel = input.MinStockLevel,
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
        product.StorageConditions = input.StorageConditions;
        product.FormattedStorageConditions = input.FormattedStorageConditions;
        product.Categories = input.Categories;
        product.ChemicalFormula = input.ChemicalFormula;
        product.MinStockLevel = input.MinStockLevel;
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

    #region Private Methods

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
