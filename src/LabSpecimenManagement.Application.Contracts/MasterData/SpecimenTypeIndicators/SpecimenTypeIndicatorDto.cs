using System;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// DTO hiển thị thông tin chỉ tiêu của loại mẫu
/// </summary>
public class SpecimenTypeIndicatorDto
{
    public Guid Id { get; set; }
    public Guid SpecimenTypeId { get; set; }
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
    /// Tên nhóm chỉ tiêu (denormalized từ IndicatorGroup)
    /// </summary>
    public string? IndicatorGroupName { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Thứ tự hiển thị
    /// </summary>
    public int SortOrder { get; set; }
}
