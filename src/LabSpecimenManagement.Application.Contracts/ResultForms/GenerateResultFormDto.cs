using System;
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
}
