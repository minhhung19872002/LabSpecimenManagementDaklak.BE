using System;

namespace LabSpecimenManagement.QualityManagement;

/// <summary>
/// DTO for Quality Manager (QLCL) pending tasks (popup reminder)
/// </summary>
public class QualityManagerPendingTaskDto
{
    /// <summary>
    /// QualityManagementTask ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// TestRequest ID
    /// </summary>
    public Guid TestRequestId { get; set; }

    /// <summary>
    /// Số hợp đồng
    /// </summary>
    public string ContractNumber { get; set; } = string.Empty;

    /// <summary>
    /// Tên công việc:
    /// - "Có kết quả cần phê duyệt" (PendingApproval)
    /// - "Kết quả cần bàn giao cho bộ phận nhận/ trả mẫu" (Approved)
    /// - "Sắp tới ngày trả kết quả thử nghiệm" (deadline <= 5 ngày)
    /// </summary>
    public string TaskName { get; set; } = string.Empty;

    /// <summary>
    /// Ngày chuyển giao
    /// </summary>
    public DateTime? TransferDate { get; set; }

    /// <summary>
    /// Ngày yêu cầu hoàn thành (Ngày YC xong)
    /// </summary>
    public DateTime? RequestCompletionDate { get; set; }

    /// <summary>
    /// Trạng thái của QualityManagementTask
    /// </summary>
    public QualityManagementStatus Status { get; set; }

    /// <summary>
    /// Số ngày còn lại đến deadline (null nếu không có deadline)
    /// </summary>
    public int? DaysUntilDeadline { get; set; }
}
