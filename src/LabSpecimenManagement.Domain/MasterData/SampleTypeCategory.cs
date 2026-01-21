using System;
using LabSpecimenManagement.Common;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Danh mục loại mẫu - Sample Type Category
/// </summary>
public class SampleTypeCategory : FullAuditedAggregateRoot<Guid>, IHasActive
{
    /// <summary>
    /// Tên danh mục loại mẫu
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Mô tả
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; }

    protected SampleTypeCategory()
    {
    }

    public SampleTypeCategory(Guid id, string name, string? description = null) : base(id)
    {
        Name = name;
        Description = description;
        IsActive = true;
    }
}
