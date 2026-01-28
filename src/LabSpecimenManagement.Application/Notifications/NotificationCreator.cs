using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace LabSpecimenManagement.Notifications;

/// <summary>
/// Service for creating notifications for specific business events
/// </summary>
public class NotificationCreator : INotificationCreator, ITransientDependency
{
    private readonly IRepository<Notification, Guid> _notificationRepository;
    private readonly IGuidGenerator _guidGenerator;
    private readonly INotificationHubService? _notificationHubService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public NotificationCreator(
        IRepository<Notification, Guid> notificationRepository,
        IGuidGenerator guidGenerator,
        INotificationHubService? notificationHubService = null)
    {
        _notificationRepository = notificationRepository;
        _guidGenerator = guidGenerator;
        _notificationHubService = notificationHubService;
    }

    /// <summary>
    /// Helper method to serialize additional data to JSON
    /// </summary>
    private static string SerializeAdditionalData(Dictionary<string, object?> data)
    {
        // Remove null values
        var cleanData = new Dictionary<string, object>();
        foreach (var kvp in data)
        {
            if (kvp.Value != null)
            {
                cleanData[kvp.Key] = kvp.Value;
            }
        }
        return JsonSerializer.Serialize(cleanData, JsonOptions);
    }

    /// <summary>
    /// Helper method to insert notification and notify user via SignalR
    /// </summary>
    private async Task InsertNotificationAndPushAsync(Notification notification)
    {
        await _notificationRepository.InsertAsync(notification);

        // Notify user about new notification via SignalR (FE will increment count by 1)
        if (_notificationHubService != null)
        {
            await _notificationHubService.NotifyNewNotificationAsync(notification.UserId, notification.Id);
        }
    }

    #region Notifications for QLKT (LabTechnicalManager)

    /// <summary>
    /// Case 1 & 6: KTV gửi chỉ tiêu cần QLKT phê duyệt
    /// </summary>
    public async Task NotifyIndicatorSubmittedToTechnicalManagerAsync(
        Guid technicalManagerId,
        string contractNumber,
        Guid taskId,
        string senderName,
        int indicatorCount,
        DateTime? deadline,
        string? sampleName = null,
        string? indicatorName = null,
        string? methodName = null)
    {
        string title;
        string message;
        NotificationType type;
        Dictionary<string, object?> additionalData;

        if (indicatorCount == 1 && !string.IsNullOrEmpty(sampleName))
        {
            // Case 6: Single indicator
            title = "Chờ phê duyệt";
            message = $"{sampleName} - {indicatorName} - {methodName}";
            type = NotificationType.SingleIndicatorSubmitted;
            additionalData = new Dictionary<string, object?>
            {
                ["contractNumber"] = contractNumber,
                ["sampleName"] = sampleName,
                ["indicatorName"] = indicatorName,
                ["methodName"] = methodName,
                ["senderName"] = senderName
            };
        }
        else
        {
            // Case 1: Multiple indicators
            title = "Chờ phê duyệt";
            message = $"{indicatorCount} chỉ tiêu từ {senderName}";
            type = NotificationType.MultipleIndicatorsSubmitted;
            additionalData = new Dictionary<string, object?>
            {
                ["contractNumber"] = contractNumber,
                ["indicatorCount"] = indicatorCount,
                ["senderName"] = senderName
            };
        }

        var notification = new Notification(
            _guidGenerator.Create(),
            technicalManagerId,
            title,
            message,
            type,
            taskId.ToString(),
            "LaboratoryTask",
            deadline,
            senderName,
            indicatorCount,
            SerializeAdditionalData(additionalData)
        );

        await InsertNotificationAndPushAsync(notification);
    }

