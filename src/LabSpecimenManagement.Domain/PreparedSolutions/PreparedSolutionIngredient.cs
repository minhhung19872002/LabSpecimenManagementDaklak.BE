using System;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.PreparedSolutions;

public class PreparedSolutionIngredient : Entity<Guid>
{
    public Guid PreparedSolutionBatchId { get; set; }

    // Product type (ProductType enum value: 0=Chemical, 5=Equipment, etc.)
    public int ProductType { get; set; }
    public string? ProductTypeName { get; set; }

    // Product catalog reference
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? UnitName { get; set; }

    // Quantity deducted from stock on creation (not decremented afterward)
    public decimal QuantityUsed { get; set; }

    // Snapshot of warehouse stock at time of creation (for display only)
    public decimal WarehouseQuantitySnapshot { get; set; }

    // True when ProductType == Equipment (5) — drives different stock logic
    public bool IsEquipment { get; set; }
}
