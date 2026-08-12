using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.Products;

public class BarcodeLotGroupDto
{
    public Guid WarehouseImportReceiptItemId { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? LotExpiryDate { get; set; }
    public List<BarcodeLotItemDto> Barcodes { get; set; } = new();
}

public class BarcodeLotItemDto
{
    public string Code { get; set; } = string.Empty;
    public InventoryBarcodeStatus Status { get; set; }
}
