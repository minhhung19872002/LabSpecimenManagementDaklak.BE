using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.ResultForms;

/// <summary>
/// Input DTO for generating result form
/// </summary>
public class GenerateResultFormInput
{
    /// <summary>
    /// TestRequest ID
    /// </summary>
    [Required]
    public Guid TestRequestId { get; set; }

    /// <summary>
    /// Sample ID
    /// </summary>
    [Required]
    public Guid SampleId { get; set; }
}

/// <summary>
/// Output DTO for generating result form
/// </summary>
public class GenerateResultFormOutput
{
    /// <summary>
    /// Generated file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Sample code for reference
    /// </summary>
    public string SampleCode { get; set; } = string.Empty;
}

/// <summary>
/// Input DTO for generating BM01 result form (single sample)
/// </summary>
public class GenerateBM01ResultFormInput
{
    /// <summary>
    /// TestRequest ID
    /// </summary>
    [Required]
    public Guid TestRequestId { get; set; }

    /// <summary>
    /// Sample ID
    /// </summary>
    [Required]
    public Guid SampleId { get; set; }

    /// <summary>
    /// Optional note to include in the result form (will be placed in {{Note}} placeholder)
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Template name (without .template.docx extension)
    /// If null, uses default template (BM01)
    /// </summary>
    public string? TemplateName { get; set; }
}

/// <summary>
/// Input DTO for generating test request form (BM05 - phiếu yêu cầu XN)
/// </summary>
public class GenerateTestRequestFormInput
{
    /// <summary>
    /// TestRequest ID
    /// </summary>
    [Required]
    public Guid TestRequestId { get; set; }
}

/// <summary>
/// Output DTO for generating test request form
/// </summary>
public class GenerateTestRequestFormOutput
{
    /// <summary>
    /// Generated file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Request number for reference
    /// </summary>
    public string RequestNumber { get; set; } = string.Empty;

    /// <summary>
    /// Contract number for reference
    /// </summary>
    public string ContractNumber { get; set; } = string.Empty;
}

/// <summary>
/// Input DTO for generating single indicator result form (In KQ Đơn)
/// </summary>
public class GenerateSingleIndicatorInput
{
    /// <summary>
    /// QualityManagementTask ID
    /// </summary>
    [Required]
    public Guid TaskId { get; set; }

    /// <summary>
    /// QualityManagementIndicator ID
    /// </summary>
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
/// Input DTO for generating mixed indicator result form (In KQ Hỗn hợp)
/// </summary>
public class GenerateMixedIndicatorInput
{
    /// <summary>
    /// QualityManagementTask ID
    /// </summary>
    [Required]
    public Guid TaskId { get; set; }

    /// <summary>
    /// List of QualityManagementIndicator IDs
    /// </summary>
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
/// Input DTO for generating mixed samples result form (In KQ Hỗn hợp cho nhiều mẫu)
/// Used from TestRequestDetail page
/// </summary>
public class GenerateMixedSamplesResultFormInput
{
    /// <summary>
    /// TestRequest ID
    /// </summary>
    [Required]
    public Guid TestRequestId { get; set; }

    /// <summary>
    /// List of Sample IDs to include in the result form
    /// </summary>
    [Required]
    public List<Guid> SampleIds { get; set; } = new();

    /// <summary>
    /// Optional note to include in the result form
    /// </summary>
    public string? Note { get; set; }
}

/// <summary>
/// DTO for template information
/// </summary>
public class TemplateDto
{
    /// <summary>
    /// Template name (without .template.docx extension) - used as value
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name (same as Name, for UI display)
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Full file name with extension
    /// </summary>
    public string FileName { get; set; } = string.Empty;
}
