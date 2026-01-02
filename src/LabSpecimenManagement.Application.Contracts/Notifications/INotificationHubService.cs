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
    /// <param name="userId">The target user ID</param>
    /// <param name="notificationId">The ID of the new notification</param>
    Task NotifyNewNotificationAsync(Guid userId, Guid notificationId);
}
