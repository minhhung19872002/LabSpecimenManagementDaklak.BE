using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.Reports;

public class UpdateReportDto
{
    public DateTime ReportDate { get; set; }

    public string? RecipientEmail { get; set; }

    public string? RecipientName { get; set; }

    public string? Standards { get; set; }

    public string? Conclusion { get; set; }

    public string? Notes { get; set; }

    /// <summary>
    /// List of TestResult IDs to include in the report
    /// </summary>
    public List<Guid> TestResultIds { get; set; } = new();
}
