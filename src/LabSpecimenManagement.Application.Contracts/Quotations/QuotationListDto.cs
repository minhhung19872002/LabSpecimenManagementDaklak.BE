using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Quotations;

/// <summary>
/// DTO rút gọn hiển thị danh sách báo giá
/// </summary>
public class QuotationListDto : EntityDto<Guid>
{
    public string QuotationNumber { get; set; } = string.Empty;
    public DateTime QuotationDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public decimal TotalCost { get; set; }
    public QuotationStatus Status { get; set; }
    public int ItemCount { get; set; }
    public int SampleCount { get; set; }
    public DateTime CreationTime { get; set; }
    public string? CreatorUserName { get; set; }
}
