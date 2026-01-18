using System;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.MasterData;

public class CreateUpdateLaboratoryDto
{
    [Required]
    [StringLength(20)]
    public string Code { get; set; }

    /// <summary>
    /// Phòng ban liên kết - Department ID
    /// </summary>
    [Required]
    public Guid DepartmentId { get; set; }

    public Guid? TechnicalManagerId { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; } = 0;
}
