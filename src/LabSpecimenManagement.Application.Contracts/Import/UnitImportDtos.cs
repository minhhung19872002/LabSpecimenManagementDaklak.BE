using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.Import;

public class UnitImportRow
{
    public string CategoryValue { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class UnitImportPreviewItem
{
    public int RowNumber { get; set; }
    public int? Category { get; set; }
    public string CategoryLabel { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsNew { get; set; }
    public Guid? ExistingId { get; set; }
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class UnitImportPreviewDto
{
    public ImportValidationResult ValidationResult { get; set; } = new();
    public List<UnitImportPreviewItem> Items { get; set; } = new();
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows { get; set; }
    public int NewUnits { get; set; }
    public int UpdateUnits { get; set; }
    public string? ErrorFileBase64 { get; set; }
    public string? ErrorFileName { get; set; }
}
