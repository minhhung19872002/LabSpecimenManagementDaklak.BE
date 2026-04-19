using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.GoodsReceipts;

public class GoodsReceipt : FullAuditedAggregateRoot<Guid>
{
    public string Code { get; set; }
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; }
    public string? SupplierAddress { get; set; }
    public string? SupplierPhone { get; set; }
    public string? PurchaserName { get; set; }
    public string? DeliveryNoteNumber { get; set; }
    public string? PreparedByName { get; set; }
    public DateTime? DocumentDate { get; set; }
    public DateTime? DocumentExpiryDate { get; set; }
    public string? SupplierRepresentativePhone { get; set; }
    public decimal VatPercent { get; set; }
    public decimal TotalAmountBeforeVat { get; set; }
    public decimal TotalVatAmount { get; set; }
    public decimal TotalAmountAfterVat { get; set; }
    public decimal TotalPaid { get; set; }
    public GoodsReceiptWarehouseStatus WarehouseStatus { get; set; }
    public GoodsReceiptOrderStatus OrderStatus { get; set; }
    public GoodsReceiptPaymentStatus PaymentStatus { get; set; }
    public string? Notes { get; set; }

    public ICollection<GoodsReceiptItem> Items { get; set; }
    public ICollection<GoodsReceiptPayment> Payments { get; set; }

    protected GoodsReceipt()
    {
        Items = new List<GoodsReceiptItem>();
        Payments = new List<GoodsReceiptPayment>();
    }

    public GoodsReceipt(Guid id, string code, Guid supplierId, string supplierName) : base(id)
    {
        Code = code;
        SupplierId = supplierId;
        SupplierName = supplierName;
        VatPercent = 8;
        WarehouseStatus = GoodsReceiptWarehouseStatus.NotImported;
        OrderStatus = GoodsReceiptOrderStatus.Draft;
        PaymentStatus = GoodsReceiptPaymentStatus.Unpaid;
        Items = new List<GoodsReceiptItem>();
        Payments = new List<GoodsReceiptPayment>();
    }

    public void RecalculateTotals()
    {
        decimal totalBeforeVat = 0;
        decimal totalVat = 0;
        foreach (var item in Items)
        {
            totalBeforeVat += item.AmountBeforeVat;
            totalVat += item.VatAmount;
        }
        TotalAmountBeforeVat = totalBeforeVat;
        TotalVatAmount = totalVat;
        TotalAmountAfterVat = totalBeforeVat + totalVat;
    }

    public void UpdatePaymentStatus()
    {
        if (TotalAmountAfterVat <= 0)
        {
            PaymentStatus = GoodsReceiptPaymentStatus.Paid;
        }
        else if (TotalPaid <= 0)
        {
            PaymentStatus = GoodsReceiptPaymentStatus.Unpaid;
        }
        else if (TotalPaid >= TotalAmountAfterVat)
        {
            PaymentStatus = GoodsReceiptPaymentStatus.Paid;
        }
        else
        {
            PaymentStatus = GoodsReceiptPaymentStatus.PartiallyPaid;
        }
    }
}
