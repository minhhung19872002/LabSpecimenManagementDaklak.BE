using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// DTO cho lịch sử thay đổi phiếu yêu cầu
/// </summary>
public class TestRequestChangeHistoryDto : EntityDto<Guid>
{
    /// <summary>
    /// ID phiếu yêu cầu
    /// </summary>
    public Guid TestRequestId { get; set; }

    /// <summary>
    /// Tên field thay đổi
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
    /// Trạng thái trước khi thay đổi
    /// </summary>
    public TestRequestStatus? PreviousStatus { get; set; }

    /// <summary>
    /// Người phê duyệt
    /// </summary>
    public Guid? ApprovedById { get; set; }

    /// <summary>
    /// Tên người phê duyệt
    /// </summary>
    public string? ApprovedByName { get; set; }

    /// <summary>
    /// Ngày phê duyệt
    /// </summary>
    public DateTime? ApprovalDate { get; set; }

    /// <summary>
    /// Lý do từ chối
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Thời gian tạo
    /// </summary>
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// Người tạo (người chỉnh sửa)
    /// </summary>
    public Guid? CreatorId { get; set; }

    /// <summary>
    /// Tên người tạo
    /// </summary>
    public string? CreatorName { get; set; }
}

