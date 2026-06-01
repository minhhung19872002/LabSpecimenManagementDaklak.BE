using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.Quotations;

/// <summary>
/// DTO for creating/updating quotation sample
/// </summary>
public class CreateUpdateQuotationSampleDto
{
    /// <summary>
    /// Sample ID (for update only)
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// Thứ tự hiển thị
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Tên mẫu (ví dụ: "Mẫu 1", "Mẫu 2"...)
    /// </summary>
    [Required]
    [StringLength(256)]
    public string SampleName { get; set; } = string.Empty;

    /// <summary>
    /// Số lượng mẫu
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Danh sách chỉ tiêu trong mẫu
    /// </summary>
    public List<CreateUpdateQuotationSampleIndicatorDto> Indicators { get; set; } = new();
}
