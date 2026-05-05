using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.Products;

public class StockMovementLog : CreationAuditedEntity<Guid>
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = null!;
    public decimal QuantityBefore { get; set; }
    public decimal QuantityAfter { get; set; }
    public decimal ChangeQuantity { get; set; }
    public StockMovementType MovementType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? ReferenceCode { get; set; }
    public string? Notes { get; set; }

    protected StockMovementLog() { }

    public StockMovementLog(
        Guid id,
        Guid productId,
        string productName,
        decimal quantityBefore,
        decimal quantityAfter,
        StockMovementType movementType,
        Guid? referenceId = null,
        string? referenceCode = null,
        string? notes = null)
        : base(id)
    {
        ProductId = productId;
        ProductName = productName;
        QuantityBefore = quantityBefore;
        QuantityAfter = quantityAfter;
        ChangeQuantity = quantityAfter - quantityBefore;
        MovementType = movementType;
        ReferenceId = referenceId;
        ReferenceCode = referenceCode;
        Notes = notes;
    }
}
