using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// L2 · Version giá của 1 dịch vụ (TestMethodIndicator).
/// Đổi giá = tạo version mới với ngày áp dụng; TestMethodIndicator.UnitPrice luôn là giá đang áp dụng
/// (được cập nhật khi version tới ngày hiệu lực).
/// EffectiveDate = null: giá gốc (trước khi có version).
/// </summary>
public class ServicePriceVersion : FullAuditedEntity<Guid>
{
    public Guid TestMethodIndicatorId { get; set; }

    public decimal UnitPrice { get; set; }

    /// <summary>Thời gian trả kết quả (ngày)</summary>
    public int? TurnaroundDays { get; set; }

    /// <summary>Ngày áp dụng (chỉ phần ngày). Null = giá gốc.</summary>
    public DateTime? EffectiveDate { get; set; }

    public string? Note { get; set; }

    protected ServicePriceVersion()
    {
    }

    public ServicePriceVersion(Guid id, Guid testMethodIndicatorId, decimal unitPrice, int? turnaroundDays, DateTime? effectiveDate, string? note = null)
        : base(id)
    {
        TestMethodIndicatorId = testMethodIndicatorId;
        UnitPrice = unitPrice;
        TurnaroundDays = turnaroundDays;
        EffectiveDate = effectiveDate?.Date;
        Note = note;
    }
}
