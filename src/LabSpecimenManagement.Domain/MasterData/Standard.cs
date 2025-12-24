using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Tiêu chuẩn - Standard
/// </summary>
public class Standard : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// Mã tiêu chuẩn - Auto generated: TCddmmyyyyxxx
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Tên tiêu chuẩn
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Giới hạn (mô tả chung)
    /// </summary>
    public string? LimitDescription { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Thứ tự hiển thị
    /// </summary>
    public int SortOrder { get; set; }

    protected Standard()
    {
    }

    public Standard(Guid id, string code, string name) : base(id)
    {
        Code = code;
        Name = name;
        IsActive = true;
        SortOrder = 0;
    }
}
