using System;
using LabSpecimenManagement.Common;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.MasterData;

public class Warehouse : FullAuditedAggregateRoot<Guid>, IHasActive
{
    public string Code { get; set; }

    public string Name { get; set; }

    public string? PhysicalLocation { get; set; }

    public bool IsActive { get; set; }

    public bool IsDefault { get; set; }

    public int SortOrder { get; set; }

    protected Warehouse()
    {
    }

    public Warehouse(Guid id, string code, string name) : base(id)
    {
        Code = code;
        Name = name;
        IsActive = true;
        SortOrder = 0;
    }
}
