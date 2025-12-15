using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Tasks;

public class TaskAssignmentDto : FullAuditedEntityDto<Guid>
{
    public string TaskNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskType Type { get; set; }
    public TaskPriority Priority { get; set; }
    public TaskAssignmentStatus Status { get; set; }
    public Guid AssignedToId { get; set; }
    public string? AssignedToName { get; set; }
    public Guid AssignedById { get; set; }
    public string? AssignedByName { get; set; }
    public string Department { get; set; } = string.Empty;
    public string? RelatedTestRequestIds { get; set; }
    public string? RelatedSpecimenIds { get; set; }
    public string? RelatedTestIds { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public int Progress { get; set; }
    public string? Notes { get; set; }
}
