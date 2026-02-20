using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.Import;

#region Excel Row DTO

/// <summary>
/// Row data từ Excel import cho Note (Ghi chú)
/// Mapping columns: Ghi chú
/// </summary>
public class NoteImportRow
{
    /// <summary>
    /// Nội dung ghi chú - dùng để match (upsert)
    /// </summary>
    public string Content { get; set; } = string.Empty;
}

#endregion

#region Preview DTOs

/// <summary>
/// Item preview cho mỗi row import
/// </summary>
public class NoteImportPreviewItem
{
    /// <summary>
    /// Số dòng trong Excel (1-based, header = 1)
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>
    /// Nội dung ghi chú
    /// </summary>
    public string Content { get; set; } = string.Empty;

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
/// Kết quả preview import Note
/// </summary>
public class NoteImportPreviewDto
{
    /// <summary>
    /// Kết quả validation tổng quan
    /// </summary>
    public ImportValidationResult ValidationResult { get; set; } = new();

    /// <summary>
    /// Danh sách items preview
    /// </summary>
    public List<NoteImportPreviewItem> Items { get; set; } = new();

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
    /// Số ghi chú mới sẽ được tạo
    /// </summary>
    public int NewNotes { get; set; }

    /// <summary>
    /// Số ghi chú sẽ được cập nhật
    /// </summary>
    public int UpdateNotes { get; set; }

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
