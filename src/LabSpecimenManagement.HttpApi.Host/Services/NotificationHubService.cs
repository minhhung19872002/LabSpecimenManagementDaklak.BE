using System;
using System.Threading.Tasks;
using LabSpecimenManagement.Hubs;
using LabSpecimenManagement.Notifications;
using Microsoft.AspNetCore.SignalR;
using Volo.Abp.DependencyInjection;

namespace LabSpecimenManagement.Services;

/// <summary>
/// Implementation of INotificationHubService for sending real-time notifications via SignalR
/// </summary>
public class NotificationHubService : INotificationHubService, ITransientDependency
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationHubService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    /// <inheritdoc />
    public async Task NotifyNewNotificationAsync(Guid userId, Guid notificationId)
    {
        await _hubContext.Clients
            .Group($"user_{userId}")
            .SendAsync("ReceiveNewNotification", notificationId);
    }
}
