using System;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.SupplementaryTasks;

public class SupplementaryTaskLog : Entity<Guid>
{
    public Guid TaskId { get; set; }
    public SupplementaryTaskStatus? OldStatus { get; set; }
    public SupplementaryTaskStatus NewStatus { get; set; }
    public DateTime ChangedAt { get; set; }
    public Guid ChangedById { get; set; }
    public string ChangedByName { get; set; }
    public string? Note { get; set; }
    public string? ImageUrls { get; set; }

    protected SupplementaryTaskLog() { }

    public SupplementaryTaskLog(Guid id, Guid taskId, SupplementaryTaskStatus? oldStatus,
        SupplementaryTaskStatus newStatus, Guid changedById, string changedByName,
        string? note = null, string? imageUrls = null) : base(id)
    {
        TaskId = taskId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        ChangedAt = DateTime.UtcNow;
        ChangedById = changedById;
        ChangedByName = changedByName;
        Note = note;
        ImageUrls = imageUrls;
    }
}
