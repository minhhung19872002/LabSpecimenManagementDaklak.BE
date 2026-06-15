using System;
using LabSpecimenManagement.Common;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.MasterData;

public class Manufacturer : FullAuditedAggregateRoot<Guid>, IHasActive
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }

    protected Manufacturer()
    {
    }

    public Manufacturer(Guid id, string code, string name) : base(id)
    {
        Code = code;
        Name = name;
        IsActive = true;
    }
}
