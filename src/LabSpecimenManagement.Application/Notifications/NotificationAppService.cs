using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using LabSpecimenManagement.Common.Exceptions;
using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.Notifications;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace LabSpecimenManagement.Notifications;

public class NotificationAppService : ApplicationService, INotificationAppService
{
    private static readonly HashSet<NotificationType> KtvAssignmentTypes = new()
    {
        NotificationType.SingleIndicatorAssignedToTechnician,
        NotificationType.MultipleIndicatorsAssignedToTechnician,
    };

    private readonly IRepository<Notification, Guid> _notificationRepository;
    private readonly IRepository<LaboratoryTaskIndicator, Guid> _laboratoryTaskIndicatorRepository;
    private readonly ICurrentUser _currentUser;
    private readonly INotificationHubService? _notificationHubService;

    public NotificationAppService(
        IRepository<Notification, Guid> notificationRepository,
        IRepository<LaboratoryTaskIndicator, Guid> laboratoryTaskIndicatorRepository,
        ICurrentUser currentUser,
        INotificationHubService? notificationHubService = null)
    {
        _notificationRepository = notificationRepository;
        _laboratoryTaskIndicatorRepository = laboratoryTaskIndicatorRepository;
        _currentUser = currentUser;
        _notificationHubService = notificationHubService;
    }

    /// <summary>
    /// Lấy danh sách thông báo của user hiện tại
    /// </summary>
    public async Task<PagedResultDto<NotificationDto>> GetMyNotificationsAsync(GetNotificationsInput input)
    {
        var currentUserId = _currentUser.Id ?? throw new Common.Exceptions.UnauthorizedAccessException("Người dùng chưa đăng nhập");

        var allNotifications = await _notificationRepository.GetListAsync(n => n.UserId == currentUserId);

        // Apply filters
        var filteredItems = allNotifications.AsEnumerable();

        if (input.IsRead.HasValue)
        {
            filteredItems = filteredItems.Where(n => n.IsRead == input.IsRead.Value);
        }

        if (input.Type.HasValue)
        {
            filteredItems = filteredItems.Where(n => n.Type == input.Type.Value);
        }

        var orderedItems = filteredItems.OrderByDescending(n => n.CreationTime).ToList();

        var totalCount = orderedItems.Count;
        var items = orderedItems
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        var dtos = items.Select(n => new NotificationDto
        {
            Id = n.Id,
            UserId = n.UserId,
            Title = n.Title,
            Message = n.Message,
            Type = n.Type,
            ReferenceId = n.ReferenceId,
            ReferenceType = n.ReferenceType,
            IsRead = n.IsRead,
            ReadAt = n.ReadAt,
            CreationTime = n.CreationTime,
            DeadlineDate = n.DeadlineDate,
            SenderName = n.SenderName,
            ItemCount = n.ItemCount,
            AdditionalData = n.AdditionalData,
            DaysRemaining = n.DeadlineDate.HasValue
                ? (int?)(n.DeadlineDate.Value.Date - DateTime.UtcNow.Date).Days
                : null
        }).ToList();

        // Compute IsTaskCompleted for KTV assignment notifications
        var ktvDtos = dtos
            .Where(d => KtvAssignmentTypes.Contains(d.Type) && !string.IsNullOrEmpty(d.ReferenceId))
            .ToList();

        if (ktvDtos.Count > 0)
        {
            var taskIds = ktvDtos
                .Select(d => Guid.TryParse(d.ReferenceId, out var g) ? g : (Guid?)null)
                .Where(g => g.HasValue)
                .Select(g => g!.Value)
                .Distinct()
                .ToArray();

            var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(ti =>
                taskIds.Contains(ti.LaboratoryTaskId) &&
                ti.TechnicianId == currentUserId &&
                ti.ApprovalStatus != IndicatorApprovalStatus.CustomerCancelled &&
                ti.ApprovalStatus != IndicatorApprovalStatus.CannotTest);

            var indicatorsByTaskId = taskIndicators
                .GroupBy(ti => ti.LaboratoryTaskId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var dto in ktvDtos)
            {
                if (Guid.TryParse(dto.ReferenceId, out var taskId) &&
                    indicatorsByTaskId.TryGetValue(taskId, out var indicators) &&
                    indicators.Count > 0)
                {
                    dto.IsTaskCompleted = indicators.All(ti => ti.ApprovalStatus == IndicatorApprovalStatus.Approved);
                }
                else
                {
                    dto.IsTaskCompleted = false;
                }
            }
        }

        return new PagedResultDto<NotificationDto>(totalCount, dtos);
    }

