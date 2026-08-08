using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.Equipments;
using LabSpecimenManagement.GoodsReceipts;
using LabSpecimenManagement.Suppliers;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.Products;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.WarehouseImports;

[RemoteService(IsEnabled = false)]
[Authorize(LabSpecimenManagementPermissions.WarehouseImportReceipts.Default)]
public class WarehouseImportReceiptAppService : ApplicationService, IWarehouseImportReceiptAppService
{
    private readonly IRepository<WarehouseImportReceipt, Guid> _receiptRepository;
    private readonly IRepository<WarehouseImportReceiptItem, Guid> _itemRepository;
    private readonly IRepository<GoodsReceipt, Guid> _goodsReceiptRepository;
    private readonly IRepository<GoodsReceiptItem, Guid> _goodsReceiptItemRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<Warehouse, Guid> _warehouseRepository;
    private readonly IRepository<Unit, Guid> _unitRepository;
    private readonly IRepository<Supplier, Guid> _supplierRepository;
    private readonly ICodeGeneratorService _codeGenerator;
    private readonly IDataFilter _dataFilter;
    private readonly IUserNameProvider _userNameProvider;
    private readonly IUnitConversionResolver _unitConversionResolver;
    private readonly IRepository<ProductSku, Guid> _productSkuRepository;
    private readonly IRepository<Manufacturer, Guid> _manufacturerRepository;
    private readonly IRepository<Equipment, Guid> _equipmentRepository;
    private readonly IRepository<InventoryBarcode, Guid> _inventoryBarcodeRepository;

    public WarehouseImportReceiptAppService(
        IRepository<WarehouseImportReceipt, Guid> receiptRepository,
        IRepository<WarehouseImportReceiptItem, Guid> itemRepository,
        IRepository<GoodsReceipt, Guid> goodsReceiptRepository,
        IRepository<GoodsReceiptItem, Guid> goodsReceiptItemRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<Warehouse, Guid> warehouseRepository,
        IRepository<Unit, Guid> unitRepository,
        IRepository<Supplier, Guid> supplierRepository,
        ICodeGeneratorService codeGenerator,
        IDataFilter dataFilter,
        IUserNameProvider userNameProvider,
        IUnitConversionResolver unitConversionResolver,
        IRepository<ProductSku, Guid> productSkuRepository,
        IRepository<Manufacturer, Guid> manufacturerRepository,
        IRepository<Equipment, Guid> equipmentRepository,
        IRepository<InventoryBarcode, Guid> inventoryBarcodeRepository)
    {
        _receiptRepository = receiptRepository;
        _itemRepository = itemRepository;
        _goodsReceiptRepository = goodsReceiptRepository;
        _goodsReceiptItemRepository = goodsReceiptItemRepository;
        _productRepository = productRepository;
        _warehouseRepository = warehouseRepository;
        _unitRepository = unitRepository;
        _supplierRepository = supplierRepository;
        _codeGenerator = codeGenerator;
        _dataFilter = dataFilter;
        _userNameProvider = userNameProvider;
        _unitConversionResolver = unitConversionResolver;
        _productSkuRepository = productSkuRepository;
        _manufacturerRepository = manufacturerRepository;
        _equipmentRepository = equipmentRepository;
        _inventoryBarcodeRepository = inventoryBarcodeRepository;
    }

    private static readonly GoodsReceiptOrderStatus[] EligibleOrderStatuses =
    {
        GoodsReceiptOrderStatus.Ordering,
        GoodsReceiptOrderStatus.WaitingReceipt
    };

    public async Task<PagedResultDto<WarehouseImportReceiptListDto>> GetListAsync(GetWarehouseImportReceiptListInput input)
    {
        var query = (await _receiptRepository.GetQueryableAsync())
            .Where(x => x.ReceiptType == WarehouseImportReceiptType.Normal);
        var goodsReceiptQuery = await _goodsReceiptRepository.GetQueryableAsync();

        if (input.FromDate.HasValue)
        {
            var from = input.FromDate.Value.Date;
            query = query.Where(x => x.ActualImportDate >= from);
        }

        if (input.ToDate.HasValue)
        {
            var to = input.ToDate.Value.Date.AddDays(1);
            query = query.Where(x => x.ActualImportDate < to);
        }

        if (input.SupplierId.HasValue)
            query = query.Where(x => x.SupplierId == input.SupplierId);

        var goodsReceiptInfoById = goodsReceiptQuery.ToDictionary(x => x.Id, x => new { x.SupplierName, x.SupplierRepresentativeName });
        var supplierNameByGoodsReceiptId = goodsReceiptInfoById.ToDictionary(x => x.Key, x => x.Value.SupplierName);

        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var search = input.Search.Trim().ToLower();
            var matchingGoodsReceiptIds = supplierNameByGoodsReceiptId
                .Where(x => x.Value != null && x.Value.ToLower().Contains(search))
                .Select(x => x.Key)
                .ToList();

            query = query.Where(x =>
                x.Code.ToLower().Contains(search) ||
                (x.GoodsReceiptCode != null && x.GoodsReceiptCode.ToLower().Contains(search)) ||
                (x.GoodsReceiptId.HasValue && matchingGoodsReceiptIds.Contains(x.GoodsReceiptId.Value)));
        }

