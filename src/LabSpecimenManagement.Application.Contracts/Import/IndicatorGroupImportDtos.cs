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
    /// Nền mẫu (không bắt buộc, nhưng nếu có thì phải đi kèm Tên phương pháp)
    /// </summary>
    public string SpecimenTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Tên phương pháp (không bắt buộc, nhưng nếu có thì phải đi kèm Nền mẫu)
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

    /// <summary>
    /// Hợp đồng phụ (Y/N từ Excel → true/false)
    /// </summary>
    public bool IsSubcontract { get; set; }

    #region Admin Import (ID-based)

    /// <summary>
    /// ID nhóm chỉ tiêu từ Excel (admin template) — dùng để match by ID thay vì tên
    /// </summary>
    public Guid? IndicatorGroupId { get; set; }

    /// <summary>
    /// ID chỉ tiêu (cha) từ Excel (admin template) — dùng để match by ID thay vì tên
    /// </summary>
    public Guid? IndicatorId { get; set; }

    /// <summary>
    /// ID chỉ tiêu phụ từ Excel (admin template) — dùng để match by ID thay vì tên
    /// </summary>
    public Guid? SubIndicatorId { get; set; }

    #endregion

    /// <summary>
    /// Đơn vị tính có định dạng HTML (bold, italic, underline từ Excel rich text)
    /// </summary>
    public string? FormattedUnit { get; set; }
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
    /// Đơn vị tính có định dạng HTML (bold, italic, underline từ Excel rich text)
    /// </summary>
    public string? FormattedUnit { get; set; }

    /// <summary>
    /// Đơn giá theo dòng import (default = 0)
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// True = tạo mới liên kết phương pháp + chỉ tiêu + nền mẫu
    /// False = cập nhật liên kết đã có
    /// </summary>
    public bool IsNewMethodLink { get; set; }

    /// <summary>
    /// Hợp đồng phụ (Y → true, N/trống → false)
    /// </summary>
    public bool IsSubcontract { get; set; }

    #endregion

    #region Change tracking (admin import)

    /// <summary>
    /// True nếu nhóm chỉ tiêu có thay đổi so với DB (default true cho backward compat)
    /// </summary>
    public bool IsGroupChanged { get; set; } = true;

    /// <summary>
    /// True nếu chỉ tiêu có thay đổi so với DB (default true cho backward compat)
    /// </summary>
    public bool IsIndicatorChanged { get; set; } = true;

    /// <summary>
    /// True nếu chỉ tiêu phụ có thay đổi so với DB (default true cho backward compat)
    /// </summary>
    public bool IsSubIndicatorChanged { get; set; } = true;

    /// <summary>
    /// True nếu liên kết phương pháp có thay đổi so với DB (default true cho backward compat)
    /// </summary>
    public bool IsMethodLinkChanged { get; set; } = true;

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

#region Admin Indicator Name Import (simplified — chỉ cập nhật tên chỉ tiêu)

public class AdminIndicatorNamePreviewItem
{
    public int RowNumber { get; set; }
    public Guid IndicatorId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string CurrentFormattedName { get; set; } = string.Empty;
    public string NewFormattedName { get; set; } = string.Empty;
    public bool IsChanged { get; set; }
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class AdminIndicatorNamePreviewDto
{
    public int TotalRows { get; set; }
    public int ChangedRows { get; set; }
    public int UnchangedRows { get; set; }
    public int ErrorRows { get; set; }
    public List<AdminIndicatorNamePreviewItem> Items { get; set; } = new();
    public string? ErrorFileBase64 { get; set; }
    public string? ErrorFileName { get; set; }
}

#endregion

#region Admin Consumption Import (tiêu hao theo Nền mẫu + Chỉ tiêu + Phương pháp)

/// <summary>
/// 1 dòng trong template "Tiêu hao.xlsx" = 1 vật tư tiêu hao cho 1 combination
/// (Nền mẫu + Chỉ tiêu + Phương pháp) → resolve ra TestMethodIndicator.
/// Cột "Khoa phòng thực hiện" (A) hiện tại bỏ qua, không validate.
/// </summary>
public class AdminConsumptionPreviewItem
{
    public int RowNumber { get; set; }

    // Combination (từ file)
    public string SpecimenTypeName { get; set; } = string.Empty;
    public string IndicatorName { get; set; } = string.Empty;
    public string TestMethodName { get; set; } = string.Empty;

    // Vật tư (từ file)
    public string ProductTypeLabel { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;

    // Số liệu
    public decimal Quantity { get; set; }
    public int EstimatedSamplesPerYear { get; set; }
    /// <summary>Tự tính = Quantity / EstimatedSamplesPerYear</summary>
    public decimal? AvgConsumptionPerSample { get; set; }

    // Resolved (chỉ có khi hợp lệ)
    public Guid? TestMethodIndicatorId { get; set; }
    public Guid? ProductId { get; set; }
    /// <summary>Không có trong template — tự lấy từ vật tư (VolumeUnitId ?? BaseUnitId) giống modal nhập tay</summary>
    public Guid? UnitId { get; set; }

    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

public class AdminConsumptionPreviewDto
{
    public int TotalRows { get; set; }
    public int ValidRows { get; set; }
    public int ErrorRows { get; set; }
    /// <summary>Số dòng combination có cột E–H trống (chưa điền vật tư) → bỏ qua, không ghi đè</summary>
    public int SkippedRows { get; set; }
    /// <summary>Số combination (TestMethodIndicator) sẽ bị ghi đè tiêu hao vật tư khi import</summary>
    public int AffectedIndicators { get; set; }
    public List<AdminConsumptionPreviewItem> Items { get; set; } = new();
    public string? ErrorFileBase64 { get; set; }
    public string? ErrorFileName { get; set; }
}

#endregion
