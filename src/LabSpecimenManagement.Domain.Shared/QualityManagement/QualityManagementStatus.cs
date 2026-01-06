namespace LabSpecimenManagement.QualityManagement;

/// <summary>
/// Trạng thái công việc Quản lý chất lượng
/// </summary>
public enum QualityManagementStatus
{
    /// <summary>
    /// Chờ phê duyệt (trạng thái ban đầu)
    /// </summary>
    PendingApproval = 0,

    /// <summary>
    /// Từ chối phê duyệt (có chỉ tiêu bị từ chối)
    /// </summary>
    Rejected = 1,

    /// <summary>
    /// Chờ phát hành (tất cả chỉ tiêu đã duyệt, chờ in KQ)
    /// </summary>
    PendingPublish = 2,

    /// <summary>
    /// Đã phát hành 1 phần (đã in một phần chỉ tiêu)
    /// </summary>
    PartialPublished = 3,

    /// <summary>
    /// Đã phát hành (đã in tất cả chỉ tiêu)
    /// </summary>
    FullyPublished = 4,

    /// <summary>
    /// Đã giao BPGN (QLCL đã xác nhận giao cho Bộ phận giao nhận)
    /// </summary>
    HandedOver = 5
}
