using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.MasterData;

public class CreateUpdateManufacturerDto
{
    [StringLength(50)]
    public string? Code { get; set; }

    [Required]
    [StringLength(500)]
    public string Name { get; set; }

    public bool IsActive { get; set; } = true;
}
