using System;
using LabSpecimenManagement.Notifications;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Notifications;

/// <summary>
/// Input DTO for getting notifications with filters
/// </summary>
public class GetNotificationsInput : PagedResultRequestDto
{
    /// <summary>
    /// Filter by read status: null = all, true = read, false = unread
    /// </summary>
    public bool? IsRead { get; set; }

    /// <summary>
    /// Filter by notification type
    /// </summary>
    public NotificationType? Type { get; set; }
}

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

    /// <summary>
    /// Ngày deadline
    /// </summary>
    public DateTime? DeadlineDate { get; set; }

    /// <summary>
    /// Tên người gửi/thực hiện action
    /// </summary>
    public string? SenderName { get; set; }

    /// <summary>
    /// Số lượng item liên quan (VD: số chỉ tiêu)
    /// </summary>
    public int? ItemCount { get; set; }

    /// <summary>
    /// Dữ liệu bổ sung dạng JSON
    /// </summary>
    public string? AdditionalData { get; set; }

    /// <summary>
    /// Số ngày còn lại đến deadline (computed)
    /// </summary>
    public int? DaysRemaining { get; set; }
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

    /// <summary>
    /// Ngày deadline
    /// </summary>
    public DateTime? DeadlineDate { get; set; }

    /// <summary>
    /// Tên người gửi/thực hiện action
    /// </summary>
    public string? SenderName { get; set; }

    /// <summary>
    /// Số lượng item liên quan (VD: số chỉ tiêu)
    /// </summary>
    public int? ItemCount { get; set; }

    /// <summary>
    /// Dữ liệu bổ sung dạng JSON
    /// </summary>
    public string? AdditionalData { get; set; }
}
