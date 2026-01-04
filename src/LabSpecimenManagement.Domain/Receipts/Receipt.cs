using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.Receipts;

/// <summary>
/// Phiếu thu - Receipt
/// Quản lý thanh toán cho phiếu yêu cầu thử nghiệm
/// </summary>
public class Receipt : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// ID của phiếu yêu cầu thử nghiệm liên quan
    /// </summary>
    public Guid TestRequestId { get; set; }

    /// <summary>
    /// Số phiếu thu (kế toán nhập thủ công)
    /// </summary>
    public string? ReceiptNumber { get; set; }

    /// <summary>
    /// Mã số biên lai (kế toán nhập thủ công)
    /// </summary>
    public string? VoucherCode { get; set; }

    /// <summary>
    /// Trạng thái phiếu thu
    /// </summary>
    public ReceiptStatus Status { get; set; }

    /// <summary>
    /// Ghi chú
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Ngày thanh toán
    /// </summary>
    public DateTime? PaidDate { get; set; }

    /// <summary>
    /// Navigation property - Phiếu yêu cầu thử nghiệm
    /// </summary>
    public virtual TestRequests.TestRequest TestRequest { get; set; } = null!;

    protected Receipt()
    {
    }

    public Receipt(Guid id, Guid testRequestId) : base(id)
    {
        TestRequestId = testRequestId;
        Status = ReceiptStatus.PendingPayment;
    }

    /// <summary>
    /// Cập nhật số phiếu thu
    /// </summary>
    public void SetReceiptNumber(string? receiptNumber)
    {
        ReceiptNumber = receiptNumber;
    }

    /// <summary>
    /// Đánh dấu đã thanh toán
    /// </summary>
    public void MarkAsPaid(string? notes = null)
    {
        Status = ReceiptStatus.Paid;
        PaidDate = DateTime.Now;
        if (notes != null)
        {
            Notes = notes;
        }
    }

    /// <summary>
    /// Đánh dấu chưa thanh toán
    /// </summary>
    public void MarkAsUnpaid(string? notes = null)
    {
        Status = ReceiptStatus.Unpaid;
        if (notes != null)
        {
            Notes = notes;
        }
    }

    /// <summary>
    /// Hủy phiếu thu
    /// </summary>
    public void Cancel(string? notes = null)
    {
        Status = ReceiptStatus.Cancelled;
        if (notes != null)
        {
            Notes = notes;
        }
    }
}
