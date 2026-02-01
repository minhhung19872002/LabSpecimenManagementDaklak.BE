using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.Import;

#region Excel Row DTO

/// <summary>
/// Row data từ Excel import cho SpecimenType
/// Mapping columns: Tên Danh mục loại, Nền mẫu, Tên chỉ tiêu
/// </summary>
public class SpecimenTypeImportRow
{
    /// <summary>
    /// Tên danh mục loại mẫu - để lookup SampleTypeCategoryId
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// Nền mẫu (Tên loại mẫu) - SpecimenType Name
    /// </summary>
    public string SpecimenTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Tên chỉ tiêu - để lookup IndicatorId
    /// </summary>
    public string IndicatorName { get; set; } = string.Empty;
}

#endregion

#region Preview DTOs

/// <summary>
/// Item chỉ tiêu trong preview
/// </summary>
public class SpecimenTypeIndicatorImportItem
{
    /// <summary>
    /// Số dòng trong Excel
    /// </summary>
    public int RowNumber { get; set; }

    /// <summary>
    /// Tên chỉ tiêu
    /// </summary>
    public string IndicatorName { get; set; } = string.Empty;

    /// <summary>
    /// ID chỉ tiêu (nếu tìm thấy)
    /// </summary>
    public Guid? IndicatorId { get; set; }

    /// <summary>
    /// Hợp lệ hay không
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Thông báo lỗi
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Group preview cho mỗi SpecimenType (gộp nhiều dòng có cùng Category + SpecimenType)
/// </summary>
public class SpecimenTypeImportPreviewGroup
{
    /// <summary>
    /// Tên danh mục loại mẫu
    /// </summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>
    /// ID danh mục loại mẫu (nếu tìm thấy)
    /// </summary>
    public Guid? CategoryId { get; set; }

    /// <summary>
    /// Nền mẫu (Tên loại mẫu)
    /// </summary>
    public string SpecimenTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Mã loại mẫu sẽ được tự động sinh
    /// </summary>
    public string? GeneratedCode { get; set; }

    /// <summary>
    /// True = Tạo mới, False = Update
    /// </summary>
    public bool IsNew { get; set; }

    /// <summary>
    /// ID nếu đã tồn tại (IsNew = false)
    /// </summary>
    public Guid? ExistingId { get; set; }

    /// <summary>
    /// Danh sách chỉ tiêu
    /// </summary>
    public List<SpecimenTypeIndicatorImportItem> Indicators { get; set; } = new();

    /// <summary>
    /// Hợp lệ hay không (tất cả indicators đều valid và category tồn tại)
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Danh sách lỗi của group
    /// </summary>
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Kết quả preview import SpecimenType
/// </summary>
public class SpecimenTypeImportPreviewDto
{
    /// <summary>
    /// Kết quả validation tổng quan
    /// </summary>
    public ImportValidationResult ValidationResult { get; set; } = new();

    /// <summary>
    /// Danh sách groups preview (mỗi group = 1 SpecimenType)
    /// </summary>
    public List<SpecimenTypeImportPreviewGroup> Groups { get; set; } = new();

    #region Summary

    /// <summary>
    /// Tổng số loại mẫu
    /// </summary>
    public int TotalSpecimenTypes { get; set; }

    /// <summary>
    /// Số loại mẫu mới sẽ được tạo
    /// </summary>
    public int NewSpecimenTypes { get; set; }

    /// <summary>
    /// Số loại mẫu sẽ được cập nhật
    /// </summary>
    public int UpdateSpecimenTypes { get; set; }

    /// <summary>
    /// Tổng số chỉ tiêu
    /// </summary>
    public int TotalIndicators { get; set; }

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
