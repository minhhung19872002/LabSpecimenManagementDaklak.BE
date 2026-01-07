using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.MasterData;

public class CreateUpdateTestResultValueDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }
}
