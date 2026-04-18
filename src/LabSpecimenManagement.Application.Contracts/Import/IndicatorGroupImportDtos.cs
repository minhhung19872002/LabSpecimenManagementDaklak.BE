using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.Import;

#region Excel Row DTO

/// <summary>
/// Row data từ Excel import cho IndicatorGroup + Indicator
/// Mapping columns: Tên nhóm chỉ tiêu, Khoa phòng thực hiện, Nền mẫu, Chỉ tiêu,
/// Chỉ tiêu phụ, Tên phương pháp, Đơn vị tính, Đơn giá
/// </summary>
public class IndicatorGroupImportRow
{
    /// <summary>
    /// Tên nhóm chỉ tiêu - dùng để match IndicatorGroup (upsert)
    /// </summary>
    public string IndicatorGroupName { get; set; } = string.Empty;

    /// <summary>
    /// Khoa phòng thực hiện - optional, phải tồn tại trong DB nếu có giá trị
    /// </summary>
    public string? DepartmentName { get; set; }

    /// <summary>
    /// Tên chỉ tiêu - dùng để match Indicator (upsert)
    /// </summary>
    public string IndicatorName { get; set; } = string.Empty;

    /// <summary>
    /// Tên chỉ tiêu có định dạng HTML (bold, italic, underline từ Excel)
    /// </summary>
    public string? FormattedIndicatorName { get; set; }

    /// <summary>
    /// Tên chỉ tiêu phụ (optional) - sẽ được tạo với ParentIndicatorId trỏ đến Indicator cha
    /// </summary>
    public string? SubIndicatorName { get; set; }

    /// <summary>
    /// Tên chỉ tiêu phụ có định dạng HTML (bold, italic, underline từ Excel)
    /// </summary>
    public string? FormattedSubIndicatorName { get; set; }

    /// <summary>
    /// Nền mẫu (bắt buộc)
    /// </summary>
    public string SpecimenTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Tên phương pháp (bắt buộc)
    /// </summary>
    public string TestMethodName { get; set; } = string.Empty;

    /// <summary>
    /// Đơn vị tính (không bắt buộc)
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// Giá trị text thô của Đơn giá để validate
    /// </summary>
    public string? UnitPriceText { get; set; }
}

#endregion

#region Preview DTOs

/// <summary>
/// Item preview cho mỗi row import
/// </summary>
public class IndicatorGroupImportPreviewItem
{
    /// <summary>
    /// Số dòng trong Excel (1-based, header = 1)
    /// </summary>
    public int RowNumber { get; set; }

    #region IndicatorGroup info

    /// <summary>
    /// Tên nhóm chỉ tiêu
    /// </summary>
    public string IndicatorGroupName { get; set; } = string.Empty;

    /// <summary>
    /// True = Tạo mới IndicatorGroup, False = Update IndicatorGroup
    /// </summary>
    public bool IsNewIndicatorGroup { get; set; }

    /// <summary>
    /// ID IndicatorGroup nếu đã tồn tại (IsNewIndicatorGroup = false)
    /// </summary>
    public Guid? ExistingIndicatorGroupId { get; set; }

    #endregion

    #region Department info

    /// <summary>
    /// Tên khoa phòng (từ Excel)
    /// </summary>
    public string? DepartmentName { get; set; }

    /// <summary>
    /// ID khoa phòng (nếu tìm thấy trong DB)
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// True nếu khoa phòng được nhập nhưng không tìm thấy trong DB
    /// </summary>
    public bool IsDepartmentInvalid { get; set; }

    #endregion

    #region Indicator info

    /// <summary>
    /// Tên chỉ tiêu
    /// </summary>
    public string IndicatorName { get; set; } = string.Empty;

    /// <summary>
    /// Tên chỉ tiêu có định dạng HTML (bold, italic, underline từ Excel)
    /// </summary>
    public string? FormattedIndicatorName { get; set; }

    /// <summary>
    /// True = Tạo mới Indicator, False = Update Indicator
    /// </summary>
    public bool IsNewIndicator { get; set; }

