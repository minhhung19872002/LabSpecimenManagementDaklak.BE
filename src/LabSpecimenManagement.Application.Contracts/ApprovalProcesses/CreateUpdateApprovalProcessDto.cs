using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.ApprovalProcesses;

public class CreateUpdateApprovalProcessDto
{
    [Required]
    public ApprovalProcessType ProcessType { get; set; }

    [Required]
    public ApprovalScope Scope { get; set; }

    [Required]
    [MinLength(1)]
    public List<CreateUpdateApprovalProcessStepDto> Steps { get; set; } = new();
}

public class CreateUpdateApprovalProcessStepDto
{
    public int StepOrder { get; set; }

    [Required]
    public Guid ApproverId { get; set; }

    [Required]
    [StringLength(256)]
    public string ApproverName { get; set; }
}
