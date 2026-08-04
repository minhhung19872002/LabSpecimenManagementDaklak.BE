using System;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.UserWarehouses;

public class UserWarehouse : Entity<Guid>, IHasCreationTime, IMayHaveCreator
{
    public Guid UserId { get; set; }

    public Guid WarehouseId { get; set; }

    public DateTime CreationTime { get; set; }

    public Guid? CreatorId { get; set; }

    protected UserWarehouse()
    {
    }

    public UserWarehouse(
        Guid id,
        Guid userId,
        Guid warehouseId) : base(id)
    {
        UserId = userId;
        WarehouseId = warehouseId;
        CreationTime = DateTime.UtcNow;
    }
}
