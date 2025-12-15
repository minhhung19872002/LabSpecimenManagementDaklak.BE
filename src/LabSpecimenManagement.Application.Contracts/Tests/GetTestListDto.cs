using System;
using LabSpecimenManagement.TestRequests;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Tests;

/// <summary>
/// DTO for filtering and paging test list
/// </summary>
public class GetTestListDto : PagedAndSortedResultRequestDto
{
    /// <summary>
    /// Search text - searches in Code, Name, Method, Standard, SpecimenCode
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Filter by status
    /// </summary>
    public TestStatus? Status { get; set; }

    /// <summary>
    /// Filter by specimen ID
    /// </summary>
    public Guid? SpecimenId { get; set; }

    /// <summary>
    /// Filter by method name
    /// </summary>
    public string? Method { get; set; }

    /// <summary>
    /// Filter by assignee ID
    /// </summary>
    public Guid? AssigneeId { get; set; }
}
