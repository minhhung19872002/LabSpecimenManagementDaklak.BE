namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// Trạng thái hiển thị tại cột "Tình trạng" trong tab Sổ nhận việc QLKT
/// Mapping từ IndicatorApprovalStatus + các field khác (TechnicianName, TestDate, Result, QualityApprovalStatus)
/// </summary>
public enum QLKTDisplayStatus
{
    /// <summary>
    /// Đang xem xét YC - QLKT đang đánh giá chỉ tiêu
    /// </summary>
    UnderReview = 0,

    /// <summary>
    /// Yêu cầu đổi PP - QLKT yêu cầu đổi phương pháp, chờ KH xác nhận
    /// </summary>
    MethodChangeRequested = 1,

    /// <summary>
    /// Đã đổi PP - KH đã xác nhận đổi phương pháp
    /// </summary>
    MethodChanged = 2,

    /// <summary>
    /// Từ chối TN - QLKT từ chối thử nghiệm
    /// </summary>
    TestingRejected = 3,

    /// <summary>
    /// YC bổ sung - QLKT yêu cầu KH bổ sung thông tin
    /// </summary>
    SupplementaryRequested = 4,

    /// <summary>
    /// KH hủy - Khách hàng đã hủy chỉ tiêu
    /// </summary>
    CustomerCancelled = 5,

    /// <summary>
    /// Không TN được - KTV báo không thể thử nghiệm
    /// </summary>
    CannotTest = 6,

    /// <summary>
    /// Chờ giao việc - Đã chấp nhận TN nhưng chưa gán KTV/PP
    /// </summary>
    WaitingForAssignment = 7,

    /// <summary>
    /// Cần làm - Đã gán KTV/PP nhưng chưa bắt đầu TN
    /// </summary>
    ToDo = 8,

    /// <summary>
    /// Đang thử nghiệm - KTV đang thực hiện TN
    /// </summary>
    Testing = 9,

    /// <summary>
    /// Chờ QLKT phê duyệt - KTV đã nhập KQ, chờ QLKT duyệt
    /// </summary>
    WaitingQLKTApproval = 10,

    /// <summary>
    /// Từ chối KQ - QLKT từ chối kết quả
    /// </summary>
    ResultRejected = 11,

    /// <summary>
    /// Chờ QLCL Duyệt - QLKT đã duyệt, chờ QLCL duyệt
    /// </summary>
    WaitingQLCLApproval = 12,

    /// <summary>
    /// QLCL từ chối - QLCL từ chối kết quả
    /// </summary>
    QLCLRejected = 13,

    /// <summary>
    /// Hoàn thành - QLCL đã duyệt
    /// </summary>
    Completed = 14,

    /// <summary>
    /// Hủy bỏ yêu cầu - Phiếu yêu cầu thử nghiệm đã bị hủy
    /// </summary>
    RequestCancelled = 15
}
