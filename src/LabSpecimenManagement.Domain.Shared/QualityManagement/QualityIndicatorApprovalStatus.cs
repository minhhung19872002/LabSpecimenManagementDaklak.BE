namespace LabSpecimenManagement.QualityManagement;

/// <summary>
/// Trạng thái phê duyệt chỉ tiêu bởi QLCL
/// </summary>
public enum QualityIndicatorApprovalStatus
{
    /// <summary>
    /// Chờ phê duyệt (QLKT gửi hoặc edit phiếu)
    /// </summary>
    PendingApproval = 0,

    /// <summary>
    /// Đã duyệt
    /// </summary>
    Approved = 1,

    /// <summary>
    /// Từ chối phê duyệt
    /// </summary>
    Rejected = 2
}
