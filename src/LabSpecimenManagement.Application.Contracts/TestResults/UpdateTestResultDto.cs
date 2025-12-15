using System;
using System.ComponentModel.DataAnnotations;
using LabSpecimenManagement.TestRequests;

namespace LabSpecimenManagement.TestResults;

public class UpdateTestResultDto
{
    [Required]
    [StringLength(200)]
    public string Parameter { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Value { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string Unit { get; set; } = string.Empty;

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
}
