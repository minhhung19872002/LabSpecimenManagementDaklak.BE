using System;
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
        Guid testRequestId,
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

        if (indicatorCount == 1 && !string.IsNullOrEmpty(sampleName))
        {
            // Case 6: Single indicator
            title = $"Hợp đồng {contractNumber}";
            message = $"<b>Hợp đồng {contractNumber}</b>\n" +
                      $"{sampleName} - {indicatorName} - {methodName}\n" +
                      $"{senderName} đã gửi kết quả chỉ tiêu cần bạn phê duyệt";
            type = NotificationType.SingleIndicatorSubmitted;
        }
        else
        {
            // Case 1: Multiple indicators
            title = $"Hợp đồng {contractNumber}";
            message = $"<b>Hợp đồng {contractNumber}</b>\n" +
                      $"{senderName} đã gửi <b>{indicatorCount} chỉ tiêu</b> cần bạn phê duyệt";
            type = NotificationType.MultipleIndicatorsSubmitted;
        }

        var notification = new Notification(
            _guidGenerator.Create(),
            technicalManagerId,
            title,
            message,
            type,
            testRequestId.ToString(),
            "LaboratoryTask",
            deadline,
            senderName,
            indicatorCount
        );

        await InsertNotificationAndPushAsync(notification);
    }

    /// <summary>
    /// Case 2: Phiếu yêu cầu chuyển trạng thái "Đã gửi mẫu"
    /// </summary>
    public async Task NotifySampleSentToLabAsync(
        Guid technicalManagerId,
        string contractNumber,
        Guid testRequestId,
        int indicatorCount,
        DateTime? deadline)
    {
        var title = $"Hợp đồng {contractNumber}";
        var message = $"<b>Hợp đồng {contractNumber}</b>\n" +
                      $"Bạn có <b>{indicatorCount} chỉ tiêu</b> cần khoa phòng của bạn thử nghiệm";

        var notification = new Notification(
            _guidGenerator.Create(),
            technicalManagerId,
            title,
            message,
            NotificationType.SampleSentToLab,
            testRequestId.ToString(),
            "LaboratoryTask",
            deadline,
            null,
            indicatorCount
        );

        await InsertNotificationAndPushAsync(notification);
    }

    /// <summary>
    /// Case 3: Reminder - chưa phân công công việc (>= 2 ngày)
    /// </summary>
    public async Task NotifyPendingWorkAssignmentAsync(
        Guid technicalManagerId,
        string contractNumber,
        Guid testRequestId,
        int pendingIndicatorCount,
        DateTime sentDate)
    {
        var title = $"Hợp đồng {contractNumber}";
        var message = $"<b>Hợp đồng {contractNumber}</b>\n" +
                      $"Bạn có <b>{pendingIndicatorCount} chỉ tiêu</b> chưa phân công công việc";

        var notification = new Notification(
            _guidGenerator.Create(),
            technicalManagerId,
            title,
            message,
            NotificationType.PendingWorkAssignment,
            testRequestId.ToString(),
            "LaboratoryTask",
            null,
            null,
            pendingIndicatorCount
        );

        await InsertNotificationAndPushAsync(notification);
    }

    /// <summary>
    /// Case 4 & 5: QLCL từ chối kết quả
    /// </summary>
    public async Task NotifyResultRejectedByQualityManagerAsync(
        Guid technicalManagerId,
        string contractNumber,
        Guid testRequestId,
        string qualityManagerName,
        int rejectedCount,
        string? sampleName = null,
        string? indicatorName = null,
        string? methodName = null)
    {
        string title = $"Hợp đồng {contractNumber}";
        string message;
        NotificationType type;

        if (rejectedCount == 1 && !string.IsNullOrEmpty(sampleName))
        {
            // Case 4: Single rejection
            message = $"<b>Hợp đồng {contractNumber}</b>\n" +
                      $"{sampleName} - {indicatorName} - {methodName}\n" +
                      $"Quản lý chất lượng <b>{qualityManagerName}</b> đã từ chối kết quả của bạn";
            type = NotificationType.SingleResultRejectedByQM;
        }
        else
        {
            // Case 5: Multiple rejections
            message = $"<b>Hợp đồng {contractNumber}</b>\n" +
                      $"Quản lý chất lượng <b>{qualityManagerName}</b> đã từ chối kết quả của bạn";
            type = NotificationType.MultipleResultsRejectedByQM;
        }

        var notification = new Notification(
            _guidGenerator.Create(),
            technicalManagerId,
            title,
            message,
            type,
            testRequestId.ToString(),
            "QualityManagement",
            null,
            qualityManagerName,
            rejectedCount
        );

        await InsertNotificationAndPushAsync(notification);
    }

    /// <summary>
    /// Case 7: QLCL duyệt kết quả
    /// </summary>
    public async Task NotifyResultApprovedByQualityManagerAsync(
        Guid technicalManagerId,
        string contractNumber,
        Guid testRequestId,
        string qualityManagerName,
        string? sampleName = null,
        string? indicatorName = null,
        string? methodName = null)
    {
        var title = $"Hợp đồng {contractNumber}";
        string message;

        if (!string.IsNullOrEmpty(sampleName))
        {
            message = $"<b>Hợp đồng {contractNumber}</b>\n" +
                      $"{sampleName} - {indicatorName} - {methodName}\n" +
                      $"Kết quả của bạn đã được Quản lý chất lượng <b>{qualityManagerName}</b> phê duyệt";
        }
        else
        {
            message = $"<b>Hợp đồng {contractNumber}</b>\n" +
                      $"Kết quả của bạn đã được Quản lý chất lượng <b>{qualityManagerName}</b> phê duyệt";
        }

        var notification = new Notification(
            _guidGenerator.Create(),
            technicalManagerId,
            title,
            message,
            NotificationType.ResultApprovedByQM,
            testRequestId.ToString(),
            "QualityManagement",
            null,
            qualityManagerName
        );

        await InsertNotificationAndPushAsync(notification);
    }

    /// <summary>
    /// Case 8.1: Có mẫu/chỉ tiêu mới thêm vào hợp đồng
    /// </summary>
    public async Task NotifyNewSamplesAddedAsync(
        Guid technicalManagerId,
        string contractNumber,
        Guid testRequestId,
        int newSampleCount,
        int newIndicatorCount)
    {
        var title = $"Hợp đồng {contractNumber}";
        var message = $"<b>Hợp đồng {contractNumber}</b>\n" +
                      $"Bạn có thêm {newSampleCount} mẫu và {newIndicatorCount} chỉ tiêu cần thực hiện thêm";

        var notification = new Notification(
            _guidGenerator.Create(),
            technicalManagerId,
            title,
            message,
            NotificationType.NewSamplesAdded,
            testRequestId.ToString(),
            "TestRequest",
            null,
            null,
            newIndicatorCount
        );

        await InsertNotificationAndPushAsync(notification);
    }

    /// <summary>
    /// Case 8.2: Mẫu/chỉ tiêu bị hủy
    /// </summary>
    public async Task NotifySamplesCancelledAsync(
        Guid technicalManagerId,
        string contractNumber,
        Guid testRequestId,
        string sampleCode,
        int cancelledIndicatorCount)
    {
        var title = $"Hợp đồng {contractNumber}";
        var message = $"<b>Hợp đồng {contractNumber}</b>\n" +
                      $"Khách hàng đã hủy {cancelledIndicatorCount} chỉ tiêu của mẫu {sampleCode}";

        var notification = new Notification(
            _guidGenerator.Create(),
            technicalManagerId,
            title,
            message,
            NotificationType.SamplesCancelled,
            testRequestId.ToString(),
            "TestRequest",
            null,
            null,
            cancelledIndicatorCount
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
        Guid testRequestId,
        string senderName,
        int indicatorCount,
        string? sampleName = null,
        string? indicatorName = null,
        string? methodName = null)
    {
        string title = $"Hợp đồng {contractNumber}";
        string message;
        NotificationType type;

        if (indicatorCount == 1 && !string.IsNullOrEmpty(sampleName))
        {
            // Case 2 QLCL: Single indicator
            message = $"<b>Hợp đồng {contractNumber}</b>\n" +
                      $"{sampleName} - {indicatorName} - {methodName}\n" +
                      $"{senderName} đã gửi kết quả chỉ tiêu cần bạn phê duyệt";
            type = NotificationType.SingleResultSubmittedToQM;
        }
        else
        {
            // Case 1 QLCL: Multiple indicators
            message = $"<b>Hợp đồng {contractNumber}</b>\n" +
                      $"{senderName} đã gửi <b>{indicatorCount} kết quả chỉ tiêu</b> cần bạn phê duyệt";
            type = NotificationType.MultipleResultsSubmittedToQM;
        }

        var notification = new Notification(
            _guidGenerator.Create(),
            qualityManagerId,
            title,
            message,
            type,
            testRequestId.ToString(),
            "QualityManagement",
            null,
            senderName,
            indicatorCount
        );

        await InsertNotificationAndPushAsync(notification);
    }

    /// <summary>
    /// Case 3 QLCL: Nhắc nhở deadline trả kết quả <= 2 ngày
    /// </summary>
    public async Task NotifyDeadlineReminderAsync(
        Guid qualityManagerId,
        string contractNumber,
        Guid testRequestId,
        DateTime deadline)
    {
        var title = $"Hợp đồng {contractNumber}";
        var message = $"<b>Hợp đồng {contractNumber}</b>\n" +
                      $"Hợp đồng {contractNumber} gần tới hạn yêu cầu trả kết quả";

        var notification = new Notification(
            _guidGenerator.Create(),
            qualityManagerId,
            title,
            message,
            NotificationType.DeadlineReminder,
            testRequestId.ToString(),
            "QualityManagement",
            deadline
        );

        await InsertNotificationAndPushAsync(notification);
    }

    /// <summary>
    /// Case 4 QLCL: Nhắc nhở bàn giao kết quả (sau khi duyệt >= 1 ngày)
    /// </summary>
    public async Task NotifyHandoverReminderAsync(
        Guid qualityManagerId,
        string contractNumber,
        Guid testRequestId,
        DateTime approvedDate)
    {
        var title = $"Hợp đồng {contractNumber}";
        var message = $"<b>Hợp đồng {contractNumber}</b>\n" +
                      $"Hợp đồng {contractNumber} cần bàn giao cho bộ phận nhận/ trả mẫu";

        var notification = new Notification(
            _guidGenerator.Create(),
            qualityManagerId,
            title,
            message,
            NotificationType.HandoverReminder,
            testRequestId.ToString(),
            "TestRequest",
            null
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
        var title = $"Hợp đồng {contractNumber}";
        var message = $"<b>Hợp đồng {contractNumber}</b>\n" +
                      $"Thay đổi cho hợp đồng {contractNumber} cần được phê duyệt";

        var notification = new Notification(
            _guidGenerator.Create(),
            qualityManagerId,
            title,
            message,
            NotificationType.TestRequestChangePendingApproval,
            testRequestId.ToString(),
            "QualityManagement",
            null
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
        var title = $"Hợp đồng {contractNumber}";
        var message = $"<b>Hợp đồng {contractNumber}</b>\n" +
                      $"Phiếu yêu cầu {contractNumber} đang có điều chỉnh";

        var notification = new Notification(
            _guidGenerator.Create(),
            technicalManagerId,
            title,
            message,
            NotificationType.TestRequestChangeRequested,
            testRequestId.ToString(),
            "TestRequest",
            null
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
        var title = $"Hợp đồng {contractNumber}";
        var message = $"<b>Hợp đồng {contractNumber}</b>\n" +
                      $"Thay đổi cho hợp đồng {contractNumber} đã được phê duyệt";

        var notification = new Notification(
            _guidGenerator.Create(),
            creatorId,
            title,
            message,
            NotificationType.ChangeApproved,
            testRequestId.ToString(),
            "TestRequest",
            null
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
        var title = $"Hợp đồng {contractNumber}";
        var message = $"<b>Hợp đồng {contractNumber}</b>\n" +
                      $"Thay đổi cho hợp đồng {contractNumber} đã bị từ chối";

        var notification = new Notification(
            _guidGenerator.Create(),
            creatorId,
            title,
            message,
            NotificationType.ChangeRejected,
            testRequestId.ToString(),
            "TestRequest",
            null
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
        var title = $"Hợp đồng {contractNumber}";
        var message = $"<b>Hợp đồng {contractNumber}</b>\n" +
                      $"Bạn có <b>{sampleCount} mẫu</b> cần gửi tới khoa phòng thử nghiệm";

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
            sampleCount
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
        var title = $"Hợp đồng {contractNumber}";
        var message = $"<b>Hợp đồng {contractNumber}</b>\n" +
                      $"Hợp đồng {contractNumber} cần trả mẫu cho khách hàng";

        var notification = new Notification(
            _guidGenerator.Create(),
            creatorId,
            title,
            message,
            NotificationType.SampleReturnReminder,
            testRequestId.ToString(),
            "TestRequest",
            null
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
        var title = $"Hợp đồng {contractNumber}";
        string message;

        if (!string.IsNullOrEmpty(sampleName))
        {
            message = $"<b>Hợp đồng {contractNumber}</b>\n" +
                      $"{sampleName} - {indicatorName} - {methodName}\n" +
                      $"Kết quả đã được Quản lý chất lượng <b>{qualityManagerName}</b> phê duyệt";
        }
        else
        {
            message = $"<b>Hợp đồng {contractNumber}</b>\n" +
                      $"Kết quả đã được Quản lý chất lượng <b>{qualityManagerName}</b> phê duyệt";
        }

        var notification = new Notification(
            _guidGenerator.Create(),
            sampleReceiverId,
            title,
            message,
            NotificationType.ResultApprovedNotification,
            testRequestId.ToString(),
            "QualityManagement",
            null,
            qualityManagerName
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
        Guid testRequestId,
        int indicatorCount,
        DateTime? deadline)
    {
        var title = $"Hợp đồng {contractNumber}";
        var message = $"<b>Hợp đồng {contractNumber}</b>\n" +
                      $"Bạn có <b>{indicatorCount} chỉ tiêu</b> cần thực hiện";

        var notification = new Notification(
            _guidGenerator.Create(),
            technicianId,
            title,
            message,
            NotificationType.MultipleIndicatorsAssignedToTechnician,
            testRequestId.ToString(),
            "TestRequest",
            deadline
        );

        await InsertNotificationAndPushAsync(notification);
    }

    /// <summary>
    /// Case 2 KTV: QLKT gán 1 chỉ tiêu cho KTV thực hiện
    /// </summary>
    public async Task NotifySingleIndicatorAssignedToTechnicianAsync(
        Guid technicianId,
        string contractNumber,
        Guid testRequestId,
        string technicalManagerName,
        string sampleName,
        string indicatorName,
        string methodName,
        DateTime? deadline)
    {
        var title = $"Hợp đồng {contractNumber}";
        var message = $"<b>Hợp đồng {contractNumber}</b>\n" +
                      $"Quản lý kỹ thuật {technicalManagerName} phân công cho bạn thử nghiệm {sampleName} - {indicatorName} - {methodName}";

        var notification = new Notification(
            _guidGenerator.Create(),
            technicianId,
            title,
            message,
            NotificationType.SingleIndicatorAssignedToTechnician,
            testRequestId.ToString(),
            "TestRequest",
            deadline
        );

        await InsertNotificationAndPushAsync(notification);
    }

    /// <summary>
    /// Case 3 KTV: Nhắc nhở có chỉ tiêu chưa thực hiện (>= 2 ngày)
    /// </summary>
    public async Task NotifyPendingIndicatorReminderAsync(
        Guid technicianId,
        string contractNumber,
        Guid testRequestId,
        int pendingIndicatorCount)
    {
        var title = $"Hợp đồng {contractNumber}";
        var message = $"<b>Hợp đồng {contractNumber}</b>\n" +
                      $"Bạn có <b>{pendingIndicatorCount} chỉ tiêu</b> chưa thực hiện";

        var notification = new Notification(
            _guidGenerator.Create(),
            technicianId,
            title,
            message,
            NotificationType.PendingIndicatorReminder,
            testRequestId.ToString(),
            "TestRequest",
            null
        );

        await InsertNotificationAndPushAsync(notification);
    }

    /// <summary>
    /// Case 4 KTV: QLKT từ chối nhiều kết quả thử nghiệm của KTV
    /// </summary>
    public async Task NotifyMultipleResultsRejectedByTechnicalManagerAsync(
        Guid technicianId,
        string contractNumber,
        Guid testRequestId,
        string technicalManagerName)
    {
        var title = $"Hợp đồng {contractNumber}";
        var message = $"<b>Hợp đồng {contractNumber}</b>\n" +
                      $"Quản lý kỹ thuật {technicalManagerName} đã từ chối kết quả của bạn";

        var notification = new Notification(
            _guidGenerator.Create(),
            technicianId,
            title,
            message,
            NotificationType.MultipleResultsRejectedByTechnicalManager,
            testRequestId.ToString(),
            "TestRequest",
            null
        );

        await InsertNotificationAndPushAsync(notification);
    }

    /// <summary>
    /// Case 5 KTV: QLKT từ chối 1 kết quả thử nghiệm của KTV
    /// </summary>
    public async Task NotifySingleResultRejectedByTechnicalManagerAsync(
        Guid technicianId,
        string contractNumber,
        Guid testRequestId,
        string technicalManagerName,
        string sampleName,
        string indicatorName,
        string methodName)
    {
        var title = $"Hợp đồng {contractNumber}";
        var message = $"<b>Hợp đồng {contractNumber}</b>\n" +
                      $"{sampleName} - {indicatorName} - {methodName}\n" +
                      $"{technicalManagerName} đã từ chối kết quả của bạn";

        var notification = new Notification(
            _guidGenerator.Create(),
            technicianId,
            title,
            message,
            NotificationType.SingleResultRejectedByTechnicalManager,
            testRequestId.ToString(),
            "TestRequest",
            null
        );

        await InsertNotificationAndPushAsync(notification);
    }

    #endregion
}
