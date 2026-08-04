using System;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.MasterData;

public class MaterialConsumptionItem : Entity<Guid>
{
    public Guid TestMethodIndicatorId { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? EquipmentId { get; set; }
    public Guid? UnitId { get; set; }
    public decimal Quantity { get; set; }
    public int? EstimatedSamplesPerYear { get; set; }
    public decimal? AvgConsumptionPerSample { get; set; }

    protected MaterialConsumptionItem()
    {
    }

    public MaterialConsumptionItem(Guid id, Guid testMethodIndicatorId, Guid? productId, Guid? equipmentId, Guid? unitId, decimal quantity, int? estimatedSamplesPerYear = null, decimal? avgConsumptionPerSample = null) : base(id)
    {
        TestMethodIndicatorId = testMethodIndicatorId;
        ProductId = productId;
        EquipmentId = equipmentId;
        UnitId = unitId;
        Quantity = quantity;
        EstimatedSamplesPerYear = estimatedSamplesPerYear;
        AvgConsumptionPerSample = avgConsumptionPerSample;
    }
}
