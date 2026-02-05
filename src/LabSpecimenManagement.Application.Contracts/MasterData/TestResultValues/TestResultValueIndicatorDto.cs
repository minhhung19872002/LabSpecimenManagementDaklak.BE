using System;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// DTO hiển thị thông tin chỉ tiêu của kết quả thử nghiệm
/// </summary>
public class TestResultValueIndicatorDto
{
    public Guid Id { get; set; }
    public Guid TestResultValueId { get; set; }
    public Guid IndicatorId { get; set; }

    /// <summary>
    /// Mã chỉ tiêu (denormalized từ Indicator)
    /// </summary>
    public string IndicatorCode { get; set; } = string.Empty;

    /// <summary>
    /// Tên chỉ tiêu (denormalized từ Indicator)
    /// </summary>
    public string IndicatorName { get; set; } = string.Empty;

    /// <summary>
    /// Tên chỉ tiêu có định dạng HTML (denormalized từ Indicator)
    /// </summary>
    public string? IndicatorFormattedName { get; set; }

    /// <summary>
    /// ID Nền mẫu
    /// </summary>
    public Guid SpecimenTypeId { get; set; }

    /// <summary>
    /// Mã nền mẫu (denormalized từ SpecimenType)
    /// </summary>
    public string SpecimenTypeCode { get; set; } = string.Empty;

    /// <summary>
    /// Tên nền mẫu (denormalized từ SpecimenType)
    /// </summary>
    public string SpecimenTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Thứ tự hiển thị
    /// </summary>
    public int SortOrder { get; set; }
}
