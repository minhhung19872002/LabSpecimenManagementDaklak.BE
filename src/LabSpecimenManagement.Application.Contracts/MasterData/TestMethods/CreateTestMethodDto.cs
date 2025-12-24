using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.MasterData;

public class CreateTestMethodDto
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; }

    [Required]
    [StringLength(100)]
    public string Unit { get; set; }

    public bool IsActive { get; set; } = true;
}
