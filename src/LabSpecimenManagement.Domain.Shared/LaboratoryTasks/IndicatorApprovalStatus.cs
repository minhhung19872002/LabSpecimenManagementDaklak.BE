namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// Trạng thái duyệt của chỉ tiêu trong công việc khoa phòng
/// </summary>
public enum IndicatorApprovalStatus
{
    /// <summary>
    /// Chờ nhập kết quả - Pending
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Đã nhập kết quả - Entered
    /// </summary>
    Entered = 1,

    /// <summary>
    /// Đã duyệt - Approved
    /// </summary>
    Approved = 2,

    /// <summary>
    /// Không duyệt (Từ chối) - Rejected
    /// </summary>
    Rejected = 3,

    /// <summary>
    /// Khách hàng hủy - CustomerCancelled
    /// </summary>
    CustomerCancelled = 4
}
