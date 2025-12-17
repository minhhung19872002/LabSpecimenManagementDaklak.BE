using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Nhóm chỉ tiêu - Indicator Group
/// </summary>
public class IndicatorGroup : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// Tên nhóm chỉ tiêu
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Thứ tự hiển thị
    /// </summary>
    public int SortOrder { get; set; }

    protected IndicatorGroup()
    {
    }

    public IndicatorGroup(Guid id, string name) : base(id)
    {
        Name = name;
        IsActive = true;
        SortOrder = 0;
    }
}
