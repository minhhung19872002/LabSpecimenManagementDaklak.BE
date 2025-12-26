using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.Notifications;

/// <summary>
/// Interface cho NotificationAppService
/// </summary>
[RemoteService(IsEnabled = false)]
public interface INotificationAppService : IApplicationService
{
    /// <summary>
    /// Lấy danh sách thông báo của user hiện tại
    /// </summary>
    Task<PagedResultDto<NotificationDto>> GetMyNotificationsAsync(PagedResultRequestDto input);

    /// <summary>
    /// Lấy số thông báo chưa đọc
    /// </summary>
    Task<int> GetUnreadCountAsync();

    /// <summary>
    /// Đánh dấu đã đọc một thông báo
    /// </summary>
    Task MarkAsReadAsync(Guid id);

    /// <summary>
    /// Đánh dấu đã đọc tất cả thông báo
    /// </summary>
    Task MarkAllAsReadAsync();

    /// <summary>
    /// Tạo thông báo mới (internal use)
    /// </summary>
    Task CreateNotificationAsync(CreateNotificationDto input);
}
