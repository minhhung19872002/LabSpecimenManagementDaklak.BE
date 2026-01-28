using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.MasterData;

public class AddIndicatorsToStandardDto
{
    [Required]
    public List<StandardIndicatorItemDto> Indicators { get; set; } = new();
}

public class StandardIndicatorItemDto
{
    [Required]
    public Guid IndicatorId { get; set; }

    [Required]
    public Guid TestMethodId { get; set; }

    [Required]
    [StringLength(100)]
    public string Unit { get; set; }

    [StringLength(10)]
    public string? ComparisonOperator { get; set; }

    [StringLength(256)]
    public string? AllowedLimit { get; set; }

    [StringLength(10)]
    public string? LowerOperator { get; set; }

    [StringLength(256)]
    public string? LowerLimit { get; set; }

    [StringLength(10)]
    public string? UpperOperator { get; set; }

    [StringLength(256)]
    public string? UpperLimit { get; set; }
}
