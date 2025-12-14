using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Reports;

public class ReportDto : FullAuditedEntityDto<Guid>
{
    public string ReportNumber { get; set; }
    public string Title { get; set; }
    public Guid TestRequestId { get; set; }
    public string? TestRequestNumber { get; set; }
    public Guid? CustomerId { get; set; }
    public string CustomerName { get; set; }
    public string? SpecimenIds { get; set; }
    public string? TestIds { get; set; }

    /// <summary>
    /// List of specimen codes (populated from SpecimenIds)
    /// </summary>
    public List<string>? SpecimenCodes { get; set; }

    /// <summary>
    /// Number of tests in this report
    /// </summary>
    public int TestCount { get; set; }

    public ReportStatus Status { get; set; }
    public DateTime ReportDate { get; set; }
    public DateTime? IssuedDate { get; set; }
    public Guid? PreparedById { get; set; }
    public string? PreparedByName { get; set; }
    public Guid? ReviewedById { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedDate { get; set; }
    public Guid? ApprovedById { get; set; }
    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedDate { get; set; }
    public string? QRCode { get; set; }
    public string? PdfPath { get; set; }
    public bool IsEmailSent { get; set; }
    public DateTime? EmailSentDate { get; set; }
    public string? Notes { get; set; }
}
