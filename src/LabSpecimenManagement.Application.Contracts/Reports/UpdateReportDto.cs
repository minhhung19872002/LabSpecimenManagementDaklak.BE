using System;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.Reports;

public class UpdateReportDto
{
    [Required]
    [StringLength(256)]
    public string Title { get; set; }

    public Guid? CustomerId { get; set; }

    [Required]
    [StringLength(256)]
    public string CustomerName { get; set; }

    public string? SpecimenIds { get; set; }

    public string? TestIds { get; set; }

    [Required]
    public ReportStatus Status { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}