    /// <summary>
    /// Case 2: Phiếu yêu cầu chuyển trạng thái "Đã gửi mẫu"
    /// </summary>
    public async Task NotifySampleSentToLabAsync(
        Guid technicalManagerId,
        string contractNumber,
        Guid taskId,
        int indicatorCount,
        DateTime? deadline)
    {
        var title = "Mẫu mới cần thử nghiệm";
        var message = $"{indicatorCount} chỉ tiêu cần thực hiện";
        var additionalData = new Dictionary<string, object?>
        {
            ["contractNumber"] = contractNumber,
            ["indicatorCount"] = indicatorCount
        };

        var notification = new Notification(
            _guidGenerator.Create(),
            technicalManagerId,
            title,
            message,
            NotificationType.SampleSentToLab,
            taskId.ToString(),
            "LaboratoryTask",
            deadline,
            null,
            indicatorCount,
            SerializeAdditionalData(additionalData)
        );

        await InsertNotificationAndPushAsync(notification);
    }

    /// <summary>
    /// Case 3: Reminder - chưa phân công công việc (>= 2 ngày)
    /// </summary>
    public async Task NotifyPendingWorkAssignmentAsync(
        Guid technicalManagerId,
        string contractNumber,
        Guid taskId,
        int pendingIndicatorCount,
        DateTime sentDate)
    {
        var title = "Nhắc nhở phân công";
        var message = $"{pendingIndicatorCount} chỉ tiêu chưa phân công";
        var additionalData = new Dictionary<string, object?>
        {
            ["contractNumber"] = contractNumber,
            ["indicatorCount"] = pendingIndicatorCount
        };

        var notification = new Notification(
            _guidGenerator.Create(),
            technicalManagerId,
            title,
            message,
            NotificationType.PendingWorkAssignment,
            taskId.ToString(),
            "LaboratoryTask",
            null,
            null,
            pendingIndicatorCount,
            SerializeAdditionalData(additionalData)
        );

        await InsertNotificationAndPushAsync(notification);
    }

    /// <summary>
    /// Case 4 & 5: QLCL từ chối kết quả
    /// </summary>
    public async Task NotifyResultRejectedByQualityManagerAsync(
        Guid technicalManagerId,
        string contractNumber,
        Guid taskId,
        string qualityManagerName,
        int rejectedCount,
        string? sampleName = null,
        string? indicatorName = null,
        string? methodName = null)
    {
        string title = "Kết quả bị từ chối";
        string message;
        NotificationType type;
        Dictionary<string, object?> additionalData;

        if (rejectedCount == 1 && !string.IsNullOrEmpty(sampleName))
        {
            // Case 4: Single rejection
            message = $"{sampleName} - {indicatorName} - {methodName}";
            type = NotificationType.SingleResultRejectedByQM;
            additionalData = new Dictionary<string, object?>
            {
                ["contractNumber"] = contractNumber,
                ["sampleName"] = sampleName,
                ["indicatorName"] = indicatorName,
                ["methodName"] = methodName,
                ["senderName"] = qualityManagerName
            };
        }
        else
        {
            // Case 5: Multiple rejections
            message = $"{rejectedCount} chỉ tiêu bị từ chối";
            type = NotificationType.MultipleResultsRejectedByQM;
            additionalData = new Dictionary<string, object?>
            {
                ["contractNumber"] = contractNumber,
                ["rejectedCount"] = rejectedCount,
                ["senderName"] = qualityManagerName
            };
        }

        var notification = new Notification(
            _guidGenerator.Create(),
            technicalManagerId,
            title,
            message,
            type,
            taskId.ToString(),
            "LaboratoryTask",
            null,
            qualityManagerName,
            rejectedCount,
            SerializeAdditionalData(additionalData)
        );

        await InsertNotificationAndPushAsync(notification);
    }

