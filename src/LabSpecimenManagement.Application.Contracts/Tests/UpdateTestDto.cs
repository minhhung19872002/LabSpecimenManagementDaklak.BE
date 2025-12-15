using System;
using System.ComponentModel.DataAnnotations;
using LabSpecimenManagement.TestRequests;

namespace LabSpecimenManagement.Tests;

public class UpdateTestDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Method { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Standard { get; set; } = string.Empty;

    public TestStatus Status { get; set; }

    public Guid? AssigneeId { get; set; }

    public DateTime? AssignmentDate { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? CompletionDate { get; set; }

    public decimal? EstimatedDuration { get; set; }

    public string? Notes { get; set; }
}
