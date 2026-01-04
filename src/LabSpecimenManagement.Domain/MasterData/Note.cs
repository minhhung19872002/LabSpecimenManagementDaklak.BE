using System;
using LabSpecimenManagement.Common;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Ghi chú - Note
/// </summary>
public class Note : FullAuditedAggregateRoot<Guid>, IHasActive
{
    /// <summary>
    /// Mã ghi chú (tự động sinh: GC-0001, GC-0002...)
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

    protected Note()
    {
    }

    public Note(Guid id, string code, string content) : base(id)
    {
        Code = code;
        Content = content;
        IsActive = true;
        SortOrder = 0;
    }
}