    /// <summary>
    /// Case 7: QLCL duyệt kết quả
    /// </summary>
    public async Task NotifyResultApprovedByQualityManagerAsync(
        Guid technicalManagerId,
        string contractNumber,
        Guid taskId,
        string qualityManagerName,
        string? sampleName = null,
        string? indicatorName = null,
        string? methodName = null)
    {
        var title = "Kết quả đã duyệt";
        string message;
        Dictionary<string, object?> additionalData;

        if (!string.IsNullOrEmpty(sampleName))
        {
            message = $"{sampleName} - {indicatorName} - {methodName}";
            additionalData = new Dictionary<string, object?>
            {
                ["contractNumber"] = contractNumber,
                ["sampleName"] = sampleName,
                ["indicatorName"] = indicatorName,
                ["methodName"] = methodName,
                ["senderName"] = qualityManagerName
            };
        }
        else
        {
            message = "Kết quả đã được phê duyệt";
            additionalData = new Dictionary<string, object?>
            {
                ["contractNumber"] = contractNumber,
                ["senderName"] = qualityManagerName
            };
        }

        var notification = new Notification(
            _guidGenerator.Create(),
            technicalManagerId,
            title,
            message,
            NotificationType.ResultApprovedByQM,
            taskId.ToString(),
            "LaboratoryTask",
            null,
            qualityManagerName,
            null,
            SerializeAdditionalData(additionalData)
        );

        await InsertNotificationAndPushAsync(notification);
    }

    /// <summary>
    /// Case 8.1: Có mẫu/chỉ tiêu mới thêm vào hợp đồng
    /// </summary>
    public async Task NotifyNewSamplesAddedAsync(
        Guid technicalManagerId,
        string contractNumber,
        Guid taskId,
        int newSampleCount,
        int newIndicatorCount)
    {
        var title = "Mẫu/chỉ tiêu mới";
        var message = $"Thêm {newSampleCount} mẫu, {newIndicatorCount} chỉ tiêu";
        var additionalData = new Dictionary<string, object?>
        {
            ["contractNumber"] = contractNumber,
            ["newSampleCount"] = newSampleCount,
            ["newIndicatorCount"] = newIndicatorCount
        };

        var notification = new Notification(
            _guidGenerator.Create(),
            technicalManagerId,
            title,
            message,
            NotificationType.NewSamplesAdded,
            taskId.ToString(),
            "LaboratoryTask",
            null,
            null,
            newIndicatorCount,
            SerializeAdditionalData(additionalData)
        );

        await InsertNotificationAndPushAsync(notification);
    }

    /// <summary>
    /// Case 8.2: Mẫu/chỉ tiêu bị hủy
    /// </summary>
    public async Task NotifySamplesCancelledAsync(
        Guid technicalManagerId,
        string contractNumber,
        Guid taskId,
        string sampleCode,
        int cancelledIndicatorCount)
    {
        var title = "Mẫu bị hủy";
        var message = $"{sampleCode} - {cancelledIndicatorCount} chỉ tiêu";
        var additionalData = new Dictionary<string, object?>
        {
            ["contractNumber"] = contractNumber,
            ["sampleCode"] = sampleCode,
            ["cancelledCount"] = cancelledIndicatorCount
        };

        var notification = new Notification(
            _guidGenerator.Create(),
            technicalManagerId,
            title,
            message,
            NotificationType.SamplesCancelled,
            taskId.ToString(),
            "LaboratoryTask",
            null,
            null,
            cancelledIndicatorCount,
            SerializeAdditionalData(additionalData)
        );

        await InsertNotificationAndPushAsync(notification);
    }

    #endregion

    #region Notifications for QLCL (QualityManager)

    /// <summary>
    /// Case 1 & 2 QLCL: QLKT gửi kết quả chỉ tiêu cần QLCL phê duyệt
    /// </summary>
    public async Task NotifyResultSubmittedToQualityManagerAsync(
        Guid qualityManagerId,
        string contractNumber,
        Guid qualityTaskId,
        string senderName,
        int indicatorCount,
        string? sampleName = null,
        string? indicatorName = null,
        string? methodName = null)
    {
        string title = "Chờ phê duyệt";
        string message;
        NotificationType type;
        Dictionary<string, object?> additionalData;

        if (indicatorCount == 1 && !string.IsNullOrEmpty(sampleName))
        {
            // Case 2 QLCL: Single indicator
            message = $"{sampleName} - {indicatorName} - {methodName}";
            type = NotificationType.SingleResultSubmittedToQM;
            additionalData = new Dictionary<string, object?>
            {
                ["contractNumber"] = contractNumber,
                ["sampleName"] = sampleName,
                ["indicatorName"] = indicatorName,
                ["methodName"] = methodName,
                ["senderName"] = senderName
            };
        }
        else
        {
            // Case 1 QLCL: Multiple indicators
            message = $"{indicatorCount} kết quả từ {senderName}";
            type = NotificationType.MultipleResultsSubmittedToQM;
            additionalData = new Dictionary<string, object?>
            {
                ["contractNumber"] = contractNumber,
                ["indicatorCount"] = indicatorCount,
                ["senderName"] = senderName
            };
        }

        var notification = new Notification(
            _guidGenerator.Create(),
            qualityManagerId,
            title,
            message,
            type,
            qualityTaskId.ToString(),
            "QualityManagement",
            null,
            senderName,
            indicatorCount,
            SerializeAdditionalData(additionalData)
        );

        await InsertNotificationAndPushAsync(notification);
    }

