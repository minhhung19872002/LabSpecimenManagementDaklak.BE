using System;
using System.Collections.Generic;
using LabSpecimenManagement.TestRequests;

namespace LabSpecimenManagement.QualityManagement;

/// <summary>
/// Response DTO for task history - supports both ResultApproval and ChangeApproval
/// </summary>
public class TaskHistoryResponseDto
{
    /// <summary>
    /// Loại công việc - Task Type
    /// </summary>
    public QualityTaskType TaskType { get; set; }

    /// <summary>
    /// Lịch sử chỉ tiêu (cho ResultApproval) - grouped by indicator
    /// </summary>
    public List<QualityTaskHistoryDto> IndicatorHistories { get; set; } = new();

    /// <summary>
    /// Lịch sử thay đổi phiếu yêu cầu (cho ChangeApproval)
    /// </summary>
    public List<ChangeApprovalHistoryDto> ChangeHistories { get; set; } = new();
}

/// <summary>
/// DTO for grouped indicator history (for ResultApproval)
/// </summary>
public class QualityTaskHistoryDto
{
    public Guid IndicatorId { get; set; }
    public string? IndicatorCode { get; set; }
    public string? IndicatorName { get; set; }
    public string? SampleCode { get; set; }
    public string? SampleName { get; set; }
    public List<QualityIndicatorStatusHistoryDto> Histories { get; set; } = new();
}

/// <summary>
/// DTO for Change Approval history
/// </summary>
public class ChangeApprovalHistoryDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = null!; // "Created", "Approved", "Rejected"
    public ChangeApprovalStatus Status { get; set; }
    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Reason { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreationTime { get; set; }
    public Guid? CreatorId { get; set; }
    public string? CreatorName { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public Guid? ApprovedById { get; set; }
    public string? ApprovedByName { get; set; }
}

/// <summary>
/// DTO for Quality Indicator Status History
/// </summary>
public class QualityIndicatorStatusHistoryDto
{
    /// <summary>
    /// ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// QualityManagementIndicator ID
    /// </summary>
    public Guid QualityManagementIndicatorId { get; set; }

    /// <summary>
    /// Indicator Code
    /// </summary>
    public string? IndicatorCode { get; set; }

    /// <summary>
    /// Indicator Name
    /// </summary>
    public string? IndicatorName { get; set; }

    /// <summary>
    /// Trạng thái cũ - Old Status
    /// </summary>
    public QualityIndicatorApprovalStatus OldStatus { get; set; }

    /// <summary>
    /// Trạng thái mới - New Status
    /// </summary>
    public QualityIndicatorApprovalStatus NewStatus { get; set; }

    /// <summary>
    /// Ghi chú - Comment
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Ngày thay đổi - Creation Time
    /// </summary>
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// ID người thay đổi - Creator ID
    /// </summary>
    public Guid? CreatorId { get; set; }

    /// <summary>
    /// Tên người thay đổi - Creator Name
    /// </summary>
    public string? CreatorName { get; set; }
}
