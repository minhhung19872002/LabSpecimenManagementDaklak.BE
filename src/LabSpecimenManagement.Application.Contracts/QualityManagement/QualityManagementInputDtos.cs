using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.QualityManagement;

/// <summary>
/// Input DTO for rejecting indicator
/// </summary>
public class RejectIndicatorDto
{
    [Required]
    public string Reason { get; set; } = null!;
}

/// <summary>
/// Input DTO for updating indicator standard
/// </summary>
public class UpdateIndicatorStandardDto
{
    public Guid? StandardId { get; set; }
}

/// <summary>
/// Input DTO for print single
/// </summary>
public class PrintSingleDto
{
    [Required]
    public Guid IndicatorId { get; set; }

    /// <summary>
    /// Optional note to include in the result form
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Template name (without .template.docx extension)
    /// If null, uses default template
    /// </summary>
    public string? TemplateName { get; set; }
}

/// <summary>
/// Input DTO for print mixed
/// </summary>
public class PrintMixedDto
{
    [Required]
    public List<Guid> IndicatorIds { get; set; } = new();

    /// <summary>
    /// Optional note to include in the result form
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Template name (without .template.docx extension)
    /// If null, uses default template
    /// </summary>
    public string? TemplateName { get; set; }
}

/// <summary>
/// Input DTO for rejecting changes
/// </summary>
public class RejectChangesDto
{
    [Required]
    public string Reason { get; set; } = null!;
}

/// <summary>
/// Input DTO for bulk approve indicators
/// </summary>
public class BulkApproveIndicatorsDto
{
    [Required]
    public List<Guid> IndicatorIds { get; set; } = new();
}

/// <summary>
/// Input DTO for bulk reject indicators
/// </summary>
public class BulkRejectIndicatorsDto
{
    [Required]
    public List<Guid> IndicatorIds { get; set; } = new();

    [Required]
    public string Reason { get; set; } = null!;
}
