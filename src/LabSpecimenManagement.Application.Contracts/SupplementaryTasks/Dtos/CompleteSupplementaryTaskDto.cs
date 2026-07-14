using System.Collections.Generic;

namespace LabSpecimenManagement.SupplementaryTasks.Dtos;

public class CompleteSupplementaryTaskDto
{
    public string? Note { get; set; }
    public List<string>? ImageUrls { get; set; }
}
