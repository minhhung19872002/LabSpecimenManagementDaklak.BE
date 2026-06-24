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
        Guid taskId,
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
        Guid taskId,
        int indicatorCount,
        DateTime? deadline);

    /// <summary>
    /// Case 3: Reminder - chưa phân công công việc (>= 2 ngày)
    /// </summary>
    Task NotifyPendingWorkAssignmentAsync(
        Guid technicalManagerId,
        string contractNumber,
        Guid taskId,
        int pendingIndicatorCount,
        DateTime sentDate);

    /// <summary>
    /// Case 4 & 5: QLCL từ chối kết quả
    /// </summary>
    Task NotifyResultRejectedByQualityManagerAsync(
        Guid technicalManagerId,
        string contractNumber,
        Guid taskId,
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
        Guid taskId,
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
        Guid taskId,
        int newSampleCount,
        int newIndicatorCount);

    /// <summary>
    /// Case 8.2: Mẫu/chỉ tiêu bị hủy
    /// </summary>
    Task NotifySamplesCancelledAsync(
        Guid technicalManagerId,
        string contractNumber,
        Guid taskId,
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
        Guid qualityTaskId,
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
        Guid qualityTaskId,
        DateTime deadline);

    /// <summary>
    /// Case 4 QLCL: Nhắc nhở bàn giao kết quả (sau khi duyệt >= 1 ngày)
    /// </summary>
    Task NotifyHandoverReminderAsync(
        Guid qualityManagerId,
        string contractNumber,
        Guid qualityTaskId,
        DateTime approvedDate);

    /// <summary>
    /// QLCL: Phiếu yêu cầu có thay đổi cần phê duyệt
    /// </summary>
    Task NotifyTestRequestChangePendingApprovalAsync(
        Guid qualityManagerId,
        string contractNumber,
        Guid testRequestId);

    #endregion

    #region Notifications for QLKT - CC (Thông báo CC cho QLKT)

    /// <summary>
    /// CC QLKT: Phiếu yêu cầu đang có điều chỉnh
    /// </summary>
    Task NotifyTestRequestChangeRequestedAsync(
        Guid technicalManagerId,
        string contractNumber,
        Guid testRequestId);

    #endregion

    #region Notifications for SampleReceiver (Bộ phận Giao nhận mẫu)

    /// <summary>
    /// Case 1 SampleReceiver: QLCL phê duyệt thay đổi cho phiếu yêu cầu
    /// </summary>
    Task NotifyChangeApprovedAsync(
        Guid creatorId,
        string contractNumber,
        Guid testRequestId);

    /// <summary>
    /// Case 2 SampleReceiver: QLCL từ chối thay đổi cho phiếu yêu cầu
    /// </summary>
    Task NotifyChangeRejectedAsync(
        Guid creatorId,
        string contractNumber,
        Guid testRequestId);

    /// <summary>
    /// Case 3 SampleReceiver: Phiếu yêu cầu mới cần gửi mẫu (Status chuyển sang Pending)
    /// </summary>
    Task NotifyNewTestRequestPendingAsync(
        Guid creatorId,
        string contractNumber,
        Guid testRequestId,
        int sampleCount);

    /// <summary>
    /// Case 4 SampleReceiver: Nhắc nhở trả mẫu cho khách hàng (Status = ResultSent)
    /// </summary>
    Task NotifySampleReturnReminderAsync(
        Guid creatorId,
        string contractNumber,
        Guid testRequestId);

    /// <summary>
    /// CC SampleReceiver: QLCL đã phê duyệt kết quả chỉ tiêu
    /// </summary>
    Task NotifyResultApprovedToSampleReceiverAsync(
        Guid sampleReceiverId,
        string contractNumber,
        Guid testRequestId,
        string qualityManagerName,
        string? sampleName = null,
        string? indicatorName = null,
        string? methodName = null);

    #endregion

    #region Notifications for KTV (Technician/Thử nghiệm viên)

    /// <summary>
    /// Case 1 KTV: QLKT gán nhiều chỉ tiêu cho KTV thực hiện
    /// </summary>
    Task NotifyMultipleIndicatorsAssignedToTechnicianAsync(
        Guid technicianId,
        string contractNumber,
        Guid taskId,
        int indicatorCount,
        DateTime? deadline);

    /// <summary>
    /// Case 2 KTV: QLKT gán 1 chỉ tiêu cho KTV thực hiện
    /// </summary>
    Task NotifySingleIndicatorAssignedToTechnicianAsync(
        Guid technicianId,
        string contractNumber,
        Guid taskId,
        string technicalManagerName,
        string sampleName,
        string indicatorName,
        string methodName,
        DateTime? deadline);

    /// <summary>
    /// Case 3 KTV: Nhắc nhở có chỉ tiêu chưa thực hiện (>= 2 ngày)
    /// </summary>
    Task NotifyPendingIndicatorReminderAsync(
        Guid technicianId,
        string contractNumber,
        Guid taskId,
        int pendingIndicatorCount);

    /// <summary>
    /// Case 4 KTV: QLKT từ chối nhiều kết quả thử nghiệm của KTV
    /// </summary>
    Task NotifyMultipleResultsRejectedByTechnicalManagerAsync(
        Guid technicianId,
        string contractNumber,
        Guid taskId,
        string technicalManagerName);

    /// <summary>
    /// Case 5 KTV: QLKT từ chối 1 kết quả thử nghiệm của KTV
    /// </summary>
    Task NotifySingleResultRejectedByTechnicalManagerAsync(
        Guid technicianId,
        string contractNumber,
        Guid taskId,
        string technicalManagerName,
        string sampleName,
        string indicatorName,
        string methodName);

    /// <summary>
    /// Case 6 KTV: KTV báo không thể thử nghiệm chỉ tiêu (gửi cho QLKT)
    /// </summary>
    Task NotifyCannotTestAsync(
        Guid technicalManagerId,
        string contractNumber,
        Guid taskId,
        string technicianName,
        string sampleName,
        string indicatorName);

    #endregion

    #region Notifications for GoodsReceipt Approval (Phê duyệt phiếu đặt hàng)

    Task NotifyGoodsReceiptPendingApprovalAsync(
        Guid approverId,
        Guid goodsReceiptId,
        string goodsReceiptCode,
        string creatorName);

    Task NotifyGoodsReceiptApprovedAsync(
        Guid creatorId,
        Guid goodsReceiptId,
        string goodsReceiptCode);

    Task NotifyGoodsReceiptRejectedAsync(
        Guid creatorId,
        Guid goodsReceiptId,
        string goodsReceiptCode);

    Task UpdateGoodsReceiptNotificationStatusAsync(
        Guid goodsReceiptId,
        Guid approverId,
        string newStatus);

    #endregion
}
