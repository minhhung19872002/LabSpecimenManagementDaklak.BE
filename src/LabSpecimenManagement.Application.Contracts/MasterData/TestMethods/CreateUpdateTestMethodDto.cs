using System;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.MasterData;

public class CreateUpdateTestMethodDto
{
    [Required]
    [StringLength(50)]
    public string Code { get; set; }

    [Required]
    [StringLength(256)]
    public string Name { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [StringLength(256)]
    public string? Standard { get; set; }

    public Guid? DepartmentId { get; set; }

    public int? EstimatedDurationHours { get; set; }

    public decimal? Price { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; } = 0;
}
