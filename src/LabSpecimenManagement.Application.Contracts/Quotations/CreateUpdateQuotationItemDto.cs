using System;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.Quotations;

/// <summary>
/// DTO tạo/cập nhật item báo giá
/// </summary>
public class CreateUpdateQuotationItemDto
{
    public Guid? Id { get; set; }

    [Required]
    public Guid IndicatorId { get; set; }

    [Required]
    public Guid TestMethodId { get; set; }

    [Required]
    [StringLength(256)]
    public string IndicatorName { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string TestMethodName { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int SampleCount { get; set; } = 1;

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    public int SortOrder { get; set; }
}
