using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.Products;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.MasterData.Units;
using LabSpecimenManagement.StockAdjustments;
using LabSpecimenManagement.WarehouseImports;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Linq;
using Volo.Abp.Uow;
using Volo.Abp.Users;

namespace LabSpecimenManagement.MaterialReceipts;

[Authorize(LabSpecimenManagementPermissions.MaterialReceipts.Default)]
public class MaterialReceiptAppService : ApplicationService, IMaterialReceiptAppService
{
    private readonly IRepository<MaterialReceipt, Guid> _receiptRepository;
    private readonly IRepository<MaterialReceiptItem, Guid> _receiptItemRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<ProductSku, Guid> _productSkuRepository;
    private readonly IRepository<Warehouse, Guid> _warehouseRepository;
    private readonly IRepository<WarehouseImportReceipt, Guid> _warehouseImportReceiptRepository;
    private readonly IRepository<WarehouseImportReceiptItem, Guid> _warehouseImportItemRepository;
    private readonly IRepository<Unit, Guid> _unitRepository;
    private readonly IRepository<Manufacturer, Guid> _manufacturerRepository;
    private readonly IInventoryBarcodeRepository _inventoryBarcodeRepository;
    private readonly IFifoStockDeductionService _fifoStockService;
    private readonly IUnitConversionResolver _unitConversionResolver;
    private readonly ICodeGeneratorService _codeGenerator;
    private readonly IUserNameProvider _userNameProvider;
    private readonly IDataFilter _dataFilter;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IRepository<StockAdjustment, Guid> _stockAdjustmentRepository;
    private readonly IRepository<StockAdjustmentItem, Guid> _stockAdjustmentItemRepository;
    private readonly IAsyncQueryableExecuter _asyncExecuter;
    private readonly InventoryBarcodeStatusService _barcodeStatusService;

    public MaterialReceiptAppService(
        IRepository<MaterialReceipt, Guid> receiptRepository,
        IRepository<MaterialReceiptItem, Guid> receiptItemRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<ProductSku, Guid> productSkuRepository,
        IRepository<Warehouse, Guid> warehouseRepository,
        IRepository<WarehouseImportReceipt, Guid> warehouseImportReceiptRepository,
        IRepository<WarehouseImportReceiptItem, Guid> warehouseImportItemRepository,
        IRepository<Unit, Guid> unitRepository,
        IRepository<Manufacturer, Guid> manufacturerRepository,
        IInventoryBarcodeRepository inventoryBarcodeRepository,
        IFifoStockDeductionService fifoStockService,
        IUnitConversionResolver unitConversionResolver,
        ICodeGeneratorService codeGenerator,
        IUserNameProvider userNameProvider,
        IDataFilter dataFilter,
        IGuidGenerator guidGenerator,
        IAsyncQueryableExecuter asyncExecuter,
        IRepository<StockAdjustment, Guid> stockAdjustmentRepository,
        IRepository<StockAdjustmentItem, Guid> stockAdjustmentItemRepository,
        InventoryBarcodeStatusService barcodeStatusService)
    {
        _receiptRepository = receiptRepository;
        _receiptItemRepository = receiptItemRepository;
        _productRepository = productRepository;
        _productSkuRepository = productSkuRepository;
        _warehouseRepository = warehouseRepository;
        _warehouseImportReceiptRepository = warehouseImportReceiptRepository;
        _warehouseImportItemRepository = warehouseImportItemRepository;
        _unitRepository = unitRepository;
        _manufacturerRepository = manufacturerRepository;
        _inventoryBarcodeRepository = inventoryBarcodeRepository;
        _fifoStockService = fifoStockService;
        _unitConversionResolver = unitConversionResolver;
        _codeGenerator = codeGenerator;
        _userNameProvider = userNameProvider;
        _dataFilter = dataFilter;
        _guidGenerator = guidGenerator;
        _asyncExecuter = asyncExecuter;
        _stockAdjustmentRepository = stockAdjustmentRepository;
        _stockAdjustmentItemRepository = stockAdjustmentItemRepository;
        _barcodeStatusService = barcodeStatusService;
    }

