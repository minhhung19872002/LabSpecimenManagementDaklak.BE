using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.Employees;

public class ResetPasswordDto
{
    /// <summary>
    /// If true, use Password field instead of auto-generating password.
    /// When true, no email will be sent.
    /// </summary>
    public bool UseManualPassword { get; set; } = false;

    /// <summary>
    /// Manual password (required when UseManualPassword = true)
    /// </summary>
    [StringLength(128)]
    public string? Password { get; set; }

    /// <summary>
    /// Confirm password (required when UseManualPassword = true)
    /// </summary>
    [StringLength(128)]
    public string? ConfirmPassword { get; set; }
}
