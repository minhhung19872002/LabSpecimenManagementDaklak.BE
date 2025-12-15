using System;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.TestRequests;

public class UpdateTestRequestDto
{
    [Required]
    public DateTime RequestDate { get; set; }

    [Required]
    [StringLength(256)]
    public string CustomerName { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? CustomerInfo { get; set; }

    [EmailAddress]
    [StringLength(256)]
    public string? CustomerEmail { get; set; }

    [Phone]
    [StringLength(50)]
    public string? CustomerPhone { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    public TestRequestStatus Status { get; set; }

    public DateTime? ExpectedCompletionDate { get; set; }

    [StringLength(256)]
    public string? Subcontractor { get; set; }
}
