namespace LabSpecimenManagement.Notifications;

/// <summary>
/// Loại thông báo
/// </summary>
public enum NotificationType
{
    #region Notification Types for QLKT (LabTechnicalManager)

    /// <summary>
    /// Mẫu đã gửi đến khoa phòng - QLKT cần phân công
    /// </summary>
    SampleSentToLab = 0,

    /// <summary>
    /// Nhắc nhở chưa phân công công việc (>= 2 ngày)
    /// </summary>
    PendingWorkAssignment = 1,

    /// <summary>
    /// KTV gửi 1 chỉ tiêu cần QLKT phê duyệt
    /// </summary>
    SingleIndicatorSubmitted = 2,

    /// <summary>
    /// KTV gửi nhiều chỉ tiêu cần QLKT phê duyệt
    /// </summary>
    MultipleIndicatorsSubmitted = 3,

    /// <summary>
    /// QLCL từ chối 1 kết quả chỉ tiêu
    /// </summary>
    SingleResultRejectedByQM = 4,

    /// <summary>
    /// QLCL từ chối nhiều kết quả chỉ tiêu
    /// </summary>
    MultipleResultsRejectedByQM = 5,

    /// <summary>
    /// QLCL duyệt kết quả chỉ tiêu
    /// </summary>
    ResultApprovedByQM = 6,

    /// <summary>
    /// Có mẫu/chỉ tiêu mới thêm vào hợp đồng
    /// </summary>
    NewSamplesAdded = 7,

    /// <summary>
    /// Mẫu/chỉ tiêu bị hủy bỏ
    /// </summary>
    SamplesCancelled = 8,

    #endregion

    #region Notification Types for QLCL (QualityManager)

    /// <summary>
    /// QLKT gửi 1 kết quả chỉ tiêu cần QLCL phê duyệt
    /// </summary>
    SingleResultSubmittedToQM = 10,

    /// <summary>
    /// QLKT gửi nhiều kết quả chỉ tiêu cần QLCL phê duyệt
    /// </summary>
    MultipleResultsSubmittedToQM = 11,

    /// <summary>
    /// Nhắc nhở deadline trả kết quả <= 2 ngày
    /// </summary>
    DeadlineReminder = 12,

    /// <summary>
    /// Nhắc nhở bàn giao kết quả (sau khi duyệt >= 1 ngày)
    /// </summary>
    HandoverReminder = 13,

    #endregion

    /// <summary>
    /// Phiếu yêu cầu có thay đổi cần phê duyệt (CC cho QLKT)
    /// </summary>
    TestRequestChangeRequested = 9,

    #region Notification Types for QLCL (QualityManager) - ChangeApproval

    /// <summary>
    /// Phiếu yêu cầu có thay đổi cần QLCL phê duyệt
    /// </summary>
    TestRequestChangePendingApproval = 14,

    #endregion

    #region Notification Types for SampleReceiver (Bộ phận Giao nhận mẫu)

    /// <summary>
    /// QLCL phê duyệt thay đổi cho phiếu yêu cầu
    /// </summary>
    ChangeApproved = 20,

    /// <summary>
    /// QLCL từ chối thay đổi cho phiếu yêu cầu
    /// </summary>
    ChangeRejected = 21,

    /// <summary>
    /// Phiếu yêu cầu mới cần gửi mẫu tới khoa phòng (Status = Pending)
    /// </summary>
    NewTestRequestPending = 22,

    /// <summary>
    /// Nhắc nhở trả mẫu cho khách hàng (Status = ResultSent)
    /// </summary>
    SampleReturnReminder = 23,

    /// <summary>
    /// CC: QLCL đã phê duyệt kết quả chỉ tiêu (gửi cho SampleReceiver)
    /// </summary>
    ResultApprovedNotification = 24,

    #endregion

    #region Notification Types for KTV (Technician/Thử nghiệm viên)

    /// <summary>
    /// QLKT gán 1 chỉ tiêu cho KTV thực hiện
    /// </summary>
    SingleIndicatorAssignedToTechnician = 30,

    /// <summary>
    /// QLKT gán nhiều chỉ tiêu cho KTV thực hiện
    /// </summary>
    MultipleIndicatorsAssignedToTechnician = 31,

    /// <summary>
    /// Nhắc nhở KTV có chỉ tiêu chưa thực hiện (>= 2 ngày)
    /// </summary>
    PendingIndicatorReminder = 32,

    /// <summary>
    /// QLKT từ chối 1 kết quả thử nghiệm của KTV
    /// </summary>
    SingleResultRejectedByTechnicalManager = 33,

    /// <summary>
    /// QLKT từ chối nhiều kết quả thử nghiệm của KTV
    /// </summary>
    MultipleResultsRejectedByTechnicalManager = 34,

    /// <summary>
    /// KTV báo không thể thử nghiệm chỉ tiêu (gửi cho QLKT)
    /// </summary>
    CannotTest = 35,

    #endregion

    #region Notification Types for GoodsReceipt Approval (Phê duyệt phiếu đặt hàng)

    /// <summary>
    /// Phiếu đặt hàng cần phê duyệt (gửi cho người duyệt)
    /// </summary>
    GoodsReceiptPendingApproval = 40,

    /// <summary>
    /// Phiếu đặt hàng đã được duyệt (gửi cho người tạo phiếu)
    /// </summary>
    GoodsReceiptApproved = 41,

    /// <summary>
    /// Phiếu đặt hàng bị từ chối (gửi cho người tạo phiếu)
    /// </summary>
    GoodsReceiptRejected = 42

    #endregion
}