    /// <summary>
    /// Case 3 QLCL: Nhắc nhở deadline trả kết quả <= 2 ngày
    /// </summary>
    public async Task NotifyDeadlineReminderAsync(
        Guid qualityManagerId,
        string contractNumber,
        Guid qualityTaskId,
        DateTime deadline)
    {
        var title = "Sắp đến hạn";
        var message = "Hợp đồng gần đến hạn trả kết quả";
        var additionalData = new Dictionary<string, object?>
        {
            ["contractNumber"] = contractNumber
        };

        var notification = new Notification(
            _guidGenerator.Create(),
            qualityManagerId,
            title,
            message,
            NotificationType.DeadlineReminder,
            qualityTaskId.ToString(),
            "QualityManagement",
            deadline,
            null,
            null,
            SerializeAdditionalData(additionalData)
        );

        await InsertNotificationAndPushAsync(notification);
    }

    /// <summary>
    /// Case 4 QLCL: Nhắc nhở bàn giao kết quả (sau khi duyệt >= 1 ngày)
    /// </summary>
    public async Task NotifyHandoverReminderAsync(
        Guid qualityManagerId,
        string contractNumber,
        Guid qualityTaskId,
        DateTime approvedDate)
    {
        var title = "Cần bàn giao";
        var message = "Hợp đồng cần bàn giao cho bộ phận nhận/trả mẫu";
        var additionalData = new Dictionary<string, object?>
        {
            ["contractNumber"] = contractNumber
        };

        var notification = new Notification(
            _guidGenerator.Create(),
            qualityManagerId,
            title,
            message,
            NotificationType.HandoverReminder,
            qualityTaskId.ToString(),
            "QualityManagement",
            null,
            null,
            null,
            SerializeAdditionalData(additionalData)
        );

        await InsertNotificationAndPushAsync(notification);
    }

    /// <summary>
    /// QLCL: Phiếu yêu cầu có thay đổi cần phê duyệt
    /// </summary>
    public async Task NotifyTestRequestChangePendingApprovalAsync(
        Guid qualityManagerId,
        string contractNumber,
        Guid testRequestId)
    {
        var title = "Thay đổi cần duyệt";
        var message = "Thay đổi phiếu yêu cầu cần phê duyệt";
        var additionalData = new Dictionary<string, object?>
        {
            ["contractNumber"] = contractNumber
        };

        var notification = new Notification(
            _guidGenerator.Create(),
            qualityManagerId,
            title,
            message,
            NotificationType.TestRequestChangePendingApproval,
            testRequestId.ToString(),
            "TestRequest",
            null,
            null,
            null,
            SerializeAdditionalData(additionalData)
        );

        await InsertNotificationAndPushAsync(notification);
    }

    #endregion

    #region Notifications for QLKT - CC (Thông báo CC cho QLKT)

