using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.Import;

#region Excel Row DTO

/// <summary>
/// Row data từ Excel import cho Standard với Indicators
/// Mapping columns: Tên tiêu chuẩn, Chỉ tiêu, Phương pháp, Đơn vị, So sánh, Giới hạn,
/// So sánh giới hạn dưới, Giới hạn dưới, So sánh giới hạn trên, Giới hạn trên
/// </summary>
public class StandardWithIndicatorImportRow
{
    /// <summary>
    /// Tên tiêu chuẩn - dùng để group và match (upsert)
    /// </summary>
    [Required(ErrorMessage = "Tên tiêu chuẩn là bắt buộc")]
    [MaxLength(256)]
    public string StandardName { get; set; } = string.Empty;

    /// <summary>
    /// Tên chỉ tiêu - phải tồn tại trong hệ thống
    /// </summary>
    [Required(ErrorMessage = "Tên chỉ tiêu là bắt buộc")]
    [MaxLength(256)]
    public string IndicatorName { get; set; } = string.Empty;

    /// <summary>
    /// Tên phương pháp - phải tồn tại trong hệ thống
    /// </summary>
    [Required(ErrorMessage = "Tên phương pháp là bắt buộc")]
    [MaxLength(256)]
    public string TestMethodName { get; set; } = string.Empty;

    /// <summary>
    /// Đơn vị tính
    /// </summary>
    [Required(ErrorMessage = "Đơn vị tính là bắt buộc")]
    [MaxLength(100)]
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Ký hiệu so sánh: &lt;, &gt;, &lt;=, &gt;=, =, Khoảng
    /// </summary>
    [Required(ErrorMessage = "Ký hiệu so sánh là bắt buộc")]
    [MaxLength(10)]
    public string ComparisonOperator { get; set; } = string.Empty;

    /// <summary>
    /// Giới hạn cho phép (dùng khi So sánh không phải "Khoảng")
    /// </summary>
    [MaxLength(256)]
    public string? AllowedLimit { get; set; }

    /// <summary>
    /// Ký hiệu so sánh giới hạn dưới: &gt;, &gt;= (dùng khi So sánh = "Khoảng")
    /// </summary>
    [MaxLength(10)]
    public string? LowerOperator { get; set; }

    /// <summary>
    /// Giá trị giới hạn dưới (dùng khi So sánh = "Khoảng")
    /// </summary>
    [MaxLength(256)]
    public string? LowerLimit { get; set; }

    /// <summary>
    /// Ký hiệu so sánh giới hạn trên: &lt;, &lt;= (dùng khi So sánh = "Khoảng")
    /// </summary>
    [MaxLength(10)]
    public string? UpperOperator { get; set; }

    /// <summary>
    /// Giá trị giới hạn trên (dùng khi So sánh = "Khoảng")
    /// </summary>
    [MaxLength(256)]
    public string? UpperLimit { get; set; }
}

#endregion

#region Preview DTOs

/// <summary>
/// Item chỉ tiêu trong preview import Standard
/// </summary>
public class StandardIndicatorImportItem
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
    /// Tên phương pháp
    /// </summary>
    public string TestMethodName { get; set; } = string.Empty;

    /// <summary>
    /// ID phương pháp nếu tìm thấy trong hệ thống
    /// </summary>
    public Guid? TestMethodId { get; set; }

    /// <summary>
    /// Đơn vị tính
    /// </summary>
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// Ký hiệu so sánh
    /// </summary>
    public string ComparisonOperator { get; set; } = string.Empty;

    /// <summary>
    /// Giới hạn cho phép
    /// </summary>
    public string? AllowedLimit { get; set; }

    /// <summary>
    /// Ký hiệu so sánh giới hạn dưới
    /// </summary>
    public string? LowerOperator { get; set; }

    /// <summary>
    /// Giá trị giới hạn dưới
    /// </summary>
    public string? LowerLimit { get; set; }

    /// <summary>
    /// Ký hiệu so sánh giới hạn trên
    /// </summary>
    public string? UpperOperator { get; set; }

    /// <summary>
    /// Giá trị giới hạn trên
    /// </summary>
    public string? UpperLimit { get; set; }

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
/// Preview result grouped by Standard
/// </summary>
public class StandardImportPreviewGroup
{
    /// <summary>
    /// Tên tiêu chuẩn
    /// </summary>
    public string StandardName { get; set; } = string.Empty;

    /// <summary>
    /// True = Tạo mới, False = Cập nhật
    /// </summary>
    public bool IsNew { get; set; }

    /// <summary>
    /// ID tiêu chuẩn nếu đã tồn tại (IsNew = false)
    /// </summary>
    public Guid? ExistingId { get; set; }

    /// <summary>
    /// Danh sách chỉ tiêu
    /// </summary>
    public List<StandardIndicatorImportItem> Indicators { get; set; } = new();
}

/// <summary>
/// Kết quả preview import Standard với Indicators
/// </summary>
public class StandardImportPreviewDto
{
    /// <summary>
    /// Kết quả validation
    /// </summary>
    public ImportValidationResult ValidationResult { get; set; } = new();

    /// <summary>
    /// Danh sách tiêu chuẩn được nhóm
    /// </summary>
    public List<StandardImportPreviewGroup> Groups { get; set; } = new();

    /// <summary>
    /// Tổng số tiêu chuẩn
    /// </summary>
    public int TotalStandards { get; set; }

    /// <summary>
    /// Số tiêu chuẩn mới
    /// </summary>
    public int NewStandards { get; set; }

    /// <summary>
    /// Số tiêu chuẩn cập nhật
    /// </summary>
    public int UpdateStandards { get; set; }

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
