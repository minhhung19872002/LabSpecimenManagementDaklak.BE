using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.SupplementaryTasks;

public class SupplementaryTask : FullAuditedAggregateRoot<Guid>
{
    public string Title { get; set; }
    public string? Description { get; set; }
    public Guid AssignedToId { get; set; }
    public string AssignedToName { get; set; }
    public DateTime? DueDate { get; set; }
    public SupplementaryTaskPriority Priority { get; set; }
    public SupplementaryTaskStatus Status { get; set; }
    public string? ImageUrls { get; set; }
    public string? CompletionNote { get; set; }
    public string? CompletionImageUrls { get; set; }
    public string? RejectionReason { get; set; }

    protected SupplementaryTask() { }

    public SupplementaryTask(Guid id, string title, Guid assignedToId, string assignedToName,
        SupplementaryTaskPriority priority, DateTime? dueDate) : base(id)
    {
        Title = title;
        AssignedToId = assignedToId;
        AssignedToName = assignedToName;
        Priority = priority;
        DueDate = dueDate;
        Status = SupplementaryTaskStatus.Pending;
    }
}
