using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Quotations;

/// <summary>
/// DTO hiển thị chi tiết item báo giá
/// </summary>
public class QuotationItemDto : EntityDto<Guid>
{
    public Guid QuotationId { get; set; }
    public Guid IndicatorId { get; set; }
    public Guid TestMethodId { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public string? IndicatorFormattedName { get; set; }
    public string TestMethodName { get; set; } = string.Empty;
    public string? TestMethodFormattedName { get; set; }
    public int SampleCount { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public int SortOrder { get; set; }
}
