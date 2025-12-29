using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.QualityManagement;

/// <summary>
/// DTO for Quality Management Task (Công việc QLCL)
/// </summary>
public class QualityManagementTaskDto : FullAuditedEntityDto<Guid>
{
    /// <summary>
    /// TestRequest ID
    /// </summary>
    public Guid TestRequestId { get; set; }

    /// <summary>
    /// Số hợp đồng - Contract Number
    /// </summary>
    public string ContractNumber { get; set; } = null!;

    /// <summary>
    /// Loại công việc - Task Type
    /// </summary>
    public QualityTaskType TaskType { get; set; }

    /// <summary>
    /// Tổng số mẫu - Total Sample Count
    /// </summary>
    public int TotalSampleCount { get; set; }

    /// <summary>
    /// Tổng số chỉ tiêu - Total Indicator Count
    /// </summary>
    public int TotalIndicatorCount { get; set; }

    /// <summary>
    /// Tên các khoa phòng - Laboratory Names (comma separated)
    /// </summary>
    public string? LaboratoryNames { get; set; }

    /// <summary>
    /// Ngày chuyển đi - Transfer Date
    /// </summary>
    public DateTime? TransferDate { get; set; }

    /// <summary>
    /// Ngày Yêu cầu xong - Request Completion Date
    /// </summary>
    public DateTime? RequestCompletionDate { get; set; }

    /// <summary>
    /// Ngày gửi KQ - Result Sent Date
    /// </summary>
    public DateTime? ResultSentDate { get; set; }

    /// <summary>
    /// Trạng thái - Status
    /// </summary>
    public QualityManagementStatus Status { get; set; }

    /// <summary>
    /// Tên khách hàng - Customer Name (from TestRequest)
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Tên QLKT - Technical Manager Names
    /// </summary>
    public string? TechnicalManagerNames { get; set; }

    /// <summary>
    /// Link đến TestRequestChangeHistory nếu là ChangeApproval
    /// </summary>
    public Guid? ChangeHistoryId { get; set; }

    /// <summary>
    /// ID của QLCL được gán để xử lý task này
    /// </summary>
    public Guid? AssignedQualityManagerId { get; set; }

    /// <summary>
    /// Tên QLCL được gán - Assigned Quality Manager Name
    /// </summary>
    public string? AssignedQualityManagerName { get; set; }

    /// <summary>
    /// Thông tin thay đổi (cho ChangeApproval tasks)
    /// </summary>
    public ChangeHistoryInfoDto? ChangeHistory { get; set; }

    /// <summary>
    /// Danh sách chỉ tiêu - Indicators
    /// </summary>
    public List<QualityManagementIndicatorDto> Indicators { get; set; } = new();
}

/// <summary>
/// DTO for Change History Info (for ChangeApproval tasks)
/// </summary>
public class ChangeHistoryInfoDto
{
    public Guid Id { get; set; }
    public string FieldName { get; set; } = null!;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public string? Reason { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreationTime { get; set; }
    public Guid? CreatedById { get; set; }
    public string? CreatedByName { get; set; }
}

/// <summary>
/// Statistics DTO for Quality Management
/// </summary>
public class QualityManagementStatisticsDto
{
    /// <summary>
    /// Số hợp đồng chờ phê duyệt
    /// </summary>
    public int PendingApprovalCount { get; set; }

    /// <summary>
    /// Số hợp đồng trễ hạn (RequestCompletionDate - CurrentDate <= 2 days)
    /// </summary>
    public int OverdueCount { get; set; }

    /// <summary>
    /// Số hợp đồng chờ ký
    /// </summary>
    public int PendingSignatureCount { get; set; }
}

/// <summary>
/// Output DTO for print result - returns generated PDF file name
/// </summary>
public class PrintResultOutputDto
{
    /// <summary>
    /// Generated PDF file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;
}
