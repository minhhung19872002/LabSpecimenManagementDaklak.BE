using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.Testing;

/// <summary>
/// DTO cho KTV báo không thể thử nghiệm
/// </summary>
public class CannotTestDto
{
    /// <summary>
    /// Lý do không thể thử nghiệm (bắt buộc)
    /// </summary>
    [Required(ErrorMessage = "Lý do không thể thử nghiệm là bắt buộc")]
    [MaxLength(1000, ErrorMessage = "Lý do không được vượt quá 1000 ký tự")]
    public string Reason { get; set; } = string.Empty;
}
