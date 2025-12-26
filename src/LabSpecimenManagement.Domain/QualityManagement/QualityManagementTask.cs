using System;
using System.Collections.Generic;
using LabSpecimenManagement.TestRequests;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.QualityManagement;

/// <summary>
/// Công việc Quản lý chất lượng - Quality Management Task
/// Mỗi record đại diện cho 1 Hợp đồng (TestRequest) cần phê duyệt
/// </summary>
public class QualityManagementTask : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// TestRequest ID (Foreign Key)
    /// </summary>
    public Guid TestRequestId { get; set; }

    /// <summary>
    /// Số hợp đồng - Contract Number (copied from TestRequest)
    /// </summary>
    public string ContractNumber { get; set; } = null!;

    /// <summary>
    /// Loại công việc - Task Type (ResultApproval hoặc ChangeApproval)
    /// </summary>
    public QualityTaskType TaskType { get; set; }

    /// <summary>
    /// Tổng số mẫu
    /// </summary>
    public int TotalSampleCount { get; set; }

    /// <summary>
    /// Tổng số chỉ tiêu
    /// </summary>
    public int TotalIndicatorCount { get; set; }

    /// <summary>
    /// Ngày chuyển đi - Transfer Date (Ngày phiếu yêu cầu được chuyển đi tới khoa phòng)
    /// </summary>
    public DateTime? TransferDate { get; set; }

    /// <summary>
    /// Ngày Yêu cầu xong - Request Completion Date (ngày xa nhất trong danh sách chỉ tiêu)
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
    /// Link đến TestRequestChangeHistory nếu là ChangeApproval
    /// </summary>
    public Guid? ChangeHistoryId { get; set; }

    /// <summary>
    /// Navigation property to TestRequest
    /// </summary>
    public virtual TestRequest TestRequest { get; set; } = null!;

    /// <summary>
    /// Collection of indicators for this task
    /// </summary>
    public virtual ICollection<QualityManagementIndicator> Indicators { get; set; }

    protected QualityManagementTask()
    {
        Indicators = new List<QualityManagementIndicator>();
    }

    public QualityManagementTask(
        Guid id,
        Guid testRequestId,
        string contractNumber,
        QualityTaskType taskType,
        int totalSampleCount,
        int totalIndicatorCount,
        DateTime? transferDate,
        DateTime? requestCompletionDate) : base(id)
    {
        TestRequestId = testRequestId;
        ContractNumber = contractNumber;
        TaskType = taskType;
        TotalSampleCount = totalSampleCount;
        TotalIndicatorCount = totalIndicatorCount;
        TransferDate = transferDate;
        RequestCompletionDate = requestCompletionDate;
        Status = QualityManagementStatus.PendingApproval;
        Indicators = new List<QualityManagementIndicator>();
    }
}
