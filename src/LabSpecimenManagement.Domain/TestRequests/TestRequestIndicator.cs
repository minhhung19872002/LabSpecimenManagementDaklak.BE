using System;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// Bảng liên kết nhiều-nhiều giữa TestRequest và Indicator
/// </summary>
public class TestRequestIndicator : Entity<Guid>
{
    /// <summary>
    /// ID phiếu yêu cầu
    /// </summary>
    public Guid TestRequestId { get; set; }

    /// <summary>
    /// ID chỉ tiêu
    /// </summary>
    public Guid IndicatorId { get; set; }

    protected TestRequestIndicator()
    {
    }

    public TestRequestIndicator(Guid id, Guid testRequestId, Guid indicatorId) : base(id)
    {
        TestRequestId = testRequestId;
        IndicatorId = indicatorId;
    }
}
