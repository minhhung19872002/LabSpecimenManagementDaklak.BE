using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.Import;

public enum ProductMasterDataImportItemStatus
{
    New = 0,
    Update = 1,
    Skipped = 2,
}

public class ProductMasterDataImportPreviewItem
{
    public int RowNumber { get; set; }
    public string IdentificationCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public string? ProductTypeName { get; set; }
    public ProductMasterDataImportItemStatus Status { get; set; }
    public Guid? ExistingProductId { get; set; }
    public bool IsValid { get; set; } = true;
    public string? SkipReason { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class ProductMasterDataImportPreviewDto
{
    public ImportValidationResult ValidationResult { get; set; } = new();
    public List<ProductMasterDataImportPreviewItem> Items { get; set; } = new();
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }
    public int SkippedRows { get; set; }
    public int NewItems { get; set; }
    public int UpdateItems { get; set; }
    public string? ErrorFileBase64 { get; set; }
    public string? ErrorFileName { get; set; }
}
