using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.MasterData.Notes;

/// <summary>
/// Interface cho Note AppService
/// </summary>
public interface INoteAppService : IApplicationService
{
    /// <summary>
    /// Lấy danh sách ghi chú có lọc và phân trang
    /// </summary>
    Task<PagedResultDto<NoteDto>> GetListAsync(NoteFilterDto input);

    /// <summary>
    /// Lấy chi tiết ghi chú theo ID
    /// </summary>
    Task<NoteDto> GetAsync(Guid id);

    /// <summary>
    /// Tạo ghi chú mới
    /// </summary>
    Task<NoteDto> CreateAsync(CreateNoteDto input);

    /// <summary>
    /// Cập nhật ghi chú
    /// </summary>
    Task<NoteDto> UpdateAsync(Guid id, UpdateNoteDto input);

    /// <summary>
    /// Xóa ghi chú
    /// </summary>
    Task DeleteAsync(Guid id);

    /// <summary>
    /// Bật/tắt trạng thái hoạt động
    /// </summary>
    Task<NoteDto> ToggleActiveAsync(Guid id);

    /// <summary>
    /// Lấy danh sách ghi chú đang hoạt động (cho dropdown selection)
    /// </summary>
    Task<List<NoteDto>> GetActiveNotesAsync();
}
