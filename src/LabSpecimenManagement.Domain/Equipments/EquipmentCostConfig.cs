using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.Equipments;

public class EquipmentCostConfig : FullAuditedEntity<Guid>
{
    public Guid EquipmentId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime EffectiveTo { get; set; }
    public decimal? OperatingHoursPerYear { get; set; }
    public int? AverageSamplesPerYear { get; set; }
    public decimal? MaintenanceCostRatio { get; set; }
    public decimal? CalibrationCostPerYear { get; set; }
    public string? Description { get; set; }
    public string? ChangeLog { get; set; }

    protected EquipmentCostConfig() { }

    public EquipmentCostConfig(
        Guid id,
        Guid equipmentId,
        DateTime effectiveFrom,
        DateTime effectiveTo) : base(id)
    {
        EquipmentId = equipmentId;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
    }
}
