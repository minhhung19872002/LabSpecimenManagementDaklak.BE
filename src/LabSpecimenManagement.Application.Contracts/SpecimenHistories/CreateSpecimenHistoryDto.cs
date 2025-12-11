using System;
using System.ComponentModel.DataAnnotations;
using LabSpecimenManagement.TestRequests;

namespace LabSpecimenManagement.SpecimenHistories;

public class CreateSpecimenHistoryDto
{
    [Required]
    public Guid SpecimenId { get; set; }

    [Required]
    [StringLength(200)]
    public string Action { get; set; }

    [Required]
    [StringLength(200)]
    public string Location { get; set; }

    [Required]
    public SpecimenStatus Status { get; set; }

    public string? Notes { get; set; }

    public Guid? PerformedById { get; set; }
}
