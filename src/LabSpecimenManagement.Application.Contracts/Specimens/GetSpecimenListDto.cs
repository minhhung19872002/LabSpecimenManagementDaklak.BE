using System;
using LabSpecimenManagement.TestRequests;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Specimens;

/// <summary>
/// DTO for filtering and paging specimen list
/// </summary>
public class GetSpecimenListDto : PagedAndSortedResultRequestDto
{
    /// <summary>
    /// Search text - searches in Code, Name, Type, TestRequestNumber
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Filter by status
    /// </summary>
    public SpecimenStatus? Status { get; set; }

    /// <summary>
    /// Filter by specimen type
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Filter by test request ID
    /// </summary>
    public Guid? TestRequestId { get; set; }

    /// <summary>
    /// Filter by start date (ReceivedDate >= FromDate)
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Filter by end date (ReceivedDate <= ToDate)
    /// </summary>
    public DateTime? ToDate { get; set; }
}