    public async Task<PagedResultDto<MaterialReceiptListDto>> GetListAsync(GetMaterialReceiptListInput input)
    {
        var query = await _receiptRepository.GetQueryableAsync();

        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var search = input.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Code.ToLower().Contains(search) ||
                x.ReceiverName.ToLower().Contains(search));
        }

        if (input.Status.HasValue)
            query = query.Where(x => x.Status == input.Status.Value);

        if (input.FromDate.HasValue)
            query = query.Where(x => x.ReceiptDate >= input.FromDate.Value.Date);

        if (input.ToDate.HasValue)
            query = query.Where(x => x.ReceiptDate <= input.ToDate.Value.Date.AddDays(1).AddTicks(-1));

        var totalCount = await _asyncExecuter.CountAsync(query);

        if (!string.IsNullOrWhiteSpace(input.Sorting))
            query = ApplySorting(query, input.Sorting);
        else
            query = query.OrderByDescending(x => x.CreationTime);

        query = query.Skip(input.SkipCount).Take(input.MaxResultCount);
        var receipts = await _asyncExecuter.ToListAsync(query);

        var creatorIds = receipts.Where(x => x.CreatorId.HasValue).Select(x => x.CreatorId!.Value).Distinct().ToList();
        var creatorNames = creatorIds.Any() ? await _userNameProvider.GetUserNamesAsync(creatorIds.Cast<Guid?>()) : new Dictionary<Guid, string>();

        var receiptIds = receipts.Select(x => x.Id).ToList();
        var itemQuery = await _receiptItemRepository.GetQueryableAsync();
        var itemCounts = await _asyncExecuter.ToListAsync(
            itemQuery.Where(x => receiptIds.Contains(x.MaterialReceiptId))
                     .GroupBy(x => x.MaterialReceiptId)
                     .Select(g => new { g.Key, Count = g.Count() }));
        var itemCountMap = itemCounts.ToDictionary(x => x.Key, x => x.Count);

        var items = receipts.Select(r => new MaterialReceiptListDto
        {
            Id = r.Id,
            Code = r.Code,
            ReceiverName = r.ReceiverName,
            ReceiptDate = r.ReceiptDate,
            Status = r.Status,
            ItemCount = itemCountMap.GetValueOrDefault(r.Id, 0),
            CreationTime = r.CreationTime,
            CreatorName = r.CreatorId.HasValue && creatorNames.ContainsKey(r.CreatorId.Value)
                ? creatorNames[r.CreatorId.Value] : null
        }).ToList();

        return new PagedResultDto<MaterialReceiptListDto>(totalCount, items);
    }

    public async Task<MaterialReceiptStatisticsDto> GetStatisticsAsync()
    {
        var query = await _receiptRepository.GetQueryableAsync();
        return new MaterialReceiptStatisticsDto
        {
            DraftCount = await _asyncExecuter.CountAsync(
                query.Where(x => x.Status == MaterialReceiptStatus.Draft)),
            ReceivedCount = await _asyncExecuter.CountAsync(
                query.Where(x => x.Status == MaterialReceiptStatus.Confirmed))
        };
    }

    public async Task<MaterialReceiptDto> GetAsync(Guid id)
    {
        var receipt = await _receiptRepository.GetAsync(id);
        var dto = ObjectMapper.Map<MaterialReceipt, MaterialReceiptDto>(receipt);

        if (receipt.CreatorId.HasValue)
        {
            var names = await _userNameProvider.GetUserNamesAsync(new List<Guid?> { receipt.CreatorId.Value });
            dto.CreatorName = names.GetValueOrDefault(receipt.CreatorId.Value);
        }

        var itemQuery = await _receiptItemRepository.GetQueryableAsync();
        var receiptItems = await _asyncExecuter.ToListAsync(
            itemQuery.Where(x => x.MaterialReceiptId == id));

        var productIds = receiptItems.Select(x => x.ProductId).Distinct().ToList();
        var productQuery = await _productRepository.GetQueryableAsync();
        var products = await _asyncExecuter.ToListAsync(
            productQuery.Where(p => productIds.Contains(p.Id)));
        var productMap = products.ToDictionary(p => p.Id);

        var skuQuery = await _productSkuRepository.GetQueryableAsync();
        var skus = await _asyncExecuter.ToListAsync(
            skuQuery.Where(s => productIds.Contains(s.ProductId)));
        var skusByProduct = skus.GroupBy(s => s.ProductId).ToDictionary(g => g.Key, g => g.ToList());

        var unitIds = skus.SelectMany(s => new[] { s.PackagingUnitId, s.VolumeUnitId })
            .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var unitQuery = await _unitRepository.GetQueryableAsync();
        var units = await _asyncExecuter.ToListAsync(unitQuery.Where(u => unitIds.Contains(u.Id)));
        var unitMap = units.ToDictionary(u => u.Id, u => u.Name);

        var centralWarehouse = await GetCentralWarehouseAsync();

        // Cho confirmed receipt: lấy lot/manufacturer từ synthetic receipt items
        var syntheticItemsByProduct = new Dictionary<Guid, List<(string? LotNumber, string? ManufacturerName)>>();
        if (receipt.Status == MaterialReceiptStatus.Confirmed && receipt.SyntheticWarehouseReceiptId.HasValue)
        {
            var synItemQuery = await _warehouseImportItemRepository.GetQueryableAsync();
            var synItems = await _asyncExecuter.ToListAsync(
                synItemQuery.Where(x => x.WarehouseImportReceiptId == receipt.SyntheticWarehouseReceiptId.Value));
            syntheticItemsByProduct = synItems.GroupBy(x => x.ProductId)
                .ToDictionary(g => g.Key, g => g.Select(x => (x.LotNumber, x.ManufacturerName)).Distinct().ToList());
        }

        dto.Items = new List<MaterialReceiptItemDto>();
        foreach (var item in receiptItems)
        {
            var itemDto = ObjectMapper.Map<MaterialReceiptItem, MaterialReceiptItemDto>(item);

            if (productMap.TryGetValue(item.ProductId, out var product))
            {
                itemDto.IdentificationCode = product.IdentificationCode;
                itemDto.TechnicalSpec = product.MainTechnicalSpecs;
                itemDto.CASNumber = product.CASNumber;
                itemDto.ProductType = (int)product.ProductType;
            }

            if (centralWarehouse != null)
            {
                itemDto.NearestExpiryDate = await GetNearestExpiryDateAsync(item.ProductId, centralWarehouse.Id);
            }

            if (skusByProduct.TryGetValue(item.ProductId, out var productSkus))
            {
                itemDto.AvailableUnits = BuildAvailableUnits(productSkus, unitMap);
            }

            // Lot/manufacturer: entity first, fallback to synthetic receipt
            if (string.IsNullOrWhiteSpace(itemDto.LotNumber) &&
                syntheticItemsByProduct.TryGetValue(item.ProductId, out var synLots) && synLots.Count > 0)
            {
                var firstLot = synLots.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x.LotNumber));
                itemDto.LotNumber = firstLot.LotNumber;
                itemDto.ManufacturerName = firstLot.ManufacturerName;
            }

            dto.Items.Add(itemDto);
        }

        return dto;
    }

    [Authorize(LabSpecimenManagementPermissions.MaterialReceipts.Create)]
    public async Task<MaterialReceiptDto> CreateAsync(CreateUpdateMaterialReceiptDto input)
    {
        await ValidateInputAsync(input);
        await ValidateCodeUniqueAsync(input.Code);

        var currentUserName = await GetCurrentUserNameAsync();

        var receipt = new MaterialReceipt(
            _guidGenerator.Create(),
            input.Code,
            currentUserName,
            CurrentUser.Id,
            input.ReceiptDate)
        {
            Notes = input.Notes
        };

        await _receiptRepository.InsertAsync(receipt, autoSave: true);
        await MapItemsAsync(receipt, input.Items);

        return await GetAsync(receipt.Id);
    }

    [SerializableUnitOfWork]
    [Authorize(LabSpecimenManagementPermissions.MaterialReceipts.Create)]
    public async Task<MaterialReceiptDto> CreateAndConfirmAsync(CreateUpdateMaterialReceiptDto input)
    {
        await AuthorizationService.CheckAsync(
            LabSpecimenManagementPermissions.MaterialReceipts.Confirm);

        await ValidateInputAsync(input);
        await ValidateCodeUniqueAsync(input.Code);

        var currentUserName = await GetCurrentUserNameAsync();
        var receipt = new MaterialReceipt(
            _guidGenerator.Create(),
            input.Code,
            currentUserName,
            CurrentUser.Id,
            input.ReceiptDate)
        {
            Notes = input.Notes
        };

        await _receiptRepository.InsertAsync(receipt, autoSave: true);
        await MapItemsAsync(receipt, input.Items);

        // Any confirmation error rolls back the new receipt and all of its items.
        return await ConfirmAsync(receipt.Id);
    }

    [Authorize(LabSpecimenManagementPermissions.MaterialReceipts.Create)]
    public async Task<MaterialReceiptDto> UpdateAsync(Guid id, CreateUpdateMaterialReceiptDto input)
    {
        var receipt = await _receiptRepository.GetAsync(id);

        if (receipt.Status != MaterialReceiptStatus.Draft)
            throw new UserFriendlyException("Chỉ có thể chỉnh sửa phiếu ở trạng thái Nháp.");

        await ValidateInputAsync(input);

        if (receipt.Code != input.Code)
            await ValidateCodeUniqueAsync(input.Code, id);

        receipt.Code = input.Code;
        receipt.ReceiptDate = input.ReceiptDate;
        receipt.Notes = input.Notes;

        var existingItems = await _asyncExecuter.ToListAsync(
            (await _receiptItemRepository.GetQueryableAsync()).Where(x => x.MaterialReceiptId == id));

        var inputItemIds = input.Items.Where(x => x.Id.HasValue).Select(x => x.Id!.Value).ToHashSet();
        var itemsToDelete = existingItems.Where(x => !inputItemIds.Contains(x.Id)).ToList();
        foreach (var item in itemsToDelete)
            await _receiptItemRepository.DeleteAsync(item.Id, autoSave: true);

        await MapItemsAsync(receipt, input.Items);

        await _receiptRepository.UpdateAsync(receipt, autoSave: true);

        return await GetAsync(receipt.Id);
    }

    [SerializableUnitOfWork]
    [Authorize(LabSpecimenManagementPermissions.MaterialReceipts.Confirm)]
    public async Task<MaterialReceiptDto> ConfirmAsync(Guid id)
    {
        var receipt = await _receiptRepository.GetAsync(id);

        if (receipt.Status != MaterialReceiptStatus.Draft)
            throw new UserFriendlyException("Chỉ có thể xác nhận phiếu ở trạng thái Nháp.");

        var itemQuery = await _receiptItemRepository.GetQueryableAsync();
        var items = await _asyncExecuter.ToListAsync(
            itemQuery.Where(x => x.MaterialReceiptId == id));

        if (!items.Any())
            throw new UserFriendlyException("Phiếu nhận vật tư phải có ít nhất 1 vật tư.");

        var centralWarehouse = await GetCentralWarehouseAsync();
        if (centralWarehouse == null)
            throw new UserFriendlyException("Không tìm thấy kho mặc định (Trung tâm kiểm nghiệm).");

        var syntheticReceipt = new WarehouseImportReceipt(
            _guidGenerator.Create(),
            receipt.Code,
            WarehouseImportReceiptType.MaterialReceipt)
        {
            ActualImportDate = receipt.ReceiptDate,
            MaterialReceiptId = receipt.Id,
            Notes = $"Tự động tạo từ phiếu nhận vật tư {receipt.Code}"
        };

        var stockSnapshots = new Dictionary<(Guid ProductId, Guid WarehouseId, Guid? ManufacturerId), decimal>();
        var syntheticItems = new List<WarehouseImportReceiptItem>();

        var stockBeforeByUnitsCache = new Dictionary<(Guid, Guid), string?>();
        foreach (var item in items)
        {
            var byUnitsKey = (item.ProductId, item.ReceivingWarehouseId);
            if (!stockBeforeByUnitsCache.ContainsKey(byUnitsKey))
            {
                var prod = await _productRepository.GetAsync(item.ProductId);
                var byUnits = await ComputeStockByUnitsAsync(item.ProductId, item.ReceivingWarehouseId, prod);
                stockBeforeByUnitsCache[byUnitsKey] = byUnits.Count >= 2
                    ? JsonSerializer.Serialize(byUnits) : null;
            }
        }

        foreach (var item in items)
        {
            var product = await _productRepository.GetAsync(item.ProductId);

            decimal baseQty;
            if (item.SkuId.HasValue)
            {
                var sku = await _productSkuRepository.GetAsync(item.SkuId.Value);
                baseQty = ResolveSkuQuantity(sku, item.UnitId, item.Quantity);
            }
            else
            {
                baseQty = await _unitConversionResolver.ResolveBaseQuantityAsync(
                    product, item.UnitId, item.Quantity, item.ProductName);
            }

            var centralStock = await GetWarehouseStockAsync(item.ProductId, centralWarehouse.Id);
            if (baseQty > centralStock)
                throw new UserFriendlyException(
                    $"Số lượng nhận của vật tư \"{item.ProductName}\" không được vượt tồn kho tại Kho Trung tâm kiểm nghiệm. " +
                    $"Tồn kho: {centralStock:N2}, số lượng yêu cầu sau quy đổi: {baseQty:N2}.");

            var lotDeductions = await _fifoStockService.DeductAsync(
                product, baseQty, StockMovementType.MaterialTransfer,
                receipt.Id, receipt.Code,
                $"Xuất kho cho phiếu nhận vật tư {receipt.Code}",
                new List<Guid> { centralWarehouse.Id },
                item.LotNumber,
                item.ManufacturerName);

            var deductedBaseQuantity = lotDeductions.Sum(x => x.DeductedBaseQty);
            if (deductedBaseQuantity != baseQty)
            {
                throw new UserFriendlyException(
                    $"Tồn kho của vật tư \"{item.ProductName}\" đã thay đổi. " +
                    "Vui lòng tải lại phiếu và thử xác nhận lại.");
            }

            product.AvailableQuantity += baseQty;
            await _productRepository.UpdateAsync(product, autoSave: true);

            item.BaseQuantity = baseQty;
            await _receiptItemRepository.UpdateAsync(item, autoSave: true);

            var remainingActualQuantity = item.Quantity;
            for (var lotIndex = 0; lotIndex < lotDeductions.Count; lotIndex++)
            {
                var lot = lotDeductions[lotIndex];
                WarehouseImportReceiptItem? originalLotItem = null;
                if (lot.WarehouseImportReceiptItemId != Guid.Empty)
                    originalLotItem = await _warehouseImportItemRepository.FindAsync(lot.WarehouseImportReceiptItemId);

                Guid? manufacturerId = originalLotItem?.ManufacturerId;
                var manufacturerName = originalLotItem?.ManufacturerName;
                if (originalLotItem?.SkuId.HasValue == true)
                {
                    var originalSku = await _productSkuRepository.FindAsync(originalLotItem.SkuId.Value);
                    if (originalSku != null)
                    {
                        manufacturerId ??= originalSku.ManufacturerId;
                        if (string.IsNullOrWhiteSpace(manufacturerName))
                            manufacturerName = (await _manufacturerRepository.FindAsync(originalSku.ManufacturerId))?.Name;
                    }
                }

                var stockKey = (item.ProductId, item.ReceivingWarehouseId, manufacturerId);
                if (!stockSnapshots.TryGetValue(stockKey, out var stockBefore))
                {
                    stockBefore = await GetReceiptChainStockAsync(
                        item.ProductId,
                        item.ReceivingWarehouseId,
                        manufacturerId);
                }
                var stockAfter = stockBefore + lot.DeductedBaseQty;
                stockSnapshots[stockKey] = stockAfter;

                var transferredBarcodes = originalLotItem == null
                    ? new List<string>()
                    : await MoveInventoryBarcodesAsync(
                        originalLotItem,
                        centralWarehouse.Id,
                        item.ReceivingWarehouseId,
                        receipt.Id,
                        lot.DeductedBaseQty);

                var isLastLot = lotIndex == lotDeductions.Count - 1;
                var actualQuantityForLot = isLastLot
                    ? remainingActualQuantity
                    : Math.Round(
                        item.Quantity * lot.DeductedBaseQty / baseQty,
                        4,
                        MidpointRounding.AwayFromZero);
                remainingActualQuantity -= actualQuantityForLot;

                syntheticItems.Add(new WarehouseImportReceiptItem(
                    _guidGenerator.Create(),
                    syntheticReceipt.Id,
                    item.ProductId,
                    item.ProductName)
                {
                    ActualReceivedQuantity = actualQuantityForLot,
                    ReceivedBaseQuantity = lot.DeductedBaseQty,
                    ConsumedBaseQuantity = 0,
                    StorageWarehouseId = item.ReceivingWarehouseId,
                    StorageWarehouseName = item.ReceivingWarehouseName,
                    UnitId = item.UnitId,
                    UnitName = item.UnitName,
                    SkuId = originalLotItem?.SkuId ?? item.SkuId,
                    LotNumber = originalLotItem?.LotNumber,
                    LotExpiryDate = originalLotItem?.LotExpiryDate,
                    ManufacturerId = manufacturerId,
                    ManufacturerName = manufacturerName,
                    BarcodeValues = JsonSerializer.Serialize(transferredBarcodes),
                    StockBeforeBaseQuantity = stockBefore,
                    StockAfterBaseQuantity = stockAfter,
                    StockBeforeByUnitsJson = stockBeforeByUnitsCache.GetValueOrDefault(
                        (item.ProductId, item.ReceivingWarehouseId)),
                    Status = WarehouseImportLineStatus.Checked
                });
            }
        }

        await _warehouseImportReceiptRepository.InsertAsync(syntheticReceipt, autoSave: true);
        foreach (var syntheticItem in syntheticItems)
            await _warehouseImportItemRepository.InsertAsync(syntheticItem, autoSave: true);

        // Compute StockAfterByUnits: re-query after items inserted
        // Must iterate ALL combos, not just those with multi-unit before —
        // a receipt can introduce a new unit type making it multi-unit after.
        foreach (var kvp in stockBeforeByUnitsCache)
        {
            var prod = await _productRepository.GetAsync(kvp.Key.Item1);
            var afterByUnits = await ComputeStockByUnitsAsync(kvp.Key.Item1, kvp.Key.Item2, prod);
            var afterJson = afterByUnits.Count >= 2 ? JsonSerializer.Serialize(afterByUnits) : null;
            if (afterJson == null && kvp.Value == null) continue;
            var affectedItems = syntheticItems
                .Where(si => si.ProductId == kvp.Key.Item1 && si.StorageWarehouseId == kvp.Key.Item2);
            foreach (var si in affectedItems)
            {
                si.StockAfterByUnitsJson = afterJson;
                await _warehouseImportItemRepository.UpdateAsync(si, autoSave: true);
            }
        }

        receipt.SyntheticWarehouseReceiptId = syntheticReceipt.Id;
        receipt.Status = MaterialReceiptStatus.Confirmed;
        await _receiptRepository.UpdateAsync(receipt, autoSave: true);

        var barcodeTargets = items
            .SelectMany(item => new[]
            {
                (item.ProductId, centralWarehouse.Id),
                (item.ProductId, item.ReceivingWarehouseId),
            });
        await _barcodeStatusService.RecalculateForProductsAsync(barcodeTargets);

        return await GetAsync(receipt.Id);
    }

    [Authorize(LabSpecimenManagementPermissions.MaterialReceipts.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var receipt = await _receiptRepository.GetAsync(id);

        if (receipt.Status != MaterialReceiptStatus.Draft)
            throw new UserFriendlyException("Chỉ có thể xóa phiếu ở trạng thái Nháp.");

        var itemQuery = await _receiptItemRepository.GetQueryableAsync();
        var items = await _asyncExecuter.ToListAsync(
            itemQuery.Where(x => x.MaterialReceiptId == id));

        foreach (var item in items)
            await _receiptItemRepository.DeleteAsync(item.Id, autoSave: true);

        await _receiptRepository.DeleteAsync(id, autoSave: true);
    }

    public async Task<string> GetNextCodeAsync()
    {
        const string prefix = "RO";
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

    public async Task<List<CentralWarehouseProductDto>> GetCentralWarehouseProductsAsync(
        string? search = null, int? productType = null)
    {
        var centralWarehouse = await GetCentralWarehouseAsync();

        // Lấy tất cả products active (BA: hiển thị tất cả mặc dù không còn tồn kho)
        var productQuery = await _productRepository.GetQueryableAsync();
        var productsQuery = productQuery.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            productsQuery = productsQuery.Where(p =>
                (p.Code != null && p.Code.ToLower().Contains(s)) ||
                p.Name.ToLower().Contains(s) ||
                (p.IdentificationCode != null && p.IdentificationCode.ToLower().Contains(s)) ||
                (p.CASNumber != null && p.CASNumber.ToLower().Contains(s)));
        }

        if (productType.HasValue)
            productsQuery = productsQuery.Where(p => (int)p.ProductType == productType.Value);

        var products = await _asyncExecuter.ToListAsync(productsQuery);
        if (!products.Any())
            return new List<CentralWarehouseProductDto>();

        var productIds = products.Select(p => p.Id).ToList();

        // Lấy stock data per lot từ kho trung tâm (nếu có)
        var lotGroupsByProduct = new Dictionary<Guid, List<(string? LotNumber, decimal TotalBaseQty, DateTime? LotExpiryDate, string? ManufacturerName)>>();
        if (centralWarehouse != null)
        {
            var importItemQuery = await _warehouseImportItemRepository.GetQueryableAsync();
            var importReceiptQuery = await _warehouseImportReceiptRepository.GetQueryableAsync();

            var lotQuery = from item in importItemQuery
                           join receipt in importReceiptQuery on item.WarehouseImportReceiptId equals receipt.Id
                           where receipt.Status == WarehouseImportReceiptStatus.Completed
                                 && item.Status == WarehouseImportLineStatus.Checked
                                 && (item.ReceivedBaseQuantity - item.ConsumedBaseQuantity) > 0
                                 && productIds.Contains(item.ProductId)
                           let effectiveWarehouseId = item.StorageWarehouseId ?? receipt.WarehouseId
                           where effectiveWarehouseId == centralWarehouse.Id
                           select new { item.ProductId, Remaining = item.ReceivedBaseQuantity - item.ConsumedBaseQuantity, item.LotNumber, item.LotExpiryDate, item.ManufacturerName };

            var lotData = await _asyncExecuter.ToListAsync(lotQuery);
            lotGroupsByProduct = lotData.GroupBy(x => x.ProductId)
                .ToDictionary(g => g.Key, g => g.GroupBy(x => x.LotNumber ?? "")
                    .Select(lg => (
                        LotNumber: lg.Key == "" ? (string?)null : lg.Key,
                        TotalBaseQty: lg.Sum(x => x.Remaining),
                        LotExpiryDate: lg.Where(x => x.LotExpiryDate.HasValue)
                            .OrderBy(x => x.LotExpiryDate)
                            .Select(x => x.LotExpiryDate)
                            .FirstOrDefault(),
                        ManufacturerName: lg.Select(x => x.ManufacturerName)
                            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
                    )).ToList());
        }

        var skuQuery = await _productSkuRepository.GetQueryableAsync();
        var allSkus = await _asyncExecuter.ToListAsync(
            skuQuery.Where(s => productIds.Contains(s.ProductId)));
        var skusByProduct = allSkus.GroupBy(s => s.ProductId).ToDictionary(g => g.Key, g => g.ToList());

        var unitIds = allSkus.SelectMany(s => new[] { s.PackagingUnitId, s.VolumeUnitId })
            .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var unitQuery = await _unitRepository.GetQueryableAsync();
        var units = await _asyncExecuter.ToListAsync(unitQuery.Where(u => unitIds.Contains(u.Id)));
        var unitMap = units.ToDictionary(u => u.Id, u => u.Name);

        // Fallback: manufacturer names từ SKU cho hàng nhập cũ không có ManufacturerName trên lot
        var manufacturerIds = allSkus.Select(s => s.ManufacturerId).Distinct().ToList();
        var manufacturerQuery = await _manufacturerRepository.GetQueryableAsync();
        var manufacturers = manufacturerIds.Count > 0
            ? await _asyncExecuter.ToListAsync(manufacturerQuery.Where(m => manufacturerIds.Contains(m.Id)))
            : new List<Manufacturer>();
        var manufacturerNameMap = manufacturers.ToDictionary(m => m.Id, m => m.Name);
        var skuManufacturerByProduct = skusByProduct.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value
                .Select(s => manufacturerNameMap.GetValueOrDefault(s.ManufacturerId))
                .FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)));

        var result = new List<CentralWarehouseProductDto>();
        foreach (var p in products)
        {
            var productSkus = skusByProduct.GetValueOrDefault(p.Id) ?? new List<ProductSku>();
            var firstSku = productSkus.FirstOrDefault();
            var availableUnits = BuildAvailableUnits(productSkus, unitMap);
            var skuDtos = productSkus.Select(s => new CentralWarehouseProductSkuDto
            {
                SkuId = s.Id,
                ManufacturerId = s.ManufacturerId,
                ManufacturerName = manufacturerNameMap.GetValueOrDefault(s.ManufacturerId),
                StorageUnitId = s.PackagingUnitId,
                StorageUnitName = s.PackagingUnitId.HasValue ? unitMap.GetValueOrDefault(s.PackagingUnitId.Value) : null,
                ConsumableUnitId = s.VolumeUnitId,
                ConsumableUnitName = s.VolumeUnitId.HasValue ? unitMap.GetValueOrDefault(s.VolumeUnitId.Value) : null,
                VolumePerUnit = s.VolumePerUnit,
                PackagingQuantity = s.PackagingQuantity
            }).ToList();

            CentralWarehouseProductDto BuildDto(decimal stockQty, DateTime? lotExpiryDate,
                string? lotNumber, string? manufacturerName)
            {
                var resolvedManufacturerName = manufacturerName
                    ?? skuManufacturerByProduct.GetValueOrDefault(p.Id);

                var lotSku = !string.IsNullOrWhiteSpace(resolvedManufacturerName)
                    ? productSkus.FirstOrDefault(s =>
                        string.Equals(
                            manufacturerNameMap.GetValueOrDefault(s.ManufacturerId),
                            resolvedManufacturerName,
                            StringComparison.OrdinalIgnoreCase))
                        ?? firstSku
                    : firstSku;

                return new CentralWarehouseProductDto
                {
                    ProductId = p.Id,
                    ProductName = p.Name,
                    FormattedName = p.FormattedName,
                    IdentificationCode = p.IdentificationCode,
                    TechnicalSpec = p.MainTechnicalSpecs,
                    CASNumber = p.CASNumber,
                    ProductType = (int)p.ProductType,
                    CentralStockBaseQty = stockQty,
                    NearestExpiryDate = lotExpiryDate,
                    LotNumber = lotNumber,
                    LotExpiryDate = lotExpiryDate,
                    ManufacturerName = resolvedManufacturerName,
                    StorageUnitName = lotSku?.PackagingUnitId.HasValue == true
                        ? unitMap.GetValueOrDefault(lotSku.PackagingUnitId!.Value) : null,
                    ConsumableUnitName = lotSku?.VolumeUnitId.HasValue == true
                        ? unitMap.GetValueOrDefault(lotSku.VolumeUnitId!.Value) : null,
                    VolumePerUnit = lotSku?.VolumePerUnit,
                    AvailableUnits = availableUnits,
                    Skus = skuDtos
                };
            }

            if (lotGroupsByProduct.TryGetValue(p.Id, out var lots) && lots.Count > 0)
            {
                foreach (var lot in lots)
                    result.Add(BuildDto(lot.TotalBaseQty, lot.LotExpiryDate, lot.LotNumber, lot.ManufacturerName));
            }
            else
            {
                result.Add(BuildDto(0, null, null, null));
            }
        }
        return result;
    }

    public async Task<ReceivingWarehouseStockDto> GetReceivingWarehouseStockAsync(
        Guid productId, Guid warehouseId)
    {
        var warehouse = await _warehouseRepository.GetAsync(warehouseId);
        if (!warehouse.IsActive || warehouse.IsDefault)
            throw new UserFriendlyException("Kho nhận không hợp lệ.");

        var product = await _productRepository.GetAsync(productId);
        var importItemQuery = await _warehouseImportItemRepository.GetQueryableAsync();
        var importReceiptQuery = await _warehouseImportReceiptRepository.GetQueryableAsync();

        // BA yêu cầu tồn tại đúng kho nhận, không cộng tồn của các kho con.
        var lotQuery = from item in importItemQuery
                       join receipt in importReceiptQuery
                           on item.WarehouseImportReceiptId equals receipt.Id
                       where item.ProductId == productId
                             && receipt.Status == WarehouseImportReceiptStatus.Completed
                             && item.Status == WarehouseImportLineStatus.Checked
                             && (item.ReceivedBaseQuantity - item.ConsumedBaseQuantity) > 0
                       let effectiveWarehouseId = item.StorageWarehouseId ?? receipt.WarehouseId
                       where effectiveWarehouseId == warehouseId
                       select new
                       {
                           Remaining = item.ReceivedBaseQuantity - item.ConsumedBaseQuantity,
                           item.SkuId
                       };

        var lots = await _asyncExecuter.ToListAsync(lotQuery);
        var availableBaseQuantity = lots.Sum(x => x.Remaining);

        ProductSku? stockSku = null;
        var stockSkuId = lots.FirstOrDefault(x => x.SkuId.HasValue)?.SkuId;
        if (stockSkuId.HasValue)
            stockSku = await _productSkuRepository.FindAsync(stockSkuId.Value);

        var storageUnitId = stockSku?.PackagingUnitId
                            ?? product.PackagingUnitId
                            ?? product.BaseUnitId;
        var consumableUnitId = stockSku?.VolumeUnitId ?? product.VolumeUnitId;
        var unitIds = new[] { storageUnitId, consumableUnitId }
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();
        var unitQuery = await _unitRepository.GetQueryableAsync();
        var units = await _asyncExecuter.ToListAsync(
            unitQuery.Where(x => unitIds.Contains(x.Id)));
        var unitMap = units.ToDictionary(x => x.Id, x => x.Name);

        return new ReceivingWarehouseStockDto
        {
            ProductId = productId,
            WarehouseId = warehouseId,
            AvailableBaseQuantity = availableBaseQuantity,
            StorageUnitName = storageUnitId.HasValue
                ? unitMap.GetValueOrDefault(storageUnitId.Value)
                : null,
            ConsumableUnitName = consumableUnitId.HasValue
                ? unitMap.GetValueOrDefault(consumableUnitId.Value)
                : null,
            VolumePerUnit = stockSku?.VolumePerUnit ?? product.VolumePerUnit
        };
    }

    // --- Private helpers ---

    private async Task<Warehouse?> GetCentralWarehouseAsync()
    {
        var warehouseQuery = await _warehouseRepository.GetQueryableAsync();
        return await _asyncExecuter.FirstOrDefaultAsync(
            warehouseQuery.Where(w => w.IsDefault && w.IsActive));
    }

    private async Task<decimal> GetWarehouseStockAsync(Guid productId, Guid warehouseId)
    {
        var importItemQuery = await _warehouseImportItemRepository.GetQueryableAsync();
        var importReceiptQuery = await _warehouseImportReceiptRepository.GetQueryableAsync();

        var query = from item in importItemQuery
                    join receipt in importReceiptQuery on item.WarehouseImportReceiptId equals receipt.Id
                    where item.ProductId == productId
                          && receipt.Status == WarehouseImportReceiptStatus.Completed
                          && item.Status == WarehouseImportLineStatus.Checked
                          && (item.ReceivedBaseQuantity - item.ConsumedBaseQuantity) > 0
                    let effectiveWarehouseId = item.StorageWarehouseId ?? receipt.WarehouseId
                    where effectiveWarehouseId == warehouseId
                    select item.ReceivedBaseQuantity - item.ConsumedBaseQuantity;

        var remainings = await _asyncExecuter.ToListAsync(query);
        return remainings.Sum();
    }

    private async Task<List<StockByUnitDto>> ComputeStockByUnitsAsync(
        Guid productId, Guid warehouseId, Product product)
    {
        var importItemQuery = await _warehouseImportItemRepository.GetQueryableAsync();
        var importReceiptQuery = await _warehouseImportReceiptRepository.GetQueryableAsync();
        var skuQuery = (await _productSkuRepository.GetQueryableAsync())
            .Where(s => s.ProductId == productId);

        var lotData = await _asyncExecuter.ToListAsync(
            from item in importItemQuery
            join receipt in importReceiptQuery on item.WarehouseImportReceiptId equals receipt.Id
            join skuCandidate in skuQuery on item.SkuId equals (Guid?)skuCandidate.Id into skuGroup
            from sku in skuGroup.DefaultIfEmpty()
            where item.ProductId == productId
                  && receipt.Status == WarehouseImportReceiptStatus.Completed
                  && item.Status == WarehouseImportLineStatus.Checked
                  && (item.ReceivedBaseQuantity - item.ConsumedBaseQuantity) > 0
            let effectiveWarehouseId = item.StorageWarehouseId ?? receipt.WarehouseId
            where effectiveWarehouseId == warehouseId
            select new
            {
                Remaining = item.ReceivedBaseQuantity - item.ConsumedBaseQuantity,
                PackagingUnitId = sku != null ? sku.PackagingUnitId : (product.PackagingUnitId ?? product.BaseUnitId),
                VolumePerUnit = sku != null ? sku.VolumePerUnit : product.VolumePerUnit,
                VolumeUnitId = sku != null ? sku.VolumeUnitId : product.VolumeUnitId,
            });

        // Resolve unit names first, then group by NAME to merge same-name units from different SKUs
        var allUnitIds = lotData.Where(x => x.PackagingUnitId.HasValue).Select(x => x.PackagingUnitId!.Value)
            .Concat(lotData.Where(x => x.VolumeUnitId.HasValue).Select(x => x.VolumeUnitId!.Value))
            .Distinct().ToList();
        var unitMap = allUnitIds.Count == 0
            ? new Dictionary<Guid, string>()
            : (await _asyncExecuter.ToListAsync(
                (await _unitRepository.GetQueryableAsync()).Where(u => allUnitIds.Contains(u.Id))))
                .ToDictionary(u => u.Id, u => u.Name);

        var grouped = lotData
            .GroupBy(x => x.PackagingUnitId.HasValue ? unitMap.GetValueOrDefault(x.PackagingUnitId.Value) : null)
            .Where(g => g.Key != null)
            .Select(g =>
            {
                var totalQty = g.Sum(x => x.Remaining);
                var totalVol = g.Sum(x => x.VolumePerUnit.HasValue && x.VolumePerUnit.Value > 0
                    ? x.Remaining * x.VolumePerUnit.Value : 0);
                return new
                {
                    PackagingUnitName = g.Key!,
                    Quantity = totalQty,
                    VolumePerUnit = totalQty > 0 && totalVol > 0
                        ? (int?)Math.Round(totalVol / totalQty)
                        : g.First().VolumePerUnit,
                    VolumeUnitId = g.Select(x => x.VolumeUnitId).FirstOrDefault(x => x.HasValue),
                };
            })
            .ToList();

        if (grouped.Count < 2) return new List<StockByUnitDto>();

        return grouped
            .OrderByDescending(x => x.Quantity)
            .Select(x => new StockByUnitDto
            {
                PackagingUnitName = x.PackagingUnitName,
                Quantity = x.Quantity,
                VolumePerUnit = x.VolumePerUnit,
                VolumeUnitName = x.VolumeUnitId.HasValue && unitMap.TryGetValue(x.VolumeUnitId.Value, out var vName)
                    ? vName : null,
            })
            .ToList();
    }

    private async Task<DateTime?> GetNearestExpiryDateAsync(Guid productId, Guid warehouseId)
    {
        var importItemQuery = await _warehouseImportItemRepository.GetQueryableAsync();
        var importReceiptQuery = await _warehouseImportReceiptRepository.GetQueryableAsync();

        var query = from item in importItemQuery
                    join receipt in importReceiptQuery on item.WarehouseImportReceiptId equals receipt.Id
                    where item.ProductId == productId
                          && receipt.Status == WarehouseImportReceiptStatus.Completed
                          && item.Status == WarehouseImportLineStatus.Checked
                          && (item.ReceivedBaseQuantity - item.ConsumedBaseQuantity) > 0
                          && item.LotExpiryDate.HasValue
                    let effectiveWarehouseId = item.StorageWarehouseId ?? receipt.WarehouseId
                    where effectiveWarehouseId == warehouseId
                    orderby item.LotExpiryDate
                    select item.LotExpiryDate;

        return await _asyncExecuter.FirstOrDefaultAsync(query);
    }

    private async Task<decimal> GetWarehouseManufacturerStockAsync(
        Guid productId,
        Guid warehouseId,
        Guid? manufacturerId)
    {
        var itemQuery = await _warehouseImportItemRepository.GetQueryableAsync();
        var receiptQuery = await _warehouseImportReceiptRepository.GetQueryableAsync();
        var lots = await _asyncExecuter.ToListAsync(
            from item in itemQuery
            join receipt in receiptQuery on item.WarehouseImportReceiptId equals receipt.Id
            where item.ProductId == productId
                  && receipt.Status == WarehouseImportReceiptStatus.Completed
                  && item.Status == WarehouseImportLineStatus.Checked
                  && (item.ReceivedBaseQuantity - item.ConsumedBaseQuantity) > 0
            let effectiveWarehouseId = item.StorageWarehouseId ?? receipt.WarehouseId
            where effectiveWarehouseId == warehouseId
            select new
            {
                Remaining = item.ReceivedBaseQuantity - item.ConsumedBaseQuantity,
                item.ManufacturerId,
                item.SkuId
            });

        var skuIds = lots.Where(x => x.SkuId.HasValue).Select(x => x.SkuId!.Value).Distinct().ToList();
        var skuManufacturers = skuIds.Count == 0
            ? new Dictionary<Guid, Guid>()
            : (await _asyncExecuter.ToListAsync(
                (await _productSkuRepository.GetQueryableAsync()).Where(x => skuIds.Contains(x.Id))))
                .ToDictionary(x => x.Id, x => x.ManufacturerId);

        return lots.Where(x =>
        {
            var lotManufacturerId = x.ManufacturerId;
            if (!lotManufacturerId.HasValue && x.SkuId.HasValue)
                lotManufacturerId = skuManufacturers.GetValueOrDefault(x.SkuId.Value);
            return lotManufacturerId == manufacturerId;
        }).Sum(x => x.Remaining);
    }

    /// <summary>
    /// Hybrid stock: receipt chain + stock adjustment baseline.
    /// Default: MAX(StockAfter) from receipt chain (immune to FIFO consumption deductions).
    /// If a confirmed stock adjustment is more recent than the latest receipt: uses adjusted stock as baseline.
    /// </summary>
    private async Task<decimal> GetReceiptChainStockAsync(
        Guid productId,
        Guid warehouseId,
        Guid? manufacturerId)
    {
        var itemQuery = await _warehouseImportItemRepository.GetQueryableAsync();
        var receiptQuery = await _warehouseImportReceiptRepository.GetQueryableAsync();

        var candidates = await _asyncExecuter.ToListAsync(
            from item in itemQuery
            join receipt in receiptQuery on item.WarehouseImportReceiptId equals receipt.Id
            where item.ProductId == productId
                  && receipt.ReceiptType == WarehouseImportReceiptType.MaterialReceipt
                  && receipt.Status == WarehouseImportReceiptStatus.Completed
                  && item.Status == WarehouseImportLineStatus.Checked
                  && item.StockAfterBaseQuantity.HasValue
            let effectiveWarehouseId = item.StorageWarehouseId ?? receipt.WarehouseId
            where effectiveWarehouseId == warehouseId
            select new
            {
                item.StockAfterBaseQuantity,
                item.ReceivedBaseQuantity,
                item.ManufacturerId,
                item.SkuId,
                receipt.CreationTime
            });

        var skuIds = candidates.Where(x => x.SkuId.HasValue).Select(x => x.SkuId!.Value).Distinct().ToList();
        var skuManufacturers = skuIds.Count == 0
            ? new Dictionary<Guid, Guid>()
            : (await _asyncExecuter.ToListAsync(
                (await _productSkuRepository.GetQueryableAsync()).Where(x => skuIds.Contains(x.Id))))
                .ToDictionary(x => x.Id, x => x.ManufacturerId);

        Guid? ResolveMfr(Guid? mfrId, Guid? skuId)
        {
            if (mfrId.HasValue) return mfrId;
            if (skuId.HasValue && skuManufacturers.TryGetValue(skuId.Value, out var mfr)) return mfr;
            return null;
        }

        var matched = candidates
            .Where(x => ResolveMfr(x.ManufacturerId, x.SkuId) == manufacturerId).ToList();
        var chainStock = matched.Count > 0
            ? matched.Max(x => x.StockAfterBaseQuantity!.Value) : 0m;
        var latestReceiptTime = matched.Count > 0
            ? matched.Max(x => (DateTime?)x.CreationTime) : null;

        var adjItemQ = await _stockAdjustmentItemRepository.GetQueryableAsync();
        var adjQ = await _stockAdjustmentRepository.GetQueryableAsync();

        var adjCandidates = await _asyncExecuter.ToListAsync(
            from adjItem in adjItemQ
            join adj in adjQ on adjItem.StockAdjustmentId equals adj.Id
            where adjItem.ProductId == productId
                  && adjItem.WarehouseId == warehouseId
                  && adj.Status == StockAdjustmentStatus.Confirmed
            select new
            {
                adjItem.QuantityBefore,
                adjItem.AdjustmentQuantity,
                adj.LastModificationTime,
                adj.CreationTime
            });

        var latestAdj = adjCandidates
            .Select(a => new
            {
                a.QuantityBefore,
                a.AdjustmentQuantity,
                AdjTime = a.LastModificationTime ?? a.CreationTime
            })
            .Where(a => !latestReceiptTime.HasValue || a.AdjTime > latestReceiptTime.Value)
            .OrderByDescending(a => a.AdjTime)
            .FirstOrDefault();

        if (latestAdj == null)
            return chainStock;

        var adjustedTotal = latestAdj.QuantityBefore - latestAdj.AdjustmentQuantity;

        var preAdjGroups = candidates
            .Where(x => x.CreationTime <= latestAdj.AdjTime)
            .GroupBy(x => ResolveMfr(x.ManufacturerId, x.SkuId))
            .Select(g => new { Mfr = g.Key, MaxStock = g.Max(x => x.StockAfterBaseQuantity!.Value) })
            .ToList();

        var totalChainBeforeAdj = preAdjGroups.Sum(g => g.MaxStock);
        var mfrChainBeforeAdj = preAdjGroups
            .FirstOrDefault(g => g.Mfr == manufacturerId)?.MaxStock ?? 0m;
        decimal allocatedStock;
        if (totalChainBeforeAdj > 0 && mfrChainBeforeAdj > 0)
            allocatedStock = adjustedTotal * mfrChainBeforeAdj / totalChainBeforeAdj;
        else
            allocatedStock = adjustedTotal;

        var receiptsAfterAdj = matched
            .Where(x => x.CreationTime > latestAdj.AdjTime)
            .Sum(x => x.ReceivedBaseQuantity);

        return Math.Max(0, allocatedStock + receiptsAfterAdj);
    }

    private async Task<List<string>> MoveInventoryBarcodesAsync(
        WarehouseImportReceiptItem originalLotItem,
        Guid sourceWarehouseId,
        Guid receivingWarehouseId,
        Guid materialReceiptId,
        decimal deductedBaseQuantity)
    {
        var remainingBaseQuantity = Math.Max(
            0,
            originalLotItem.ReceivedBaseQuantity - originalLotItem.ConsumedBaseQuantity);
        var transferPlan = CalculateBarcodeTransferPlan(
            deductedBaseQuantity,
            remainingBaseQuantity);

        var query = await _inventoryBarcodeRepository.GetQueryableAsync();
        var allOriginBarcodes = await _asyncExecuter.ToListAsync(query
            .Where(x => x.OriginWarehouseImportReceiptItemId == originalLotItem.Id)
            .OrderBy(x => x.CreationTime));
        var available = allOriginBarcodes
            .Where(x => x.CurrentWarehouseId == sourceWarehouseId)
            .ToList();

        // Legacy lots without barcodes: generate the missing ones and insert them in one batch.
        var nextLegacyIndex = allOriginBarcodes.Count + 1;
        var legacyBarcodes = new List<InventoryBarcode>();
        while (available.Count < transferPlan.RequiredSourceBarcodeCount)
        {
            var code = $"LEGACY-{originalLotItem.Id:N}-{nextLegacyIndex:D3}";
            nextLegacyIndex++;
            var barcode = new InventoryBarcode(
                _guidGenerator.Create(),
                code,
                originalLotItem.ProductId,
                originalLotItem.Id,
                sourceWarehouseId)
            {
                SkuId = originalLotItem.SkuId
            };
            legacyBarcodes.Add(barcode);
            available.Add(barcode);
        }
        // Insert before updating: the legacy barcodes may be among the ones moved below.
        // BulkInsertAsync leaves them untracked, so UpdateManyAsync attaches them as Modified.
        if (legacyBarcodes.Count > 0)
            await _inventoryBarcodeRepository.BulkInsertAsync(legacyBarcodes);

        // Move the selected barcodes in one batch (one SaveChanges instead of one per barcode).
        // autoSave: true keeps the DB state visible to later steps of the same confirm
        // (next lot of the same item, barcode status recalculation).
        var selected = available.Take(transferPlan.SourceBarcodesToMove).ToList();
        foreach (var barcode in selected)
        {
            barcode.CurrentWarehouseId = receivingWarehouseId;
            barcode.LastMaterialReceiptId = materialReceiptId;
        }
        if (selected.Count > 0)
            await _inventoryBarcodeRepository.UpdateManyAsync(selected, autoSave: true);

        return selected.Select(x => x.Code).ToList();
    }

    internal static BarcodeTransferPlan CalculateBarcodeTransferPlan(
        decimal deductedBaseQuantity,
        decimal remainingBaseQuantity)
    {
        if (deductedBaseQuantity <= 0)
            return new BarcodeTransferPlan(0, 0);

        var wholeUnitCount = (int)Math.Floor(deductedBaseQuantity);
        var hasFraction = deductedBaseQuantity > wholeUnitCount;
        var sourceBarcodesToMove = wholeUnitCount + (hasFraction ? 1 : 0);

        return new BarcodeTransferPlan(sourceBarcodesToMove, sourceBarcodesToMove);
    }

    internal readonly record struct BarcodeTransferPlan(
        int SourceBarcodesToMove,
        int RequiredSourceBarcodeCount);

    private async Task MapItemsAsync(MaterialReceipt receipt, List<CreateUpdateMaterialReceiptItemDto> inputItems)
    {
        foreach (var inputItem in inputItems)
        {
            MaterialReceiptItem item;

            if (inputItem.Id.HasValue)
            {
                item = await _receiptItemRepository.GetAsync(inputItem.Id.Value);
                if (item.MaterialReceiptId != receipt.Id)
                    throw new UserFriendlyException("Dòng vật tư không thuộc phiếu nhận hiện tại.");
            }
            else
            {
                item = new MaterialReceiptItem(
                    _guidGenerator.Create(),
                    receipt.Id,
                    inputItem.ProductId,
                    "",
                    inputItem.Quantity,
                    inputItem.ReceivingWarehouseId,
                    "");
            }

            var product = await _productRepository.GetAsync(inputItem.ProductId);
            item.ProductId = inputItem.ProductId;
            item.ProductName = product.Name;
            item.Quantity = inputItem.Quantity;
            item.UnitId = inputItem.UnitId;
            item.SkuId = inputItem.SkuId;
            item.ReceivingWarehouseId = inputItem.ReceivingWarehouseId;

            if (inputItem.UnitId.HasValue)
            {
                var unit = await _unitRepository.FindAsync(inputItem.UnitId.Value);
                item.UnitName = unit?.Name;
            }
            else
            {
                item.UnitName = null;
            }

            var warehouse = await _warehouseRepository.GetAsync(inputItem.ReceivingWarehouseId);
            item.ReceivingWarehouseName = warehouse.Name;
            item.LotNumber = inputItem.LotNumber;
            item.ManufacturerName = inputItem.ManufacturerName;

            if (inputItem.Id.HasValue)
                await _receiptItemRepository.UpdateAsync(item, autoSave: true);
            else
                await _receiptItemRepository.InsertAsync(item, autoSave: true);
        }
    }

    private async Task ValidateCodeUniqueAsync(string code, Guid? excludeId = null)
    {
        var query = await _receiptRepository.GetQueryableAsync();
        var exists = await _asyncExecuter.AnyAsync(
            query.Where(x => x.Code == code && (!excludeId.HasValue || x.Id != excludeId.Value)));

        if (exists)
            throw new UserFriendlyException($"Mã phiếu \"{code}\" đã tồn tại.");
    }

    private async Task ValidateInputAsync(CreateUpdateMaterialReceiptDto input)
    {
        if (input.Items == null || input.Items.Count == 0)
            throw new UserFriendlyException("Phiếu nhận vật tư phải có ít nhất 1 vật tư.");

        foreach (var item in input.Items)
        {
            if (item.Quantity <= 0 || decimal.Truncate(item.Quantity) != item.Quantity)
                throw new UserFriendlyException(
                    $"Số lượng của vật tư phải là số nguyên lớn hơn 0.");
        }

        var warehouseIds = input.Items
            .Select(x => x.ReceivingWarehouseId)
            .Distinct()
            .ToList();
        var warehouseQuery = await _warehouseRepository.GetQueryableAsync();
        var receivingWarehouses = await _asyncExecuter.ToListAsync(
            warehouseQuery.Where(x => warehouseIds.Contains(x.Id)));

        if (receivingWarehouses.Count != warehouseIds.Count ||
            receivingWarehouses.Any(x => !x.IsActive || x.IsDefault))
        {
            throw new UserFriendlyException(
                "Kho nhận không hợp lệ hoặc là kho Trung tâm kiểm nghiệm.");
        }

        foreach (var inputItem in input.Items.Where(x => x.SkuId.HasValue))
        {
            var sku = await _productSkuRepository.GetAsync(inputItem.SkuId!.Value);
            if (sku.ProductId != inputItem.ProductId ||
                inputItem.UnitId != sku.PackagingUnitId)
            {
                throw new UserFriendlyException(
                    "Đơn vị hoặc cấu hình SKU của vật tư không hợp lệ.");
            }
        }
    }

    private async Task<string> GetCurrentUserNameAsync()
    {
        if (CurrentUser.Id.HasValue)
        {
            var names = await _userNameProvider.GetUserNamesAsync(new List<Guid?> { CurrentUser.Id.Value });
            return names.GetValueOrDefault(CurrentUser.Id.Value) ?? CurrentUser.UserName ?? "Unknown";
        }
        return CurrentUser.UserName ?? "Unknown";
    }

    private static List<ProductUnitOptionDto> BuildAvailableUnits(
        List<ProductSku> skus, Dictionary<Guid, string> unitMap)
    {
        var units = new Dictionary<Guid, ProductUnitOptionDto>();

        foreach (var sku in skus)
        {
            if (sku.PackagingUnitId.HasValue && !units.ContainsKey(sku.PackagingUnitId.Value))
            {
                units[sku.PackagingUnitId.Value] = new ProductUnitOptionDto
                {
                    UnitId = sku.PackagingUnitId.Value,
                    UnitName = unitMap.GetValueOrDefault(sku.PackagingUnitId.Value) ?? "",
                    IsConsumable = false
                };
            }
        }

        return units.Values.ToList();
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
            return Math.Round(
                quantity / sku.VolumePerUnit.Value,
                4,
                MidpointRounding.AwayFromZero);
        }

        return quantity;
    }

    private static IQueryable<MaterialReceipt> ApplySorting(IQueryable<MaterialReceipt> query, string sorting)
    {
        var parts = sorting.Split(' ', 2);
        var field = parts[0].ToLower();
        var desc = parts.Length > 1 && parts[1].ToLower() == "desc";

        return field switch
        {
            "code" => desc ? query.OrderByDescending(x => x.Code) : query.OrderBy(x => x.Code),
            "receiptdate" => desc ? query.OrderByDescending(x => x.ReceiptDate) : query.OrderBy(x => x.ReceiptDate),
            "receivername" => desc ? query.OrderByDescending(x => x.ReceiverName) : query.OrderBy(x => x.ReceiverName),
            "status" => desc ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            _ => query.OrderByDescending(x => x.CreationTime)
        };
    }
}
