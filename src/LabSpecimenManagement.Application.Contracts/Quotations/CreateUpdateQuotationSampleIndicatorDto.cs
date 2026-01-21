using System;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.Quotations;

/// <summary>
/// DTO for creating/updating quotation sample indicator
/// </summary>
public class CreateUpdateQuotationSampleIndicatorDto
{
    /// <summary>
    /// Indicator ID (for update only)
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Indicator ID
    /// </summary>
    [Required]
    public Guid IndicatorId { get; set; }

    /// <summary>
    /// Test Method ID
    /// </summary>
    [Required]
    public Guid TestMethodId { get; set; }

    /// <summary>
    /// Tên chỉ tiêu (snapshot)
    /// </summary>
    [Required]
    [StringLength(256)]
    public string IndicatorName { get; set; } = string.Empty;

    /// <summary>
    /// Tên phương pháp (snapshot)
    /// </summary>
    [Required]
    [StringLength(256)]
    public string TestMethodName { get; set; } = string.Empty;

    /// <summary>
    /// Đơn giá
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Thứ tự hiển thị trong mẫu
    /// </summary>
    public int SortOrder { get; set; }
}
