using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LabSpecimenManagement.Products;

namespace LabSpecimenManagement.MasterData.MaterialConsumption;

public class MaterialConsumptionItemDto
{
    public Guid Id { get; set; }
    public Guid TestMethodIndicatorId { get; set; }
    public Guid? ProductId { get; set; }
    public string? ProductName { get; set; }
    public ProductType? ProductType { get; set; }
    public Guid? EquipmentId { get; set; }
    public string? EquipmentName { get; set; }
    public string? EquipmentSerialNumber { get; set; }
    public bool IsEquipment { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitName { get; set; }
    public decimal Quantity { get; set; }
    public int? EstimatedSamplesPerYear { get; set; }
    public decimal? AvgConsumptionPerSample { get; set; }
}

public class SaveMaterialConsumptionDto
{
    [Required]
    public List<SaveMaterialConsumptionItemDto> Items { get; set; } = new();
}

public class SaveMaterialConsumptionItemDto
{
    public Guid? ProductId { get; set; }
    public Guid? EquipmentId { get; set; }
    public bool IsEquipment { get; set; }
    public Guid? UnitId { get; set; }

    [Range(0d, double.MaxValue, MinimumIsExclusive = true, ErrorMessage = "Số lượng phải lớn hơn 0")]
    public decimal Quantity { get; set; }
    public int? EstimatedSamplesPerYear { get; set; }
    public decimal? AvgConsumptionPerSample { get; set; }
}

public class CheckConsumptionStatusInput
{
    [Required]
    public List<ConsumptionStatusItemInput> Items { get; set; } = new();
}

public class ConsumptionStatusItemInput
{
    [Required]
    public Guid TestMethodId { get; set; }

    [Required]
    public Guid IndicatorId { get; set; }

    public Guid? SpecimenTypeId { get; set; }
}

public class ConsumptionStatusDto
{
    public Guid TestMethodId { get; set; }
    public Guid IndicatorId { get; set; }
    public Guid? SpecimenTypeId { get; set; }
    public Guid? TestMethodIndicatorId { get; set; }
    public bool HasConsumption { get; set; }
}
