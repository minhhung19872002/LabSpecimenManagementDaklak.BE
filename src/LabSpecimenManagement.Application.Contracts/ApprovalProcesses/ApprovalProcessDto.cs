using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.ApprovalProcesses;

public class ApprovalProcessDto : FullAuditedEntityDto<Guid>
{
    public ApprovalProcessType ProcessType { get; set; }
    public ApprovalScope Scope { get; set; }
    public bool IsActive { get; set; }
    public List<ApprovalProcessStepDto> Steps { get; set; } = new();
}

public class ApprovalProcessStepDto : EntityDto<Guid>
{
    public int StepOrder { get; set; }
    public Guid ApproverId { get; set; }
    public string ApproverName { get; set; }
}
