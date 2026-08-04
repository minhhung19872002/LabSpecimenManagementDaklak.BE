using System;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.RoleWarehouses;

public class RoleWarehouse : Entity<Guid>, IHasCreationTime, IMayHaveCreator
{
    public Guid RoleId { get; set; }

    public Guid WarehouseId { get; set; }

    public DateTime CreationTime { get; set; }

    public Guid? CreatorId { get; set; }

    protected RoleWarehouse()
    {
    }

    public RoleWarehouse(
        Guid id,
        Guid roleId,
        Guid warehouseId) : base(id)
    {
        RoleId = roleId;
        WarehouseId = warehouseId;
        CreationTime = DateTime.UtcNow;
    }
}
