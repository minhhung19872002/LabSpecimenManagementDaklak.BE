using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.SupplementaryTasks.Dtos;

public class CreateSupplementaryTaskDto
{
    [Required]
    [MaxLength(500)]
    public string Title { get; set; }

    public string? Description { get; set; }

    [Required]
    public Guid AssignedToId { get; set; }

    public DateTime? DueDate { get; set; }

    public SupplementaryTaskPriority Priority { get; set; } = SupplementaryTaskPriority.Medium;

    public bool IsPrivate { get; set; }

    public List<string> ImageUrls { get; set; } = new();
}
