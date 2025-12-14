using System;

namespace LabSpecimenManagement.Reports;

public class ReportVerifyResultDto
{
    public bool IsValid { get; set; }
    public string? ReportNumber { get; set; }
    public string? Title { get; set; }
    public string? CustomerName { get; set; }
    public ReportStatus? Status { get; set; }
    public DateTime? IssuedDate { get; set; }
    public string? Message { get; set; }
}
