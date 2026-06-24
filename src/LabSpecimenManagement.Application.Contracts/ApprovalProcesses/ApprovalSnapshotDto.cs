using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.ApprovalProcesses;

public class ApprovalSnapshotDto : EntityDto<Guid>
{
    public Guid ReferenceId { get; set; }
    public string ReferenceType { get; set; }
    public string ReferenceCode { get; set; }
    public ApprovalProcessType ProcessType { get; set; }
    public int TotalSteps { get; set; }
    public int CurrentStepOrder { get; set; }
    public ApprovalSnapshotStatus Status { get; set; }
    public DateTime CreationTime { get; set; }
    public List<ApprovalSnapshotStepDto> Steps { get; set; } = new();
}

public class ApprovalSnapshotStepDto : EntityDto<Guid>
{
    public int StepOrder { get; set; }
    public Guid ApproverId { get; set; }
    public string ApproverName { get; set; }
    public ApprovalStepStatus Status { get; set; }
    public DateTime? ActionDate { get; set; }
    public Guid? ActorId { get; set; }
    public string? ActorName { get; set; }
    public string? Notes { get; set; }
}
