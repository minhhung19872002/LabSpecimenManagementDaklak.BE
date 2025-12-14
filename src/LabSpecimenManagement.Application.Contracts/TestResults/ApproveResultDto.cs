namespace LabSpecimenManagement.TestResults;

/// <summary>
/// DTO for director to approve test result (Step 2)
/// </summary>
public class ApproveResultDto
{
    /// <summary>
    /// Whether the result is approved or rejected
    /// </summary>
    public bool IsApproved { get; set; }

    /// <summary>
    /// Approval comments from the director
    /// </summary>
    public string? ApprovalComments { get; set; }
}
