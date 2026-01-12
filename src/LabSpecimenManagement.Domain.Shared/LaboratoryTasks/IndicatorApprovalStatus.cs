namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// Trạng thái duyệt của chỉ tiêu trong công việc khoa phòng
/// </summary>
public enum IndicatorApprovalStatus
{
    /// <summary>
    /// Chờ nhập kết quả / Chờ QLKT đánh giá - Pending
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Đã nhập kết quả - Entered
    /// </summary>
    Entered = 1,

    /// <summary>
    /// Đã duyệt - Approved (QLKT duyệt kết quả)
    /// </summary>
    Approved = 2,

    /// <summary>
    /// Không duyệt (Từ chối kết quả) - Rejected
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// Khách hàng hủy - CustomerCancelled
    /// </summary>
    CustomerCancelled = 4,

    /// <summary>
    /// Yêu cầu đổi phương pháp - QLKT yêu cầu đổi PP, chờ KH xác nhận
    /// </summary>
    MethodChangeRequested = 5,

    /// <summary>
    /// Đã đổi phương pháp - KH đã xác nhận đổi PP
    /// </summary>
    MethodChanged = 6,

    /// <summary>
    /// Từ chối thử nghiệm - QLKT từ chối TN, KH đã xác nhận
    /// </summary>
    TestingRejected = 7,

    /// <summary>
    /// Chấp nhận thử nghiệm - QLKT chấp nhận chỉ tiêu này để TN
    /// </summary>
    AcceptedForTesting = 8,

    /// <summary>
    /// Không TN được - KTV báo không thể thử nghiệm
    /// </summary>
    CannotTest = 9
}
