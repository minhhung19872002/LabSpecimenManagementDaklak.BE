using System;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// DTO cho danh sách công việc cần làm của SampleReceiver
/// </summary>
public class SampleReceiverPendingTaskDto
{
    public Guid Id { get; set; }
    public string ContractNumber { get; set; } = string.Empty;
    public string TaskName { get; set; } = string.Empty;
    public DateTime? PendingDate { get; set; }
    public DateTime? ExpectedCompletionDate { get; set; }
    public TestRequestStatus Status { get; set; }
}
