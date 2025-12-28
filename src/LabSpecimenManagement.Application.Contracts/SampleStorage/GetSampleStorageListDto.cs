using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.SampleStorage;

/// <summary>
/// Input DTO cho lấy danh sách quản lý mẫu
/// </summary>
public class GetSampleStorageListDto : PagedAndSortedResultRequestDto
{
    /// <summary>
    /// Tìm kiếm theo số hợp đồng, tên khách hàng
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Lọc theo trạng thái
    /// </summary>
    public SampleStorageStatus? Status { get; set; }
}
