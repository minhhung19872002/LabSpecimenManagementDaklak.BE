using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.Import;

public class ManufacturerImportRow
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class ManufacturerImportPreviewItem
{
    public int RowNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsNew { get; set; }
    public Guid? ExistingId { get; set; }
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class ManufacturerImportPreviewDto
{
    public ImportValidationResult ValidationResult { get; set; } = new();
    public List<ManufacturerImportPreviewItem> Items { get; set; } = new();
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }
    public int NewManufacturers { get; set; }
    public int UpdateManufacturers { get; set; }
    public string? ErrorFileBase64 { get; set; }
    public string? ErrorFileName { get; set; }
}
