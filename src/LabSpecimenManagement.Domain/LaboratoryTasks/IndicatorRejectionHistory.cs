using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// Lịch sử từ chối kết quả chỉ tiêu
/// </summary>
public class IndicatorRejectionHistory : CreationAuditedEntity<Guid>
{
    /// <summary>
    /// LaboratoryTaskIndicator ID (Foreign Key)
    /// </summary>
    public Guid IndicatorId { get; set; }

    /// <summary>
    /// Lý do từ chối
    /// </summary>
    public string RejectionReason { get; set; } = string.Empty;

    /// <summary>
    /// Ngày từ chối
    /// </summary>
    public DateTime RejectedDate { get; set; }

    /// <summary>
    /// Người từ chối - User ID
    /// </summary>
    public Guid RejectedById { get; set; }

    /// <summary>
    /// true = KTV báo không TN được, false = QLKT/QLCL từ chối kết quả
    /// </summary>
    public bool IsCannotTest { get; set; }

    /// <summary>
    /// Vai trò người báo: "Technician" hoặc "QualityManager"
    /// </summary>
    public string? ReportedByRole { get; set; }

    /// <summary>
    /// Navigation property to LaboratoryTaskIndicator
    /// </summary>
    public virtual LaboratoryTaskIndicator Indicator { get; set; } = null!;

    /// <summary>
    /// Navigation property to Rejection History Images
    /// </summary>
    public virtual ICollection<RejectionHistoryImage> Images { get; set; } = new List<RejectionHistoryImage>();

    protected IndicatorRejectionHistory()
    {
    }

    public IndicatorRejectionHistory(
        Guid id,
        Guid indicatorId,
        string rejectionReason,
        Guid rejectedById,
        DateTime rejectedDate,
        bool isCannotTest = false,
        string? reportedByRole = null) : base(id)
    {
        IndicatorId = indicatorId;
        RejectionReason = rejectionReason;
        RejectedById = rejectedById;
        RejectedDate = rejectedDate;
        IsCannotTest = isCannotTest;
        ReportedByRole = reportedByRole;
    }
}
