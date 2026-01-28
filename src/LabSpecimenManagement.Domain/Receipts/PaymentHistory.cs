using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.Receipts;

/// <summary>
/// Lịch sử thanh toán - PaymentHistory
/// Lưu thông tin từng lần thanh toán cho phiếu thu
/// </summary>
public class PaymentHistory : FullAuditedEntity<Guid>
{
    /// <summary>
    /// ID của phiếu thu liên quan
    /// </summary>
    public Guid ReceiptId { get; set; }

    /// <summary>
    /// Số thứ tự lần thanh toán (1, 2, 3...)
    /// </summary>
    public int PaymentNumber { get; set; }

    /// <summary>
    /// Số tiền thanh toán
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Số tiền còn lại cần thanh toán tại thời điểm thanh toán
    /// </summary>
    public decimal RemainingAmountBeforePayment { get; set; }

    /// <summary>
    /// Phương thức thanh toán (Tiền mặt / Chuyển khoản)
    /// </summary>
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>
    /// Ngày thanh toán
    /// </summary>
    public DateTime PaymentDate { get; set; }

    /// <summary>
    /// Ghi chú
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Navigation property - Phiếu thu
    /// </summary>
    public virtual Receipt Receipt { get; set; } = null!;

    protected PaymentHistory()
    {
    }

    public PaymentHistory(
        Guid id,
        Guid receiptId,
        int paymentNumber,
        decimal amount,
        decimal remainingAmountBeforePayment,
        PaymentMethod paymentMethod,
        DateTime paymentDate,
        string? notes = null) : base(id)
    {
        ReceiptId = receiptId;
        PaymentNumber = paymentNumber;
        Amount = amount;
        RemainingAmountBeforePayment = remainingAmountBeforePayment;
        PaymentMethod = paymentMethod;
        PaymentDate = paymentDate;
        Notes = notes;
    }
}