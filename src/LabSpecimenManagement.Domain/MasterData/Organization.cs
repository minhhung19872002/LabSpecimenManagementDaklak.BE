using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Co so/Doanh nghiep - Organization
/// </summary>
public class Organization : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// Ten co so/doanh nghiep
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Trang thai hoat dong
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Thu tu hien thi
    /// </summary>
    public int SortOrder { get; set; }

    protected Organization()
    {
    }

    public Organization(Guid id, string name) : base(id)
    {
        Name = name;
        IsActive = true;
        SortOrder = 0;
    }
}
