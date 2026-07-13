using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.SupplementaryTasks.Dtos;

public class SupplementaryTaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string? Description { get; set; }
    public Guid AssignedToId { get; set; }
    public string AssignedToName { get; set; }
    public DateTime? DueDate { get; set; }
    public SupplementaryTaskPriority Priority { get; set; }
    public SupplementaryTaskStatus Status { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public DateTime CreationTime { get; set; }
}
