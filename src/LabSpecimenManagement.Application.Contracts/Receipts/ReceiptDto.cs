using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Receipts;

/// <summary>
/// DTO cho phiếu thu
/// </summary>
public class ReceiptDto : FullAuditedEntityDto<Guid>
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

    #region From TestRequest

    /// <summary>
    /// Số hợp đồng (từ TestRequest)
    /// </summary>
    public string? ContractNumber { get; set; }

    /// <summary>
    /// Ngày yêu cầu (từ TestRequest)
    /// </summary>
    public DateTime? RequestDate { get; set; }

    /// <summary>
    /// Ngày hoàn thành dự kiến (từ TestRequest)
    /// </summary>
    public DateTime? ExpectedCompletionDate { get; set; }

    /// <summary>
    /// Tổng tiền (từ TestRequest)
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Tên khách hàng (từ TestRequest)
    /// </summary>
    public string? CustomerName { get; set; }

    #endregion
}
