using LabSpecimenManagement.MasterData.Notes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers.MasterData;

[RemoteService(Name = "Default")]
[Route("api/v1/notes")]
[Authorize]
public class NoteController : LabSpecimenManagementController
{
    private readonly INoteAppService _noteAppService;

    public NoteController(INoteAppService noteAppService)
    {
        _noteAppService = noteAppService;
    }

    /// <summary>
    /// Lấy danh sách ghi chú có lọc và phân trang
    /// </summary>
    [HttpGet]
    public virtual Task<PagedResultDto<NoteDto>> GetListAsync([FromQuery] NoteFilterDto input)
    {
        return _noteAppService.GetListAsync(input);
    }

    /// <summary>
    /// Lấy chi tiết ghi chú theo ID
    /// </summary>
    [HttpGet("{id}")]
    public virtual Task<NoteDto> GetAsync(Guid id)
    {
        return _noteAppService.GetAsync(id);
    }

    /// <summary>
    /// Tạo ghi chú mới
    /// </summary>
    [HttpPost]
    public virtual Task<NoteDto> CreateAsync([FromBody] CreateNoteDto input)
    {
        return _noteAppService.CreateAsync(input);
    }

    /// <summary>
    /// Cập nhật ghi chú
    /// </summary>
    [HttpPut("{id}")]
    public virtual Task<NoteDto> UpdateAsync(Guid id, [FromBody] UpdateNoteDto input)
    {
        return _noteAppService.UpdateAsync(id, input);
    }

    /// <summary>
    /// Xóa ghi chú
    /// </summary>
    [HttpDelete("{id}")]
    public virtual Task DeleteAsync(Guid id)
    {
        return _noteAppService.DeleteAsync(id);
    }

    /// <summary>
    /// Bật/tắt trạng thái hoạt động
    /// </summary>
    [HttpPut("{id}/toggle-active")]
    public virtual Task<NoteDto> ToggleActiveAsync(Guid id)
    {
        return _noteAppService.ToggleActiveAsync(id);
    }

    /// <summary>
    /// Lấy danh sách ghi chú đang hoạt động (cho dropdown selection)
    /// </summary>
    [HttpGet("active")]
    public virtual Task<List<NoteDto>> GetActiveNotesAsync()
    {
        return _noteAppService.GetActiveNotesAsync();
    }
}
