using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.MasterData.Notes;

/// <summary>
/// DTO lọc/tìm kiếm Ghi chú
/// </summary>
public class NoteFilterDto : PagedAndSortedResultRequestDto
{
    /// <summary>
    /// Từ khóa tìm kiếm (theo nội dung hoặc mã)
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Lọc theo trạng thái hoạt động
    /// </summary>
    public bool? IsActive { get; set; }
}
