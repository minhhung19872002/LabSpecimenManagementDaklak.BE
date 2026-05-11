using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.Equipments;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.Products;
using LabSpecimenManagement.Receipts;
using LabSpecimenManagement.Suppliers;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace LabSpecimenManagement.GoodsReceipts;

[RemoteService(IsEnabled = false)]
[Authorize(LabSpecimenManagementPermissions.GoodsReceipts.Default)]
public class GoodsReceiptAppService : ApplicationService, IGoodsReceiptAppService
{
    private readonly IRepository<GoodsReceipt, Guid> _goodsReceiptRepository;
    private readonly IRepository<GoodsReceiptItem, Guid> _itemRepository;
    private readonly IRepository<GoodsReceiptPayment, Guid> _paymentRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<ProductUnitConversion, Guid> _conversionRepository;
    private readonly IRepository<Supplier, Guid> _supplierRepository;
    private readonly IRepository<Unit, Guid> _unitRepository;
    private readonly IRepository<Equipment, Guid> _equipmentRepository;
    private readonly ICodeGeneratorService _codeGenerator;
    private readonly IDataFilter _dataFilter;
    private readonly IUserNameProvider _userNameProvider;

    public GoodsReceiptAppService(
        IRepository<GoodsReceipt, Guid> goodsReceiptRepository,
        IRepository<GoodsReceiptItem, Guid> itemRepository,
        IRepository<GoodsReceiptPayment, Guid> paymentRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<ProductUnitConversion, Guid> conversionRepository,
        IRepository<Supplier, Guid> supplierRepository,
        IRepository<Unit, Guid> unitRepository,
        IRepository<Equipment, Guid> equipmentRepository,
        ICodeGeneratorService codeGenerator,
        IDataFilter dataFilter,
        IUserNameProvider userNameProvider)
    {
        _goodsReceiptRepository = goodsReceiptRepository;
        _itemRepository = itemRepository;
        _paymentRepository = paymentRepository;
        _productRepository = productRepository;
        _conversionRepository = conversionRepository;
        _supplierRepository = supplierRepository;
        _unitRepository = unitRepository;
        _equipmentRepository = equipmentRepository;
        _codeGenerator = codeGenerator;
        _dataFilter = dataFilter;
        _userNameProvider = userNameProvider;
    }

    public async Task<PagedResultDto<GoodsReceiptListDto>> GetListAsync(GetGoodsReceiptListInput input)
    {
        var query = await _goodsReceiptRepository.GetQueryableAsync();

        if (input.SupplierId.HasValue)
            query = query.Where(x => x.SupplierId == input.SupplierId.Value);

        if (input.ProductId.HasValue)
        {
            var itemQuery = await _itemRepository.GetQueryableAsync();
            var receiptIds = itemQuery
                .Where(i => i.ProductId == input.ProductId.Value)
                .Select(i => i.GoodsReceiptId)
                .Distinct();
            query = query.Where(x => receiptIds.Contains(x.Id));
        }

        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var search = input.Search.Trim().ToLower();
            query = query.Where(x =>
                x.Code.ToLower().Contains(search) ||
                x.SupplierName.ToLower().Contains(search));
        }

        if (input.WarehouseStatus.HasValue)
            query = query.Where(x => x.WarehouseStatus == input.WarehouseStatus.Value);

        if (input.OrderStatus.HasValue)
            query = query.Where(x => x.OrderStatus == input.OrderStatus.Value);

        if (input.PaymentStatus.HasValue)
            query = query.Where(x => x.PaymentStatus == input.PaymentStatus.Value);

        if (input.FromDate.HasValue)
            query = query.Where(x => x.DocumentDate >= input.FromDate.Value);

        if (input.ToDate.HasValue)
            query = query.Where(x => x.DocumentDate <= input.ToDate.Value);

        var totalCount = query.Count();

