using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// DTO for a single timeline step
/// </summary>
public class TimelineStepDto
{
    /// <summary>
    /// Step title (e.g., "Mới", "Chờ xử lý", "Đã gửi mẫu")
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Status of this step: completed, current, pending
    /// </summary>
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Date when this step was reached
    /// </summary>
    public DateTime? Date { get; set; }

    /// <summary>
    /// User ID who performed this action
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// User name who performed this action
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Additional information (e.g., laboratory names for SampleSent step)
    /// </summary>
    public List<string>? ExtraInfo { get; set; }

    /// <summary>
    /// Note/Reason (e.g., cancellation reason)
    /// </summary>
    public string? Note { get; set; }
}

/// <summary>
/// DTO for TestRequest timeline
/// </summary>
public class TestRequestTimelineDto
{
    /// <summary>
    /// TestRequest ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Current status
    /// </summary>
    public TestRequestStatus Status { get; set; }

    /// <summary>
    /// Timeline steps (ordered from newest to oldest)
    /// </summary>
    public List<TimelineStepDto> Steps { get; set; } = new();
}
