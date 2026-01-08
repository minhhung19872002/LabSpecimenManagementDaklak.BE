using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.Receipts;

/// <summary>
/// DTO thông tin thanh toán cho modal
/// </summary>
public class PaymentInfoDto
{
    /// <summary>
    /// ID của phiếu thu
    /// </summary>
    public Guid ReceiptId { get; set; }

    /// <summary>
    /// ID của phiếu yêu cầu
    /// </summary>
    public Guid TestRequestId { get; set; }

    /// <summary>
    /// Số hợp đồng
    /// </summary>
    public string? ContractNumber { get; set; }

    /// <summary>
    /// Tổng chi phí
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Tổng số tiền đã thanh toán
    /// </summary>
    public decimal PaidAmount { get; set; }

    /// <summary>
    /// Số tiền còn lại cần thanh toán
    /// </summary>
    public decimal RemainingAmount { get; set; }

    /// <summary>
    /// Trạng thái phiếu thu
    /// </summary>
    public ReceiptStatus Status { get; set; }

    /// <summary>
    /// Danh sách lịch sử thanh toán
    /// </summary>
    public List<PaymentHistoryDto> PaymentHistories { get; set; } = new();
}
