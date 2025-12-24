using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.MasterData;

public class UpdateStandardDto
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; }

    [StringLength(500)]
    public string? LimitDescription { get; set; }

    public bool IsActive { get; set; }
}
