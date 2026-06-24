using System;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.ApprovalProcesses;

public class ApprovalSnapshotStep : Entity<Guid>
{
    public Guid ApprovalSnapshotId { get; set; }

    public int StepOrder { get; set; }

    public Guid ApproverId { get; set; }

    public string ApproverName { get; set; }

    public ApprovalStepStatus Status { get; set; }

    public DateTime? ActionDate { get; set; }

    public Guid? ActorId { get; set; }

    public string? ActorName { get; set; }

    public string? Notes { get; set; }

    protected ApprovalSnapshotStep()
    {
    }

    public ApprovalSnapshotStep(Guid id, Guid approvalSnapshotId, int stepOrder,
        Guid approverId, string approverName) : base(id)
    {
        ApprovalSnapshotId = approvalSnapshotId;
        StepOrder = stepOrder;
        ApproverId = approverId;
        ApproverName = approverName;
        Status = ApprovalStepStatus.Pending;
    }
}
