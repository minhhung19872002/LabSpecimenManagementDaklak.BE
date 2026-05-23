using System;

namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// DTO for Lab Technical Manager pending tasks (popup reminder)
/// </summary>
public class LabTechManagerPendingTaskDto
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
    /// - "Cần duyệt kết quả"
    /// - "Cần phân công công việc"
    /// - "Sắp tới ngày trả kết quả thử nghiệm"
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
    /// Trạng thái của LaboratoryTask
    /// </summary>
    public LaboratoryTaskStatus Status { get; set; }

    /// <summary>
    /// Số ngày còn lại đến deadline (null nếu không có deadline)
    /// </summary>
    public int? DaysUntilDeadline { get; set; }

    /// <summary>
    /// Loại task: "LaboratoryTask" (default) hoặc "IndicatorMethodSetup"
    /// </summary>
    public string TaskType { get; set; } = "LaboratoryTask";

    /// <summary>
    /// Thời gian tạo (để sort mới nhất lên đầu)
    /// </summary>
    public DateTime CreationTime { get; set; }
}
