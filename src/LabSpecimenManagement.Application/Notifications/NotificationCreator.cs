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

    public NotificationCreator(
        IRepository<Notification, Guid> notificationRepository,
        IGuidGenerator guidGenerator)
    {
        _notificationRepository = notificationRepository;
        _guidGenerator = guidGenerator;
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

        await _notificationRepository.InsertAsync(notification);
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

        await _notificationRepository.InsertAsync(notification);
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

        await _notificationRepository.InsertAsync(notification);
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

        await _notificationRepository.InsertAsync(notification);
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

        await _notificationRepository.InsertAsync(notification);
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

        await _notificationRepository.InsertAsync(notification);
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

        await _notificationRepository.InsertAsync(notification);
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

        await _notificationRepository.InsertAsync(notification);
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

        await _notificationRepository.InsertAsync(notification);
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

        await _notificationRepository.InsertAsync(notification);
    }

    #endregion
}
