using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.QualityManagement;

/// <summary>
/// Lịch sử thay đổi trạng thái chỉ tiêu QLCL
/// </summary>
public class QualityIndicatorStatusHistory : CreationAuditedEntity<Guid>
{
    /// <summary>
    /// QualityManagementIndicator ID (Foreign Key)
    /// </summary>
    public Guid QualityManagementIndicatorId { get; set; }

    /// <summary>
    /// Trạng thái cũ - Old Status
    /// </summary>
    public QualityIndicatorApprovalStatus OldStatus { get; set; }

    /// <summary>
    /// Trạng thái mới - New Status
    /// </summary>
    public QualityIndicatorApprovalStatus NewStatus { get; set; }

    /// <summary>
    /// Ghi chú/Lý do - Comment (lý do từ chối nếu có)
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Navigation property to QualityManagementIndicator
    /// </summary>
    public virtual QualityManagementIndicator QualityManagementIndicator { get; set; } = null!;

    protected QualityIndicatorStatusHistory()
    {
    }

    public QualityIndicatorStatusHistory(
        Guid id,
        Guid qualityManagementIndicatorId,
        QualityIndicatorApprovalStatus oldStatus,
        QualityIndicatorApprovalStatus newStatus,
        string? comment = null) : base(id)
    {
        QualityManagementIndicatorId = qualityManagementIndicatorId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        Comment = comment;
    }
}
