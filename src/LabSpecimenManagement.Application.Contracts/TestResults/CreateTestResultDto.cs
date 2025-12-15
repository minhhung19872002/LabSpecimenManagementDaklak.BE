using System;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.TestResults;

public class CreateTestResultDto
{
    [Required]
    public Guid TestId { get; set; }

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

    public string? ChartData { get; set; }

    public string? Notes { get; set; }
}
