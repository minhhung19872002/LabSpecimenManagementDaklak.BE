using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// DTO để cập nhật kết quả thí nghiệm cho chỉ tiêu
/// </summary>
public class UpdateIndicatorResultDto
{
    /// <summary>
    /// Kết quả thí nghiệm
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Result { get; set; } = string.Empty;

    /// <summary>
    /// Đơn vị tính
    /// </summary>
    [StringLength(50)]
    public string? Unit { get; set; }
}
