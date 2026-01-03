using System;
using LabSpecimenManagement.MasterData;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.Quotations;

/// <summary>
/// Chi tiết báo giá - Quotation Item
/// </summary>
public class QuotationItem : Entity<Guid>
{
    /// <summary>
    /// Quotation ID (Foreign Key)
    /// </summary>
    public Guid QuotationId { get; set; }

    /// <summary>
    /// Indicator ID (Foreign Key)
    /// </summary>
    public Guid IndicatorId { get; set; }

    /// <summary>
    /// Test Method ID (Foreign Key)
    /// </summary>
    public Guid TestMethodId { get; set; }

    /// <summary>
    /// Tên chỉ tiêu (snapshot tại thời điểm tạo)
    /// </summary>
    public string IndicatorName { get; set; } = string.Empty;

    /// <summary>
    /// Tên phương pháp (snapshot tại thời điểm tạo)
    /// </summary>
    public string TestMethodName { get; set; } = string.Empty;

    /// <summary>
    /// Số mẫu
    /// </summary>
    public int SampleCount { get; set; } = 1;

    /// <summary>
    /// Đơn giá (từ IndicatorTestMethod.UnitPrice, snapshot tại thời điểm tạo)
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Thành tiền = SampleCount * UnitPrice
    /// </summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// Thứ tự hiển thị
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Navigation property to Quotation
    /// </summary>
    public virtual Quotation Quotation { get; set; } = null!;

    /// <summary>
    /// Navigation property to Indicator
    /// </summary>
    public virtual Indicator Indicator { get; set; } = null!;

    /// <summary>
    /// Navigation property to TestMethod
    /// </summary>
    public virtual TestMethod TestMethod { get; set; } = null!;

    protected QuotationItem()
    {
    }

    public QuotationItem(
        Guid id,
        Guid quotationId,
        Guid indicatorId,
        Guid testMethodId,
        string indicatorName,
        string testMethodName,
        int sampleCount,
        decimal unitPrice,
        int sortOrder) : base(id)
    {
        QuotationId = quotationId;
        IndicatorId = indicatorId;
        TestMethodId = testMethodId;
        IndicatorName = indicatorName;
        TestMethodName = testMethodName;
        SampleCount = sampleCount;
        UnitPrice = unitPrice;
        TotalPrice = sampleCount * unitPrice;
        SortOrder = sortOrder;
    }

    /// <summary>
    /// Tính lại thành tiền
    /// </summary>
    public void RecalculateTotalPrice()
    {
        TotalPrice = SampleCount * UnitPrice;
    }
}
