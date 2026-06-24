using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.ApprovalProcesses;

public class ApprovalSnapshot : CreationAuditedEntity<Guid>
{
    public Guid ReferenceId { get; set; }

    public string ReferenceType { get; set; }

    public string ReferenceCode { get; set; }

    public ApprovalProcessType ProcessType { get; set; }

    public int TotalSteps { get; set; }

    public int CurrentStepOrder { get; set; }

    public ApprovalSnapshotStatus Status { get; set; }

    public ICollection<ApprovalSnapshotStep> Steps { get; set; }

    protected ApprovalSnapshot()
    {
        Steps = new List<ApprovalSnapshotStep>();
    }

    public ApprovalSnapshot(Guid id, Guid referenceId, string referenceType,
        string referenceCode, ApprovalProcessType processType, int totalSteps) : base(id)
    {
        ReferenceId = referenceId;
        ReferenceType = referenceType;
        ReferenceCode = referenceCode;
        ProcessType = processType;
        TotalSteps = totalSteps;
        CurrentStepOrder = 0;
        Status = ApprovalSnapshotStatus.Pending;
        Steps = new List<ApprovalSnapshotStep>();
    }
}
