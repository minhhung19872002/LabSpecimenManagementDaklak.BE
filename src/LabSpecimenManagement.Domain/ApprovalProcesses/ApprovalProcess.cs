using System;
using System.Collections.Generic;
using LabSpecimenManagement.Common;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.ApprovalProcesses;

public class ApprovalProcess : FullAuditedAggregateRoot<Guid>, IHasActive
{
    public ApprovalProcessType ProcessType { get; set; }

    public ApprovalScope Scope { get; set; }

    public bool IsActive { get; set; }

    public ICollection<ApprovalProcessStep> Steps { get; set; }

    protected ApprovalProcess()
    {
        Steps = new List<ApprovalProcessStep>();
    }

    public ApprovalProcess(Guid id, ApprovalProcessType processType, ApprovalScope scope) : base(id)
    {
        ProcessType = processType;
        Scope = scope;
        IsActive = true;
        Steps = new List<ApprovalProcessStep>();
    }
}
