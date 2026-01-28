using System;
using System.Collections.Generic;
using LabSpecimenManagement.MasterData;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.Quotations;

/// <summary>
/// Báo giá - Quotation
/// </summary>
public class Quotation : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// Mã báo giá - tự động sinh (BG-YYYYMMDD-XXXX)
    /// </summary>
    public string QuotationNumber { get; set; } = string.Empty;

    /// <summary>
    /// Ngày báo giá
    /// </summary>
    public DateTime QuotationDate { get; set; }

    /// <summary>
    /// Cơ sở/Doanh nghiệp ID (nếu chọn từ danh sách)
    /// </summary>
    public Guid? OrganizationId { get; set; }

    /// <summary>
    /// Tên khách hàng
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Mã tỉnh thành
    /// </summary>
    public string? ProvinceCode { get; set; }

    /// <summary>
    /// Mã quận huyện
    /// </summary>
    public string? DistrictCode { get; set; }

    /// <summary>
    /// Mã phường xã
    /// </summary>
    public string? WardCode { get; set; }

    /// <summary>
    /// Địa chỉ chi tiết
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// Mã số thuế
    /// </summary>
    public string? TaxCode { get; set; }

    /// <summary>
    /// Người liên hệ
    /// </summary>
    public string? ContactPerson { get; set; }

    /// <summary>
    /// Số điện thoại
    /// </summary>
    public string? Phone { get; set; }

    /// <summary>
    /// Tổng chi phí (tự động tính từ các items)
    /// </summary>
    public decimal TotalCost { get; set; }

    /// <summary>
    /// Ghi chú
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Trạng thái: Draft (nháp), Sent (đã gửi), Cancelled (hủy)
    /// </summary>
    public QuotationStatus Status { get; set; }

    /// <summary>
    /// Navigation property to Organization
    /// </summary>
    public virtual Organization? Organization { get; set; }

    /// <summary>
    /// Collection of quotation samples
    /// </summary>
    public virtual ICollection<QuotationSample> Samples { get; set; }

    protected Quotation()
    {
        Samples = new List<QuotationSample>();
    }

    public Quotation(Guid id, string quotationNumber, DateTime quotationDate, string? customerName = null) : base(id)
    {
        QuotationNumber = quotationNumber;
        QuotationDate = quotationDate;
        CustomerName = customerName;
        Status = QuotationStatus.Draft;
        Samples = new List<QuotationSample>();
    }

    /// <summary>
    /// Tính lại tổng chi phí từ các samples
    /// </summary>
    public void RecalculateTotalCost()
    {
        decimal total = 0;
        foreach (var sample in Samples)
        {
            sample.RecalculateTotalPrice();
            total += sample.TotalPrice;
        }
        TotalCost = total;
    }
}
