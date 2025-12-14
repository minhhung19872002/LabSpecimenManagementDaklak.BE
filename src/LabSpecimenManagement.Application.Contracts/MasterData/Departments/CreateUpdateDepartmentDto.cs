using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.MasterData;

public class CreateUpdateDepartmentDto
{
    [Required]
    [StringLength(20)]
    public string Code { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; } = 0;
}
