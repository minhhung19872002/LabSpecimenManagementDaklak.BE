using System;

namespace LabSpecimenManagement.Receipts;

/// <summary>
/// DTO cho lịch sử thanh toán
/// </summary>
public class PaymentHistoryDto
{
    /// <summary>
    /// ID của lịch sử thanh toán
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// ID của phiếu thu
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
    /// Phương thức thanh toán
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
    /// Thời gian tạo
    /// </summary>
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// Tên người tạo
    /// </summary>
    public string? CreatorName { get; set; }
}
