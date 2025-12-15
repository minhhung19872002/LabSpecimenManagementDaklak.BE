using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.Reports;

/// <summary>
/// Phiếu kết quả thử nghiệm - Test Report
/// ISO/IEC 17025:2017 Section 7.8
/// </summary>
public class Report : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// Số báo cáo (auto-generated: RPT-YYYYMMDD-XXXX)
    /// </summary>
    public string ReportNumber { get; set; }

    /// <summary>
    /// Tiêu đề báo cáo
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// ID yêu cầu thử nghiệm
    /// </summary>
    public Guid TestRequestId { get; set; }

    /// <summary>
    /// ID khách hàng
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// Tên khách hàng (denormalized)
    /// </summary>
    public string CustomerName { get; set; }

    /// <summary>
    /// Danh sách ID mẫu (JSON array)
    /// </summary>
    public string? SpecimenIds { get; set; }

    /// <summary>
    /// Danh sách ID phép thử (JSON array)
    /// </summary>
    public string? TestIds { get; set; }

    /// <summary>
    /// Trạng thái báo cáo
    /// </summary>
    public ReportStatus Status { get; set; }

    /// <summary>
    /// Ngày lập báo cáo
    /// </summary>
    public DateTime ReportDate { get; set; }

    /// <summary>
    /// Ngày phát hành
    /// </summary>
    public DateTime? IssuedDate { get; set; }

    /// <summary>
    /// Người lập báo cáo
    /// </summary>
    public Guid? PreparedById { get; set; }

    /// <summary>
    /// Người kiểm tra
    /// </summary>
    public Guid? ReviewedById { get; set; }

    /// <summary>
    /// Ngày kiểm tra
    /// </summary>
    public DateTime? ReviewedDate { get; set; }

    /// <summary>
    /// Người phê duyệt
    /// </summary>
    public Guid? ApprovedById { get; set; }

    /// <summary>
    /// Ngày phê duyệt
    /// </summary>
    public DateTime? ApprovedDate { get; set; }

    /// <summary>
    /// QR Code URL
    /// </summary>
    public string? QRCode { get; set; }

    /// <summary>
    /// Đường dẫn file PDF
    /// </summary>
    public string? PdfPath { get; set; }

    /// <summary>
    /// Đã gửi email cho khách hàng chưa
    /// </summary>
    public bool IsEmailSent { get; set; }

    /// <summary>
    /// Ngày gửi email
    /// </summary>
    public DateTime? EmailSentDate { get; set; }

    /// <summary>
    /// Ghi chú
    /// </summary>
    public string? Notes { get; set; }

    protected Report()
    {
    }

    public Report(
        Guid id,
        string reportNumber,
        string title,
        Guid testRequestId,
        string customerName
    ) : base(id)
    {
        ReportNumber = reportNumber;
        Title = title;
        TestRequestId = testRequestId;
        CustomerName = customerName;
        ReportDate = DateTime.Now;
        Status = ReportStatus.Draft;
        IsEmailSent = false;
    }
}
