using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.Import;

#region Excel Row DTO

/// <summary>
/// Row data từ Excel import cho TestResultValue (Kết quả thử nghiệm)
/// Mapping columns: Tên kết quả, Mô tả, Nền mẫu, Chỉ tiêu
/// </summary>
public class TestResultValueImportRow
{
    /// <summary>
    /// Tên kết quả - dùng để group và match (upsert)
    /// </summary>
    [Required(ErrorMessage = "Tên kết quả là bắt buộc")]
    [MaxLength(256)]
    public string ResultName { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả (optional)
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Tên nền mẫu (SpecimenType) - optional, nếu có phải tồn tại trong hệ thống
    /// </summary>
    [MaxLength(256)]
    public string? SpecimenTypeName { get; set; }

    /// <summary>
    /// Tên chỉ tiêu - optional, nếu có phải tồn tại trong hệ thống và thuộc về nền mẫu
    /// </summary>
    [MaxLength(256)]
    public string? IndicatorName { get; set; }
}

#endregion

#region Preview DTOs

/// <summary>
/// Item chỉ tiêu trong preview import TestResultValue
/// </summary>
public class TestResultValueIndicatorImportItem
{
    /// <summary>
    /// Số dòng trong Excel (1-based, header = 1)
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>
    /// Tên nền mẫu
    /// </summary>
    public string? SpecimenTypeName { get; set; }

    /// <summary>
    /// ID nền mẫu nếu tìm thấy trong hệ thống
    /// </summary>
    public Guid? SpecimenTypeId { get; set; }

    /// <summary>
    /// Tên chỉ tiêu
    /// </summary>
    public string? IndicatorName { get; set; }

    /// <summary>
    /// ID chỉ tiêu nếu tìm thấy trong hệ thống
    /// </summary>
    public Guid? IndicatorId { get; set; }

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
/// Preview result grouped by TestResultValue
/// </summary>
public class TestResultValueImportPreviewGroup
{
    /// <summary>
    /// Tên kết quả
    /// </summary>
    public string ResultName { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// True = Tạo mới, False = Cập nhật
    /// </summary>
    public bool IsNew { get; set; }

    /// <summary>
    /// ID kết quả nếu đã tồn tại (IsNew = false)
    /// </summary>
    public Guid? ExistingId { get; set; }

    /// <summary>
    /// Danh sách chỉ tiêu liên kết
    /// </summary>
    public List<TestResultValueIndicatorImportItem> Indicators { get; set; } = new();
}

/// <summary>
/// Kết quả preview import TestResultValue
/// </summary>
public class TestResultValueImportPreviewDto
{
    /// <summary>
    /// Kết quả validation
    /// </summary>
    public ImportValidationResult ValidationResult { get; set; } = new();

    /// <summary>
    /// Danh sách kết quả được nhóm
    /// </summary>
    public List<TestResultValueImportPreviewGroup> Groups { get; set; } = new();

    /// <summary>
    /// Tổng số kết quả
    /// </summary>
    public int TotalResults { get; set; }

    /// <summary>
    /// Số kết quả mới
    /// </summary>
    public int NewResults { get; set; }

    /// <summary>
    /// Số kết quả cập nhật
    /// </summary>
    public int UpdateResults { get; set; }

    /// <summary>
    /// Tổng số liên kết chỉ tiêu
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
