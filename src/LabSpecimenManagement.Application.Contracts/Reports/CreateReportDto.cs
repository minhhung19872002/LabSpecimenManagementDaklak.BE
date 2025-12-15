using System;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.Reports;

public class CreateReportDto
{
    [Required]
    [StringLength(256)]
    public string Title { get; set; }

    [Required]
    public Guid TestRequestId { get; set; }

    public Guid? CustomerId { get; set; }

    [Required]
    [StringLength(256)]
    public string CustomerName { get; set; }

    /// <summary>
    /// JSON array of specimen IDs
    /// </summary>
    public string? SpecimenIds { get; set; }

    /// <summary>
    /// JSON array of test IDs
    /// </summary>
    public string? TestIds { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}
