using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// DTO để cập nhật thông tin phương pháp phụ (áp dụng cho chỉ tiêu có phương pháp "-")
/// </summary>
public class UpdateSubContractMethodDto
{
    /// <summary>
    /// Tên phương pháp phụ
    /// </summary>
    [Required]
    [StringLength(500)]
    public string SubContractMethodName { get; set; } = string.Empty;

    /// <summary>
    /// Số hợp đồng phụ
    /// </summary>
    [Required]
    [StringLength(200)]
    public string SubContractNumber { get; set; } = string.Empty;

    /// <summary>
    /// Chi phí phương pháp phụ
    /// </summary>
    public decimal? SubContractCost { get; set; }
}
