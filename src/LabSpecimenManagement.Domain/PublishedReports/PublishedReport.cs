using System;
using LabSpecimenManagement.QualityManagement;
using LabSpecimenManagement.TestRequests;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.PublishedReports;

/// <summary>
/// Entity tracking lịch sử xuất báo cáo (mỗi lần in = 1 record)
/// Chỉ dùng để log, KHÔNG dùng để tính status
/// </summary>
public class PublishedReport : FullAuditedEntity<Guid>
{
    /// <summary>
    /// TestRequest ID (Foreign Key)
    /// </summary>
    public Guid TestRequestId { get; set; }

    /// <summary>
    /// QualityManagementTask ID (Foreign Key)
    /// </summary>
    public Guid QualityManagementTaskId { get; set; }

    /// <summary>
    /// Tên file PDF đã tạo (e.g., a1b2c3_KQ_DON_S001.pdf)
    /// </summary>
    public string FileName { get; set; } = null!;

    /// <summary>
    /// Loại báo cáo: "Single" (1 chỉ tiêu) hoặc "Mixed" (nhiều chỉ tiêu)
    /// </summary>
    public string ReportType { get; set; } = null!;

    /// <summary>
    /// Ghi chú khi in báo cáo
    /// </summary>
    public string? PrintNote { get; set; }

    /// <summary>
    /// Danh sách QualityManagementIndicator IDs trong báo cáo (JSON array)
    /// </summary>
    public string IndicatorIds { get; set; } = "[]";

    /// <summary>
    /// Navigation property to TestRequest
    /// </summary>
    public virtual TestRequest TestRequest { get; set; } = null!;

    /// <summary>
    /// Navigation property to QualityManagementTask
    /// </summary>
    public virtual QualityManagementTask QualityManagementTask { get; set; } = null!;

    protected PublishedReport()
    {
    }

    public PublishedReport(
        Guid id,
        Guid testRequestId,
        Guid qualityManagementTaskId,
        string fileName,
        string reportType,
        string? printNote,
        string indicatorIds) : base(id)
    {
        TestRequestId = testRequestId;
        QualityManagementTaskId = qualityManagementTaskId;
        FileName = fileName;
        ReportType = reportType;
        PrintNote = printNote;
        IndicatorIds = indicatorIds;
    }
}
