using System;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.WarehouseImports;

public class WarehouseImportReceiptItem : Entity<Guid>
{
    public Guid WarehouseImportReceiptId { get; set; }
    public Guid? GoodsReceiptItemId { get; set; }
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
    public string? BarcodeValues { get; set; }
    public decimal? StockBeforeBaseQuantity { get; set; }
    public decimal? StockAfterBaseQuantity { get; set; }
    public string? StockBeforeByUnitsJson { get; set; }
    public string? StockAfterByUnitsJson { get; set; }

    // Pricing fields (Nhóm 1 — Vật tư)
    public decimal? UnitPrice { get; set; }
    public int? VatPercent { get; set; }
    public decimal? TotalLineAmount { get; set; }

    // Equipment fields (Nhóm 2)
    public bool IsEquipment { get; set; }
    public string? AssetCode { get; set; }
    public string? SerialNumber { get; set; }
    public Guid? ManufacturerId { get; set; }
    public string? ManufacturerName { get; set; }
    public EquipmentReceiveStatus EquipmentReceiveStatus { get; set; }
    public int? DepreciationPeriod { get; set; }
    public decimal? OriginalPrice { get; set; }
    public string? Location { get; set; }
    public int EquipmentIndex { get; set; }

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

    public WarehouseImportReceiptItem(
        Guid id,
        Guid warehouseImportReceiptId,
        Guid productId,
        string productName) : base(id)
    {
        WarehouseImportReceiptId = warehouseImportReceiptId;
        ProductId = productId;
        ProductName = productName;
        Status = WarehouseImportLineStatus.Checked;
    }
}
