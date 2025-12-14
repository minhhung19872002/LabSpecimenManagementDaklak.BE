using System;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.Tasks;

public class CreateTaskAssignmentDto
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
    public Guid AssignedToId { get; set; }

    [Required]
    [StringLength(100)]
    public string Department { get; set; }

    /// <summary>
    /// JSON array of test request IDs
    /// </summary>
    public string? RelatedTestRequestIds { get; set; }

    /// <summary>
    /// JSON array of specimen IDs
    /// </summary>
    public string? RelatedSpecimenIds { get; set; }

    /// <summary>
    /// JSON array of test IDs
    /// </summary>
    public string? RelatedTestIds { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}
