using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.SupplementaryTasks.Dtos;

public class SupplementaryTaskLogDto
{
    public Guid Id { get; set; }
    public SupplementaryTaskStatus? OldStatus { get; set; }
    public SupplementaryTaskStatus NewStatus { get; set; }
    public DateTime ChangedAt { get; set; }
    public Guid ChangedById { get; set; }
    public string ChangedByName { get; set; }
    public string? Note { get; set; }
    public List<string> ImageUrls { get; set; } = new();
}
