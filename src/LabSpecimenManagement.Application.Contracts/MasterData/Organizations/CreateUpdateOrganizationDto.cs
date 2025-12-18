using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.MasterData.Organizations;

public class CreateUpdateOrganizationDto
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }
}
