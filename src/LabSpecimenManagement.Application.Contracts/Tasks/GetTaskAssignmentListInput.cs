using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Tasks;

public class GetTaskAssignmentListInput : PagedAndSortedResultRequestDto
{
    /// <summary>
    /// Filter by task number, title, or assignee name
    /// </summary>
    public string? Filter { get; set; }

    /// <summary>
    /// Filter by status (null = all)
    /// </summary>
    public TaskAssignmentStatus? Status { get; set; }

    /// <summary>
    /// Filter by department name
    /// </summary>
    public string? Department { get; set; }

    /// <summary>
    /// Filter by priority
    /// </summary>
    public TaskPriority? Priority { get; set; }

    /// <summary>
    /// Filter by task type
    /// </summary>
    public TaskType? Type { get; set; }

    /// <summary>
    /// Filter by assigned user ID
    /// </summary>
    public Guid? AssignedToId { get; set; }

    /// <summary>
    /// Filter by due date from
    /// </summary>
    public DateTime? DueDateFrom { get; set; }

    /// <summary>
    /// Filter by due date to
    /// </summary>
    public DateTime? DueDateTo { get; set; }
}
