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
    HandoverReminder = 13

    #endregion
}