        if (input.DisplayStatus.HasValue && input.DisplayStatus.Value != WarehouseImportReceiptDisplayStatusFilter.All)
        {
            var allReceiptIds = query.Select(x => x.Id).ToList();
            var itemsQuery = await _itemRepository.GetQueryableAsync();
            var defectiveReceiptIds = itemsQuery
                .Where(x => allReceiptIds.Contains(x.WarehouseImportReceiptId) && x.Status == WarehouseImportLineStatus.Defective)
                .Select(x => x.WarehouseImportReceiptId)
                .Distinct()
                .ToList();

            query = input.DisplayStatus.Value switch
            {
                WarehouseImportReceiptDisplayStatusFilter.InProgress =>
                    query.Where(x => x.Status == WarehouseImportReceiptStatus.InProgress),
                WarehouseImportReceiptDisplayStatusFilter.Completed =>
                    query.Where(x => x.Status == WarehouseImportReceiptStatus.Completed && !defectiveReceiptIds.Contains(x.Id)),
                WarehouseImportReceiptDisplayStatusFilter.Defective =>
                    query.Where(x => x.Status == WarehouseImportReceiptStatus.Completed && defectiveReceiptIds.Contains(x.Id)),
                _ => query
            };
        }

        var totalCount = await AsyncExecuter.CountAsync(query);

        var items = await AsyncExecuter.ToListAsync(
            query.OrderByDescending(x => x.CreationTime).ApplyPaging(input));

        var dtos = ObjectMapper.Map<List<WarehouseImportReceipt>, List<WarehouseImportReceiptListDto>>(items);
        foreach (var dto in dtos)
        {
            if (dto.GoodsReceiptId.HasValue && goodsReceiptInfoById.TryGetValue(dto.GoodsReceiptId.Value, out var grInfo))
            {
                dto.SupplierName = grInfo.SupplierName;
                dto.ContactPersonName = grInfo.SupplierRepresentativeName;
            }
        }
        await EnrichListDtosAsync(items, dtos);

