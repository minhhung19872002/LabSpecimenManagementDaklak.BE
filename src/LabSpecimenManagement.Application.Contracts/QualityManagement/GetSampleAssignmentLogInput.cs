using System;
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

    public GetSampleAssignmentLogInput()
    {
        // Default sorting: RequestCompletionDate ascending
        Sorting = "RequestCompletionDate asc";
    }
}
