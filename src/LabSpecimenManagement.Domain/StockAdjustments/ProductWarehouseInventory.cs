using System;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.StockAdjustments;

public class ProductWarehouseInventory : Entity<Guid>
{
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }
    public DateTime LastInventoryDate { get; set; }

    protected ProductWarehouseInventory() { }

    public ProductWarehouseInventory(
        Guid id,
        Guid productId,
        Guid warehouseId,
        DateTime lastInventoryDate) : base(id)
    {
        ProductId = productId;
        WarehouseId = warehouseId;
        LastInventoryDate = lastInventoryDate;
    }
}
