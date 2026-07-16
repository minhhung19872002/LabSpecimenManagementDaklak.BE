using System;
using Volo.Abp.Domain.Entities.Auditing;
using LabSpecimenManagement.Receipts;

namespace LabSpecimenManagement.GoodsReceipts;

public class GoodsReceiptPayment : FullAuditedEntity<Guid>
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
    // JSON array of InvoiceFileInfoDto from /api/v1/files/upload
    public string? InvoiceFileIds { get; set; }

    protected GoodsReceiptPayment()
    {
    }

    public GoodsReceiptPayment(
        Guid id,
        Guid goodsReceiptId,
        int paymentNumber,
        decimal amount,
        decimal remainingBeforePayment,
        PaymentMethod paymentMethod,
        DateTime paymentDate,
        string? phone = null,
        string? representativeName = null,
        string? notes = null,
        string? invoiceFileIds = null) : base(id)
    {
        GoodsReceiptId = goodsReceiptId;
        PaymentNumber = paymentNumber;
        Amount = amount;
        RemainingBeforePayment = remainingBeforePayment;
        PaymentMethod = paymentMethod;
        PaymentDate = paymentDate;
        Phone = phone;
        RepresentativeName = representativeName;
        Notes = notes;
        InvoiceFileIds = invoiceFileIds;
    }
}
