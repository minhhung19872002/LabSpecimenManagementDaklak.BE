using System;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.MasterData;

public class CreateUpdateWarehouseDto
{
    [Required]
    [StringLength(50)]
    public string Code { get; set; }

    [Required]
    [StringLength(256)]
    public string Name { get; set; }

    [StringLength(500)]
    public string? PhysicalLocation { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid? ParentWarehouseId { get; set; }

    [Required]
    public Guid LaboratoryId { get; set; }
}
