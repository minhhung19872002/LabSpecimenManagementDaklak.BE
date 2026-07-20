using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Equipments;

public class EquipmentCostConfigDto : EntityDto<Guid>
{
    public Guid EquipmentId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime EffectiveTo { get; set; }
    public decimal? OperatingHoursPerYear { get; set; }
    public int? AverageSamplesPerYear { get; set; }
    public decimal? MaintenanceCostRatio { get; set; }
    public decimal? CalibrationCostPerYear { get; set; }
    public string? Description { get; set; }
    public DateTime CreationTime { get; set; }
    public string? CreatorName { get; set; }
}

public class CreateEquipmentCostConfigDto
{
    [Required]
    public DateTime EffectiveFrom { get; set; }

    [Required]
    public DateTime EffectiveTo { get; set; }

    public decimal? OperatingHoursPerYear { get; set; }
    public int? AverageSamplesPerYear { get; set; }
    public decimal? MaintenanceCostRatio { get; set; }
    public decimal? CalibrationCostPerYear { get; set; }
}
