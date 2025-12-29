using LabSpecimenManagement.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

/// <summary>
/// Controller cho thông báo
/// </summary>
[RemoteService(Name = "Default")]
[Route("api/v1/notifications")]
[Authorize]
public class NotificationController : LabSpecimenManagementController
{
    private readonly INotificationAppService _notificationAppService;

    public NotificationController(INotificationAppService notificationAppService)
    {
        _notificationAppService = notificationAppService;
    }

    /// <summary>
    /// Lấy danh sách thông báo của user hiện tại
    /// </summary>
    [HttpGet]
    public virtual Task<PagedResultDto<NotificationDto>> GetMyNotificationsAsync([FromQuery] GetNotificationsInput input)
    {
        return _notificationAppService.GetMyNotificationsAsync(input);
    }

    /// <summary>
    /// Lấy số thông báo chưa đọc
    /// </summary>
    [HttpGet("unread-count")]
    public virtual Task<int> GetUnreadCountAsync()
    {
        return _notificationAppService.GetUnreadCountAsync();
    }

    /// <summary>
    /// Đánh dấu đã đọc một thông báo
    /// </summary>
    [HttpPost("{id}/mark-read")]
    public virtual Task MarkAsReadAsync(Guid id)
    {
        return _notificationAppService.MarkAsReadAsync(id);
    }

    /// <summary>
    /// Đánh dấu đã đọc tất cả thông báo
    /// </summary>
    [HttpPost("mark-all-read")]
    public virtual Task MarkAllAsReadAsync()
    {
        return _notificationAppService.MarkAllAsReadAsync();
    }
}
