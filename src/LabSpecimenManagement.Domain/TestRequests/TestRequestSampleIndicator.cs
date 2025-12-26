using System;
using LabSpecimenManagement.MasterData;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// Bảng trung gian liên kết Sample với Indicator
/// </summary>
public class TestRequestSampleIndicator : Entity<Guid>
{
    /// <summary>
    /// TestRequestSample ID (Foreign Key)
    /// </summary>
    public Guid TestRequestSampleId { get; set; }

    /// <summary>
    /// Indicator ID (Foreign Key)
    /// </summary>
    public Guid IndicatorId { get; set; }

    /// <summary>
    /// Laboratory ID - Khoa phòng thực hiện (Foreign Key)
    /// </summary>
    public Guid? LaboratoryId { get; set; }

    /// <summary>
    /// Test Method ID - Phương pháp thử (Foreign Key)
    /// </summary>
    public Guid? TestMethodId { get; set; }

    /// <summary>
    /// Đơn giá tại thời điểm tạo (snapshot)
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Ghi chú cho chỉ tiêu này
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Ngày chuyển giao
    /// </summary>
    public DateTime? TransferDate { get; set; }

    /// <summary>
    /// Ngày yêu cầu
    /// </summary>
    public DateTime? RequestDate { get; set; }

    /// <summary>
    /// Đã gửi khoa phòng
    /// </summary>
    public bool SentToDepartment { get; set; }

    /// <summary>
    /// Navigation property to TestRequestSample
    /// </summary>
    public virtual TestRequestSample TestRequestSample { get; set; } = null!;

    /// <summary>
    /// Navigation property to Indicator
    /// </summary>
    public virtual Indicator Indicator { get; set; } = null!;

    /// <summary>
    /// Navigation property to Laboratory
    /// </summary>
    public virtual Laboratory? Laboratory { get; set; }

    /// <summary>
    /// Navigation property to TestMethod
    /// </summary>
    public virtual TestMethod? TestMethod { get; set; }

    protected TestRequestSampleIndicator()
    {
    }

    public TestRequestSampleIndicator(Guid id, Guid testRequestSampleId, Guid indicatorId) : base(id)
    {
        TestRequestSampleId = testRequestSampleId;
        IndicatorId = indicatorId;
    }
}
