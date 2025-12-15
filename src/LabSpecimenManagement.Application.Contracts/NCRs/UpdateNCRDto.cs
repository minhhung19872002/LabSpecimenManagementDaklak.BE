using System;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.NCRs;

public class UpdateNCRDto
{
    [Required]
    [StringLength(256)]
    public string Title { get; set; }

    [Required]
    [StringLength(2000)]
    public string Description { get; set; }

    [Required]
    public NCRType Type { get; set; }

    [Required]
    public NCRSeverity Severity { get; set; }

    [Required]
    public NCRStatus Status { get; set; }

    [StringLength(50)]
    public string? RelatedEntityType { get; set; }

    public Guid? RelatedEntityId { get; set; }

    [StringLength(50)]
    public string? RelatedEntityCode { get; set; }

    [StringLength(100)]
    public string? Department { get; set; }

    public Guid? AssignedToId { get; set; }

    [StringLength(2000)]
    public string? RootCause { get; set; }

    [StringLength(2000)]
    public string? CorrectiveAction { get; set; }

    [StringLength(2000)]
    public string? PreventiveAction { get; set; }

    [Required]
    public DateTime DueDate { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}
