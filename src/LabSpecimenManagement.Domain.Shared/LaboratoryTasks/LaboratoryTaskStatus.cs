namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// Trạng thái công việc của khoa phòng
/// </summary>
public enum LaboratoryTaskStatus
{
    /// <summary>
    /// Đang đánh giá thử nghiệm - QLKT đang xem xét các chỉ tiêu (ban đầu khi TR vào UnderReview)
    /// </summary>
    UnderEvaluation = 0,

    /// <summary>
    /// Chờ KH phản hồi - QLKT đã yêu cầu đổi PP hoặc từ chối, chờ KH xác nhận
    /// </summary>
    WaitingCustomerResponse = 1,

    /// <summary>
    /// Chấp nhận 1 phần - Một số chỉ tiêu được chấp nhận, một số bị từ chối/hủy
    /// </summary>
    PartialAccepted = 2,

    /// <summary>
    /// Chấp nhận thử nghiệm - Tất cả chỉ tiêu của phòng được chấp nhận
    /// </summary>
    FullyAccepted = 3,

    /// <summary>
    /// Từ chối thử nghiệm - Tất cả chỉ tiêu của phòng bị từ chối
    /// </summary>
    TestingRejected = 4,

    /// <summary>
    /// Đã giao việc - Tất cả chỉ tiêu đã được giao việc (đã có KTV và phương pháp)
    /// </summary>
    Assigned = 5,

    /// <summary>
    /// Đang thử nghiệm - Có ≥1 chỉ tiêu đang thử nghiệm (KTV đã check-in)
    /// </summary>
    InProgress = 6,

    /// <summary>
    /// Chờ duyệt kết quả - Có ≥1 chỉ tiêu KTV đã gửi KQ (chờ QLKT duyệt)
    /// </summary>
    WaitingApproval = 7,

    /// <summary>
    /// Đã duyệt - Tất cả chỉ tiêu đã được QLKT duyệt
    /// </summary>
    Approved = 8,

    /// <summary>
    /// Từ chối (KQ không đạt) - QLKT từ chối kết quả, TNV cần làm lại
    /// </summary>
    Rejected = 9,

    /// <summary>
    /// Đã gửi QL Chất lượng - Có ≥1 chỉ tiêu đã gửi cho QLCL
    /// </summary>
    SentToQuality = 10
}
