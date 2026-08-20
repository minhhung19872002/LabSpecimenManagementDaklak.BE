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
    public string RequestNumber { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int SpecimenCount { get; set; }
}

public class PendingApprovalDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // TestResult, Report
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class ActivityLogDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
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
    public string Label { get; set; } = string.Empty;
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
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public string EntityCode { get; set; } = string.Empty;
    public Guid? PerformedById { get; set; }
    public string? PerformedByName { get; set; }
    public DateTime PerformedAt { get; set; }
}

#region Dashboard Overview DTOs

/// <summary>
/// Filter DTO for dashboard overview
/// </summary>
public class DashboardFilterDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
}

/// <summary>
/// Overview statistics DTO
/// </summary>
public class DashboardOverviewDto
{
    public int TotalContracts { get; set; }
    public int TotalSamples { get; set; }
    public int TotalIndicators { get; set; }
    public int TotalNCRs { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalGrossRevenue { get; set; }
    public int OnTimeTests { get; set; }
    public int LateTests { get; set; }
    public int PendingApproval { get; set; }
}

/// <summary>
/// Recent request DTO for dashboard
/// </summary>
public class DashboardRecentRequestDto
{
    public Guid Id { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime? PendingDate { get; set; }
    public int SampleCount { get; set; }
    public int IndicatorCount { get; set; }
    public decimal TotalCost { get; set; }
    public int Status { get; set; }
}

/// <summary>
/// Paged result for recent requests
/// </summary>
public class DashboardRecentRequestPagedResultDto
{
    public List<DashboardRecentRequestDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
}

/// <summary>
/// Revenue by customer DTO
/// </summary>
public class RevenueByCustomerDto
{
    public string CustomerName { get; set; } = string.Empty;
    public int TotalSamples { get; set; }
    public int TotalIndicators { get; set; }
    public decimal Revenue { get; set; }
}

/// <summary>
/// Paged result for revenue by customer
/// </summary>
public class RevenueByCustomerPagedResultDto
{
    public List<RevenueByCustomerDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
}

/// <summary>
/// Monthly revenue DTO for chart
/// </summary>
public class MonthlyRevenueDto
{
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int ContractCount { get; set; }
    public int TotalSamples { get; set; }
    public int TotalIndicators { get; set; }
    public decimal Revenue { get; set; }
}

/// <summary>
/// Customer name lookup DTO
/// </summary>
public class CustomerNameDto
{
    public string CustomerName { get; set; } = string.Empty;
}

/// <summary>
/// Revenue by contact person DTO
/// </summary>
public class RevenueByContactDto
{
    public string ContactPerson { get; set; } = string.Empty;
    public int TotalSamples { get; set; }
    public int TotalIndicators { get; set; }
    public decimal Revenue { get; set; }
}

/// <summary>
/// Paged result for revenue by contact person
/// </summary>
public class RevenueByContactPagedResultDto
{
    public List<RevenueByContactDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
}

#endregion
