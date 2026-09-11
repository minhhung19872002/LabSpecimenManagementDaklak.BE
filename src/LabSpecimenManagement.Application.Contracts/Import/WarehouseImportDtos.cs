using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.Import;

public class WarehouseImportRow
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ParentWarehouseCode { get; set; }
    public string? PhysicalLocation { get; set; }
    public string? LaboratoryName { get; set; }
}

public class WarehouseImportPreviewItem
{
    public int RowNumber { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ParentWarehouseCode { get; set; }
    public string? PhysicalLocation { get; set; }
    public string? LaboratoryName { get; set; }
    public bool IsNew { get; set; }
    public Guid? ExistingId { get; set; }
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class WarehouseImportPreviewDto
{
    public ImportValidationResult ValidationResult { get; set; } = new();
    public List<WarehouseImportPreviewItem> Items { get; set; } = new();
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }
    public int NewWarehouses { get; set; }
    public int UpdateWarehouses { get; set; }
    public string? ErrorFileBase64 { get; set; }
    public string? ErrorFileName { get; set; }
}
