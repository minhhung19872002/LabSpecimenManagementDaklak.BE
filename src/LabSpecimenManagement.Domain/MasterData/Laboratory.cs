using System;
using LabSpecimenManagement.Common;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Khoa phòng - Laboratory
/// </summary>
public class Laboratory : FullAuditedAggregateRoot<Guid>, IHasActive
{
    /// <summary>
    /// Mã khoa phòng - Laboratory Code (unique)
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Tên khoa phòng - Laboratory Name (unique)
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// ID Quản lý kỹ thuật - Technical Manager User ID
    /// </summary>
    public Guid? TechnicalManagerId { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Thứ tự hiển thị
    /// </summary>
    public int SortOrder { get; set; }

    protected Laboratory()
    {
    }

    public Laboratory(Guid id, string code, string name) : base(id)
    {
        Code = code;
        Name = name;
        IsActive = true;
        SortOrder = 0;
    }
}
