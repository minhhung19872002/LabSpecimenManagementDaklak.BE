using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.MasterData.Notes;

/// <summary>
/// DTO cho Ghi chú
/// </summary>
public class NoteDto : FullAuditedEntityDto<Guid>
{
    /// <summary>
    /// Mã ghi chú
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Nội dung ghi chú
    /// </summary>
    public string Content { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Thứ tự sắp xếp
    /// </summary>
    public int SortOrder { get; set; }
}
