using System;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.MasterData;

public class UpdateTestMethodIndicatorDto
{
    /// <summary>
    /// Đơn giá
    /// </summary>
    [Range(0, double.MaxValue, ErrorMessage = "Đơn giá phải >= 0")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Đơn vị tính riêng cho chỉ tiêu
    /// </summary>
    [StringLength(100)]
    public string? Unit { get; set; }

    /// <summary>
    /// Đơn vị tính có định dạng HTML
    /// </summary>
    [StringLength(500)]
    public string? FormattedUnit { get; set; }

    /// <summary>
    /// Phương pháp thử mới (nếu muốn đổi)
    /// </summary>
    public Guid? NewTestMethodId { get; set; }

    /// <summary>
    /// Nền mẫu mới (nếu muốn đổi). Dùng Guid.Empty để set null.
    /// </summary>
    public Guid? NewSpecimenTypeId { get; set; }
}
