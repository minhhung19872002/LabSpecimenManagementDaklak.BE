using System;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.TestResults;

public class CreateTestResultDto
{
    [Required]
    public Guid TestId { get; set; }

    [Required]
    [StringLength(200)]
    public string Parameter { get; set; }

    [Required]
    [StringLength(500)]
    public string Value { get; set; }

    [Required]
    [StringLength(50)]
    public string Unit { get; set; }

    public decimal? LowerLimit { get; set; }

    public decimal? UpperLimit { get; set; }

    public bool? IsPass { get; set; }

    public string? ChartData { get; set; }

    public string? Notes { get; set; }
}
