using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// Lịch sử in phiếu bàn giao (BM03 - Sổ quản lý mẫu).
/// Mỗi lần BPGN bấm "In phiếu bàn giao" ghi 1 dòng; CreationTime = ngày in, CreatorId = người in.
/// </summary>
public class TestRequestDeliveryHistory : CreationAuditedEntity<Guid>
{
    public Guid TestRequestId { get; set; }

    /// <summary>
    /// Tên người in tại thời điểm in (snapshot để hiển thị)
    /// </summary>
    public string? PrintedByUserName { get; set; }

    /// <summary>
    /// Trạng thái phiếu yêu cầu sau khi in
    /// </summary>
    public TestRequestStatus StatusAfter { get; set; }

    public TestRequestDeliveryHistory()
    {
    }

    public TestRequestDeliveryHistory(
        Guid id,
        Guid testRequestId,
        string? printedByUserName,
        TestRequestStatus statusAfter) : base(id)
    {
        TestRequestId = testRequestId;
        PrintedByUserName = printedByUserName;
        StatusAfter = statusAfter;
    }
}
