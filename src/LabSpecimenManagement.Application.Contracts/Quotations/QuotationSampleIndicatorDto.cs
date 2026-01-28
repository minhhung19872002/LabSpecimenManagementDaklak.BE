using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Quotations;

/// <summary>
/// DTO for displaying quotation sample indicator
/// </summary>
public class QuotationSampleIndicatorDto : EntityDto<Guid>
{
    /// <summary>
    /// QuotationSample ID
    /// </summary>
    public Guid QuotationSampleId { get; set; }

    /// <summary>
    /// Indicator ID
    /// </summary>
    public Guid IndicatorId { get; set; }

    /// <summary>
    /// Test Method ID
    /// </summary>
    public Guid TestMethodId { get; set; }

    /// <summary>
    /// Tên chỉ tiêu
    /// </summary>
    public string IndicatorName { get; set; } = string.Empty;

    /// <summary>
    /// Tên chỉ tiêu đã format (có sub/sup script)
    /// </summary>
    public string? IndicatorFormattedName { get; set; }

    /// <summary>
    /// Tên phương pháp
    /// </summary>
    public string TestMethodName { get; set; } = string.Empty;

    /// <summary>
    /// Tên phương pháp đã format (có sub/sup script)
    /// </summary>
    public string? TestMethodFormattedName { get; set; }

    /// <summary>
    /// Đơn giá
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Thứ tự hiển thị trong mẫu
    /// </summary>
    public int SortOrder { get; set; }
}
