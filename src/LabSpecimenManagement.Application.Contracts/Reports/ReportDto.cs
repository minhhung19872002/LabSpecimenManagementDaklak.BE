using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Reports;

public class ReportDto : FullAuditedEntityDto<Guid>
{
    public string ReportNumber { get; set; }
    public DateTime ReportDate { get; set; }
    public Guid TestRequestId { get; set; }
    public string? QRCode { get; set; }
    public ReportStatus Status { get; set; }
    public Guid? ApprovedById { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public DateTime? SentDate { get; set; }
    public string? RecipientEmail { get; set; }
    public string? RecipientName { get; set; }
    public string? Standards { get; set; }
    public string? Conclusion { get; set; }
    public string? Notes { get; set; }

    // Navigation properties (optional, for frontend convenience)
    public List<Guid> TestResultIds { get; set; } = new();
}
