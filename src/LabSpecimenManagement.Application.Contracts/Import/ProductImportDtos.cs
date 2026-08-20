using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.Import;

public class ProductImportRow
{
    public string? WarehouseId { get; set; }             // A(1)
    public string? WarehouseName { get; set; }           // B(2)
    public string? ProductTypeName { get; set; }         // C(3)
    public string? IdentificationCode { get; set; }      // D(4)
    public string? ProductName { get; set; }             // E(5)
    public decimal? Quantity { get; set; }               // F(6): Số lượng đầu kỳ
    public string? StorageUnitName { get; set; }         // G(7): ĐVT lưu kho
    public decimal? ConsumedStorageQty { get; set; }     // H(8): Sô lượng tiêu hao
    public DateTime? ExpiryDate { get; set; }            // I(9): Hạn sử dụng
    public DateTime? ReceivedDate { get; set; }          // J(10): Ngày nhận
    public decimal? EndingStorageQty { get; set; }       // K(11): Tồn kho cuối kỳ
    public string? LotNumber { get; set; }               // L(12): Code/ Lot
    public string? ManufacturerName { get; set; }        // M(13): Nhà sản xuất
    public string? Notes { get; set; }                   // N(14): Ghi chú
}

public enum ProductImportItemStatus
{
    New = 0,
    Update = 1,
    Skipped = 2,
}

public class ProductImportPreviewItem
{
    public int RowNumber { get; set; }
    public string IdentificationCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string? ProductTypeName { get; set; }
    public string? LotNumber { get; set; }
    public decimal Quantity { get; set; }
    public string? StorageUnitName { get; set; }
    public decimal? ConsumedStorageQty { get; set; }
    public decimal? EndingStorageQty { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public string? ManufacturerName { get; set; }
    public string? Notes { get; set; }
    public ProductImportItemStatus Status { get; set; }
    public Guid? ExistingProductId { get; set; }
    public bool IsValid { get; set; }
    public string? SkipReason { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class ProductImportPreviewDto
{
    public ImportValidationResult ValidationResult { get; set; } = new();
    public List<ProductImportPreviewItem> Items { get; set; } = new();
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }
    public int SkippedRows { get; set; }
    public int NewItems { get; set; }
    public int UpdateItems { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public string? ErrorFileBase64 { get; set; }
    public string? ErrorFileName { get; set; }
}
