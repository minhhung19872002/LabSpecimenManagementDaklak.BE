using System;
using System.Collections.Generic;
using System.Linq;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.Quotations;

/// <summary>
/// Mẫu trong báo giá - Quotation Sample
/// </summary>
public class QuotationSample : Entity<Guid>
{
    /// <summary>
    /// Quotation ID (Foreign Key)
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
    /// Số lượng mẫu
    /// </summary>
    public int Quantity { get; set; } = 1;

    /// <summary>
    /// Tổng tiền của mẫu này (tự động tính từ các indicators)
    /// </summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// Navigation property to Quotation
    /// </summary>
    public virtual Quotation Quotation { get; set; } = null!;

    /// <summary>
    /// Collection of indicators in this sample
    /// </summary>
    public virtual ICollection<QuotationSampleIndicator> Indicators { get; set; }

    protected QuotationSample()
    {
        Indicators = new List<QuotationSampleIndicator>();
    }

    public QuotationSample(Guid id, Guid quotationId, int sortOrder, string sampleName) : base(id)
    {
        QuotationId = quotationId;
        SortOrder = sortOrder;
        SampleName = sampleName;
        Indicators = new List<QuotationSampleIndicator>();
    }

    /// <summary>
    /// Tính lại tổng tiền từ các indicators
    /// </summary>
    public void RecalculateTotalPrice()
    {
        TotalPrice = Indicators.Sum(i => i.UnitPrice) * Quantity;
    }
}
