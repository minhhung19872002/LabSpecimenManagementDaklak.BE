using System;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// DTO lịch sử in phiếu bàn giao
/// </summary>
public class TestRequestDeliveryHistoryDto
{
    public Guid Id { get; set; }
    public Guid TestRequestId { get; set; }
    public DateTime PrintedDate { get; set; }
    public string? PrintedByUserName { get; set; }
    public TestRequestStatus StatusAfter { get; set; }
}

/// <summary>
/// Kết quả in phiếu bàn giao
/// </summary>
public class PrintHandoverOutput
{
    /// <summary>
    /// Tên file .docx trong wwwroot/temp để mở bằng Document Editor
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    public TestRequestStatus Status { get; set; }
}
