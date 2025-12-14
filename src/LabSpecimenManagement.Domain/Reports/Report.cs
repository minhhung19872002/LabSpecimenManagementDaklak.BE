using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;
using LabSpecimenManagement.TestRequests;

namespace LabSpecimenManagement.Reports;

/// <summary>
/// Báo cáo kết quả thử nghiệm - Test Report
/// ISO/IEC 17025:2017 - Section 7.8 Reporting of results
/// </summary>
public class Report : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// Số báo cáo - Report number (REP-YYYYMMDD-XXXX)
    /// </summary>
    public string ReportNumber { get; set; }

    /// <summary>
    /// Ngày báo cáo - Report date
    /// </summary>
    public DateTime ReportDate { get; set; }

    /// <summary>
    /// Phiếu yêu cầu - Test Request
    /// </summary>
    public Guid TestRequestId { get; set; }

    /// <summary>
    /// Mã QR code cho báo cáo
    /// </summary>
    public string? QRCode { get; set; }

    /// <summary>
    /// Trạng thái báo cáo - Report status
    /// </summary>
    public ReportStatus Status { get; set; }

    /// <summary>
    /// Người phê duyệt - Approver
    /// </summary>
    public Guid? ApprovedById { get; set; }

    /// <summary>
    /// Ngày phê duyệt - Approved date
    /// </summary>
    public DateTime? ApprovedDate { get; set; }

    /// <summary>
    /// Ngày gửi báo cáo - Sent date
    /// </summary>
    public DateTime? SentDate { get; set; }

    /// <summary>
    /// Email người nhận - Recipient email
    /// </summary>
    public string? RecipientEmail { get; set; }

    /// <summary>
    /// Tên người nhận - Recipient name
    /// </summary>
    public string? RecipientName { get; set; }

    /// <summary>
    /// Tiêu chuẩn áp dụng (JSON) - Applied standards
    /// </summary>
    public string? Standards { get; set; }

    /// <summary>
    /// Kết luận chung - General conclusion
    /// </summary>
    public string? Conclusion { get; set; }

    /// <summary>
    /// Ghi chú - Notes
    /// </summary>
    public string? Notes { get; set; }

    public virtual TestRequest TestRequest { get; set; }

    /// <summary>
    /// Danh sách kết quả trong báo cáo - Test results included in report
    /// </summary>
    public virtual ICollection<ReportTestResult> ReportTestResults { get; set; }

    protected Report()
    {
        ReportTestResults = new List<ReportTestResult>();
    }

    public Report(
        Guid id,
        string reportNumber,
        DateTime reportDate,
        Guid testRequestId
    ) : base(id)
    {
        ReportNumber = reportNumber;
        ReportDate = reportDate;
        TestRequestId = testRequestId;
        Status = ReportStatus.Draft;
        ReportTestResults = new List<ReportTestResult>();
    }
}
