namespace LabSpecimenManagement.Notifications;

/// <summary>
/// Loại thông báo
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Yêu cầu phê duyệt thay đổi phiếu
    /// </summary>
    ChangeApprovalRequest = 0,

    /// <summary>
    /// Thay đổi được duyệt
    /// </summary>
    ChangeApproved = 1,

    /// <summary>
    /// Thay đổi bị từ chối
    /// </summary>
    ChangeRejected = 2,

    /// <summary>
    /// Yêu cầu phê duyệt kết quả
    /// </summary>
    ResultApprovalRequest = 3,

    /// <summary>
    /// Kết quả được duyệt
    /// </summary>
    ResultApproved = 4,

    /// <summary>
    /// Kết quả bị từ chối
    /// </summary>
    ResultRejected = 5,

    /// <summary>
    /// Có phiếu yêu cầu mới
    /// </summary>
    NewTestRequest = 6,

    /// <summary>
    /// Nhắc nhở deadline
    /// </summary>
    DeadlineReminder = 7
}
