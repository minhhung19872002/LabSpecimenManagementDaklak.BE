using System;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// DTO để duyệt hoặc từ chối kết quả chỉ tiêu
/// </summary>
public class ApproveIndicatorDto
{
    /// <summary>
    /// True = Phê duyệt, False = Từ chối
    /// </summary>
    [Required]
    public bool Approve { get; set; }

    /// <summary>
    /// Lý do từ chối (bắt buộc nếu Approve = false)
    /// </summary>
    [StringLength(1000)]
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Tiêu chuẩn so sánh (optional)
    /// </summary>
    public Guid? StandardId { get; set; }
}
