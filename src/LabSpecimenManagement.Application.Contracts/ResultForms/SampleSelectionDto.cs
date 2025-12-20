using System;

namespace LabSpecimenManagement.ResultForms;

/// <summary>
/// DTO for sample selection in result form generation
/// </summary>
public class SampleForSelectionDto
{
    /// <summary>
    /// Sample ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Sample code
    /// </summary>
    public string SampleCode { get; set; } = string.Empty;

    /// <summary>
    /// Sample name
    /// </summary>
    public string SampleName { get; set; } = string.Empty;

    /// <summary>
    /// Testing status text (e.g., "Đã gửi kết quả", "Đang thí nghiệm")
    /// </summary>
    public string TestingStatus { get; set; } = string.Empty;

    /// <summary>
    /// Whether the sample can generate result form (only when ResultSent)
    /// </summary>
    public bool CanGenerateResult { get; set; }

    /// <summary>
    /// Requested completion date
    /// </summary>
    public DateTime? RequestedCompletionDate { get; set; }
}
