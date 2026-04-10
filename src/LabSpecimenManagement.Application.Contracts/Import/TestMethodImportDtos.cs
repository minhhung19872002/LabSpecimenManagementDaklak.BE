using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.Import;

#region Excel Row DTO

/// <summary>
/// Row data từ Excel import cho TestMethod
/// Mapping columns: Tên phương pháp, Mô tả
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
}

#endregion

#region Preview DTOs

/// <summary>
/// Preview result for TestMethod import
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
    /// True = Tạo mới, False = Cập nhật
    /// </summary>
    public bool IsNew { get; set; }

    /// <summary>
    /// ID phương pháp nếu đã tồn tại (IsNew = false)
    /// </summary>
    public Guid? ExistingId { get; set; }
}

/// <summary>
/// Kết quả preview import TestMethod
/// </summary>
public class TestMethodImportPreviewDto
{
    /// <summary>
    /// Kết quả validation
    /// </summary>
    public ImportValidationResult ValidationResult { get; set; } = new();

    /// <summary>
    /// Danh sách phương pháp
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
    /// File lỗi dạng base64 (chỉ có khi có lỗi)
    /// </summary>
    public string? ErrorFileBase64 { get; set; }

    /// <summary>
    /// Tên file lỗi
    /// </summary>
    public string? ErrorFileName { get; set; }
}

#endregion
