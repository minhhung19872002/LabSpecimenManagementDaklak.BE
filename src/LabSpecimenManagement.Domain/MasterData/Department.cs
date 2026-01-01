using System;
using LabSpecimenManagement.Common;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Phòng ban - Department
/// </summary>
public class Department : FullAuditedAggregateRoot<Guid>, IHasActive
{
    /// <summary>
    /// Mã phòng ban - Department Code
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Tên phòng ban - Department Name
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Mô tả - Description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Thứ tự hiển thị
    /// </summary>
    public int SortOrder { get; set; }

    protected Department()
    {
    }

    public Department(Guid id, string code, string name) : base(id)
    {
        Code = code;
        Name = name;
        IsActive = true;
        SortOrder = 0;
    }
}
