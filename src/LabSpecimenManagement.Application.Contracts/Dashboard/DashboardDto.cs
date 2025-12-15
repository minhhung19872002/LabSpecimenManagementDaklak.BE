using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.Dashboard;

public class DashboardStatisticsDto
{
    // Test Request Statistics
    public int TotalRequests { get; set; }
    public int PendingRequests { get; set; }
    public int InProgressRequests { get; set; }
    public int CompletedRequests { get; set; }

    // Specimen Statistics
    public int TotalSpecimens { get; set; }

    // Test Statistics
    public int TotalTests { get; set; }
    public int PendingTests { get; set; }
    public int InProgressTests { get; set; }
    public int CompletedTests { get; set; }

    // NCR Statistics
    public int OpenNCRs { get; set; }

    // Approval Statistics
    public int PendingApprovalResults { get; set; }

    // Report Statistics
    public int TotalReports { get; set; }
}

public class RecentTestRequestDto
{
    public Guid Id { get; set; }
    public string RequestNumber { get; set; }
    public DateTime RequestDate { get; set; }
    public string CustomerName { get; set; }
    public string Status { get; set; }
    public int SpecimenCount { get; set; }
}

public class PendingApprovalDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public string Type { get; set; } // TestResult, Report
    public string Title { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedByName { get; set; }
    public string Status { get; set; }
}

public class ActivityLogDto
{
    public Guid Id { get; set; }
    public string Action { get; set; }
    public string EntityType { get; set; }
    public string EntityCode { get; set; }
    public string Description { get; set; }
    public string UserName { get; set; }
    public DateTime Timestamp { get; set; }
}

public class DashboardDataDto
{
    public DashboardStatisticsDto Statistics { get; set; }
    public List<RecentTestRequestDto> RecentRequests { get; set; }
    public List<PendingApprovalDto> PendingApprovals { get; set; }
    public List<ActivityLogDto> RecentActivities { get; set; }
}

/// <summary>
/// Summary DTO matching frontend's DashboardSummaryDto interface
/// </summary>
public class DashboardSummaryDto
{
    public int TotalTestRequests { get; set; }
    public int PendingTestRequests { get; set; }
    public int TotalSpecimens { get; set; }
    public int SpecimenInTesting { get; set; }
    public int TotalTests { get; set; }
    public int PendingTests { get; set; }
    public int CompletedTests { get; set; }
    public int TotalTestResults { get; set; }
    public int PendingApprovalResults { get; set; }
    public int OpenNCRs { get; set; }
    public int PendingTasks { get; set; }
    public int TotalReports { get; set; }
    public int IssuedReports { get; set; }
}

/// <summary>
/// Chart data DTO for frontend charts
/// </summary>
public class ChartDataDto
{
    public List<string> Labels { get; set; } = new();
    public List<ChartDatasetDto> Datasets { get; set; } = new();
}

public class ChartDatasetDto
{
    public string Label { get; set; }
    public List<int> Data { get; set; } = new();
    public string? BackgroundColor { get; set; }
    public string? BorderColor { get; set; }
}

/// <summary>
/// Recent activity DTO for activity log
/// </summary>
public class RecentActivityDto
{
    public Guid Id { get; set; }
    public string Type { get; set; }
    public string Description { get; set; }
    public string EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string EntityCode { get; set; }
    public Guid? PerformedById { get; set; }
    public string? PerformedByName { get; set; }
    public DateTime PerformedAt { get; set; }
}