    /// <summary>
    /// ID Indicator nếu đã tồn tại (IsNewIndicator = false)
    /// </summary>
    public Guid? ExistingIndicatorId { get; set; }

    #endregion

    #region Sub-Indicator info

    /// <summary>
    /// Tên chỉ tiêu phụ (từ Excel)
    /// </summary>
    public string? SubIndicatorName { get; set; }

    /// <summary>
    /// Tên chỉ tiêu phụ có định dạng HTML (bold, italic, underline từ Excel)
    /// </summary>
    public string? FormattedSubIndicatorName { get; set; }

    /// <summary>
    /// True = Tạo mới Sub-Indicator, False = Update Sub-Indicator
    /// </summary>
    public bool IsNewSubIndicator { get; set; }

    /// <summary>
    /// ID Sub-Indicator nếu đã tồn tại (IsNewSubIndicator = false)
    /// </summary>
    public Guid? ExistingSubIndicatorId { get; set; }

    /// <summary>
    /// True nếu có chỉ tiêu phụ
    /// </summary>
    public bool HasSubIndicator { get; set; }

    #endregion

    #region Specimen/TestMethod info

    /// <summary>
    /// Tên nền mẫu (từ Excel)
    /// </summary>
    public string SpecimenTypeName { get; set; } = string.Empty;

    /// <summary>
    /// ID nền mẫu (nếu tìm thấy trong DB)
    /// </summary>
    public Guid? SpecimenTypeId { get; set; }

    /// <summary>
    /// True nếu nền mẫu không tồn tại trong DB
    /// </summary>
    public bool IsSpecimenTypeInvalid { get; set; }

    /// <summary>
    /// Tên phương pháp (từ Excel)
    /// </summary>
    public string TestMethodName { get; set; } = string.Empty;

    /// <summary>
    /// ID phương pháp (nếu tìm thấy trong DB)
    /// </summary>
    public Guid? TestMethodId { get; set; }

    /// <summary>
    /// True nếu phương pháp không tồn tại trong DB
    /// </summary>
    public bool IsTestMethodInvalid { get; set; }

    /// <summary>
    /// Đơn vị tính theo dòng import (optional)
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// Đơn giá theo dòng import (default = 0)
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// True = tạo mới liên kết phương pháp + chỉ tiêu + nền mẫu
    /// False = cập nhật liên kết đã có
    /// </summary>
    public bool IsNewMethodLink { get; set; }

    #endregion

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
/// Kết quả preview import IndicatorGroup + Indicator
/// </summary>
public class IndicatorGroupImportPreviewDto
{
    /// <summary>
    /// Kết quả validation tổng quan
    /// </summary>
    public ImportValidationResult ValidationResult { get; set; } = new();

    /// <summary>
    /// Danh sách items preview
    /// </summary>
    public List<IndicatorGroupImportPreviewItem> Items { get; set; } = new();

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
    /// Số IndicatorGroup mới sẽ được tạo
    /// </summary>
    public int NewIndicatorGroups { get; set; }

    /// <summary>
    /// Số IndicatorGroup sẽ được cập nhật
    /// </summary>
    public int UpdateIndicatorGroups { get; set; }

    /// <summary>
    /// Số Indicator mới sẽ được tạo
    /// </summary>
    public int NewIndicators { get; set; }

    /// <summary>
    /// Số Indicator sẽ được cập nhật
    /// </summary>
    public int UpdateIndicators { get; set; }

    /// <summary>
    /// Số Sub-Indicator mới sẽ được tạo
    /// </summary>
    public int NewSubIndicators { get; set; }

    /// <summary>
    /// Số Sub-Indicator sẽ được cập nhật
    /// </summary>
    public int UpdateSubIndicators { get; set; }

    /// <summary>
    /// Số liên kết TestMethodIndicator mới sẽ được tạo
    /// </summary>
    public int NewMethodLinks { get; set; }

    /// <summary>
    /// Số liên kết TestMethodIndicator sẽ được cập nhật
    /// </summary>
    public int UpdateMethodLinks { get; set; }

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
