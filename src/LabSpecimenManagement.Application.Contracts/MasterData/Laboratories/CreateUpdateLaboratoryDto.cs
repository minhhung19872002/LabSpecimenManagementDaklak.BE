using System;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.MasterData;

public class CreateUpdateLaboratoryDto
{
    [Required]
    [StringLength(20)]
    public string Code { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    public Guid? TechnicalManagerId { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; } = 0;
}
