using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.Products;

public class InventoryBarcode : CreationAuditedEntity<Guid>
{
    public string Code { get; set; }
    public Guid ProductId { get; set; }
    public Guid? SkuId { get; set; }
    public Guid OriginWarehouseImportReceiptItemId { get; set; }
    public Guid CurrentWarehouseId { get; set; }
    public Guid? LastMaterialReceiptId { get; set; }
    public InventoryBarcodeStatus Status { get; set; } = InventoryBarcodeStatus.NotUsed;

    protected InventoryBarcode()
    {
    }

    public InventoryBarcode(
        Guid id,
        string code,
        Guid productId,
        Guid originWarehouseImportReceiptItemId,
        Guid currentWarehouseId) : base(id)
    {
        Code = code;
        ProductId = productId;
        OriginWarehouseImportReceiptItemId = originWarehouseImportReceiptItemId;
        CurrentWarehouseId = currentWarehouseId;
    }
}
