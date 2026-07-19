using System;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.WarehouseImports;

public class WarehouseImportReceiptItem : Entity<Guid>
{
    public Guid WarehouseImportReceiptId { get; set; }
    public Guid GoodsReceiptItemId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal OrderedQuantity { get; set; }
    public decimal ActualReceivedQuantity { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitName { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? LotExpiryDate { get; set; }
    public WarehouseImportLineStatus Status { get; set; }
    public string? QualityDocumentFileId { get; set; }
    public string? QualityDocumentFileName { get; set; }
    public string? QualityDocumentFilePath { get; set; }
    public long? QualityDocumentFileSize { get; set; }
    public string? Notes { get; set; }
    public Guid? SkuId { get; set; }
    public Guid? StorageWarehouseId { get; set; }
    public string? StorageWarehouseName { get; set; }
    public decimal ReceivedBaseQuantity { get; set; }
    public decimal ConsumedBaseQuantity { get; set; }

    protected WarehouseImportReceiptItem()
    {
    }

    public WarehouseImportReceiptItem(
        Guid id,
        Guid warehouseImportReceiptId,
        Guid goodsReceiptItemId,
        Guid productId,
        string productName,
        decimal orderedQuantity) : base(id)
    {
        WarehouseImportReceiptId = warehouseImportReceiptId;
        GoodsReceiptItemId = goodsReceiptItemId;
        ProductId = productId;
        ProductName = productName;
        OrderedQuantity = orderedQuantity;
        Status = WarehouseImportLineStatus.Checked;
    }
}
