using System;
using LabSpecimenManagement.Common;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Nhóm chỉ tiêu - Indicator Group
/// </summary>
public class IndicatorGroup : FullAuditedAggregateRoot<Guid>, IHasActive
{
    /// <summary>
    /// Tên nhóm chỉ tiêu
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; }

    protected IndicatorGroup()
    {
    }

    public IndicatorGroup(Guid id, string name) : base(id)
    {
        Name = name;
        IsActive = true;
    }
}
