using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.Statistics;

public class StatisticsOverviewDto
{
    public int TotalTestRequests { get; set; }
    public int TotalSpecimens { get; set; }
    public int TotalTests { get; set; }
    public int TotalReports { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal TATComplianceRate { get; set; }
}

public class SampleMatrixDto
{
    public string SpecimenType { get; set; }
    public int TotalCount { get; set; }
    public int ReceivedCount { get; set; }
    public int TestingCount { get; set; }
    public int CompletedCount { get; set; }
}

public class TATComplianceDto
{
    public string Period { get; set; } // Week, Month
    public int TotalRequests { get; set; }
    public int OnTimeRequests { get; set; }
    public int DelayedRequests { get; set; }
    public decimal ComplianceRate { get; set; }
}

public class WorkloadByDepartmentDto
{
    public string Department { get; set; }
    public int TotalTasks { get; set; }
    public int PendingTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int CompletedTasks { get; set; }
    public decimal CompletionRate { get; set; }
}

public class MonthlyTrendDto
{
    public string Month { get; set; }
    public int TestRequests { get; set; }
    public int Specimens { get; set; }
    public int Tests { get; set; }
    public int Reports { get; set; }
}

public class TATStatusDto
{
    public int OnTimeCount { get; set; }
    public int LateCount { get; set; }
}

public class LabStatisticsDto
{
    public Guid LabId { get; set; }
    public string LabName { get; set; }
    public int InProgressCount { get; set; }
    public int PendingCount { get; set; }
    public int TechnicianCount { get; set; }
}

public class TechnicianWorkDto
{
    public Guid TechnicianId { get; set; }
    public string TechnicianName { get; set; }
    public int TestingCount { get; set; }
    public int NotTestedCount { get; set; }
    public int WaitingApprovalCount { get; set; }
    public int RejectedCount { get; set; }
    public int ApprovedCount { get; set; }
    public int TotalCount { get; set; }
}

public class DepartmentWorkAssignmentDto
{
    public Guid LabId { get; set; }
    public string LabName { get; set; }
    public int TotalIndicators { get; set; }
    public List<TechnicianWorkDto> Technicians { get; set; } = new();
}

public class RegionalBubbleDto
{
    public string Ward { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalSamples { get; set; }
    public int TotalIndicators { get; set; }
}

public class StatisticsDataDto
{
    public StatisticsOverviewDto Overview { get; set; }
    public List<SampleMatrixDto> SampleMatrix { get; set; }
    public List<TATComplianceDto> TATCompliance { get; set; }
    public List<WorkloadByDepartmentDto> WorkloadByDepartment { get; set; }
    public List<MonthlyTrendDto> MonthlyTrends { get; set; }
    public List<LabStatisticsDto> LabStatistics { get; set; }
    public TATStatusDto TATStatus { get; set; }
    public List<DepartmentWorkAssignmentDto> DepartmentWorkAssignments { get; set; }
    public List<RegionalBubbleDto> RegionalBubbles { get; set; }
}
