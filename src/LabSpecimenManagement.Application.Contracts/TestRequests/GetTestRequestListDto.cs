using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// DTO for filtering and paging test request list
/// </summary>
public class GetTestRequestListDto : PagedAndSortedResultRequestDto
{
    /// <summary>
    /// Search text - searches in RequestNumber, CustomerName, CustomerEmail, CustomerPhone
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Filter by status
    /// </summary>
    public TestRequestStatus? Status { get; set; }

    /// <summary>
    /// Filter by start date (RequestDate >= FromDate)
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Filter by end date (RequestDate <= ToDate)
    /// </summary>
    public DateTime? ToDate { get; set; }

    /// <summary>
    /// Filter by customer name (partial match)
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Filter by expected completion date (ExpectedCompletionDate >= ExpectedCompletionDateFrom)
    /// </summary>
    public DateTime? ExpectedCompletionDateFrom { get; set; }

    /// <summary>
    /// Filter by expected completion date (ExpectedCompletionDate <= ExpectedCompletionDateTo)
    /// </summary>
    public DateTime? ExpectedCompletionDateTo { get; set; }
}
