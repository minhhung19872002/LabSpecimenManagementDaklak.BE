using System;
using Volo.Abp.Application.Dtos;
using LabSpecimenManagement.TestRequests;

namespace LabSpecimenManagement.TestResults;

public class TestResultDto : FullAuditedEntityDto<Guid>
{
    public Guid TestId { get; set; }

    public string Parameter { get; set; }

    public string Value { get; set; }

    public string Unit { get; set; }

    public decimal? LowerLimit { get; set; }

    public decimal? UpperLimit { get; set; }

    public bool? IsPass { get; set; }

    public ResultApprovalStatus ApprovalStatus { get; set; }

    public Guid? EnteredById { get; set; }

    public DateTime? EntryDate { get; set; }

    public Guid? ReviewerId { get; set; }

    public DateTime? ReviewDate { get; set; }

    public string? ReviewComment { get; set; }

    public Guid? ApproverId { get; set; }

    public DateTime? ApprovalDate { get; set; }

    public string? ApprovalComment { get; set; }

    public string? ChartData { get; set; }

    public string? Notes { get; set; }

    // Navigation properties
    public string? TestCode { get; set; }

    public string? TestName { get; set; }
}
