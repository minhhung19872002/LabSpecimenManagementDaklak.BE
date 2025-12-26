using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.QualityManagement;

/// <summary>
/// Input DTO for getting Quality Management Task list
/// </summary>
public class GetQualityManagementListInput : PagedAndSortedResultRequestDto
{
    /// <summary>
    /// Search by contract number or technical manager name
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Filter by status
    /// </summary>
    public QualityManagementStatus? Status { get; set; }

    /// <summary>
    /// Filter by task type
    /// </summary>
    public QualityTaskType? TaskType { get; set; }

    /// <summary>
    /// Filter from date (TransferDate)
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Filter to date (TransferDate)
    /// </summary>
    public DateTime? ToDate { get; set; }
}
