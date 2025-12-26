using System;
using LabSpecimenManagement.Notifications;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.Notifications;

/// <summary>
/// Thông báo hệ thống
/// </summary>
public class Notification : CreationAuditedEntity<Guid>
{
    /// <summary>
    /// ID người nhận thông báo
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Tiêu đề thông báo
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Nội dung thông báo
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Loại thông báo
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// ID đối tượng liên quan (VD: TestRequestId)
    /// </summary>
    public string? ReferenceId { get; set; }

    /// <summary>
    /// Loại đối tượng liên quan (VD: TestRequest, LaboratoryTask)
    /// </summary>
    public string? ReferenceType { get; set; }

    /// <summary>
    /// Đã đọc hay chưa
    /// </summary>
    public bool IsRead { get; set; } = false;

    /// <summary>
    /// Thời gian đọc
    /// </summary>
    public DateTime? ReadAt { get; set; }

    public Notification()
    {
    }

    public Notification(
        Guid id,
        Guid userId,
        string title,
        string message,
        NotificationType type,
        string? referenceId = null,
        string? referenceType = null) : base(id)
    {
        UserId = userId;
        Title = title;
        Message = message;
        Type = type;
        ReferenceId = referenceId;
        ReferenceType = referenceType;
        IsRead = false;
    }

    /// <summary>
    /// Đánh dấu đã đọc
    /// </summary>
    public void MarkAsRead()
    {
        if (!IsRead)
        {
            IsRead = true;
            ReadAt = DateTime.UtcNow;
        }
    }
}
