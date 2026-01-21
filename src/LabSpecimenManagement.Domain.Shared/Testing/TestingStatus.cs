namespace LabSpecimenManagement.Testing;

/// <summary>
/// Trạng thái công việc xét nghiệm ở Task level (góc nhìn Thử nghiệm viên)
/// </summary>
public enum TestingStatus
{
    /// <summary>
    /// Chưa bắt đầu - Mới được QLKT assign, chưa check-in chỉ tiêu nào
    /// </summary>
    NotStarted = 0,

    /// <summary>
    /// Đang thử nghiệm - Đã check-in ít nhất 1 chỉ tiêu (ấn "Thực hiện")
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Chờ phê duyệt - Đã gửi ít nhất 1 chỉ tiêu cho QLKT duyệt
    /// </summary>
    PendingApproval = 2,

    /// <summary>
    /// Bị từ chối - Có ít nhất 1 chỉ tiêu bị QLKT từ chối
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// Hoàn thành - Tất cả chỉ tiêu đã được QLKT duyệt
    /// </summary>
    Completed = 4,

    /// <summary>
    /// Không TN được - Có chỉ tiêu KTV báo không thể thử nghiệm
    /// </summary>
    CannotTest = 5
}
