using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.Import;

#region Excel Row DTO

/// <summary>
/// Row data từ Excel import cho SampleTypeCategory
/// Mapping columns: Tên Danh mục loại, Mô tả
/// </summary>
public class SampleTypeCategoryImportRow
{
    /// <summary>
    /// Tên danh mục loại mẫu - dùng để match (upsert)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả
    /// </summary>
    public string? Description { get; set; }
}

#endregion

#region Preview DTOs

/// <summary>
/// Item preview cho mỗi row import
/// </summary>
public class SampleTypeCategoryImportPreviewItem
{
    /// <summary>
    /// Số dòng trong Excel (1-based, header = 1)
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>
    /// Tên danh mục loại mẫu
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// True = Tạo mới, False = Update
    /// </summary>
    public bool IsNew { get; set; }

    /// <summary>
    /// ID nếu đã tồn tại (IsNew = false)
    /// </summary>
    public Guid? ExistingId { get; set; }

    #region Validation

    /// <summary>
    /// Hợp lệ hay không
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Danh sách lỗi
    /// </summary>
    public List<string> Errors { get; set; } = new();

    #endregion
}

/// <summary>
/// Kết quả preview import SampleTypeCategory
/// </summary>
public class SampleTypeCategoryImportPreviewDto
{
    /// <summary>
    /// Kết quả validation tổng quan
    /// </summary>
    public ImportValidationResult ValidationResult { get; set; } = new();

    /// <summary>
    /// Danh sách items preview
    /// </summary>
    public List<SampleTypeCategoryImportPreviewItem> Items { get; set; } = new();

    #region Summary

    /// <summary>
    /// Tổng số dòng
    /// </summary>
    public int TotalRows { get; set; }

    /// <summary>
    /// Số dòng hợp lệ
    /// </summary>
    public int ValidRows { get; set; }

    /// <summary>
    /// Số dòng không hợp lệ
    /// </summary>
    public int InvalidRows { get; set; }

    /// <summary>
    /// Số danh mục mới sẽ được tạo
    /// </summary>
    public int NewCategories { get; set; }

    /// <summary>
    /// Số danh mục sẽ được cập nhật
    /// </summary>
    public int UpdateCategories { get; set; }

    #endregion

    #region Error file

    /// <summary>
    /// File lỗi dạng base64 (chỉ có khi có lỗi)
    /// </summary>
    public string? ErrorFileBase64 { get; set; }

    /// <summary>
    /// Tên file lỗi
    /// </summary>
    public string? ErrorFileName { get; set; }

    #endregion
}

#endregion
