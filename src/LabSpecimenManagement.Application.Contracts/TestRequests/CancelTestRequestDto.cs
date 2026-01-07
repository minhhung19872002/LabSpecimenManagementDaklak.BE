using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// DTO cho hủy yêu cầu thử nghiệm
/// </summary>
public class CancelTestRequestDto
{
    /// <summary>
    /// Lý do hủy (bắt buộc)
    /// </summary>
    [Required(ErrorMessage = "Vui lòng nhập lý do hủy")]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}
