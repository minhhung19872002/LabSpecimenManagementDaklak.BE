using System;
using LabSpecimenManagement.LaboratoryTasks;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.QualityManagement;

/// <summary>
/// Chỉ tiêu trong công việc QLCL - Quality Management Indicator
/// </summary>
public class QualityManagementIndicator : Entity<Guid>
{
    /// <summary>
    /// QualityManagementTask ID (Foreign Key)
    /// </summary>
    public Guid QualityManagementTaskId { get; set; }

    /// <summary>
    /// LaboratoryTaskIndicator ID (Foreign Key)
    /// </summary>
    public Guid LaboratoryTaskIndicatorId { get; set; }

    /// <summary>
    /// Tiêu chuẩn so sánh - Standard ID
    /// </summary>
    public Guid? StandardId { get; set; }

    /// <summary>
    /// Giới hạn tham khảo - Reference Limit (auto-fill từ Standard)
    /// </summary>
    public string? ReferenceLimit { get; set; }

    /// <summary>
    /// Trạng thái phê duyệt - Approval Status
    /// </summary>
    public QualityIndicatorApprovalStatus ApprovalStatus { get; set; }

    /// <summary>
    /// Ngày duyệt - Approval Date
    /// </summary>
    public DateTime? ApprovalDate { get; set; }

    /// <summary>
    /// ID người duyệt - Approved By ID
    /// </summary>
    public Guid? ApprovedById { get; set; }

    /// <summary>
    /// Lý do từ chối - Rejection Reason
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Navigation property to QualityManagementTask
    /// </summary>
    public virtual QualityManagementTask QualityManagementTask { get; set; } = null!;

    /// <summary>
    /// Navigation property to LaboratoryTaskIndicator
    /// </summary>
    public virtual LaboratoryTaskIndicator LaboratoryTaskIndicator { get; set; } = null!;

    protected QualityManagementIndicator()
    {
    }

    public QualityManagementIndicator(
        Guid id,
        Guid qualityManagementTaskId,
        Guid laboratoryTaskIndicatorId) : base(id)
    {
        QualityManagementTaskId = qualityManagementTaskId;
        LaboratoryTaskIndicatorId = laboratoryTaskIndicatorId;
        ApprovalStatus = QualityIndicatorApprovalStatus.PendingApproval;
    }
}