        return new PagedResultDto<WarehouseImportReceiptListDto>(totalCount, dtos);
    }

    public async Task<WarehouseImportReceiptStatisticsDto> GetStatisticsAsync(DateTime? fromDate, DateTime? toDate)
    {
        var today = Clock.Now.Date;
        var from = (fromDate ?? new DateTime(today.Year, today.Month, 1)).Date;
        var to = (toDate ?? today).Date.AddDays(1);

        var receiptQuery = await _receiptRepository.GetQueryableAsync();
        var receiptsInRange = receiptQuery
            .Where(x => x.ActualImportDate >= from && x.ActualImportDate < to)
            .ToList();

        var totalReceipts = receiptsInRange.Count;

        var receiptIdsInRange = receiptsInRange.Select(x => x.Id).ToList();
        var itemsQuery = await _itemRepository.GetQueryableAsync();
        var defectiveLots = itemsQuery
            .Count(x => receiptIdsInRange.Contains(x.WarehouseImportReceiptId) && x.Status == WarehouseImportLineStatus.Defective);

        return new WarehouseImportReceiptStatisticsDto
        {
            TotalReceipts = totalReceipts,
            DefectiveLots = defectiveLots
        };
    }

    public async Task<WarehouseImportReceiptDto> GetAsync(Guid id)
    {
        var receipt = await GetReceiptWithItemsAsync(id);
        return await MapToDetailDtoAsync(receipt);
    }

    [Authorize(LabSpecimenManagementPermissions.WarehouseImportReceipts.Create)]
    public async Task<WarehouseImportReceiptDto> CreateAsync(CreateUpdateWarehouseImportReceiptDto input)
    {
        var goodsReceipt = await _goodsReceiptRepository.GetAsync(input.GoodsReceiptId);
        if (!EligibleOrderStatuses.Contains(goodsReceipt.OrderStatus))
            throw new UserFriendlyException("Phiếu mua hàng không ở trạng thái cho phép nhập kho.");

        var code = await GenerateCodeAsync();
        var receipt = new WarehouseImportReceipt(GuidGenerator.Create(), code, goodsReceipt.Id, goodsReceipt.Code);
        await ApplyHeaderAsync(receipt, input);

        await _receiptRepository.InsertAsync(receipt, autoSave: true);
        await ReplaceItemsAsync(receipt, input.Items);

        return await GetAsync(receipt.Id);
    }

    [Authorize(LabSpecimenManagementPermissions.WarehouseImportReceipts.Create)]
    public async Task<WarehouseImportReceiptDto> UpdateAsync(Guid id, CreateUpdateWarehouseImportReceiptDto input)
    {
        var receipt = await _receiptRepository.GetAsync(id);
        if (receipt.Status != WarehouseImportReceiptStatus.InProgress)
            throw new UserFriendlyException("Phiếu đã hoàn thành, không thể chỉnh sửa.");

        await ApplyHeaderAsync(receipt, input);
        await _receiptRepository.UpdateAsync(receipt, autoSave: true);
        await ReplaceItemsAsync(receipt, input.Items);

        return await GetAsync(receipt.Id);
    }

    [Authorize(LabSpecimenManagementPermissions.WarehouseImportReceipts.Confirm)]
    public async Task<WarehouseImportReceiptDto> ConfirmAsync(Guid id)
    {
        var receipt = await GetReceiptWithItemsAsync(id);
        if (receipt.Status != WarehouseImportReceiptStatus.InProgress)
            throw new UserFriendlyException("Phiếu đã được xác nhận trước đó.");

        if (receipt.Items.Count == 0)
            throw new UserFriendlyException("Phiếu nhập kho chưa có vật tư nào.");

        var goodsReceipt = await _goodsReceiptRepository.GetAsync(receipt.GoodsReceiptId!.Value);
        var poLines = (await _goodsReceiptItemRepository.GetQueryableAsync())
            .Where(x => x.GoodsReceiptId == goodsReceipt.Id)
            .ToList();

        // Material items: validate quantities
        var materialItems = receipt.Items.Where(x => !x.IsEquipment).ToList();
        var checkedItemsByLine = materialItems
            .Where(x => x.Status == WarehouseImportLineStatus.Checked)
            .GroupBy(x => x.GoodsReceiptItemId!.Value);

        foreach (var group in checkedItemsByLine)
        {
            var poLine = poLines.FirstOrDefault(x => x.Id == group.Key);
            if (poLine == null)
                throw new UserFriendlyException("Không tìm thấy dòng hàng tương ứng trên phiếu mua hàng.");

            var cumulativeCompletedChecked = await GetCumulativeCheckedQtyAsync(poLine.Id, excludeReceiptId: receipt.Id);
            var remaining = poLine.Quantity - cumulativeCompletedChecked;

            // Convert each item's qty to order/purchase unit before summing
            var skuIdsInGroup = group.Where(x => x.SkuId.HasValue).Select(x => x.SkuId!.Value).Distinct().ToList();
            var skusInGroup = skuIdsInGroup.Count > 0
                ? (await _productSkuRepository.GetQueryableAsync()).Where(x => skuIdsInGroup.Contains(x.Id)).ToList()
                : new List<ProductSku>();
            var skuMapInGroup = skusInGroup.ToDictionary(x => x.Id);

            decimal sumCurrentChecked = 0;
            foreach (var item in group)
            {
                if (item.SkuId.HasValue && skuMapInGroup.TryGetValue(item.SkuId.Value, out var itemSku))
                    sumCurrentChecked += ResolveToOrderQuantity(itemSku, item.UnitId, item.ActualReceivedQuantity);
                else
                    sumCurrentChecked += item.ActualReceivedQuantity;
            }

            if (sumCurrentChecked > remaining)
                throw new UserFriendlyException($"Số lượng nhận của \"{poLine.ProductName}\" vượt quá số lượng còn lại có thể nhận ({remaining}).");
        }

        var defaultWarehouse = (await _warehouseRepository.GetQueryableAsync())
            .FirstOrDefault(x => x.IsDefault);

        foreach (var item in materialItems.Where(x => x.Status == WarehouseImportLineStatus.Checked))
        {
            var product = await _productRepository.GetAsync(item.ProductId);
            decimal baseQty;
            if (item.SkuId.HasValue)
            {
                var sku = await _productSkuRepository.GetAsync(item.SkuId.Value);
                baseQty = ResolveSkuQuantity(sku, item.UnitId, item.ActualReceivedQuantity);
                item.ManufacturerId ??= sku.ManufacturerId;
                if (string.IsNullOrWhiteSpace(item.ManufacturerName))
                {
                    var manufacturer = await _manufacturerRepository.FindAsync(sku.ManufacturerId);
                    item.ManufacturerName = manufacturer?.Name;
                }
            }
            else
            {
                baseQty = await _unitConversionResolver.ResolveBaseQuantityAsync(product, item.UnitId, item.ActualReceivedQuantity, item.ProductName);
            }
            product.AvailableQuantity += baseQty;
            await _productRepository.UpdateAsync(product, autoSave: true);

            item.ReceivedBaseQuantity = baseQty;

            if (defaultWarehouse != null)
            {
                item.StorageWarehouseId = defaultWarehouse.Id;
                item.StorageWarehouseName = defaultWarehouse.Name;
                var barcodeCount = Math.Max(1, (int)Math.Ceiling(baseQty));
                var barcodeCodes = new List<string>();
                for (var sequence = 1; sequence <= barcodeCount; sequence++)
                {
                    var code = BuildInventoryBarcodeCode(
                        item.LotNumber,
                        product.IdentificationCode,
                        receipt.ActualImportDate,
                        receipt.Code,
                        sequence);
                    var uniqueCode = code;
                    var suffix = 1;
                    while (await _inventoryBarcodeRepository.AnyAsync(x => x.Code == uniqueCode))
                        uniqueCode = $"{code}-{suffix++:D2}";

                    var barcode = new InventoryBarcode(
                        GuidGenerator.Create(),
                        uniqueCode,
                        item.ProductId,
                        item.Id,
                        defaultWarehouse.Id)
                    {
                        SkuId = item.SkuId
                    };
                    await _inventoryBarcodeRepository.InsertAsync(barcode, autoSave: true);
                    barcodeCodes.Add(uniqueCode);
                }
                item.BarcodeValues = JsonSerializer.Serialize(barcodeCodes);
            }

            await _itemRepository.UpdateAsync(item, autoSave: true);
        }

        // Equipment items: create Equipment entities for received items
        var equipmentItems = receipt.Items.Where(x => x.IsEquipment && x.EquipmentReceiveStatus == EquipmentReceiveStatus.Received).ToList();
        foreach (var item in equipmentItems)
        {
            var product = await _productRepository.FindAsync(item.ProductId);
            var equipCode = await GenerateEquipmentCodeAsync();
            var equipment = new Equipment(GuidGenerator.Create(), equipCode, item.ProductName, item.SerialNumber ?? "")
            {
                GoodsReceiptId = goodsReceipt.Id,
                GoodsReceiptCode = goodsReceipt.Code,
                SupplierId = goodsReceipt.SupplierId,
                SupplierName = goodsReceipt.SupplierName,
                Model = null,
                Manufacturer = item.ManufacturerName,
                UnitId = item.UnitId,
                UnitName = item.UnitName,
                Price = item.OriginalPrice ?? 0,
                AssetCode = item.AssetCode,
                DepreciationPeriod = item.DepreciationPeriod,
                Location = item.Location,
                PowerConsumption = product?.PowerConsumption,
                ImportDate = receipt.ActualImportDate,
                ProductId = item.ProductId,
            };
            await _equipmentRepository.InsertAsync(equipment, autoSave: true);
        }

        receipt.Status = WarehouseImportReceiptStatus.Completed;
        await _receiptRepository.UpdateAsync(receipt, autoSave: true);

        await RecomputeOrderStatusAsync(goodsReceipt, poLines);

        return await GetAsync(receipt.Id);
    }

    [Authorize(LabSpecimenManagementPermissions.WarehouseImportReceipts.Create)]
    public async Task DeleteAsync(Guid id)
    {
        var receipt = await _receiptRepository.GetAsync(id);
        if (receipt.Status != WarehouseImportReceiptStatus.InProgress)
            throw new UserFriendlyException("Phiếu đã hoàn thành, không thể xóa.");

        await DeleteItemsAsync(id);
        await _receiptRepository.DeleteAsync(id);
    }

    public async Task<string> GetNextCodeAsync()
    {
        return await GenerateCodeAsync();
    }

    public async Task<List<EligibleGoodsReceiptForImportDto>> GetEligibleGoodsReceiptsAsync()
    {
        var query = await _goodsReceiptRepository.GetQueryableAsync();
        var receipts = query
            .Where(x => EligibleOrderStatuses.Contains(x.OrderStatus))
            .OrderByDescending(x => x.CreationTime)
            .ToList();

        return receipts.Select(x => new EligibleGoodsReceiptForImportDto
        {
            Id = x.Id,
            Code = x.Code,
            SupplierId = x.SupplierId,
            SupplierName = x.SupplierName,
            ExpectedDeliveryDate = x.ExpectedDeliveryDate
        }).ToList();
    }

    public async Task<List<GoodsReceiptItemForImportDto>> GetImportableItemsAsync(Guid goodsReceiptId)
    {
        var lines = (await _goodsReceiptItemRepository.GetQueryableAsync())
            .Where(x => x.GoodsReceiptId == goodsReceiptId)
            .ToList();

        var completedReceiptIds = await GetCompletedReceiptIdsAsync(excludeReceiptId: null);
        var importDateByReceiptId = (await _receiptRepository.GetQueryableAsync())
            .Where(x => completedReceiptIds.Contains(x.Id))
            .ToDictionary(x => x.Id, x => x.ActualImportDate);
        var allHistoricalItems = (await _itemRepository.GetQueryableAsync())
            .Where(x => completedReceiptIds.Contains(x.WarehouseImportReceiptId))
            .ToList()
            .OrderBy(x => importDateByReceiptId.TryGetValue(x.WarehouseImportReceiptId, out var date) ? date : DateTime.MinValue)
            .ToList();

        // Batch-load SKUs for all historical items to enable unit conversion
        var allHistoricalSkuIds = allHistoricalItems
            .Where(x => x.SkuId.HasValue).Select(x => x.SkuId!.Value).Distinct().ToList();
        var allHistoricalSkuMap = allHistoricalSkuIds.Count > 0
            ? (await _productSkuRepository.GetQueryableAsync()).Where(x => allHistoricalSkuIds.Contains(x.Id)).ToDictionary(x => x.Id)
            : new Dictionary<Guid, ProductSku>();

        var result = new List<GoodsReceiptItemForImportDto>();
        foreach (var line in lines)
        {
            var historicalForLine = allHistoricalItems.Where(x => x.GoodsReceiptItemId == line.Id).ToList();

            // Convert each historical item's qty to order/purchase unit before summing
            decimal alreadyReceived = 0;
            foreach (var hist in historicalForLine.Where(x => x.Status == WarehouseImportLineStatus.Checked))
            {
                if (hist.SkuId.HasValue && allHistoricalSkuMap.TryGetValue(hist.SkuId.Value, out var histSku))
                    alreadyReceived += ResolveToOrderQuantity(histSku, hist.UnitId, hist.ActualReceivedQuantity);
                else
                    alreadyReceived += hist.ActualReceivedQuantity;
            }

            var remaining = Math.Max(0, line.Quantity - alreadyReceived);

            var dto = new GoodsReceiptItemForImportDto
            {
                GoodsReceiptItemId = line.Id,
                ProductId = line.ProductId ?? Guid.Empty,
                ProductName = line.ProductName,
                UnitId = line.UnitId,
                UnitName = line.UnitName,
                OrderedQuantity = line.Quantity,
                AlreadyReceivedQuantity = alreadyReceived,
                RemainingOrderableQuantity = remaining,
                IsEquipment = line.IsEquipment,
                ManufacturerId = line.ManufacturerId,
                ManufacturerName = line.Manufacturer,
                PreviousLots = historicalForLine.Select(x => new PreviousReceivedLotDto
                {
                    LotNumber = x.LotNumber,
                    LotExpiryDate = x.LotExpiryDate,
                    ActualReceivedQuantity = x.ActualReceivedQuantity,
                    SkuId = x.SkuId,
                    UnitName = x.UnitName,
                    StorageWarehouseName = x.StorageWarehouseName,
                    Status = x.Status,
                    QualityDocumentFileId = x.QualityDocumentFileId,
                    QualityDocumentFileName = x.QualityDocumentFileName,
                    QualityDocumentFileSize = x.QualityDocumentFileSize,
                    IsEquipment = x.IsEquipment,
                    EquipmentIndex = x.EquipmentIndex,
                    AssetCode = x.AssetCode,
                    SerialNumber = x.SerialNumber,
                    ManufacturerId = x.ManufacturerId,
                    ManufacturerName = x.ManufacturerName,
                    EquipmentReceiveStatus = (int)x.EquipmentReceiveStatus,
                    DepreciationPeriod = x.DepreciationPeriod,
                    OriginalPrice = x.OriginalPrice,
                    Location = x.Location,
                }).ToList()
            };

            var product = line.ProductId.HasValue ? await _productRepository.FindAsync(line.ProductId.Value) : null;
            if (product != null)
            {
                dto.ProductType = (int)product.ProductType;
                dto.InstallationLocation = product.InstallationLocation;

                if (product.BaseUnitId.HasValue && product.BaseUnitId != line.UnitId)
                {
                    var baseUnit = await _unitRepository.FindAsync(product.BaseUnitId.Value);
                    dto.BaseUnitName = baseUnit?.Name;
                    try
                    {
                        dto.BaseOrderedQuantity = await _unitConversionResolver.ResolveBaseQuantityAsync(
                            product, line.UnitId, line.Quantity, line.ProductName);
                    }
                    catch (UserFriendlyException)
                    {
                        // BaseOrderedQuantity is a display-only fallback; skip if no conversion rule found
                    }
                }

                // Populate product info
                dto.CASNumber = product.CASNumber;
                dto.PackagingSpec = product.FormattedConcentration ?? product.Concentration;

                var skus = (await _productSkuRepository.GetQueryableAsync())
                    .Where(s => s.ProductId == product.Id)
                    .ToList();

                if (skus.Any())
                {
                    var manufacturerIds = skus.Select(s => s.ManufacturerId).Distinct().ToList();
                    var unitIds = skus
                        .SelectMany(s => new[] { s.BaseUnitId, s.PackagingUnitId, s.VolumeUnitId })
                        .Where(u => u.HasValue).Select(u => u!.Value).Distinct().ToList();

                    var manufacturers = (await _manufacturerRepository.GetQueryableAsync())
                        .Where(m => manufacturerIds.Contains(m.Id)).ToList();
                    var units = (await _unitRepository.GetQueryableAsync())
                        .Where(u => unitIds.Contains(u.Id)).ToList();

                    dto.Skus = skus.Select(s => new ProductSkuForImportDto
                    {
                        SkuId = s.Id,
                        ManufacturerId = s.ManufacturerId,
                        ManufacturerName = manufacturers.FirstOrDefault(m => m.Id == s.ManufacturerId)?.Name ?? "",
                        PurchaseUnitId = s.BaseUnitId,
                        PurchaseUnitName = s.BaseUnitId.HasValue ? units.FirstOrDefault(u => u.Id == s.BaseUnitId.Value)?.Name : null,
                        PackagingQuantity = s.PackagingQuantity,
                        StorageUnitId = s.PackagingUnitId,
                        StorageUnitName = s.PackagingUnitId.HasValue ? units.FirstOrDefault(u => u.Id == s.PackagingUnitId.Value)?.Name : null,
                        VolumePerUnit = s.VolumePerUnit,
                        ConsumableUnitId = s.VolumeUnitId,
                        ConsumableUnitName = s.VolumeUnitId.HasValue ? units.FirstOrDefault(u => u.Id == s.VolumeUnitId.Value)?.Name : null,
                    }).ToList();
                }
            }

            result.Add(dto);
        }

        return result;
    }

    public async Task<List<DefectiveHistoryItemDto>> GetDefectiveHistoryAsync(Guid goodsReceiptId)
    {
        var completedReceipts = (await _receiptRepository.GetQueryableAsync())
            .Where(x => x.GoodsReceiptId == goodsReceiptId && x.Status == WarehouseImportReceiptStatus.Completed)
            .ToList();

        if (completedReceipts.Count == 0)
        {
            return new List<DefectiveHistoryItemDto>();
        }

        var receiptIds = completedReceipts.Select(x => x.Id).ToList();
        var defectiveItems = (await _itemRepository.GetQueryableAsync())
            .Where(x => receiptIds.Contains(x.WarehouseImportReceiptId) && x.Status == WarehouseImportLineStatus.Defective)
            .ToList();

        if (defectiveItems.Count == 0)
        {
            return new List<DefectiveHistoryItemDto>();
        }

        var receiptById = completedReceipts.ToDictionary(x => x.Id, x => x);
        var productIds = defectiveItems.Select(x => x.ProductId).Distinct().ToList();
        var productById = (await _productRepository.GetQueryableAsync())
            .Where(x => productIds.Contains(x.Id))
            .ToList()
            .ToDictionary(x => x.Id, x => new { x.Code, x.CASNumber, x.Manufacturer });

        var creatorNameByReceiptId = new Dictionary<Guid, string?>();
        foreach (var receipt in completedReceipts)
        {
            creatorNameByReceiptId[receipt.Id] = await _userNameProvider.GetUserNameAsync(receipt.CreatorId) ?? "Admin";
        }

        var result = defectiveItems.Select(item =>
        {
            var receipt = receiptById[item.WarehouseImportReceiptId];
            return new DefectiveHistoryItemDto
            {
                ReceiptId = receipt.Id,
                ReceiptCode = receipt.Code,
                ProductCode = productById.TryGetValue(item.ProductId, out var p) ? p.Code : null,
                CasNumber = p?.CASNumber,
                Manufacturer = p?.Manufacturer,
                ProductName = item.ProductName,
                ActualReceivedQuantity = item.ActualReceivedQuantity,
                UnitName = item.UnitName,
                Notes = item.Notes,
                CreatorName = creatorNameByReceiptId[item.WarehouseImportReceiptId],
                ReceiptCreationTime = receipt.CreationTime
            };
        }).OrderByDescending(x => x.ReceiptCreationTime).ToList();

        return result;
    }

    private async Task ApplyHeaderAsync(WarehouseImportReceipt receipt, CreateUpdateWarehouseImportReceiptDto input)
    {
        receipt.ActualImportDate = input.ActualImportDate;
        receipt.WarehouseKeeperId = input.WarehouseKeeperId;
        receipt.WarehouseKeeperName = input.WarehouseKeeperName;
        receipt.WarehouseId = input.WarehouseId;
        receipt.DeliveryDocumentFiles = input.DeliveryDocumentFiles.Count > 0
            ? System.Text.Json.JsonSerializer.Serialize(input.DeliveryDocumentFiles)
            : null;
        receipt.Notes = input.Notes;
        receipt.TotalAmount = input.TotalAmount;
        await ResolveAndApplySupplierAsync(receipt, input.SupplierId, input.SupplierName);

        if (input.WarehouseId.HasValue)
        {
            var warehouse = await _warehouseRepository.FindAsync(input.WarehouseId.Value);
            receipt.WarehouseName = warehouse?.Name;
        }
    }

    private async Task ResolveAndApplySupplierAsync(WarehouseImportReceipt receipt, Guid? inputSupplierId, string? inputSupplierName)
    {
        if (string.IsNullOrWhiteSpace(inputSupplierName))
        {
            receipt.SupplierId = null;
            receipt.SupplierName = null;
            return;
        }

        var supplierQuery = await _supplierRepository.GetQueryableAsync();

        // 1. Try by provided ID
        if (inputSupplierId.HasValue)
        {
            var byId = supplierQuery.FirstOrDefault(x => x.Id == inputSupplierId.Value);
            if (byId != null)
            {
                receipt.SupplierId = byId.Id;
                receipt.SupplierName = byId.Name;
                return;
            }
        }

        // 2. Try by name (case-insensitive)
        var name = inputSupplierName.Trim();
        var byName = supplierQuery.FirstOrDefault(x => x.Name.ToLower() == name.ToLower());
        if (byName != null)
        {
            receipt.SupplierId = byName.Id;
            receipt.SupplierName = byName.Name;
            return;
        }

        // 3. Create new supplier
        var todayPrefix = _codeGenerator.GetTodayPrefix("SUP");
        var count = supplierQuery.Count(x => x.Code.StartsWith(todayPrefix));
        var code = _codeGenerator.GenerateCodeWithDate("SUP", count);
        var newSupplier = new Supplier(GuidGenerator.Create(), code, name) { IsActive = true };
        await _supplierRepository.InsertAsync(newSupplier, autoSave: true);
        receipt.SupplierId = newSupplier.Id;
        receipt.SupplierName = newSupplier.Name;
    }

    private async Task ReplaceItemsAsync(WarehouseImportReceipt receipt, List<CreateUpdateWarehouseImportReceiptItemDto> inputItems)
    {
        await DeleteItemsAsync(receipt.Id);

        if (inputItems.Count == 0)
        {
            return;
        }

        var lineIds = inputItems.Select(x => x.GoodsReceiptItemId).Distinct().ToList();
        var poLines = (await _goodsReceiptItemRepository.GetQueryableAsync())
            .Where(x => lineIds.Contains(x.Id))
            .ToList()
            .ToDictionary(x => x.Id, x => x);

        foreach (var input in inputItems)
        {
            if (!poLines.TryGetValue(input.GoodsReceiptItemId, out var poLine))
                throw new UserFriendlyException("Không tìm thấy dòng hàng tương ứng trên phiếu mua hàng.");

            if (input.IsEquipment && input.EquipmentReceiveStatus == EquipmentReceiveStatus.Received
                && string.IsNullOrWhiteSpace(input.AssetCode))
                throw new UserFriendlyException("Thiết bị đã nhận phải có mã tài sản.");

            var item = new WarehouseImportReceiptItem(
                GuidGenerator.Create(),
                receipt.Id,
                poLine.Id,
                poLine.ProductId ?? Guid.Empty,
                poLine.ProductName,
                poLine.Quantity)
            {
                ActualReceivedQuantity = input.IsEquipment ? 1 : input.ActualReceivedQuantity,
                UnitId = input.SkuId.HasValue && input.SelectedUnitId.HasValue ? input.SelectedUnitId : poLine.UnitId,
                UnitName = input.SkuId.HasValue && input.SelectedUnitId.HasValue ? input.SelectedUnitName : poLine.UnitName,
                LotNumber = input.LotNumber,
                LotExpiryDate = input.LotExpiryDate,
                Status = input.Status,
                QualityDocumentFileId = input.QualityDocumentFileId,
                QualityDocumentFileName = input.QualityDocumentFileName,
                QualityDocumentFilePath = input.QualityDocumentFilePath,
                QualityDocumentFileSize = input.QualityDocumentFileSize,
                Notes = input.Notes,
                SkuId = input.SkuId,
                StorageWarehouseId = input.StorageWarehouseId,
                StorageWarehouseName = input.StorageWarehouseName,
                UnitPrice = input.UnitPrice,
                VatPercent = input.VatPercent,
                TotalLineAmount = input.TotalLineAmount,
                IsEquipment = input.IsEquipment,
                AssetCode = input.AssetCode,
                SerialNumber = input.SerialNumber,
                ManufacturerId = input.ManufacturerId,
                ManufacturerName = input.ManufacturerName,
                EquipmentReceiveStatus = input.EquipmentReceiveStatus,
                DepreciationPeriod = input.DepreciationPeriod,
                OriginalPrice = input.OriginalPrice,
                Location = input.Location,
                EquipmentIndex = input.EquipmentIndex
            };

            await _itemRepository.InsertAsync(item, autoSave: true);
        }
    }

    private async Task DeleteItemsAsync(Guid receiptId)
    {
        var existing = (await _itemRepository.GetQueryableAsync())
            .Where(x => x.WarehouseImportReceiptId == receiptId)
            .ToList();

        foreach (var item in existing)
        {
            await _itemRepository.DeleteAsync(item, autoSave: true);
        }
    }

    private async Task<WarehouseImportReceipt> GetReceiptWithItemsAsync(Guid id)
    {
        var receipt = await _receiptRepository.GetAsync(id);
        receipt.Items = (await _itemRepository.GetQueryableAsync())
            .Where(x => x.WarehouseImportReceiptId == id)
            .ToList();
        return receipt;
    }

    private async Task<List<Guid>> GetCompletedReceiptIdsAsync(Guid? excludeReceiptId)
    {
        return (await _receiptRepository.GetQueryableAsync())
            .Where(x => x.Status == WarehouseImportReceiptStatus.Completed && x.Id != excludeReceiptId)
            .Select(x => x.Id)
            .ToList();
    }

    private async Task<decimal> GetCumulativeCheckedQtyAsync(Guid goodsReceiptItemId, Guid? excludeReceiptId)
    {
        var completedReceiptIds = await GetCompletedReceiptIdsAsync(excludeReceiptId);
        if (completedReceiptIds.Count == 0) return 0;

        var items = (await _itemRepository.GetQueryableAsync())
            .Where(x =>
                x.GoodsReceiptItemId == goodsReceiptItemId &&
                x.Status == WarehouseImportLineStatus.Checked &&
                completedReceiptIds.Contains(x.WarehouseImportReceiptId))
            .ToList();

        if (items.Count == 0) return 0;

        var skuIds = items.Where(x => x.SkuId.HasValue).Select(x => x.SkuId!.Value).Distinct().ToList();
        var skuMap = skuIds.Count > 0
            ? (await _productSkuRepository.GetQueryableAsync()).Where(x => skuIds.Contains(x.Id)).ToDictionary(x => x.Id)
            : new Dictionary<Guid, ProductSku>();

        decimal total = 0;
        foreach (var item in items)
        {
            if (item.SkuId.HasValue && skuMap.TryGetValue(item.SkuId.Value, out var sku))
                total += ResolveToOrderQuantity(sku, item.UnitId, item.ActualReceivedQuantity);
            else
                total += item.ActualReceivedQuantity;
        }
        return total;
    }

    private async Task RecomputeOrderStatusAsync(GoodsReceipt goodsReceipt, List<GoodsReceiptItem> poLines)
    {
        var fullyReceived = true;

        // Check material lines
        foreach (var line in poLines.Where(x => !x.IsEquipment))
        {
            var cumulative = await GetCumulativeCheckedQtyAsync(line.Id, excludeReceiptId: null);
            if (cumulative < line.Quantity)
            {
                fullyReceived = false;
                break;
            }
        }

        // Check equipment lines: count received items across all completed receipts
        if (fullyReceived)
        {
            var completedReceiptIds = await GetCompletedReceiptIdsAsync(excludeReceiptId: null);
            var allEquipmentItems = (await _itemRepository.GetQueryableAsync())
                .Where(x => completedReceiptIds.Contains(x.WarehouseImportReceiptId) && x.IsEquipment
                    && x.EquipmentReceiveStatus == EquipmentReceiveStatus.Received)
                .ToList();

            foreach (var line in poLines.Where(x => x.IsEquipment))
            {
                var receivedCount = allEquipmentItems.Count(x => x.GoodsReceiptItemId == line.Id);
                if (receivedCount < (int)line.Quantity)
                {
                    fullyReceived = false;
                    break;
                }
            }
        }

        if (fullyReceived)
        {
            goodsReceipt.OrderStatus = GoodsReceiptOrderStatus.Completed;
            goodsReceipt.WarehouseStatus = GoodsReceiptWarehouseStatus.Imported;
        }
        else if (goodsReceipt.OrderStatus is GoodsReceiptOrderStatus.Ordering or GoodsReceiptOrderStatus.WaitingReceipt)
        {
            var today = DateTime.Now.Date;
            goodsReceipt.OrderStatus = goodsReceipt.ExpectedDeliveryDate.HasValue && today >= goodsReceipt.ExpectedDeliveryDate.Value.Date
                ? GoodsReceiptOrderStatus.WaitingReceipt
                : GoodsReceiptOrderStatus.Ordering;
        }

        await _goodsReceiptRepository.UpdateAsync(goodsReceipt, autoSave: true);
    }

    private async Task EnrichListDtosAsync(List<WarehouseImportReceipt> entities, List<WarehouseImportReceiptListDto> dtos)
    {
        await _userNameProvider.EnrichWithUserNamesAsync(
            dtos,
            dto => entities.First(e => e.Id == dto.Id).CreatorId,
            (dto, name) => dto.CreatorName = name);

        var receiptIds = entities.Select(x => x.Id).ToList();
        var itemsQuery = await _itemRepository.GetQueryableAsync();
        var defectiveReceiptIds = itemsQuery
            .Where(x => receiptIds.Contains(x.WarehouseImportReceiptId) && x.Status == WarehouseImportLineStatus.Defective)
            .Select(x => x.WarehouseImportReceiptId)
            .Distinct()
            .ToList();

        foreach (var dto in dtos)
        {
            dto.HasDefectiveItem = defectiveReceiptIds.Contains(dto.Id);
        }
    }

    private async Task<WarehouseImportReceiptDto> MapToDetailDtoAsync(WarehouseImportReceipt receipt)
    {
        var dto = ObjectMapper.Map<WarehouseImportReceipt, WarehouseImportReceiptDto>(receipt);
        dto.CreatorName = await _userNameProvider.GetUserNameAsync(receipt.CreatorId) ?? "Admin";
        dto.HasDefectiveItem = receipt.Items.Any(x => x.Status == WarehouseImportLineStatus.Defective);

        var goodsReceiptItemIds = receipt.Items
            .Where(x => x.GoodsReceiptItemId.HasValue)
            .Select(x => x.GoodsReceiptItemId!.Value).Distinct().ToList();
        var purchaseUnitByPoLineId = (await _goodsReceiptItemRepository.GetQueryableAsync())
            .Where(x => goodsReceiptItemIds.Contains(x.Id))
            .ToDictionary(x => x.Id, x => x.UnitName);

        var skuIds = receipt.Items.Where(x => x.SkuId.HasValue).Select(x => x.SkuId!.Value).Distinct().ToList();
        var skuMap = skuIds.Count > 0
            ? (await _productSkuRepository.GetQueryableAsync()).Where(x => skuIds.Contains(x.Id)).ToDictionary(x => x.Id)
            : new Dictionary<Guid, ProductSku>();
        var manufacturerIds = skuMap.Values.Select(x => x.ManufacturerId).Distinct().ToList();
        var manufacturerNames = manufacturerIds.Count > 0
            ? (await _manufacturerRepository.GetQueryableAsync()).Where(x => manufacturerIds.Contains(x.Id)).ToDictionary(x => x.Id, x => x.Name)
            : new Dictionary<Guid, string>();

        dto.Items = receipt.Items.Select(x =>
        {
            var itemDto = ObjectMapper.Map<WarehouseImportReceiptItem, WarehouseImportReceiptItemDto>(x);
            itemDto.PurchaseUnitName = x.GoodsReceiptItemId.HasValue && purchaseUnitByPoLineId.TryGetValue(x.GoodsReceiptItemId.Value, out var u) ? u : null;
            if (x.SkuId.HasValue && skuMap.TryGetValue(x.SkuId.Value, out var sku)
                && manufacturerNames.TryGetValue(sku.ManufacturerId, out var mfName))
                itemDto.ManufacturerName = mfName;
            return itemDto;
        }).ToList();

        var goodsReceipt = receipt.GoodsReceiptId.HasValue
            ? await _goodsReceiptRepository.FindAsync(receipt.GoodsReceiptId.Value)
            : null;
        if (goodsReceipt != null)
        {
            dto.SupplierName = receipt.SupplierName ?? goodsReceipt.SupplierName;
            dto.ContactPersonName = goodsReceipt.SupplierRepresentativeName;
        }
        dto.SupplierId = receipt.SupplierId;
        dto.TotalAmount = receipt.TotalAmount;

        return dto;
    }

    private async Task<string> GenerateCodeAsync()
    {
        const string prefix = "GR";
        const string dateFormat = "ddMMyyyy";
        const int sequencePadding = 2;
        using (_dataFilter.Disable<Volo.Abp.ISoftDelete>())
        {
            var todayPrefix = _codeGenerator.GetTodayPrefix(prefix, dateFormat);
            var query = await _receiptRepository.GetQueryableAsync();
            var count = query.Count(x => x.Code.StartsWith(todayPrefix));
            return _codeGenerator.GenerateCodeWithDate(prefix, count, dateFormat, sequencePadding);
        }
    }

    private static decimal ResolveSkuQuantity(ProductSku sku, Guid? selectedUnitId, decimal quantity)
    {
        // Storage unit (PackagingUnitId) → as-is
        if (selectedUnitId == sku.PackagingUnitId || sku.PackagingUnitId == null)
            return quantity;

        // Purchase unit (BaseUnitId) → multiply by PackagingQuantity
        if (selectedUnitId == sku.BaseUnitId)
        {
            if (sku.BaseUnitId == sku.PackagingUnitId)
                return quantity;
            return quantity * (sku.PackagingQuantity ?? 1);
        }

        // Consumable unit (VolumeUnitId) → divide by VolumePerUnit, round up 2dp
        if (selectedUnitId == sku.VolumeUnitId && sku.VolumePerUnit > 0)
        {
            var ratio = quantity / sku.VolumePerUnit.Value;
            return Math.Round(Math.Ceiling(ratio * 100m) / 100m, 2);
        }

        return quantity;
    }

    private static string BuildInventoryBarcodeCode(
        string? lotNumber,
        string? identificationCode,
        DateTime importDate,
        string receiptCode,
        int sequence)
    {
        var parts = new[]
        {
            lotNumber,
            identificationCode,
            importDate.ToString("ddMMyyyy"),
            receiptCode,
            sequence.ToString("D3")
        }.Where(x => !string.IsNullOrWhiteSpace(x));

        var normalized = string.Join("-", parts).Normalize(NormalizationForm.FormD);
        return new string(normalized
            .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            .Where(c => c >= 32 && c <= 126)
            .ToArray());
    }

    // Converts stored ActualReceivedQuantity (in selected unit) → purchase/order unit (same unit as poLine.Quantity)
    private static decimal ResolveToOrderQuantity(ProductSku sku, Guid? selectedUnitId, decimal quantity)
    {
        // Storage unit → divide by packagingQuantity → purchase unit
        if (selectedUnitId == sku.PackagingUnitId
            && sku.PackagingUnitId != sku.BaseUnitId
            && (sku.PackagingQuantity ?? 0) > 0)
            return quantity / sku.PackagingQuantity!.Value;

        // Purchase unit → as-is
        if (selectedUnitId == sku.BaseUnitId || selectedUnitId == null)
            return quantity;

        // Consumable unit → storage → purchase
        if (selectedUnitId == sku.VolumeUnitId
            && (sku.VolumePerUnit ?? 0) > 0
            && (sku.PackagingQuantity ?? 0) > 0
            && sku.PackagingUnitId != sku.BaseUnitId)
            return (quantity / sku.VolumePerUnit!.Value) / sku.PackagingQuantity!.Value;

        return quantity;
    }

    private async Task<string> GenerateEquipmentCodeAsync()
    {
        using (_dataFilter.Disable<Volo.Abp.ISoftDelete>())
        {
            var todayPrefix = _codeGenerator.GetTodayPrefix("TB");
            var equipQuery = await _equipmentRepository.GetQueryableAsync();
            var count = equipQuery.Count(x => x.Code.StartsWith(todayPrefix));
            return _codeGenerator.GenerateCodeWithDate("TB", count);
        }
    }
}
