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
    private readonly IRepository<Unit, Guid> _unitRepository;
    private readonly IRepository<GoodsReceiptItem, Guid> _goodsReceiptItemRepository;
    private readonly IRepository<GoodsReceipt, Guid> _goodsReceiptRepository;
    private readonly IRepository<Supplier, Guid> _supplierRepository;
    private readonly IRepository<WarehouseImportReceiptItem, Guid> _warehouseImportReceiptItemRepository;
    private readonly IRepository<WarehouseImportReceipt, Guid> _warehouseImportReceiptRepository;
    private readonly ICodeGeneratorService _codeGenerator;
    private readonly IDataFilter _dataFilter;

    public ProductAppService(
        IRepository<Product, Guid> productRepository,
        IRepository<ProductAttachment, Guid> attachmentRepository,
        IRepository<ProductUnitConversion, Guid> conversionRepository,
        IRepository<Unit, Guid> unitRepository,
        IRepository<GoodsReceiptItem, Guid> goodsReceiptItemRepository,
        IRepository<GoodsReceipt, Guid> goodsReceiptRepository,
        IRepository<Supplier, Guid> supplierRepository,
        IRepository<WarehouseImportReceiptItem, Guid> warehouseImportReceiptItemRepository,
        IRepository<WarehouseImportReceipt, Guid> warehouseImportReceiptRepository,
        ICodeGeneratorService codeGenerator,
        IDataFilter dataFilter)
    {
        _productRepository = productRepository;
        _attachmentRepository = attachmentRepository;
        _conversionRepository = conversionRepository;
        _unitRepository = unitRepository;
        _goodsReceiptItemRepository = goodsReceiptItemRepository;
        _goodsReceiptRepository = goodsReceiptRepository;
        _supplierRepository = supplierRepository;
        _warehouseImportReceiptItemRepository = warehouseImportReceiptItemRepository;
        _warehouseImportReceiptRepository = warehouseImportReceiptRepository;
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

        var totalCount = query.Count();

        // Apply sorting
        var importItemQuery = await _warehouseImportReceiptItemRepository.GetQueryableAsync();
        var importReceiptQuery = await _warehouseImportReceiptRepository.GetQueryableAsync();

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
            StorageConditions = input.StorageConditions,
            FormattedStorageConditions = input.FormattedStorageConditions,
            Categories = input.Categories,
            MinStockLevel = input.MinStockLevel,
            MaxStockLevel = input.MaxStockLevel,
            IsActive = input.IsActive,
            IsDraft = input.IsDraft,
        };

        await _productRepository.InsertAsync(product, autoSave: true);

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
        product.StorageConditions = input.StorageConditions;
        product.FormattedStorageConditions = input.FormattedStorageConditions;
        product.Categories = input.Categories;
        product.MinStockLevel = input.MinStockLevel;
        product.MaxStockLevel = input.MaxStockLevel;
        product.IsActive = input.IsActive;
        product.IsDraft = input.IsDraft;

        await _productRepository.UpdateAsync(product, autoSave: true);

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

            return dto;
        }).ToList();
    }

    #endregion
}
