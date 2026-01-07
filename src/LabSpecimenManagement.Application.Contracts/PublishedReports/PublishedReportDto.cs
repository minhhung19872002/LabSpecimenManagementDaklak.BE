using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.PublishedReports;

public class PublishedReportDto
{
    public Guid Id { get; set; }
    public Guid TestRequestId { get; set; }
    public Guid QualityManagementTaskId { get; set; }
    public string FileName { get; set; } = null!;
    public string ReportType { get; set; } = null!;
    public string? PrintNote { get; set; }
    public List<Guid> IndicatorIds { get; set; } = new();
    public List<string> IndicatorNames { get; set; } = new();
    public DateTime CreationTime { get; set; }
    public string? CreatorName { get; set; }
}
