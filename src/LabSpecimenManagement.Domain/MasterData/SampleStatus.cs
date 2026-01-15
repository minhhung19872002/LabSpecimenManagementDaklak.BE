using System;
using LabSpecimenManagement.Common;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Tình trạng mẫu - Sample Condition (Master Data)
/// </summary>
public class SampleCondition : FullAuditedAggregateRoot<Guid>, IHasActive
{
    /// <summary>
    /// Tên tình trạng mẫu
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; }

    protected SampleCondition()
    {
    }

    public SampleCondition(Guid id, string name, string? description = null) : base(id)
    {
        Name = name;
        Description = description;
        IsActive = true;
    }
}
