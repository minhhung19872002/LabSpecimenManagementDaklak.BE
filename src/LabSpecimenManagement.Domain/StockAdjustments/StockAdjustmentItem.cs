using System;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.StockAdjustments;

public class StockAdjustmentItem : Entity<Guid>
{
    public Guid StockAdjustmentId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public Guid UnitId { get; set; }
    public string UnitName { get; set; }
    public decimal QuantityBefore { get; set; }
    public decimal ActualQuantity { get; set; }
    public decimal AdjustmentQuantity { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public Guid? ActualUnitId { get; set; }
    public string? ActualUnitName { get; set; }
    public string? Reason { get; set; }
    public decimal? ExpectedStock { get; set; }
    public decimal? ConsumableQuantityBefore { get; set; }
    public string? LotNumber { get; set; }
    public string? ManufacturerName { get; set; }

    protected StockAdjustmentItem()
    {
    }

    public StockAdjustmentItem(
        Guid id,
        Guid stockAdjustmentId,
        Guid productId,
        string productName,
        Guid unitId,
        string unitName,
        decimal quantityBefore,
        decimal actualQuantity) : base(id)
    {
        StockAdjustmentId = stockAdjustmentId;
        ProductId = productId;
        ProductName = productName;
        UnitId = unitId;
        UnitName = unitName;
        QuantityBefore = quantityBefore;
        ActualQuantity = actualQuantity;
        AdjustmentQuantity = quantityBefore - actualQuantity;
    }
}
