using System;
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
    /// Navigation property to LaboratoryTaskIndicator
    /// </summary>
    public virtual LaboratoryTaskIndicator Indicator { get; set; } = null!;

    protected IndicatorRejectionHistory()
    {
    }

    public IndicatorRejectionHistory(
        Guid id,
        Guid indicatorId,
        string rejectionReason,
        Guid rejectedById,
        DateTime rejectedDate) : base(id)
    {
        IndicatorId = indicatorId;
        RejectionReason = rejectionReason;
        RejectedById = rejectedById;
        RejectedDate = rejectedDate;
    }
}
