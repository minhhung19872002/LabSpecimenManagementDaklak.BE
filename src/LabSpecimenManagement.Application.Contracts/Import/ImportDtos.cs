using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.Import;

#region Excel Row DTOs - Mapping from Excel columns

/// <summary>
/// Row data from TestRequest Excel import
/// </summary>
public class TestRequestImportRow
{
    [Required(ErrorMessage = "Tên khách hàng là bắt buộc")]
    [MaxLength(256)]
    public string CustomerName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    [MaxLength(256)]
    public string? CustomerEmail { get; set; }

    [MaxLength(50)]
    public string? CustomerPhone { get; set; }

    [MaxLength(500)]
    public string? CustomerAddress { get; set; }

    [Range(1, 365, ErrorMessage = "TAT phải từ 1-365 ngày")]
    public int? TatDays { get; set; }

    [MaxLength(256)]
    public string? Subcontractor { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

/// <summary>
/// Row data from Specimen Excel import
/// </summary>
public class SpecimenImportRow
{
    [Required(ErrorMessage = "Mã yêu cầu là bắt buộc")]
    public string TestRequestNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên mẫu là bắt buộc")]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Loại mẫu là bắt buộc")]
    [MaxLength(256)]
    public string Type { get; set; } = string.Empty;

    public DateTime? ReceivedDate { get; set; }

    [MaxLength(256)]
    public string? StorageLocation { get; set; }

    [Range(0.001, double.MaxValue, ErrorMessage = "Số lượng phải > 0")]
    public decimal Quantity { get; set; } = 1;

    [Required(ErrorMessage = "Đơn vị là bắt buộc")]
    [MaxLength(50)]
    public string Unit { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Deviation { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }
}

/// <summary>
/// Row data from Customer Excel import
/// </summary>
public class CustomerImportRow
{
    [Required(ErrorMessage = "Tên khách hàng là bắt buộc")]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Email không đúng định dạng")]
    [MaxLength(256)]
    public string? Email { get; set; }

    [MaxLength(50)]
    public string? Phone { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(20)]
    public string? TaxCode { get; set; }

    [MaxLength(100)]
    public string? ContactPerson { get; set; }

    [MaxLength(50)]
    public string? ContactPhone { get; set; }
}

/// <summary>
/// Row data from TestMethod Excel import
/// </summary>
public class TestMethodImportRow
{
    [Required(ErrorMessage = "Mã phương pháp là bắt buộc")]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên phương pháp là bắt buộc")]
    [MaxLength(256)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? Unit { get; set; }
}

#endregion

#region Validation Result DTOs

/// <summary>
/// Single row validation error
/// </summary>
public class ImportRowError
{
    public int RowNumber { get; set; }
    public string ColumnName { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string? Value { get; set; }
}

/// <summary>
/// Import validation result
/// </summary>
public class ImportValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int InvalidRows => TotalRows - ValidRows;
    public List<ImportRowError> Errors { get; set; } = new();
}

/// <summary>
/// Import result after processing
/// </summary>
public class ImportResultDto
{
    public bool Success { get; set; }
    public int TotalRows { get; set; }
    public int ImportedRows { get; set; }
    public int SkippedRows { get; set; }
    public int FailedRows { get; set; }
    public List<ImportRowError> Errors { get; set; } = new();
    public string? Message { get; set; }

    /// <summary>
    /// File lỗi dạng base64 (chỉ có khi có lỗi)
    /// </summary>
    public string? ErrorFileBase64 { get; set; }

    /// <summary>
    /// Tên file lỗi
    /// </summary>
    public string? ErrorFileName { get; set; }
}

/// <summary>
/// Validation-only result (preview before import)
/// </summary>
public class ImportPreviewDto<T> where T : class
{
    public ImportValidationResult ValidationResult { get; set; } = new();
    public List<T> ValidData { get; set; } = new();
}

/// <summary>
/// FE-resolved location code override for import
/// Used when FE resolves Province/Ward codes via new-vn-provinces library
/// </summary>
public class LocationCodeOverride
{
    public int RowNumber { get; set; }
    public string? ProvinceCode { get; set; }
    public string? WardCode { get; set; }
}

#endregion
