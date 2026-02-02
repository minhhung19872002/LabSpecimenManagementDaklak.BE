using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// DTO for batch operations on TestMethodIndicators
/// Uses composite key (IndicatorId + SpecimenTypeId) to identify records
/// </summary>
public class BulkIndicatorKeysDto
{
    [Required]
    public List<IndicatorKeyDto> Keys { get; set; } = new();
}

/// <summary>
/// Composite key for identifying a TestMethodIndicator record
/// </summary>
public class IndicatorKeyDto
{
    [Required]
    public Guid IndicatorId { get; set; }

    /// <summary>
    /// Nền mẫu - nullable to support records without specimen type
    /// </summary>
    public Guid? SpecimenTypeId { get; set; }
}