    /// <summary>
    /// CC QLKT: Phiếu yêu cầu đang có điều chỉnh
    /// </summary>
    public async Task NotifyTestRequestChangeRequestedAsync(
        Guid technicalManagerId,
        string contractNumber,
        Guid testRequestId)
    {
        var title = "Phiếu đang điều chỉnh";
        var message = "Phiếu yêu cầu đang có điều chỉnh";
        var additionalData = new Dictionary<string, object?>
        {
            ["contractNumber"] = contractNumber
        };

        var notification = new Notification(
            _guidGenerator.Create(),
            technicalManagerId,
            title,
            message,
            NotificationType.TestRequestChangeRequested,
            testRequestId.ToString(),
            "TestRequest",
            null,
            null,
            null,
            SerializeAdditionalData(additionalData)
        );

        await InsertNotificationAndPushAsync(notification);
    }

    #endregion

    #region Notifications for SampleReceiver (Bộ phận Giao nhận mẫu)

    /// <summary>
    /// Case 1 SampleReceiver: QLCL phê duyệt thay đổi cho phiếu yêu cầu
    /// </summary>
    public async Task NotifyChangeApprovedAsync(
        Guid creatorId,
        string contractNumber,
        Guid testRequestId)
    {
        var title = "Thay đổi đã duyệt";
        var message = "Thay đổi phiếu đã được phê duyệt";
        var additionalData = new Dictionary<string, object?>
        {
            ["contractNumber"] = contractNumber
        };

        var notification = new Notification(
            _guidGenerator.Create(),
            creatorId,
            title,
            message,
            NotificationType.ChangeApproved,
            testRequestId.ToString(),
            "TestRequest",
            null,
            null,
            null,
            SerializeAdditionalData(additionalData)
        );

        await InsertNotificationAndPushAsync(notification);
    }

    /// <summary>
    /// Case 2 SampleReceiver: QLCL từ chối thay đổi cho phiếu yêu cầu
    /// </summary>
    public async Task NotifyChangeRejectedAsync(
        Guid creatorId,
        string contractNumber,
        Guid testRequestId)
    {
        var title = "Thay đổi bị từ chối";
        var message = "Thay đổi phiếu đã bị từ chối";
        var additionalData = new Dictionary<string, object?>
        {
            ["contractNumber"] = contractNumber
        };

        var notification = new Notification(
            _guidGenerator.Create(),
            creatorId,
            title,
            message,
            NotificationType.ChangeRejected,
            testRequestId.ToString(),
            "TestRequest",
            null,
            null,
            null,
            SerializeAdditionalData(additionalData)
        );

        await InsertNotificationAndPushAsync(notification);
    }

    /// <summary>
    /// Case 3 SampleReceiver: Phiếu yêu cầu mới cần gửi mẫu (Status chuyển sang Pending)
    /// </summary>
    public async Task NotifyNewTestRequestPendingAsync(
        Guid creatorId,
        string contractNumber,
        Guid testRequestId,
        int sampleCount)
    {
        var title = "Mẫu cần gửi";
        var message = $"{sampleCount} mẫu cần gửi tới khoa phòng";
        var additionalData = new Dictionary<string, object?>
        {
            ["contractNumber"] = contractNumber,
            ["sampleCount"] = sampleCount
        };

        var notification = new Notification(
            _guidGenerator.Create(),
            creatorId,
            title,
            message,
            NotificationType.NewTestRequestPending,
            testRequestId.ToString(),
            "TestRequest",
            null,
            null,
            sampleCount,
            SerializeAdditionalData(additionalData)
        );

        await InsertNotificationAndPushAsync(notification);
    }

    /// <summary>
    /// Case 4 SampleReceiver: Nhắc nhở trả mẫu cho khách hàng (Status = ResultSent)
    /// </summary>
    public async Task NotifySampleReturnReminderAsync(
        Guid creatorId,
        string contractNumber,
        Guid testRequestId)
    {
        var title = "Nhắc trả mẫu";
        var message = "Hợp đồng cần trả mẫu cho khách hàng";
        var additionalData = new Dictionary<string, object?>
        {
            ["contractNumber"] = contractNumber
        };

        var notification = new Notification(
            _guidGenerator.Create(),
            creatorId,
            title,
            message,
            NotificationType.SampleReturnReminder,
            testRequestId.ToString(),
            "TestRequest",
            null,
            null,
            null,
            SerializeAdditionalData(additionalData)
        );

        await InsertNotificationAndPushAsync(notification);
    }

