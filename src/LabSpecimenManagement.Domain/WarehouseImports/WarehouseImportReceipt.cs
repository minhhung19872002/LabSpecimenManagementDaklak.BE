using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.WarehouseImports;

public class WarehouseImportReceipt : FullAuditedAggregateRoot<Guid>
{
    public string Code { get; set; }
    public Guid GoodsReceiptId { get; set; }
    public string GoodsReceiptCode { get; set; }
    public DateTime ActualImportDate { get; set; }
    public Guid? WarehouseKeeperId { get; set; }
    public string? WarehouseKeeperName { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public string? DeliveryDocumentFileId { get; set; }
    public string? DeliveryDocumentFileName { get; set; }
    public string? DeliveryDocumentFilePath { get; set; }
    public long? DeliveryDocumentFileSize { get; set; }
    // JSON array of DeliveryDocumentFileDto (max 5 files)
    public string? DeliveryDocumentFiles { get; set; }
    public string? Notes { get; set; }
    public WarehouseImportReceiptStatus Status { get; set; }

    public ICollection<WarehouseImportReceiptItem> Items { get; set; }

    protected WarehouseImportReceipt()
    {
        Items = new List<WarehouseImportReceiptItem>();
    }

    public WarehouseImportReceipt(
        Guid id,
        string code,
        Guid goodsReceiptId,
        string goodsReceiptCode) : base(id)
    {
        Code = code;
        GoodsReceiptId = goodsReceiptId;
        GoodsReceiptCode = goodsReceiptCode;
        Status = WarehouseImportReceiptStatus.InProgress;
        Items = new List<WarehouseImportReceiptItem>();
    }
}
