using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
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
        IUnitConversionResolver unitConversionResolver)
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
    }

    private static readonly GoodsReceiptOrderStatus[] EligibleOrderStatuses =
    {
        GoodsReceiptOrderStatus.Ordering,
        GoodsReceiptOrderStatus.WaitingReceipt
    };

    public async Task<PagedResultDto<WarehouseImportReceiptListDto>> GetListAsync(GetWarehouseImportReceiptListInput input)
    {
        var query = await _receiptRepository.GetQueryableAsync();
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
                x.GoodsReceiptCode.ToLower().Contains(search) ||
                matchingGoodsReceiptIds.Contains(x.GoodsReceiptId));
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
            if (goodsReceiptInfoById.TryGetValue(dto.GoodsReceiptId, out var grInfo))
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

        var goodsReceipt = await _goodsReceiptRepository.GetAsync(receipt.GoodsReceiptId);
        var poLines = (await _goodsReceiptItemRepository.GetQueryableAsync())
            .Where(x => x.GoodsReceiptId == goodsReceipt.Id && !x.IsEquipment)
            .ToList();

        var checkedItemsByLine = receipt.Items
            .Where(x => x.Status == WarehouseImportLineStatus.Checked)
            .GroupBy(x => x.GoodsReceiptItemId);

        foreach (var group in checkedItemsByLine)
        {
            var poLine = poLines.FirstOrDefault(x => x.Id == group.Key);
            if (poLine == null)
                throw new UserFriendlyException("Không tìm thấy dòng hàng tương ứng trên phiếu mua hàng.");

            var cumulativeCompletedChecked = await GetCumulativeCheckedQtyAsync(poLine.Id, excludeReceiptId: receipt.Id);
            var sumCurrentChecked = group.Sum(x => x.ActualReceivedQuantity);
            var remaining = poLine.Quantity - cumulativeCompletedChecked;

            if (sumCurrentChecked > remaining)
                throw new UserFriendlyException($"Số lượng nhận của \"{poLine.ProductName}\" vượt quá số lượng còn lại có thể nhận ({remaining}).");
        }

        foreach (var item in receipt.Items.Where(x => x.Status == WarehouseImportLineStatus.Checked))
        {
            var product = await _productRepository.GetAsync(item.ProductId);
            var baseQty = await _unitConversionResolver.ResolveBaseQuantityAsync(product, item.UnitId, item.ActualReceivedQuantity, item.ProductName);
            product.AvailableQuantity += baseQty;
            await _productRepository.UpdateAsync(product, autoSave: true);
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
            .Where(x => x.GoodsReceiptId == goodsReceiptId && !x.IsEquipment)
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

        var result = new List<GoodsReceiptItemForImportDto>();
        foreach (var line in lines)
        {
            var historicalForLine = allHistoricalItems.Where(x => x.GoodsReceiptItemId == line.Id).ToList();
            var alreadyReceived = historicalForLine
                .Where(x => x.Status == WarehouseImportLineStatus.Checked)
                .Sum(x => x.ActualReceivedQuantity);
            var remaining = Math.Max(0, line.Quantity - alreadyReceived);

            var dto = new GoodsReceiptItemForImportDto
            {
                GoodsReceiptItemId = line.Id,
                ProductId = line.ProductId!.Value,
                ProductName = line.ProductName,
                UnitId = line.UnitId,
                UnitName = line.UnitName,
                OrderedQuantity = line.Quantity,
                AlreadyReceivedQuantity = alreadyReceived,
                RemainingOrderableQuantity = remaining,
                PreviousLots = historicalForLine.Select(x => new PreviousReceivedLotDto
                {
                    LotNumber = x.LotNumber,
                    LotExpiryDate = x.LotExpiryDate,
                    ActualReceivedQuantity = x.ActualReceivedQuantity,
                    Status = x.Status,
                    QualityDocumentFileId = x.QualityDocumentFileId,
                    QualityDocumentFileName = x.QualityDocumentFileName,
                    QualityDocumentFileSize = x.QualityDocumentFileSize
                }).ToList()
            };

            var product = await _productRepository.FindAsync(line.ProductId.Value);
            if (product != null && product.BaseUnitId.HasValue && product.BaseUnitId != line.UnitId)
            {
                var baseUnit = await _unitRepository.FindAsync(product.BaseUnitId.Value);
                dto.BaseUnitName = baseUnit?.Name;
                dto.BaseOrderedQuantity = await _unitConversionResolver.ResolveBaseQuantityAsync(
                    product, line.UnitId, line.Quantity, line.ProductName);
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
        var productCodeById = (await _productRepository.GetQueryableAsync())
            .Where(x => productIds.Contains(x.Id))
            .ToList()
            .ToDictionary(x => x.Id, x => x.Code);

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
                ProductCode = productCodeById.TryGetValue(item.ProductId, out var code) ? code : null,
                ProductName = item.ProductName,
                ActualReceivedQuantity = item.ActualReceivedQuantity,
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

            var item = new WarehouseImportReceiptItem(
                GuidGenerator.Create(),
                receipt.Id,
                poLine.Id,
                poLine.ProductId!.Value,
                poLine.ProductName,
                poLine.Quantity)
            {
                ActualReceivedQuantity = input.ActualReceivedQuantity,
                UnitId = poLine.UnitId,
                UnitName = poLine.UnitName,
                LotNumber = input.LotNumber,
                LotExpiryDate = input.LotExpiryDate,
                Status = input.Status,
                QualityDocumentFileId = input.QualityDocumentFileId,
                QualityDocumentFileName = input.QualityDocumentFileName,
                QualityDocumentFilePath = input.QualityDocumentFilePath,
                QualityDocumentFileSize = input.QualityDocumentFileSize,
                Notes = input.Notes
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

        if (completedReceiptIds.Count == 0)
        {
            return 0;
        }

        return (await _itemRepository.GetQueryableAsync())
            .Where(x =>
                x.GoodsReceiptItemId == goodsReceiptItemId &&
                x.Status == WarehouseImportLineStatus.Checked &&
                completedReceiptIds.Contains(x.WarehouseImportReceiptId))
            .Sum(x => (decimal?)x.ActualReceivedQuantity) ?? 0;
    }

    private async Task RecomputeOrderStatusAsync(GoodsReceipt goodsReceipt, List<GoodsReceiptItem> poLines)
    {
        var fullyReceived = true;
        foreach (var line in poLines)
        {
            var cumulative = await GetCumulativeCheckedQtyAsync(line.Id, excludeReceiptId: null);
            if (cumulative < line.Quantity)
            {
                fullyReceived = false;
                break;
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
        dto.Items = receipt.Items
            .Select(x => ObjectMapper.Map<WarehouseImportReceiptItem, WarehouseImportReceiptItemDto>(x))
            .ToList();

        var goodsReceipt = await _goodsReceiptRepository.FindAsync(receipt.GoodsReceiptId);
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
}
