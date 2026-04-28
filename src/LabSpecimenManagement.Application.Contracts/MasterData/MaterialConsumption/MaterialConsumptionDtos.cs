using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LabSpecimenManagement.Products;

namespace LabSpecimenManagement.MasterData.MaterialConsumption;

public class MaterialConsumptionItemDto
{
    public Guid Id { get; set; }
    public Guid TestMethodIndicatorId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public ProductType ProductType { get; set; }
    public Guid UnitId { get; set; }
    public string UnitName { get; set; }
    public decimal Quantity { get; set; }
}

public class SaveMaterialConsumptionDto
{
    [Required]
    public List<SaveMaterialConsumptionItemDto> Items { get; set; } = new();
}

public class SaveMaterialConsumptionItemDto
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    public Guid UnitId { get; set; }

    [Range(0.0001, double.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
    public decimal Quantity { get; set; }
}
