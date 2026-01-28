namespace LabSpecimenManagement.Testing;

/// <summary>
/// Trạng thái thử nghiệm của chỉ tiêu (Indicator level)
/// </summary>
public enum IndicatorTestingStatus
{
    /// <summary>
    /// Cần làm - Chưa check-in (chưa ấn "Thực hiện")
    /// </summary>
    Todo = 0,

    /// <summary>
    /// Đang thí nghiệm - Đã check-in (ấn "Thực hiện"), đang nhập kết quả
    /// </summary>
    Testing = 1,

    /// <summary>
    /// Chờ phê duyệt - Đã gửi kết quả cho QLKT
    /// </summary>
    PendingApproval = 2,

    /// <summary>
    /// Bị từ chối - QLKT từ chối kết quả
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// Đã xong - QLKT đã duyệt kết quả
    /// </summary>
    Done = 4,

    /// <summary>
    /// KH hủy - Khách hàng đã hủy chỉ tiêu này
    /// </summary>
    Cancelled = 5,

    /// <summary>
    /// Không TN được - KTV báo không thể thử nghiệm
    /// </summary>
    CannotTest = 6
}
