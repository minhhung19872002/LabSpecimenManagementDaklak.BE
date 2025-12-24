using System;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Bảng trung gian liên kết Tiêu chuẩn và Chỉ tiêu (Many-to-Many)
/// Chỉ tiêu so sánh trong tiêu chuẩn
/// </summary>
public class StandardIndicator : Entity<Guid>
{
    /// <summary>
    /// ID Tiêu chuẩn
    /// </summary>
    public Guid StandardId { get; set; }

    /// <summary>
    /// ID Chỉ tiêu
    /// </summary>
    public Guid IndicatorId { get; set; }

    /// <summary>
    /// ID Phương pháp thử
    /// </summary>
    public Guid TestMethodId { get; set; }

    /// <summary>
    /// Đơn vị (lấy từ TestMethod)
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// Ký hiệu so sánh: <=, >=, <, >, =
    /// </summary>
    public string? ComparisonOperator { get; set; }

    /// <summary>
    /// Giới hạn cho phép
    /// </summary>
    public string? AllowedLimit { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Thứ tự hiển thị trong tiêu chuẩn
    /// </summary>
    public int SortOrder { get; set; }

    protected StandardIndicator()
    {
    }

    public StandardIndicator(
        Guid id,
        Guid standardId,
        Guid indicatorId,
        Guid testMethodId,
        string unit) : base(id)
    {
        StandardId = standardId;
        IndicatorId = indicatorId;
        TestMethodId = testMethodId;
        Unit = unit;
        IsActive = true;
        SortOrder = 0;
    }
}
