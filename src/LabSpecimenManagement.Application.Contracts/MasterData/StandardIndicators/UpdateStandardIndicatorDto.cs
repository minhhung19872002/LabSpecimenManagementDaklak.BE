using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.MasterData;

public class UpdateStandardIndicatorDto
{
    [StringLength(10)]
    public string? ComparisonOperator { get; set; }

    [StringLength(256)]
    public string? AllowedLimit { get; set; }

    public bool IsActive { get; set; }
}
