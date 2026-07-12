using System;
using System.Threading.Tasks;

namespace LabSpecimenManagement.Notifications;

/// <summary>
/// Service interface for sending real-time notifications via SignalR
/// </summary>
public interface INotificationHubService
{
    /// <summary>
    /// Notifies the user that a new notification has been created
    /// FE will increment the unread count by 1
    /// </summary>
    Task NotifyNewNotificationAsync(Guid userId, Guid notificationId);

    /// <summary>
    /// Notifies an external temporary account that their access has expired — FE will force logout
    /// </summary>
    Task NotifyAccountExpiredAsync(Guid userId);
}