    /// <summary>
    /// CC SampleReceiver: QLCL đã phê duyệt kết quả chỉ tiêu
    /// </summary>
    public async Task NotifyResultApprovedToSampleReceiverAsync(
        Guid sampleReceiverId,
        string contractNumber,
        Guid testRequestId,
        string qualityManagerName,
        string? sampleName = null,
        string? indicatorName = null,
        string? methodName = null)
    {
        var title = "Kết quả đã duyệt";
        string message;
        Dictionary<string, object?> additionalData;

        if (!string.IsNullOrEmpty(sampleName))
        {
            message = $"{sampleName} - {indicatorName} - {methodName}";
            additionalData = new Dictionary<string, object?>
            {
                ["contractNumber"] = contractNumber,
                ["sampleName"] = sampleName,
                ["indicatorName"] = indicatorName,
                ["methodName"] = methodName,
                ["senderName"] = qualityManagerName
            };
        }
        else
        {
            message = "Kết quả đã được phê duyệt";
            additionalData = new Dictionary<string, object?>
            {
                ["contractNumber"] = contractNumber,
                ["senderName"] = qualityManagerName
            };
        }

        var notification = new Notification(
            _guidGenerator.Create(),
            sampleReceiverId,
            title,
            message,
            NotificationType.ResultApprovedNotification,
            testRequestId.ToString(),
            "TestRequest",
            null,
            qualityManagerName,
            null,
            SerializeAdditionalData(additionalData)
        );

        await InsertNotificationAndPushAsync(notification);
    }

    #endregion

    #region Notifications for KTV (Technician/Thử nghiệm viên)

    /// <summary>
    /// Case 1 KTV: QLKT gán nhiều chỉ tiêu cho KTV thực hiện
    /// </summary>
    public async Task NotifyMultipleIndicatorsAssignedToTechnicianAsync(
        Guid technicianId,
        string contractNumber,
        Guid taskId,
        int indicatorCount,
        DateTime? deadline)
    {
        var title = "Công việc mới";
        var message = $"{indicatorCount} chỉ tiêu cần thực hiện";
        var additionalData = new Dictionary<string, object?>
        {
            ["contractNumber"] = contractNumber,
            ["indicatorCount"] = indicatorCount
        };

        var notification = new Notification(
            _guidGenerator.Create(),
            technicianId,
            title,
            message,
            NotificationType.MultipleIndicatorsAssignedToTechnician,
            taskId.ToString(),
            "Testing",
            deadline,
            null,
            indicatorCount,
            SerializeAdditionalData(additionalData)
        );

        await InsertNotificationAndPushAsync(notification);
    }

    /// <summary>
    /// Case 2 KTV: QLKT gán 1 chỉ tiêu cho KTV thực hiện
    /// </summary>
    public async Task NotifySingleIndicatorAssignedToTechnicianAsync(
        Guid technicianId,
        string contractNumber,
        Guid taskId,
        string technicalManagerName,
        string sampleName,
        string indicatorName,
        string methodName,
        DateTime? deadline)
    {
        var title = "Công việc mới";
        var message = $"{sampleName} - {indicatorName} - {methodName}";
        var additionalData = new Dictionary<string, object?>
        {
            ["contractNumber"] = contractNumber,
            ["sampleName"] = sampleName,
            ["indicatorName"] = indicatorName,
            ["methodName"] = methodName,
            ["senderName"] = technicalManagerName
        };

        var notification = new Notification(
            _guidGenerator.Create(),
            technicianId,
            title,
            message,
            NotificationType.SingleIndicatorAssignedToTechnician,
            taskId.ToString(),
            "Testing",
            deadline,
            technicalManagerName,
            1,
            SerializeAdditionalData(additionalData)
        );

        await InsertNotificationAndPushAsync(notification);
    }

