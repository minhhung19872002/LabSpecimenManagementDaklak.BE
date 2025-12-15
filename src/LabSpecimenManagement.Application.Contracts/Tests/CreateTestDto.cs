using System;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.Tests;

public class CreateTestDto
{
    [Required]
    public Guid SpecimenId { get; set; }

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Method { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Standard { get; set; } = string.Empty;

    public Guid? AssigneeId { get; set; }

    public decimal? EstimatedDuration { get; set; }

    public string? Notes { get; set; }
}
