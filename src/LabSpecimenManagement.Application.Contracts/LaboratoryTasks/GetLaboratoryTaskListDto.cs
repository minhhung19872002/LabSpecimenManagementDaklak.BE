using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// DTO for filtering Laboratory Task list
/// </summary>
public class GetLaboratoryTaskListDto : PagedAndSortedResultRequestDto
{
    /// <summary>
    /// Tìm kiếm theo số hợp đồng, số phiếu - Search
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Lọc theo khoa phòng - Laboratory ID
    /// </summary>
    public Guid? LaboratoryId { get; set; }

    /// <summary>
    /// Lọc theo quản lý kỹ thuật - Technical Manager ID
    /// </summary>
    public Guid? TechnicalManagerId { get; set; }

    /// <summary>
    /// Lọc theo trạng thái - Status
    /// </summary>
    public LaboratoryTaskStatus? Status { get; set; }

    /// <summary>
    /// Ngày chuyển từ - Transfer Date From
    /// </summary>
    public DateTime? TransferDateFrom { get; set; }

    /// <summary>
    /// Ngày chuyển đến - Transfer Date To
    /// </summary>
    public DateTime? TransferDateTo { get; set; }

    /// <summary>
    /// Ngày YC hoàn thành từ - Request Completion Date From
    /// </summary>
    public DateTime? RequestCompletionDateFrom { get; set; }

    /// <summary>
    /// Ngày YC hoàn thành đến - Request Completion Date To
    /// </summary>
    public DateTime? RequestCompletionDateTo { get; set; }
}
