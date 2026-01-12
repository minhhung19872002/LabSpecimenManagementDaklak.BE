using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.QualityManagement;

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

    /// <summary>
    /// Optional Vice President user ID - to replace {{VicePresident}} placeholder in PDF
    /// </summary>
    public Guid? VicePresidentId { get; set; }
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

    /// <summary>
    /// Optional Vice President user ID - to replace {{VicePresident}} placeholder in PDF
    /// </summary>
    public Guid? VicePresidentId { get; set; }
}

/// <summary>
/// Input DTO for batch print multiple indicators with multiple templates
/// Used for "In KQ Đơn" redesign - generates (labs × templates) PDFs
/// </summary>
public class PrintMultiSingleDto
{
    /// <summary>
    /// List of QualityManagementIndicator IDs to print
    /// </summary>
    [Required]
    public List<Guid> IndicatorIds { get; set; } = new();

    /// <summary>
    /// List of template names to use (without .template.docx extension)
    /// </summary>
    [Required]
    public List<string> TemplateNames { get; set; } = new();

    /// <summary>
    /// Optional note to include in the result forms
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Optional Vice President user ID - to replace {{VicePresident}} placeholder in PDF
    /// </summary>
    public Guid? VicePresidentId { get; set; }
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
