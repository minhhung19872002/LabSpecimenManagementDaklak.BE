using System;

namespace LabSpecimenManagement.MasterData;

public class TestMethodIndicatorDto
{
    public Guid Id { get; set; }
    public Guid TestMethodId { get; set; }
    public Guid IndicatorId { get; set; }
    public string IndicatorCode { get; set; }
    public string IndicatorName { get; set; }
    public string? IndicatorFormattedName { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }

    /// <summary>
    /// ID chỉ tiêu cha (null nếu là chỉ tiêu gốc)
    /// </summary>
    public Guid? ParentIndicatorId { get; set; }

    /// <summary>
    /// Nền mẫu (Loại mẫu) - ID
    /// </summary>
    public Guid? SpecimenTypeId { get; set; }

    /// <summary>
    /// Mã nền mẫu
    /// </summary>
    public string? SpecimenTypeCode { get; set; }

    /// <summary>
    /// Tên nền mẫu
    /// </summary>
    public string? SpecimenTypeName { get; set; }

    /// <summary>
    /// Đơn vị tính (riêng cho chỉ tiêu này trong phương pháp, fallback từ TestMethod.Unit)
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// Đơn vị tính có định dạng HTML
    /// </summary>
    public string? FormattedUnit { get; set; }
}
