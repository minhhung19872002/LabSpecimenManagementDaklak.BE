namespace LabSpecimenManagement.QualityManagement;

/// <summary>
/// Trạng thái công việc Quản lý chất lượng
/// </summary>
public enum QualityManagementStatus
{
    /// <summary>
    /// Chờ phê duyệt
    /// </summary>
    PendingApproval = 0,

    /// <summary>
    /// Đang thử nghiệm (còn chỉ tiêu chưa gửi KQ)
    /// </summary>
    Testing = 1,

    /// <summary>
    /// Đã duyệt (tất cả chỉ tiêu đã duyệt)
    /// </summary>
    Approved = 2,

    /// <summary>
    /// Từ chối phê duyệt (có chỉ tiêu bị từ chối chưa gửi lại)
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// Chờ ký (sau khi In KQ)
    /// </summary>
    PendingSignature = 4,

    /// <summary>
    /// Đã trả KQ
    /// </summary>
    ResultSent = 5
}
