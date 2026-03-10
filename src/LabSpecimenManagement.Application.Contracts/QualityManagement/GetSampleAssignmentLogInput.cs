using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.QualityManagement;

/// <summary>
/// Input DTO for getting Sample Assignment Log list
/// </summary>
public class GetSampleAssignmentLogInput : PagedAndSortedResultRequestDto
{
    /// <summary>
    /// Search by contract number, sample code, sample name, indicator name
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Filter by TestRequest status
    /// </summary>
    public TestRequests.TestRequestStatus? TestRequestStatus { get; set; }

    /// <summary>
    /// Filter from date (RequestCompletionDate)
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Filter to date (RequestCompletionDate)
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Filter by Laboratory IDs (multi-select)
    /// </summary>
    public List<Guid>? LaboratoryIds { get; set; }

    /// <summary>
    /// Lọc theo chỉ tiêu - Indicator IDs (multi-select)
    /// </summary>
    public List<Guid>? IndicatorIds { get; set; }

    /// <summary>
    /// Lọc theo thử nghiệm viên - Technician IDs (multi-select)
    /// </summary>
    public List<Guid>? TechnicianIds { get; set; }

    /// <summary>
    /// Filter by QLCLDisplayStatus values (computed in-memory)
    /// </summary>
    public List<int>? QlclDisplayStatuses { get; set; }

    public GetSampleAssignmentLogInput()
    {
        // Default sorting: RequestCompletionDate ascending
        Sorting = "RequestCompletionDate asc";
    }
}

/// <summary>
/// Statistics DTO for Sample Assignment Log - counts by QLKTDisplayStatus
/// </summary>
/// <summary>
/// Statistics DTO for QLCL Sample Assignment Log - counts by QLCLDisplayStatus
/// </summary>
public class QLCLSampleAssignmentLogStatisticsDto
{
    public int Total { get; set; }
    public int ToDo { get; set; }              // ToDo + Testing
    public int WaitingQLKTApproval { get; set; }
    public int WaitingQLCLApproval { get; set; }
    public int WaitingForPublishing { get; set; }
    public int Published { get; set; }
}

public class SampleAssignmentLogStatisticsDto
{
    public int Total { get; set; }
    public int UnderReview { get; set; }
    public int ToDo { get; set; }
    public int Testing { get; set; }
    public int WaitingApproval { get; set; }
    public int ResultRejected { get; set; }
    public int Completed { get; set; }
}
