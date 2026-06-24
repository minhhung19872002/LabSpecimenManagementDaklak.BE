using System;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.ApprovalProcesses;

public class ApprovalProcessStep : Entity<Guid>
{
    public Guid ApprovalProcessId { get; set; }

    public int StepOrder { get; set; }

    public Guid ApproverId { get; set; }

    public string ApproverName { get; set; }

    protected ApprovalProcessStep()
    {
    }

    public ApprovalProcessStep(Guid id, Guid approvalProcessId, int stepOrder,
        Guid approverId, string approverName) : base(id)
    {
        ApprovalProcessId = approvalProcessId;
        StepOrder = stepOrder;
        ApproverId = approverId;
        ApproverName = approverName;
    }
}
