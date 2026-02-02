using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.Import;

#region Excel Row DTO

/// <summary>
/// Row data từ Excel import cho TestMethod với Indicators
/// Mapping columns: Tên phương pháp, Mô tả, Đơn vị tính, Chỉ tiêu, Đơn giá
/// </summary>
public class TestMethodWithIndicatorImportRow
{
    /// <summary>
    /// Tên phương pháp - dùng để group và match (upsert)
    /// </summary>
    [Required(ErrorMessage = "Tên phương pháp là bắt buộc")]
    [MaxLength(256)]
    public string TestMethodName { get; set; } = string.Empty;

    /// <summary>
    /// Tên phương pháp có định dạng HTML (bold, italic, underline từ Excel)
    /// </summary>
    [MaxLength(2000)]
    public string? FormattedName { get; set; }

    /// <summary>
    /// Mô tả - có thể bỏ trống
    /// </summary>
    [MaxLength(2000)]
    public string? Description { get; set; }

    /// <summary>
    /// Đơn vị tính
    /// </summary>
    [Required(ErrorMessage = "Đơn vị tính là bắt buộc")]
    [MaxLength(50)]
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Tên chỉ tiêu - phải tồn tại trong hệ thống
    /// </summary>
    [Required(ErrorMessage = "Tên chỉ tiêu là bắt buộc")]
    [MaxLength(256)]
    public string IndicatorName { get; set; } = string.Empty;

    /// <summary>
    /// Tên nền mẫu (SpecimenType) - bắt buộc, phải tồn tại trong hệ thống
    /// </summary>
    [Required(ErrorMessage = "Nền mẫu là bắt buộc")]
    [MaxLength(256)]
    public string SpecimenTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Đơn giá cho chỉ tiêu trong phương pháp này
    /// </summary>
    [Required(ErrorMessage = "Đơn giá là bắt buộc")]
    [Range(0, double.MaxValue, ErrorMessage = "Đơn giá phải >= 0")]
    public decimal UnitPrice { get; set; }
}

#endregion

#region Preview DTOs

/// <summary>
/// Item chỉ tiêu trong preview
/// </summary>
public class TestMethodIndicatorImportItem
{
    /// <summary>
    /// Số dòng trong Excel (1-based, header = 1)
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>
    /// Tên chỉ tiêu
    /// </summary>
    public string IndicatorName { get; set; } = string.Empty;

    /// <summary>
    /// ID chỉ tiêu nếu tìm thấy trong hệ thống
    /// </summary>
    public Guid? IndicatorId { get; set; }

    /// <summary>
    /// Tên nền mẫu
    /// </summary>
    public string? SpecimenTypeName { get; set; }

    /// <summary>
    /// ID nền mẫu nếu tìm thấy trong hệ thống
    /// </summary>
    public Guid? SpecimenTypeId { get; set; }

    /// <summary>
    /// Đơn giá
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Hợp lệ hay không
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Thông báo lỗi nếu không hợp lệ
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Preview result grouped by TestMethod
/// </summary>
public class TestMethodImportPreviewGroup
{
    /// <summary>
    /// Tên phương pháp (plain text)
    /// </summary>
    public string TestMethodName { get; set; } = string.Empty;

    /// <summary>
    /// Tên phương pháp có định dạng HTML (bold, italic, underline từ Excel)
    /// </summary>
    public string? FormattedName { get; set; }

    /// <summary>
    /// Mô tả
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Đơn vị tính
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// True = Tạo mới, False = Cập nhật
    /// </summary>
    public bool IsNew { get; set; }

    /// <summary>
    /// ID phương pháp nếu đã tồn tại (IsNew = false)
    /// </summary>
    public Guid? ExistingId { get; set; }

    /// <summary>
    /// Danh sách chỉ tiêu
    /// </summary>
    public List<TestMethodIndicatorImportItem> Indicators { get; set; } = new();
}

/// <summary>
/// Kết quả preview import TestMethod với Indicators
/// </summary>
public class TestMethodImportPreviewDto
{
    /// <summary>
    /// Kết quả validation
    /// </summary>
    public ImportValidationResult ValidationResult { get; set; } = new();

    /// <summary>
    /// Danh sách phương pháp được nhóm
    /// </summary>
    public List<TestMethodImportPreviewGroup> Groups { get; set; } = new();

    /// <summary>
    /// Tổng số phương pháp
    /// </summary>
    public int TotalTestMethods { get; set; }

    /// <summary>
    /// Số phương pháp mới
    /// </summary>
    public int NewTestMethods { get; set; }

    /// <summary>
    /// Số phương pháp cập nhật
    /// </summary>
    public int UpdateTestMethods { get; set; }

    /// <summary>
    /// Tổng số chỉ tiêu
    /// </summary>
    public int TotalIndicators { get; set; }

    /// <summary>
    /// File lỗi dạng base64 (chỉ có khi có lỗi)
    /// </summary>
    public string? ErrorFileBase64 { get; set; }

    /// <summary>
    /// Tên file lỗi
    /// </summary>
    public string? ErrorFileName { get; set; }
}

#endregion
