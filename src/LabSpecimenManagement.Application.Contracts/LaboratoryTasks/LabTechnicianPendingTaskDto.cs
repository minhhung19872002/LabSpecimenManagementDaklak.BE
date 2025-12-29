using System;

namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// DTO for Lab Technician (KTV) pending tasks (popup reminder)
/// </summary>
public class LabTechnicianPendingTaskDto
{
    /// <summary>
    /// LaboratoryTask ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Số hợp đồng
    /// </summary>
    public string ContractNumber { get; set; } = string.Empty;

    /// <summary>
    /// Tên công việc:
    /// - "Có kết quả cần thực hiện lại" (Rejected)
    /// - "Có kết quả chỉ tiêu cần gửi" (Entered)
    /// - "Có chỉ tiêu cần thực hiện" (Pending)
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
    /// Số indicator đang Pending (chưa bắt đầu)
    /// </summary>
    public int PendingCount { get; set; }

    /// <summary>
    /// Số indicator đang Entered (đã nhập KQ, chưa gửi)
    /// </summary>
    public int EnteredCount { get; set; }

    /// <summary>
    /// Số indicator đang Rejected (bị từ chối)
    /// </summary>
    public int RejectedCount { get; set; }
}
