using System;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.Testing;

public class IndicatorConsumption : Entity<Guid>
{
    public Guid LaboratoryTaskIndicatorId { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? EquipmentId { get; set; }
    public Guid? UnitId { get; set; }
    public decimal RecommendedQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public bool IsDeducted { get; set; }

    protected IndicatorConsumption() { }

    public IndicatorConsumption(
        Guid id,
        Guid laboratoryTaskIndicatorId,
        Guid? productId,
        Guid? equipmentId,
        Guid? unitId,
        decimal recommendedQuantity) : base(id)
    {
        LaboratoryTaskIndicatorId = laboratoryTaskIndicatorId;
        ProductId = productId;
        EquipmentId = equipmentId;
        UnitId = unitId;
        RecommendedQuantity = recommendedQuantity;
        ActualQuantity = recommendedQuantity;
    }
}
