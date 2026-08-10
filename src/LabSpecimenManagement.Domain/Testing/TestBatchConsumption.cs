using System;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.Testing;

public class TestBatchConsumption : Entity<Guid>
{
    public Guid TestBatchId { get; set; }

    public Guid? ProductId { get; set; }

    public Guid? EquipmentId { get; set; }

    public Guid? UnitId { get; set; }

    public decimal RecommendedQuantity { get; set; }

    public decimal ActualQuantity { get; set; }

    public bool IsDeducted { get; set; }

    public Guid? WarehouseId { get; set; }

    protected TestBatchConsumption() { }

    public TestBatchConsumption(
        Guid id,
        Guid testBatchId,
        Guid? productId,
        Guid? equipmentId,
        Guid? unitId,
        decimal recommendedQuantity) : base(id)
    {
        TestBatchId = testBatchId;
        ProductId = productId;
        EquipmentId = equipmentId;
        UnitId = unitId;
        RecommendedQuantity = recommendedQuantity;
        ActualQuantity = recommendedQuantity;
    }
}
