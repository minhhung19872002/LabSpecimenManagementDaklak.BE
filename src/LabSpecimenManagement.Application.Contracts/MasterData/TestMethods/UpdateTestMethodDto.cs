using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.MasterData;

public class UpdateTestMethodDto
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; }

    [StringLength(512)]
    public string? FormattedName { get; set; }

    [Required]
    [StringLength(100)]
    public string Unit { get; set; }

    [StringLength(512)]
    public string? FormattedUnit { get; set; }

    public bool IsActive { get; set; }
}
