using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// Lịch sử thay đổi phiếu yêu cầu thử nghiệm
/// </summary>
public class TestRequestChangeHistory : CreationAuditedEntity<Guid>
{
    /// <summary>
    /// ID phiếu yêu cầu
    /// </summary>
    public Guid TestRequestId { get; set; }

    /// <summary>
    /// Tên field thay đổi (hoặc "Snapshot" cho toàn bộ)
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Giá trị cũ (JSON)
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// Giá trị mới (JSON)
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// Trạng thái phê duyệt
    /// </summary>
    public ChangeApprovalStatus ApprovalStatus { get; set; }

    /// <summary>
    /// Trạng thái trước khi thay đổi (để rollback)
    /// </summary>
    public TestRequestStatus? PreviousStatus { get; set; }

    /// <summary>
    /// Người phê duyệt
    /// </summary>
    public Guid? ApprovedById { get; set; }

    /// <summary>
    /// Ngày phê duyệt
    /// </summary>
    public DateTime? ApprovalDate { get; set; }

    /// <summary>
    /// Lý do từ chối
    /// </summary>
    public string? RejectionReason { get; set; }

    public TestRequestChangeHistory()
    {
    }

    public TestRequestChangeHistory(
        Guid id,
        Guid testRequestId,
        string fieldName,
        string? oldValue,
        string? newValue,
        TestRequestStatus? previousStatus = null) : base(id)
    {
        TestRequestId = testRequestId;
        FieldName = fieldName;
        OldValue = oldValue;
        NewValue = newValue;
        PreviousStatus = previousStatus;
        ApprovalStatus = ChangeApprovalStatus.Pending;
    }
}