    /// <summary>
    /// Case 3 KTV: Nhắc nhở có chỉ tiêu chưa thực hiện (>= 2 ngày)
    /// </summary>
    public async Task NotifyPendingIndicatorReminderAsync(
        Guid technicianId,
        string contractNumber,
        Guid taskId,
        int pendingIndicatorCount)
    {
        var title = "Nhắc nhở thực hiện";
        var message = $"{pendingIndicatorCount} chỉ tiêu chưa thực hiện";
        var additionalData = new Dictionary<string, object?>
        {
            ["contractNumber"] = contractNumber,
            ["indicatorCount"] = pendingIndicatorCount
        };

        var notification = new Notification(
            _guidGenerator.Create(),
            technicianId,
            title,
            message,
            NotificationType.PendingIndicatorReminder,
            taskId.ToString(),
            "Testing",
            null,
            null,
            pendingIndicatorCount,
            SerializeAdditionalData(additionalData)
        );

        await InsertNotificationAndPushAsync(notification);
    }

    /// <summary>
    /// Case 4 KTV: QLKT từ chối nhiều kết quả thử nghiệm của KTV
    /// </summary>
    public async Task NotifyMultipleResultsRejectedByTechnicalManagerAsync(
        Guid technicianId,
        string contractNumber,
        Guid taskId,
        string technicalManagerName)
    {
        var title = "Kết quả bị từ chối";
        var message = $"Kết quả bị {technicalManagerName} từ chối";
        var additionalData = new Dictionary<string, object?>
        {
            ["contractNumber"] = contractNumber,
            ["senderName"] = technicalManagerName
        };

        var notification = new Notification(
            _guidGenerator.Create(),
            technicianId,
            title,
            message,
            NotificationType.MultipleResultsRejectedByTechnicalManager,
            taskId.ToString(),
            "Testing",
            null,
            technicalManagerName,
            null,
            SerializeAdditionalData(additionalData)
        );

        await InsertNotificationAndPushAsync(notification);
    }

    /// <summary>
    /// Case 5 KTV: QLKT từ chối 1 kết quả thử nghiệm của KTV
    /// </summary>
    public async Task NotifySingleResultRejectedByTechnicalManagerAsync(
        Guid technicianId,
        string contractNumber,
        Guid taskId,
        string technicalManagerName,
        string sampleName,
        string indicatorName,
        string methodName)
    {
        var title = "Kết quả bị từ chối";
        var message = $"{sampleName} - {indicatorName} - {methodName}";
        var additionalData = new Dictionary<string, object?>
        {
            ["contractNumber"] = contractNumber,
            ["sampleName"] = sampleName,
            ["indicatorName"] = indicatorName,
            ["methodName"] = methodName,
            ["senderName"] = technicalManagerName
        };

        var notification = new Notification(
            _guidGenerator.Create(),
            technicianId,
            title,
            message,
            NotificationType.SingleResultRejectedByTechnicalManager,
            taskId.ToString(),
            "Testing",
            null,
            technicalManagerName,
            1,
            SerializeAdditionalData(additionalData)
        );

        await InsertNotificationAndPushAsync(notification);
    }

    /// <summary>
    /// Case 6 KTV: KTV báo không thể thử nghiệm chỉ tiêu (gửi cho QLKT)
    /// </summary>
    public async Task NotifyCannotTestAsync(
        Guid technicalManagerId,
        string contractNumber,
        Guid taskId,
        string technicianName,
        string sampleName,
        string indicatorName)
    {
        var title = "Báo không TN được";
        var message = $"{sampleName} - {indicatorName}";
        var additionalData = new Dictionary<string, object?>
        {
            ["contractNumber"] = contractNumber,
            ["sampleName"] = sampleName,
            ["indicatorName"] = indicatorName,
            ["senderName"] = technicianName
        };

        var notification = new Notification(
            _guidGenerator.Create(),
            technicalManagerId,
            title,
            message,
            NotificationType.CannotTest,
            taskId.ToString(),
            "LaboratoryTask",
            null,
            technicianName,
            null,
            SerializeAdditionalData(additionalData)
        );

        await InsertNotificationAndPushAsync(notification);
    }

    #endregion
}
