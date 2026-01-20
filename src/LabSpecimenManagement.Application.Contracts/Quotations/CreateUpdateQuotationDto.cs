using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.Quotations;

/// <summary>
/// DTO tạo/cập nhật báo giá
/// </summary>
public class CreateUpdateQuotationDto
{
    public DateTime QuotationDate { get; set; } = DateTime.Now;

    public Guid? OrganizationId { get; set; }

    [StringLength(256)]
    public string? CustomerName { get; set; }

    [StringLength(10)]
    public string? ProvinceCode { get; set; }

    [StringLength(10)]
    public string? DistrictCode { get; set; }

    [StringLength(10)]
    public string? WardCode { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(50)]
    public string? TaxCode { get; set; }

    [StringLength(256)]
    public string? ContactPerson { get; set; }

    [StringLength(50)]
    public string? Phone { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    public QuotationStatus Status { get; set; } = QuotationStatus.Draft;

    public List<CreateUpdateQuotationSampleDto> Samples { get; set; } = new();
}
