using System;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.MasterData;

public class MaterialConsumptionItem : Entity<Guid>
{
    public Guid TestMethodIndicatorId { get; set; }
    public Guid ProductId { get; set; }
    public Guid UnitId { get; set; }
    public decimal Quantity { get; set; }

    protected MaterialConsumptionItem()
    {
    }

    public MaterialConsumptionItem(Guid id, Guid testMethodIndicatorId, Guid productId, Guid unitId, decimal quantity) : base(id)
    {
        TestMethodIndicatorId = testMethodIndicatorId;
        ProductId = productId;
        UnitId = unitId;
        Quantity = quantity;
    }
}
