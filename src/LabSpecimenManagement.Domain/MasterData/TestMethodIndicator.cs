using System;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Bảng trung gian liên kết Phương pháp thử và Chỉ tiêu (Many-to-Many)
/// </summary>
public class TestMethodIndicator : Entity<Guid>
{
    /// <summary>
    /// ID Phương pháp thử
    /// </summary>
    public Guid TestMethodId { get; set; }

    /// <summary>
    /// ID Chỉ tiêu
    /// </summary>
    public Guid IndicatorId { get; set; }

    /// <summary>
    /// Đơn giá riêng cho chỉ tiêu trong phương pháp này
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Thứ tự hiển thị trong phương pháp
    /// </summary>
    public int SortOrder { get; set; }

    protected TestMethodIndicator()
    {
    }

    public TestMethodIndicator(Guid id, Guid testMethodId, Guid indicatorId, decimal unitPrice) : base(id)
    {
        TestMethodId = testMethodId;
        IndicatorId = indicatorId;
        UnitPrice = unitPrice;
        SortOrder = 0;
    }
}
