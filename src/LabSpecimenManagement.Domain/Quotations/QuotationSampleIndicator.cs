using System;
using LabSpecimenManagement.MasterData;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.Quotations;

/// <summary>
/// Chỉ tiêu trong mẫu báo giá - Quotation Sample Indicator
/// </summary>
public class QuotationSampleIndicator : Entity<Guid>
{
    /// <summary>
    /// QuotationSample ID (Foreign Key)
    /// </summary>
    public Guid QuotationSampleId { get; set; }

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
    /// Đơn giá (từ TestMethodIndicator.UnitPrice, snapshot tại thời điểm tạo)
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Thứ tự hiển thị trong mẫu
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Loại mẫu ID (nullable, Foreign Key)
    /// </summary>
    public Guid? SpecimenTypeId { get; set; }

    /// <summary>
    /// Tên loại mẫu (snapshot tại thời điểm tạo)
    /// </summary>
    public string? SpecimenTypeName { get; set; }

    /// <summary>
    /// Ghi chú cho chỉ tiêu
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Navigation property to QuotationSample
    /// </summary>
    public virtual QuotationSample QuotationSample { get; set; } = null!;

    /// <summary>
    /// Navigation property to Indicator
    /// </summary>
    public virtual Indicator Indicator { get; set; } = null!;

    /// <summary>
    /// Navigation property to TestMethod
    /// </summary>
    public virtual TestMethod TestMethod { get; set; } = null!;

    /// <summary>
    /// Navigation property to SpecimenType
    /// </summary>
    public virtual SpecimenType? SpecimenType { get; set; }

    protected QuotationSampleIndicator()
    {
    }

    public QuotationSampleIndicator(
        Guid id,
        Guid quotationSampleId,
        Guid indicatorId,
        Guid testMethodId,
        string indicatorName,
        string testMethodName,
        decimal unitPrice,
        int sortOrder,
        Guid? specimenTypeId = null,
        string? specimenTypeName = null,
        string? note = null) : base(id)
    {
        QuotationSampleId = quotationSampleId;
        IndicatorId = indicatorId;
        TestMethodId = testMethodId;
        IndicatorName = indicatorName;
        TestMethodName = testMethodName;
        UnitPrice = unitPrice;
        SortOrder = sortOrder;
        SpecimenTypeId = specimenTypeId;
        SpecimenTypeName = specimenTypeName;
        Note = note;
    }
}
