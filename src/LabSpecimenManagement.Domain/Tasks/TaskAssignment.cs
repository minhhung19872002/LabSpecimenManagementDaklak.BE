using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.Tasks;

/// <summary>
/// Phân công công việc - Task Assignment
/// </summary>
public class TaskAssignment : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// Mã công việc (auto-generated: TASK-YYYYMMDD-XXXX)
    /// </summary>
    public string TaskNumber { get; set; }

    /// <summary>
    /// Tiêu đề công việc
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Mô tả công việc
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Loại công việc
    /// </summary>
    public TaskType Type { get; set; }

    /// <summary>
    /// Mức ưu tiên
    /// </summary>
    public TaskPriority Priority { get; set; }

    /// <summary>
    /// Trạng thái
    /// </summary>
    public TaskAssignmentStatus Status { get; set; }

    /// <summary>
    /// Người được giao
    /// </summary>
    public Guid AssignedToId { get; set; }

    /// <summary>
    /// Người giao việc
    /// </summary>
    public Guid AssignedById { get; set; }

    /// <summary>
    /// Phòng ban
    /// </summary>
    public string Department { get; set; }

    /// <summary>
    /// Danh sách ID yêu cầu thử nghiệm liên quan (JSON array)
    /// </summary>
    public string? RelatedTestRequestIds { get; set; }

    /// <summary>
    /// Danh sách ID mẫu liên quan (JSON array)
    /// </summary>
    public string? RelatedSpecimenIds { get; set; }

    /// <summary>
    /// Danh sách ID phép thử liên quan (JSON array)
    /// </summary>
    public string? RelatedTestIds { get; set; }

    /// <summary>
    /// Hạn hoàn thành
    /// </summary>
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Ngày bắt đầu thực tế
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// Ngày hoàn thành thực tế
    /// </summary>
    public DateTime? CompletedDate { get; set; }

    /// <summary>
    /// Tiến độ (0-100%)
    /// </summary>
    public int Progress { get; set; }

    /// <summary>
    /// Ghi chú
    /// </summary>
    public string? Notes { get; set; }

    protected TaskAssignment()
    {
    }

    public TaskAssignment(
        Guid id,
        string taskNumber,
        string title,
        TaskType type,
        TaskPriority priority,
        Guid assignedToId,
        Guid assignedById,
        string department,
        DateTime dueDate
    ) : base(id)
    {
        TaskNumber = taskNumber;
        Title = title;
        Type = type;
        Priority = priority;
        AssignedToId = assignedToId;
        AssignedById = assignedById;
        Department = department;
        DueDate = dueDate;
        Status = TaskAssignmentStatus.Pending;
        Progress = 0;
    }
}
