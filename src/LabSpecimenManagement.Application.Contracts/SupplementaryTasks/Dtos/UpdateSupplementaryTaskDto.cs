using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.SupplementaryTasks.Dtos;

public class UpdateSupplementaryTaskDto
{
    [Required]
    [MaxLength(500)]
    public string Title { get; set; }

    public string? Description { get; set; }

    [Required]
    public Guid AssignedToId { get; set; }

    public DateTime? DueDate { get; set; }

    public SupplementaryTaskPriority Priority { get; set; }

    public SupplementaryTaskStatus Status { get; set; }

    public List<string> ImageUrls { get; set; } = new();
}
