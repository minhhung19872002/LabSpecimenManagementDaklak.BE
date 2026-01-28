using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Quotations;

/// <summary>
/// DTO for displaying quotation sample
/// </summary>
public class QuotationSampleDto : EntityDto<Guid>
{
    /// <summary>
    /// Quotation ID
    /// </summary>
    public Guid QuotationId { get; set; }

    /// <summary>
    /// Thứ tự hiển thị
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Tên mẫu (ví dụ: "Mẫu 1", "Mẫu 2"...)
    /// </summary>
    public string SampleName { get; set; } = string.Empty;

    /// <summary>
    /// Tổng tiền của mẫu này
    /// </summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// Danh sách chỉ tiêu trong mẫu
    /// </summary>
    public List<QuotationSampleIndicatorDto> Indicators { get; set; } = new();
}