        IQueryable<GoodsReceipt> sortedQuery;
        if (!string.IsNullOrWhiteSpace(input.Sorting))
        {
            var sorting = input.Sorting.Trim().ToLower();
            sortedQuery = sorting switch
            {
                "code asc" => query.OrderBy(x => x.Code),
                "code desc" => query.OrderByDescending(x => x.Code),
                "suppliername asc" => query.OrderBy(x => x.SupplierName),
                "suppliername desc" => query.OrderByDescending(x => x.SupplierName),
                "documentdate asc" => query.OrderBy(x => x.DocumentDate),
                "documentdate desc" => query.OrderByDescending(x => x.DocumentDate),
                "totalamountaftervat asc" => query.OrderBy(x => x.TotalAmountAfterVat),
                "totalamountaftervat desc" => query.OrderByDescending(x => x.TotalAmountAfterVat),
                "creationtime asc" => query.OrderBy(x => x.CreationTime),
                "creationtime desc" => query.OrderByDescending(x => x.CreationTime),
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

        var dtos = items.Select(x => ObjectMapper.Map<GoodsReceipt, GoodsReceiptListDto>(x)).ToList();

        await _userNameProvider.EnrichWithUserNamesAsync(
            dtos,
            dto => items.First(x => x.Id == dto.Id).CreatorId,
            (dto, name) => dto.CreatorName = name);

        return new PagedResultDto<GoodsReceiptListDto>(totalCount, dtos);
    }

    public async Task<GoodsReceiptDto> GetAsync(Guid id)
    {
        var receipt = await _goodsReceiptRepository.GetAsync(id);
        return await MapToDetailDtoAsync(receipt);
    }

    [Authorize(LabSpecimenManagementPermissions.GoodsReceipts.Create)]
    public async Task<GoodsReceiptDto> CreateAsync(CreateUpdateGoodsReceiptDto input)
    {
        var supplier = await _supplierRepository.GetAsync(input.SupplierId);
        var code = await GenerateCodeAsync();

        var receipt = new GoodsReceipt(
            GuidGenerator.Create(),
            code,
            supplier.Id,
            supplier.Name)
        {
            SupplierAddress = supplier.Address,
            SupplierPhone = supplier.Phone,
            PurchaserName = input.PurchaserName,
            DeliveryNoteNumber = input.DeliveryNoteNumber,
            PreparedByName = CurrentUser.Name ?? CurrentUser.UserName,
            DocumentDate = input.DocumentDate,
            DocumentExpiryDate = input.DocumentExpiryDate,
            SupplierRepresentativePhone = input.SupplierRepresentativePhone,
            VatPercent = input.VatPercent,
            Notes = input.Notes,
        };

        await _goodsReceiptRepository.InsertAsync(receipt, autoSave: true);

        await SaveItemsAsync(receipt.Id, input.Items, input.VatPercent);

        var savedItems = (await _itemRepository.GetQueryableAsync())
            .Where(x => x.GoodsReceiptId == receipt.Id).ToList();
        receipt.Items = savedItems;
        receipt.RecalculateTotals();
        await _goodsReceiptRepository.UpdateAsync(receipt, autoSave: true);

        return await GetAsync(receipt.Id);
    }

    [Authorize(LabSpecimenManagementPermissions.GoodsReceipts.Edit)]
    public async Task<GoodsReceiptDto> UpdateAsync(Guid id, CreateUpdateGoodsReceiptDto input)
    {
        var receipt = await _goodsReceiptRepository.GetAsync(id);

        if (receipt.WarehouseStatus == GoodsReceiptWarehouseStatus.Imported)
            throw new UserFriendlyException("Không thể sửa phiếu đã nhập kho.");

        var supplier = await _supplierRepository.GetAsync(input.SupplierId);

        receipt.SupplierId = supplier.Id;
        receipt.SupplierName = supplier.Name;
        receipt.SupplierAddress = supplier.Address;
        receipt.SupplierPhone = supplier.Phone;
        receipt.PurchaserName = input.PurchaserName;
        receipt.DeliveryNoteNumber = input.DeliveryNoteNumber;
        receipt.DocumentDate = input.DocumentDate;
        receipt.DocumentExpiryDate = input.DocumentExpiryDate;
        receipt.SupplierRepresentativePhone = input.SupplierRepresentativePhone;
        receipt.VatPercent = input.VatPercent;
        receipt.Notes = input.Notes;

        // Replace items
        var existingItems = (await _itemRepository.GetQueryableAsync())
            .Where(x => x.GoodsReceiptId == id).ToList();

        var inputIds = input.Items
            .Where(x => x.Id.HasValue)
            .Select(x => x.Id!.Value)
            .ToHashSet();

        foreach (var existing in existingItems)
        {
            if (!inputIds.Contains(existing.Id))
                await _itemRepository.DeleteAsync(existing, autoSave: true);
        }

        await SaveItemsAsync(id, input.Items, input.VatPercent, existingItems);

        var savedItems = (await _itemRepository.GetQueryableAsync())
            .Where(x => x.GoodsReceiptId == id).ToList();
        receipt.Items = savedItems;
        receipt.RecalculateTotals();
        receipt.UpdatePaymentStatus();
        await _goodsReceiptRepository.UpdateAsync(receipt, autoSave: true);

        return await GetAsync(id);
    }

    [Authorize(LabSpecimenManagementPermissions.GoodsReceipts.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var receipt = await _goodsReceiptRepository.GetAsync(id);

        if (receipt.WarehouseStatus == GoodsReceiptWarehouseStatus.Imported)
            throw new UserFriendlyException("Không thể xóa phiếu đã nhập kho.");

        var items = (await _itemRepository.GetQueryableAsync())
            .Where(x => x.GoodsReceiptId == id).ToList();
        foreach (var item in items)
            await _itemRepository.DeleteAsync(item, autoSave: true);

        var payments = (await _paymentRepository.GetQueryableAsync())
            .Where(x => x.GoodsReceiptId == id).ToList();
        foreach (var payment in payments)
            await _paymentRepository.DeleteAsync(payment, autoSave: true);

        await _goodsReceiptRepository.DeleteAsync(id);
    }

    [Authorize(LabSpecimenManagementPermissions.GoodsReceipts.Delete)]
    public async Task<int> BatchDeleteAsync(List<Guid> ids)
    {
        if (ids == null || !ids.Any()) return 0;

        var count = 0;
        foreach (var id in ids)
        {
            try
            {
                await DeleteAsync(id);
                count++;
            }
            catch (UserFriendlyException)
            {
                // Skip receipts that can't be deleted (already imported)
            }
        }
        return count;
    }

    [Authorize(LabSpecimenManagementPermissions.GoodsReceipts.Approve)]
    public async Task<GoodsReceiptDto> ApproveAsync(Guid id)
    {
        var receipt = await _goodsReceiptRepository.GetAsync(id);

        if (receipt.OrderStatus == GoodsReceiptOrderStatus.Approved)
            throw new UserFriendlyException("Phiếu đã được duyệt.");

        receipt.OrderStatus = GoodsReceiptOrderStatus.Approved;
        await _goodsReceiptRepository.UpdateAsync(receipt, autoSave: true);

        return await GetAsync(id);
    }

    [Authorize(LabSpecimenManagementPermissions.GoodsReceipts.ImportToWarehouse)]
    public async Task<GoodsReceiptDto> ImportToWarehouseAsync(Guid id)
    {
        var receipt = await _goodsReceiptRepository.GetAsync(id);

        if (receipt.OrderStatus != GoodsReceiptOrderStatus.Approved)
            throw new UserFriendlyException("Chỉ có thể nhập kho phiếu đã được duyệt.");

        if (receipt.WarehouseStatus == GoodsReceiptWarehouseStatus.Imported)
            throw new UserFriendlyException("Phiếu đã được nhập kho.");

        var items = (await _itemRepository.GetQueryableAsync())
            .Where(x => x.GoodsReceiptId == id).ToList();

        var conversionsQuery = await _conversionRepository.GetQueryableAsync();

        foreach (var item in items)
        {
            if (item.IsEquipment)
            {
                var equipCode = await GenerateEquipmentCodeAsync();
                var equipment = new Equipment(GuidGenerator.Create(), equipCode, item.ProductName, item.SerialNumber ?? "")
                {
                    GoodsReceiptId = id,
                    GoodsReceiptCode = receipt.Code,
                    SupplierId = receipt.SupplierId,
                    SupplierName = receipt.SupplierName,
                    Model = item.Model,
                    Manufacturer = item.Manufacturer,
                    UnitId = item.UnitId,
                    UnitName = item.UnitName,
                    Price = item.UnitPrice,
                    LotNumber = item.LotNumber,
                    LotExpiryDate = item.LotExpiryDate,
                };
                await _equipmentRepository.InsertAsync(equipment, autoSave: true);
                continue;
            }

            var product = await _productRepository.GetAsync(item.ProductId!.Value);
            decimal baseQty;

            if (item.UnitId == product.BaseUnitId)
            {
                baseQty = item.Quantity;
            }
            else
            {
                var conversion = conversionsQuery.FirstOrDefault(c =>
                    c.ProductId == item.ProductId && c.ConvertedUnitId == item.UnitId);

                if (conversion == null)
                    throw new UserFriendlyException($"Không tìm thấy quy đổi đơn vị cho \"{item.ProductName}\". Vui lòng kiểm tra lại.");

                if (conversion.ConvertedQuantity == 0)
                    throw new UserFriendlyException($"Quy đổi đơn vị cho \"{item.ProductName}\" có số lượng quy đổi = 0. Vui lòng kiểm tra lại.");

                decimal ratio = conversion.Quantity / conversion.ConvertedQuantity;
                baseQty = item.Quantity * ratio;
            }

            product.AvailableQuantity += baseQty;
            await _productRepository.UpdateAsync(product, autoSave: true);
        }

        // Update supplier financials
        var supplier = await _supplierRepository.GetAsync(receipt.SupplierId);
        supplier.TotalPurchase += receipt.TotalAmountAfterVat;
        supplier.TotalPurchaseAfterReturn += receipt.TotalAmountAfterVat;
        supplier.OutstandingDebt += receipt.TotalAmountAfterVat - receipt.TotalPaid;
        await _supplierRepository.UpdateAsync(supplier, autoSave: true);

        receipt.WarehouseStatus = GoodsReceiptWarehouseStatus.Imported;
        await _goodsReceiptRepository.UpdateAsync(receipt, autoSave: true);

        return await GetAsync(id);
    }

    [Authorize(LabSpecimenManagementPermissions.GoodsReceipts.AddPayment)]
    public async Task<GoodsReceiptDto> AddPaymentAsync(Guid id, CreateGoodsReceiptPaymentDto input)
    {
        var receipt = await _goodsReceiptRepository.GetAsync(id);
        var remaining = receipt.TotalAmountAfterVat - receipt.TotalPaid;

        if (input.Amount <= 0)
            throw new UserFriendlyException("Số tiền thanh toán phải lớn hơn 0.");

        if (input.Amount > remaining)
            throw new UserFriendlyException("Số tiền thanh toán không được lớn hơn số tiền còn lại.");

        var paymentsQuery = await _paymentRepository.GetQueryableAsync();
        var paymentCount = paymentsQuery.Count(x => x.GoodsReceiptId == id);

        var payment = new GoodsReceiptPayment(
            GuidGenerator.Create(),
            id,
            paymentCount + 1,
            input.Amount,
            remaining,
            input.PaymentMethod,
            input.PaymentDate,
            input.Phone,
            input.RepresentativeName,
            input.Notes);

        await _paymentRepository.InsertAsync(payment, autoSave: true);

        receipt.TotalPaid += input.Amount;
        receipt.UpdatePaymentStatus();
        await _goodsReceiptRepository.UpdateAsync(receipt, autoSave: true);

        // Update supplier debt if already imported
        if (receipt.WarehouseStatus == GoodsReceiptWarehouseStatus.Imported)
        {
            var supplier = await _supplierRepository.GetAsync(receipt.SupplierId);
            supplier.OutstandingDebt -= input.Amount;
            await _supplierRepository.UpdateAsync(supplier, autoSave: true);
        }

        return await GetAsync(id);
    }

    public async Task<List<RecentLotDto>> GetRecentLotNumbersAsync(Guid productId)
    {
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
        var itemQuery = await _itemRepository.GetQueryableAsync();
        var receiptQuery = await _goodsReceiptRepository.GetQueryableAsync();

        var lots = (from item in itemQuery
                    join receipt in receiptQuery on item.GoodsReceiptId equals receipt.Id
                    where item.ProductId == productId
                        && item.LotNumber != null
                        && item.LotNumber != ""
                        && receipt.CreationTime >= sevenDaysAgo
                        && (item.LotExpiryDate == null || item.LotExpiryDate > DateTime.UtcNow)
                    orderby receipt.CreationTime descending
                    select new RecentLotDto
                    {
                        LotNumber = item.LotNumber!,
                        LotExpiryDate = item.LotExpiryDate
                    })
                    .ToList()
                    .GroupBy(x => x.LotNumber)
                    .Select(g => g.First())
                    .Take(20)
                    .ToList();

        return lots;
    }

    #region Private Methods

    private async Task<string> GenerateCodeAsync()
    {
        using (_dataFilter.Disable<Volo.Abp.ISoftDelete>())
        {
            var todayPrefix = _codeGenerator.GetTodayPrefix("PNH");
            var query = await _goodsReceiptRepository.GetQueryableAsync();
            var count = query.Count(x => x.Code.StartsWith(todayPrefix));
            return _codeGenerator.GenerateCodeWithDate("PNH", count);
        }
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

    private async Task SaveItemsAsync(
        Guid receiptId,
        List<CreateUpdateGoodsReceiptItemDto> items,
        decimal defaultVatPercent,
        List<GoodsReceiptItem>? existingItems = null)
    {
        var unitQuery = await _unitRepository.GetQueryableAsync();
        var productQuery = await _productRepository.GetQueryableAsync();

        foreach (var itemDto in items)
        {
            if (itemDto.LotExpiryDate.HasValue && itemDto.LotExpiryDate.Value.Date < DateTime.UtcNow.Date)
                throw new UserFriendlyException($"Lô hàng đã hết hạn (HSD: {itemDto.LotExpiryDate.Value:dd/MM/yyyy}). Không thể sử dụng lô đã hết hạn.");

            if (itemDto.IsEquipment)
            {
                if (itemDto.Quantity != 1)
                    throw new UserFriendlyException("Số lượng thiết bị trong mỗi dòng phải là 1. Vui lòng thêm dòng riêng cho mỗi thiết bị.");
                if (string.IsNullOrWhiteSpace(itemDto.SerialNumber))
                    throw new UserFriendlyException("Thiết bị phải có serial number.");
            }

            string resolvedName;
            Guid? resolvedProductId;
            Guid? resolvedUnitId;
            string? resolvedUnitName;

            if (itemDto.IsEquipment)
            {
                if (string.IsNullOrWhiteSpace(itemDto.ProductName))
                    throw new UserFriendlyException("Thiết bị phải có tên.");
                resolvedName = itemDto.ProductName!;
                resolvedProductId = null;
                resolvedUnitId = itemDto.UnitId;
                resolvedUnitName = itemDto.UnitId.HasValue
                    ? unitQuery.FirstOrDefault(u => u.Id == itemDto.UnitId)?.Name
                    : null;
            }
            else
            {
                if (!itemDto.ProductId.HasValue || itemDto.ProductId == Guid.Empty)
                    throw new UserFriendlyException("Hàng hóa phải có vật phẩm.");
                var product = productQuery.FirstOrDefault(p => p.Id == itemDto.ProductId.Value)
                    ?? throw new UserFriendlyException($"Không tìm thấy vật phẩm với ID {itemDto.ProductId}.");
                if (!itemDto.UnitId.HasValue || itemDto.UnitId == Guid.Empty)
                    throw new UserFriendlyException("Hàng hóa phải có đơn vị.");
                var unit = unitQuery.FirstOrDefault(u => u.Id == itemDto.UnitId.Value)
                    ?? throw new UserFriendlyException($"Không tìm thấy đơn vị với ID {itemDto.UnitId}.");
                resolvedName = product.Name;
                resolvedProductId = product.Id;
                resolvedUnitId = unit.Id;
                resolvedUnitName = unit.Name;
            }

            if (itemDto.Id.HasValue && existingItems != null)
            {
                var existing = existingItems.FirstOrDefault(x => x.Id == itemDto.Id.Value);
                if (existing != null)
                {
                    existing.ProductId = resolvedProductId;
                    existing.ProductName = resolvedName;
                    existing.UnitId = resolvedUnitId;
                    existing.UnitName = resolvedUnitName;
                    existing.Quantity = itemDto.Quantity;
                    existing.UnitPrice = itemDto.UnitPrice;
                    existing.VatPercent = itemDto.VatPercent;
                    existing.LotNumber = itemDto.LotNumber;
                    existing.LotExpiryDate = itemDto.LotExpiryDate;
                    existing.IsEquipment = itemDto.IsEquipment;
                    existing.SerialNumber = itemDto.SerialNumber;
                    existing.Model = itemDto.Model;
                    existing.Manufacturer = itemDto.Manufacturer;
                    existing.Calculate();
                    await _itemRepository.UpdateAsync(existing, autoSave: true);
                    continue;
                }
            }

            var item = new GoodsReceiptItem(
                GuidGenerator.Create(),
                receiptId,
                resolvedProductId,
                resolvedName,
                resolvedUnitId,
                resolvedUnitName)
            {
                Quantity = itemDto.Quantity,
                UnitPrice = itemDto.UnitPrice,
                VatPercent = itemDto.VatPercent,
                LotNumber = itemDto.LotNumber,
                LotExpiryDate = itemDto.LotExpiryDate,
                IsEquipment = itemDto.IsEquipment,
                SerialNumber = itemDto.SerialNumber,
                Model = itemDto.Model,
                Manufacturer = itemDto.Manufacturer,
            };
            item.Calculate();
            await _itemRepository.InsertAsync(item, autoSave: true);
        }
    }

    private async Task<GoodsReceiptDto> MapToDetailDtoAsync(GoodsReceipt receipt)
    {
        var dto = ObjectMapper.Map<GoodsReceipt, GoodsReceiptDto>(receipt);
        dto.CreatorName = await _userNameProvider.GetUserNameAsync(receipt.CreatorId) ?? "Admin";

        var items = (await _itemRepository.GetQueryableAsync())
            .Where(x => x.GoodsReceiptId == receipt.Id)
            .OrderBy(x => x.Id)
            .ToList();
        dto.Items = items.Select(x => ObjectMapper.Map<GoodsReceiptItem, GoodsReceiptItemDto>(x)).ToList();

        var payments = (await _paymentRepository.GetQueryableAsync())
            .Where(x => x.GoodsReceiptId == receipt.Id)
            .OrderBy(x => x.PaymentNumber)
            .ToList();
        dto.Payments = payments.Select(x => ObjectMapper.Map<GoodsReceiptPayment, GoodsReceiptPaymentDto>(x)).ToList();

        return dto;
    }

    #endregion
}
