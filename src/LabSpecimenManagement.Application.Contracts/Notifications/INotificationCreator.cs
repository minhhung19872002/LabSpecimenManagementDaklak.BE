using System;
using System.Threading.Tasks;

namespace LabSpecimenManagement.Notifications;

/// <summary>
/// Interface for creating notifications for specific business events
/// </summary>
public interface INotificationCreator
{
    #region Notifications for QLKT (LabTechnicalManager)

    /// <summary>
    /// Case 1 & 6: KTV gửi chỉ tiêu cần QLKT phê duyệt
    /// </summary>
    Task NotifyIndicatorSubmittedToTechnicalManagerAsync(
        Guid technicalManagerId,
        string contractNumber,
        Guid testRequestId,
        string senderName,
        int indicatorCount,
        DateTime? deadline,
        string? sampleName = null,
        string? indicatorName = null,
        string? methodName = null);

    /// <summary>
    /// Case 2: Phiếu yêu cầu chuyển trạng thái "Đã gửi mẫu"
    /// </summary>
    Task NotifySampleSentToLabAsync(
        Guid technicalManagerId,
        string contractNumber,
        Guid testRequestId,
        int indicatorCount,
        DateTime? deadline);

    /// <summary>
    /// Case 3: Reminder - chưa phân công công việc (>= 2 ngày)
    /// </summary>
    Task NotifyPendingWorkAssignmentAsync(
        Guid technicalManagerId,
        string contractNumber,
        Guid testRequestId,
        int pendingIndicatorCount,
        DateTime sentDate);

    /// <summary>
    /// Case 4 & 5: QLCL từ chối kết quả
    /// </summary>
    Task NotifyResultRejectedByQualityManagerAsync(
        Guid technicalManagerId,
        string contractNumber,
        Guid testRequestId,
        string qualityManagerName,
        int rejectedCount,
        string? sampleName = null,
        string? indicatorName = null,
        string? methodName = null);

    /// <summary>
    /// Case 7: QLCL duyệt kết quả
    /// </summary>
    Task NotifyResultApprovedByQualityManagerAsync(
        Guid technicalManagerId,
        string contractNumber,
        Guid testRequestId,
        string qualityManagerName,
        string? sampleName = null,
        string? indicatorName = null,
        string? methodName = null);

    /// <summary>
    /// Case 8.1: Có mẫu/chỉ tiêu mới thêm vào hợp đồng
    /// </summary>
    Task NotifyNewSamplesAddedAsync(
        Guid technicalManagerId,
        string contractNumber,
        Guid testRequestId,
        int newSampleCount,
        int newIndicatorCount);

    /// <summary>
    /// Case 8.2: Mẫu/chỉ tiêu bị hủy
    /// </summary>
    Task NotifySamplesCancelledAsync(
        Guid technicalManagerId,
        string contractNumber,
        Guid testRequestId,
        string sampleCode,
        int cancelledIndicatorCount);

    #endregion

    #region Notifications for QLCL (QualityManager)

    /// <summary>
    /// Case 1 & 2 QLCL: QLKT gửi kết quả chỉ tiêu cần QLCL phê duyệt
    /// </summary>
    Task NotifyResultSubmittedToQualityManagerAsync(
        Guid qualityManagerId,
        string contractNumber,
        Guid testRequestId,
        string senderName,
        int indicatorCount,
        string? sampleName = null,
        string? indicatorName = null,
        string? methodName = null);

    /// <summary>
    /// Case 3 QLCL: Nhắc nhở deadline trả kết quả <= 2 ngày
    /// </summary>
    Task NotifyDeadlineReminderAsync(
        Guid qualityManagerId,
        string contractNumber,
        Guid testRequestId,
        DateTime deadline);

    /// <summary>
    /// Case 4 QLCL: Nhắc nhở bàn giao kết quả (sau khi duyệt >= 1 ngày)
    /// </summary>
    Task NotifyHandoverReminderAsync(
        Guid qualityManagerId,
        string contractNumber,
        Guid testRequestId,
        DateTime approvedDate);

    #endregion
}
