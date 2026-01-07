using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// Lịch sử yêu cầu đổi phương pháp cho chỉ tiêu
/// </summary>
public class MethodChangeHistory : FullAuditedEntity<Guid>
{
    /// <summary>
    /// LaboratoryTaskIndicator ID (Foreign Key)
    /// </summary>
    public Guid IndicatorId { get; set; }

    /// <summary>
    /// Phương pháp cũ - Old Method ID (nullable nếu chưa có PP)
    /// </summary>
    public Guid? OldMethodId { get; set; }

    /// <summary>
    /// Tên phương pháp cũ (snapshot)
    /// </summary>
    [MaxLength(500)]
    public string? OldMethodName { get; set; }

    /// <summary>
    /// Phương pháp mới được đề xuất - New Method ID
    /// </summary>
    public Guid NewMethodId { get; set; }

    /// <summary>
    /// Tên phương pháp mới (snapshot)
    /// </summary>
    [MaxLength(500)]
    public string NewMethodName { get; set; } = string.Empty;

    /// <summary>
    /// Lý do yêu cầu đổi phương pháp
    /// </summary>
    [MaxLength(2000)]
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Ngày yêu cầu đổi PP
    /// </summary>
    public DateTime RequestedDate { get; set; }

    /// <summary>
    /// Người yêu cầu đổi PP - QLKT User ID
    /// </summary>
    public Guid RequestedByUserId { get; set; }

    /// <summary>
    /// Tên người yêu cầu đổi PP (snapshot)
    /// </summary>
    [MaxLength(256)]
    public string RequestedByUserName { get; set; } = string.Empty;

    /// <summary>
    /// Ngày KH xác nhận (null nếu chưa xác nhận)
    /// </summary>
    public DateTime? ConfirmedDate { get; set; }

    /// <summary>
    /// Người xác nhận - BPGN User ID (null nếu chưa xác nhận)
    /// </summary>
    public Guid? ConfirmedByUserId { get; set; }

    /// <summary>
    /// Tên người xác nhận (snapshot)
    /// </summary>
    [MaxLength(256)]
    public string? ConfirmedByUserName { get; set; }

    /// <summary>
    /// KH đã xác nhận đổi PP chưa (true = đồng ý, false = không đồng ý, null = chờ xác nhận)
    /// </summary>
    public bool? IsApproved { get; set; }

    /// <summary>
    /// Navigation property to LaboratoryTaskIndicator
    /// </summary>
    public virtual LaboratoryTaskIndicator Indicator { get; set; } = null!;

    protected MethodChangeHistory()
    {
    }

    public MethodChangeHistory(
        Guid id,
        Guid indicatorId,
        Guid? oldMethodId,
        string? oldMethodName,
        Guid newMethodId,
        string newMethodName,
        string reason,
        DateTime requestedDate,
        Guid requestedByUserId,
        string requestedByUserName) : base(id)
    {
        IndicatorId = indicatorId;
        OldMethodId = oldMethodId;
        OldMethodName = oldMethodName;
        NewMethodId = newMethodId;
        NewMethodName = newMethodName;
        Reason = reason;
        RequestedDate = requestedDate;
        RequestedByUserId = requestedByUserId;
        RequestedByUserName = requestedByUserName;
        IsApproved = null; // Chờ xác nhận
    }

    /// <summary>
    /// Xác nhận đổi PP
    /// </summary>
    public void Approve(Guid confirmedByUserId, string confirmedByUserName)
    {
        IsApproved = true;
        ConfirmedDate = DateTime.Now;
        ConfirmedByUserId = confirmedByUserId;
        ConfirmedByUserName = confirmedByUserName;
    }

    /// <summary>
    /// Từ chối đổi PP
    /// </summary>
    public void Reject(Guid confirmedByUserId, string confirmedByUserName)
    {
        IsApproved = false;
        ConfirmedDate = DateTime.Now;
        ConfirmedByUserId = confirmedByUserId;
        ConfirmedByUserName = confirmedByUserName;
    }
}
