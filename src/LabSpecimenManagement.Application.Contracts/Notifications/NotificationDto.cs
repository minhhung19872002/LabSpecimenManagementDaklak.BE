using System;
using LabSpecimenManagement.Notifications;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Notifications;

/// <summary>
/// DTO cho thông báo
/// </summary>
public class NotificationDto : EntityDto<Guid>
{
    /// <summary>
    /// ID người nhận
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
    /// Loại đối tượng liên quan
    /// </summary>
    public string? ReferenceType { get; set; }

    /// <summary>
    /// Đã đọc hay chưa
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// Thời gian đọc
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// Thời gian tạo
    /// </summary>
    public DateTime CreationTime { get; set; }
}

/// <summary>
/// DTO tạo thông báo
/// </summary>
public class CreateNotificationDto
{
    /// <summary>
    /// ID người nhận
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
    /// ID đối tượng liên quan
    /// </summary>
    public string? ReferenceId { get; set; }

    /// <summary>
    /// Loại đối tượng liên quan
    /// </summary>
    public string? ReferenceType { get; set; }
}
