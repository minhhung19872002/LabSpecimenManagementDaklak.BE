using System;
using LabSpecimenManagement.TestRequests;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.SpecimenHistories;

/// <summary>
/// DTO for filtering and paging specimen history list
/// </summary>
public class GetSpecimenHistoryListDto : PagedAndSortedResultRequestDto
{
    /// <summary>
    /// Search text - searches in SpecimenCode, Action, Location, Notes
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Filter by status
    /// </summary>
    public SpecimenStatus? Status { get; set; }

    /// <summary>
    /// Filter by specimen ID
    /// </summary>
    public Guid? SpecimenId { get; set; }

    /// <summary>
    /// Filter by action
    /// </summary>
    public string? Action { get; set; }
}
