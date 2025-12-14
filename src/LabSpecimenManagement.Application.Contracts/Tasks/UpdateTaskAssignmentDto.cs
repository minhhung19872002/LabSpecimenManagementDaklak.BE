using System;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.Tasks;

public class UpdateTaskAssignmentDto
{
    [Required]
    [StringLength(256)]
    public string Title { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [Required]
    public TaskType Type { get; set; }

    [Required]
    public TaskPriority Priority { get; set; }

    [Required]
    public TaskAssignmentStatus Status { get; set; }

    [Required]
    public Guid AssignedToId { get; set; }

    [Required]
    [StringLength(100)]
    public string Department { get; set; }

    public string? RelatedTestRequestIds { get; set; }

    public string? RelatedSpecimenIds { get; set; }

    public string? RelatedTestIds { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    [Range(0, 100)]
    public int Progress { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}
