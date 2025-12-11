using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// Kết quả thử nghiệm - Test Result
/// ISO/IEC 17025:2017 - Section 7.8 Reporting of results
/// </summary>
public class TestResult : FullAuditedAggregateRoot<Guid>
{
    public Guid TestId { get; set; }

    /// <summary>
    /// Thông số - Parameter name
    /// </summary>
    public string Parameter { get; set; }

    /// <summary>
    /// Giá trị kết quả - Result value
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// Đơn vị - Unit
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// Giới hạn dưới - Lower limit (if applicable)
    /// </summary>
    public decimal? LowerLimit { get; set; }

    /// <summary>
    /// Giới hạn trên - Upper limit (if applicable)
    /// </summary>
    public decimal? UpperLimit { get; set; }

    /// <summary>
    /// Đạt/Không đạt - Pass/Fail
    /// </summary>
    public bool? IsPass { get; set; }

    /// <summary>
    /// Trạng thái duyệt - Approval status
    /// </summary>
    public ResultApprovalStatus ApprovalStatus { get; set; }

    /// <summary>
    /// Người nhập kết quả - Entered by
    /// </summary>
    public Guid? EnteredById { get; set; }

    /// <summary>
    /// Ngày nhập kết quả - Entry date
    /// </summary>
    public DateTime? EntryDate { get; set; }

    /// <summary>
    /// Người duyệt bước 1 - Reviewer (Step 1)
    /// </summary>
    public Guid? ReviewerId { get; set; }

    /// <summary>
    /// Ngày duyệt bước 1 - Review date (Step 1)
    /// </summary>
    public DateTime? ReviewDate { get; set; }

    /// <summary>
    /// Comment từ người duyệt bước 1
    /// </summary>
    public string? ReviewComment { get; set; }

    /// <summary>
    /// Người phê duyệt bước 2 - Approver (Step 2)
    /// </summary>
    public Guid? ApproverId { get; set; }

    /// <summary>
    /// Ngày phê duyệt bước 2 - Approval date (Step 2)
    /// </summary>
    public DateTime? ApprovalDate { get; set; }

    /// <summary>
    /// Comment từ người phê duyệt bước 2
    /// </summary>
    public string? ApprovalComment { get; set; }

    /// <summary>
    /// Dữ liệu biểu đồ (JSON) - Chart data for visualization
    /// </summary>
    public string? ChartData { get; set; }

    /// <summary>
    /// Ghi chú - Notes
    /// </summary>
    public string? Notes { get; set; }

    public virtual Test Test { get; set; }

    protected TestResult()
    {
    }

    public TestResult(
        Guid id,
        Guid testId,
        string parameter,
        string value,
        string unit
    ) : base(id)
    {
        TestId = testId;
        Parameter = parameter;
        Value = value;
        Unit = unit;
        ApprovalStatus = ResultApprovalStatus.Draft;
    }
}
