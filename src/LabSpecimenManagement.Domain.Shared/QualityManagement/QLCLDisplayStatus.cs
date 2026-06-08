namespace LabSpecimenManagement.QualityManagement;

/// <summary>
/// Trạng thái hiển thị tại cột "Tình trạng" trong tab Sổ nhận việc QLCL
/// Mapping từ TestRequestStatus + IndicatorApprovalStatus + QualityIndicatorApprovalStatus + các field khác
/// </summary>
public enum QLCLDisplayStatus
{
    /// <summary>
    /// Mới - Phiếu mới tạo
    /// </summary>
    New = 0,

    /// <summary>
    /// Chờ thanh toán
    /// </summary>
    WaitingForPayment = 1,

    /// <summary>
    /// Đang xem xét YC - QLCL đang xem xét yêu cầu
    /// </summary>
    UnderReview = 2,

    /// <summary>
    /// Từ chối - QLCL từ chối yêu cầu thử nghiệm
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// Chờ giao việc - Đã chấp nhận, chờ giao cho KTV
    /// </summary>
    WaitingForAssignment = 4,

    /// <summary>
    /// Cần làm - Đã giao việc cho KTV
    /// </summary>
    ToDo = 5,

    /// <summary>
    /// Đang thử nghiệm - KTV đang thực hiện
    /// </summary>
    Testing = 6,

    /// <summary>
    /// Chờ QLKT phê duyệt - KTV đã gửi kết quả
    /// </summary>
    WaitingQLKTApproval = 7,

    /// <summary>
    /// Từ chối KQ - QLKT từ chối kết quả
    /// </summary>
    ResultRejected = 8,

    /// <summary>
    /// Chờ QLCL Duyệt - QLKT đã duyệt, chờ QLCL duyệt
    /// </summary>
    WaitingQLCLApproval = 9,

    /// <summary>
    /// QLCL từ chối - QLCL từ chối kết quả
    /// </summary>
    QLCLRejected = 10,

    /// <summary>
    /// Chờ phát hành - QLCL đã duyệt, chờ phát hành báo cáo
    /// </summary>
    WaitingForPublishing = 11,

    /// <summary>
    /// Đã phát hành báo cáo
    /// </summary>
    Published = 12,

    /// <summary>
    /// Hủy bỏ yêu cầu - Phiếu yêu cầu thử nghiệm đã bị hủy
    /// </summary>
    RequestCancelled = 13
}
