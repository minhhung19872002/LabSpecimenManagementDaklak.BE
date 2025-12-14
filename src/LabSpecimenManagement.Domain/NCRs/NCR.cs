using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.NCRs;

/// <summary>
/// Báo cáo không phù hợp - Non-Conformance Report
/// ISO/IEC 17025:2017 Section 7.10
/// </summary>
public class NCR : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// Mã NCR (auto-generated: NCR-YYYYMMDD-XXXX)
    /// </summary>
    public string NCRNumber { get; set; }

    /// <summary>
    /// Tiêu đề
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// Mô tả chi tiết
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// Loại NCR
    /// </summary>
    public NCRType Type { get; set; }

    /// <summary>
    /// Mức độ nghiêm trọng
    /// </summary>
    public NCRSeverity Severity { get; set; }

    /// <summary>
    /// Trạng thái
    /// </summary>
    public NCRStatus Status { get; set; }

    /// <summary>
    /// Loại đối tượng liên quan (TestResult, Specimen, Equipment...)
    /// </summary>
    public string? RelatedEntityType { get; set; }

    /// <summary>
    /// ID đối tượng liên quan
    /// </summary>
    public Guid? RelatedEntityId { get; set; }

    /// <summary>
    /// Mã đối tượng liên quan (để hiển thị)
    /// </summary>
    public string? RelatedEntityCode { get; set; }

    /// <summary>
    /// Phòng ban liên quan
    /// </summary>
    public string? Department { get; set; }

    /// <summary>
    /// Người báo cáo
    /// </summary>
    public Guid ReportedById { get; set; }

    /// <summary>
    /// Ngày báo cáo
    /// </summary>
    public DateTime ReportedAt { get; set; }

    /// <summary>
    /// Người được giao xử lý
    /// </summary>
    public Guid? AssignedToId { get; set; }

    /// <summary>
    /// Nguyên nhân gốc
    /// </summary>
    public string? RootCause { get; set; }

    /// <summary>
    /// Hành động khắc phục
    /// </summary>
    public string? CorrectiveAction { get; set; }

    /// <summary>
    /// Hành động phòng ngừa
    /// </summary>
    public string? PreventiveAction { get; set; }

    /// <summary>
    /// Hạn xử lý
    /// </summary>
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Người đóng NCR
    /// </summary>
    public Guid? ClosedById { get; set; }

    /// <summary>
    /// Ngày đóng
    /// </summary>
    public DateTime? ClosedAt { get; set; }

    /// <summary>
    /// Ghi chú
    /// </summary>
    public string? Notes { get; set; }

    protected NCR()
    {
    }

    public NCR(
        Guid id,
        string ncrNumber,
        string title,
        string description,
        NCRType type,
        NCRSeverity severity,
        Guid reportedById,
        DateTime dueDate
    ) : base(id)
    {
        NCRNumber = ncrNumber;
        Title = title;
        Description = description;
        Type = type;
        Severity = severity;
        ReportedById = reportedById;
        ReportedAt = DateTime.Now;
        DueDate = dueDate;
        Status = NCRStatus.Open;
    }
}