    /// <summary>
    /// Lấy số thông báo chưa đọc
    /// </summary>
    public async Task<int> GetUnreadCountAsync()
    {
        var currentUserId = _currentUser.Id ?? throw new Common.Exceptions.UnauthorizedAccessException("Người dùng chưa đăng nhập");

        var unreadNotifications = await _notificationRepository.GetListAsync(
            n => n.UserId == currentUserId && !n.IsRead);
        return unreadNotifications.Count;
    }

    /// <summary>
    /// Đánh dấu đã đọc một thông báo
    /// </summary>
    public async Task MarkAsReadAsync(Guid id)
    {
        var currentUserId = _currentUser.Id ?? throw new Common.Exceptions.UnauthorizedAccessException("Người dùng chưa đăng nhập");

        var notification = await _notificationRepository.GetAsync(id);

        if (notification.UserId != currentUserId)
        {
            throw new ForbiddenAccessException("Bạn không có quyền truy cập thông báo này");
        }

        notification.MarkAsRead();
        await _notificationRepository.UpdateAsync(notification);
    }

    /// <summary>
    /// Đánh dấu đã đọc tất cả thông báo
    /// </summary>
    public async Task MarkAllAsReadAsync()
    {
        var currentUserId = _currentUser.Id ?? throw new Common.Exceptions.UnauthorizedAccessException("Người dùng chưa đăng nhập");

        var unreadNotifications = await _notificationRepository.GetListAsync(
            n => n.UserId == currentUserId && !n.IsRead);

        foreach (var notification in unreadNotifications)
        {
            notification.MarkAsRead();
        }

        await _notificationRepository.UpdateManyAsync(unreadNotifications);
    }

    /// <summary>
    /// Tạo thông báo mới (internal use)
    /// </summary>
    public async Task CreateNotificationAsync(CreateNotificationDto input)
    {
        var notification = new Notification(
            GuidGenerator.Create(),
            input.UserId,
            input.Title,
            input.Message,
            input.Type,
            input.ReferenceId,
            input.ReferenceType,
            input.DeadlineDate,
            input.SenderName,
            input.ItemCount,
            input.AdditionalData
        );

        await _notificationRepository.InsertAsync(notification);

        // Push realtime notification
        if (_notificationHubService != null)
        {
            await _notificationHubService.NotifyNewNotificationAsync(notification.UserId, notification.Id);
        }
    }

    /// <summary>
    /// Tạo thông báo cho nhiều users cùng lúc
    /// </summary>
    public async Task CreateNotificationsForUsersAsync(
        IEnumerable<Guid> userIds,
        string title,
        string message,
        NotificationType type,
        string? referenceId = null,
        string? referenceType = null)
    {
        var notifications = userIds.Select(userId => new Notification(
            GuidGenerator.Create(),
            userId,
            title,
            message,
            type,
            referenceId,
            referenceType
        )).ToList();

        await _notificationRepository.InsertManyAsync(notifications);

        // Push realtime notifications to all users
        if (_notificationHubService != null)
        {
            foreach (var notification in notifications)
            {
                await _notificationHubService.NotifyNewNotificationAsync(notification.UserId, notification.Id);
            }
        }
    }
}
