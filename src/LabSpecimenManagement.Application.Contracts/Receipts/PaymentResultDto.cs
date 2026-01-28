using System;

namespace LabSpecimenManagement.Receipts;

/// <summary>
/// DTO kết quả sau khi thanh toán
/// </summary>
public class PaymentResultDto
{
    /// <summary>
    /// ID của lịch sử thanh toán vừa tạo
    /// </summary>
    public Guid PaymentHistoryId { get; set; }

    /// <summary>
    /// Số tiền đã thanh toán
    /// </summary>
    public decimal PaidAmount { get; set; }

    /// <summary>
    /// Số tiền còn lại cần thanh toán
    /// </summary>
    public decimal RemainingAmount { get; set; }

    /// <summary>
    /// Trạng thái phiếu thu sau khi thanh toán
    /// </summary>
    public ReceiptStatus ReceiptStatus { get; set; }

    /// <summary>
    /// Thông báo kết quả
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
