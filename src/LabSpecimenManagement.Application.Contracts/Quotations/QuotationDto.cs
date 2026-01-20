using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Quotations;

/// <summary>
/// DTO hiển thị chi tiết báo giá
/// </summary>
public class QuotationDto : FullAuditedEntityDto<Guid>
{
    public string QuotationNumber { get; set; } = string.Empty;
    public DateTime QuotationDate { get; set; }
    public Guid? OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? ProvinceCode { get; set; }
    public string? DistrictCode { get; set; }
    public string? WardCode { get; set; }
    public string? Address { get; set; }
    public string? TaxCode { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public decimal TotalCost { get; set; }
    public string? Notes { get; set; }
    public QuotationStatus Status { get; set; }
    public List<QuotationSampleDto> Samples { get; set; } = new();
}
