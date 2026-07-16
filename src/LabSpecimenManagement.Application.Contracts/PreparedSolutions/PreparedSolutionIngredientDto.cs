using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.PreparedSolutions;

public class PreparedSolutionIngredientDto : EntityDto<Guid>
{
    public Guid PreparedSolutionBatchId { get; set; }
    public int ProductType { get; set; }
    public string? ProductTypeName { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? UnitName { get; set; }
    public decimal QuantityUsed { get; set; }
    public decimal WarehouseQuantitySnapshot { get; set; }
    public bool IsEquipment { get; set; }
}
