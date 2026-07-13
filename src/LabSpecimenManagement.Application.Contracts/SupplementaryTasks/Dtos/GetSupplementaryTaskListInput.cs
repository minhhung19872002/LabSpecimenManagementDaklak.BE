using System;

namespace LabSpecimenManagement.SupplementaryTasks.Dtos;

public class GetSupplementaryTaskListInput
{
    public string? Search { get; set; }
    public SupplementaryTaskStatus? Status { get; set; }
    public SupplementaryTaskPriority? Priority { get; set; }
    public Guid? AssignedToId { get; set; }
    public int SkipCount { get; set; } = 0;
    public int MaxResultCount { get; set; } = 50;
}
