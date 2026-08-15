using System;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.MaterialReceipts;

public class MaterialReceiptItem : Entity<Guid>
{
    public Guid MaterialReceiptId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitName { get; set; }
    public decimal Quantity { get; set; }
    public decimal BaseQuantity { get; set; }
    public Guid? SkuId { get; set; }
    public Guid ReceivingWarehouseId { get; set; }
    public string ReceivingWarehouseName { get; set; }
    public string? LotNumber { get; set; }
    public string? ManufacturerName { get; set; }

    protected MaterialReceiptItem()
    {
    }

    public MaterialReceiptItem(
        Guid id,
        Guid materialReceiptId,
        Guid productId,
        string productName,
        decimal quantity,
        Guid receivingWarehouseId,
        string receivingWarehouseName) : base(id)
    {
        MaterialReceiptId = materialReceiptId;
        ProductId = productId;
        ProductName = productName;
        Quantity = quantity;
        ReceivingWarehouseId = receivingWarehouseId;
        ReceivingWarehouseName = receivingWarehouseName;
    }
}
