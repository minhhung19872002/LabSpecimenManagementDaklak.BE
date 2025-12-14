namespace LabSpecimenManagement.TestResults;

/// <summary>
/// DTO for department head to review test result (Step 1)
/// </summary>
public class ReviewResultDto
{
    /// <summary>
    /// Whether the result is approved or rejected
    /// </summary>
    public bool IsApproved { get; set; }

    /// <summary>
    /// Review comments from the department head
    /// </summary>
    public string? ReviewComments { get; set; }
}
