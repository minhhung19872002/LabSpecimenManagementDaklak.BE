using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.Receipts;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.GoodsReceipts;

public class GoodsReceiptListDto : EntityDto<Guid>
{
    public string Code { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? SupplierAddress { get; set; }
    public DateTime? DocumentDate { get; set; }
    public GoodsReceiptWarehouseStatus WarehouseStatus { get; set; }
    public GoodsReceiptOrderStatus OrderStatus { get; set; }
    public GoodsReceiptPaymentStatus PaymentStatus { get; set; }
    public decimal TotalAmountAfterVat { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public string? CreatorName { get; set; }
    public string? Notes { get; set; }
    public DateTime CreationTime { get; set; }
}

public class GoodsReceiptDto : EntityDto<Guid>
{
    public string Code { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? SupplierAddress { get; set; }
    public string? SupplierPhone { get; set; }
    public string? PurchaserName { get; set; }
    public string? DeliveryNoteNumber { get; set; }
    public string? PreparedByName { get; set; }
    public DateTime? DocumentDate { get; set; }
    public DateTime? DocumentExpiryDate { get; set; }
    public string? SupplierRepresentativePhone { get; set; }
    public string? SupplierRepresentativeName { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
    public Guid? PurchaserId { get; set; }
    public string? PurchaserFullName { get; set; }
    public decimal VatPercent { get; set; }
    public decimal TotalAmountBeforeVat { get; set; }
    public decimal TotalVatAmount { get; set; }
    public decimal TotalAmountAfterVat { get; set; }
    public decimal TotalPaid { get; set; }
    public GoodsReceiptWarehouseStatus WarehouseStatus { get; set; }
    public GoodsReceiptOrderStatus OrderStatus { get; set; }
    public GoodsReceiptPaymentStatus PaymentStatus { get; set; }
    public string? Notes { get; set; }
    public string? CreatorName { get; set; }
    public DateTime CreationTime { get; set; }

    public List<GoodsReceiptItemDto> Items { get; set; } = new();
    public List<GoodsReceiptPaymentDto> Payments { get; set; } = new();
}

public class RejectGoodsReceiptDto
{
    public string? Notes { get; set; }
}

public class GoodsReceiptItemDto : EntityDto<Guid>
{
    public Guid GoodsReceiptId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public string? IdentificationCode { get; set; }
    public Guid UnitId { get; set; }
    public string UnitName { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal AmountBeforeVat { get; set; }
    public decimal VatPercent { get; set; }
    public decimal VatAmount { get; set; }
    public decimal AmountAfterVat { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? LotExpiryDate { get; set; }
    public bool IsEquipment { get; set; }
    public string? SerialNumber { get; set; }
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }
    public Guid? ManufacturerId { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
}

public class GoodsReceiptPaymentDto : EntityDto<Guid>
{
    public Guid GoodsReceiptId { get; set; }
    public int PaymentNumber { get; set; }
    public decimal Amount { get; set; }
    public decimal RemainingBeforePayment { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? Phone { get; set; }
    public string? RepresentativeName { get; set; }
    public string? Notes { get; set; }
    public DateTime CreationTime { get; set; }
    public List<InvoiceFileInfoDto> InvoiceFiles { get; set; } = new();
}

public class InvoiceFileInfoDto
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long? FileSize { get; set; }
}

public class CreateUpdateGoodsReceiptDto
{
    public Guid? SupplierId { get; set; }

    [StringLength(500)]
    public string? SupplierAddress { get; set; }

    [StringLength(20)]
    public string? SupplierPhone { get; set; }

    [StringLength(200)]
    public string? PurchaserName { get; set; }

    [StringLength(100)]
    public string? DeliveryNoteNumber { get; set; }

    public DateTime? DocumentDate { get; set; }
    public DateTime? DocumentExpiryDate { get; set; }

    [StringLength(20)]
    public string? SupplierRepresentativePhone { get; set; }

    [StringLength(200)]
    public string? SupplierRepresentativeName { get; set; }

    public DateTime? ExpectedDeliveryDate { get; set; }
    public Guid? PurchaserId { get; set; }

    public decimal VatPercent { get; set; } = 8;

    [StringLength(2000)]
    public string? Notes { get; set; }

    public List<CreateUpdateGoodsReceiptItemDto> Items { get; set; } = new();
}

public class CreateUpdateGoodsReceiptItemDto
{
    public Guid? Id { get; set; }

    public Guid? ProductId { get; set; }

    [StringLength(500)]
    public string? ProductName { get; set; }

    public Guid? UnitId { get; set; }

    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal VatPercent { get; set; }

    [StringLength(100)]
    public string? LotNumber { get; set; }

    public DateTime? LotExpiryDate { get; set; }

    public bool IsEquipment { get; set; }

    [StringLength(200)]
    public string? SerialNumber { get; set; }

    [StringLength(256)]
    public string? Model { get; set; }

    [StringLength(500)]
    public string? Manufacturer { get; set; }

    public Guid? ManufacturerId { get; set; }

    [StringLength(500)]
    public string? ManufacturerName { get; set; }

    public Guid? SupplierId { get; set; }

    public DateTime? ReceivedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public Guid? WarehouseId { get; set; }
}

public class GetGoodsReceiptListInput : PagedRequestDto
{
    public string? Search { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? ProductId { get; set; }
    public GoodsReceiptWarehouseStatus? WarehouseStatus { get; set; }
    public GoodsReceiptOrderStatus? OrderStatus { get; set; }
    public GoodsReceiptPaymentStatus? PaymentStatus { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class RecentLotDto
{
    public string LotNumber { get; set; }
    public DateTime? LotExpiryDate { get; set; }
}

public class GoodsReceiptStatisticsDto
{
    public int PendingApproval { get; set; }
    public int Ordering { get; set; }
    public int WaitingReceipt { get; set; }
    public int Rejected { get; set; }
}

public class LatestSupplierInfoDto
{
    public string? SupplierRepresentativePhone { get; set; }
    public string? SupplierRepresentativeName { get; set; }
}

public class CreateGoodsReceiptPaymentDto
{
    [Required]
    public decimal Amount { get; set; }

    [Required]
    public PaymentMethod PaymentMethod { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(200)]
    public string? RepresentativeName { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    public List<InvoiceFileInfoDto> InvoiceFiles { get; set; } = new();
}

public class GeneratePurchaseRequestOutput
{
    public string FileName { get; set; } = string.Empty;
}
