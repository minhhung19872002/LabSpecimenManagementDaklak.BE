namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// Trạng thái công việc của khoa phòng
/// Ưu tiên hiển thị (khi có nhiều trạng thái): SentToQuality > Approved > PendingApproval > Testing > Assigned
/// </summary>
public enum LaboratoryTaskStatus
{
    /// <summary>
    /// Chưa giao việc - Chưa assign KTV cho chỉ tiêu nào hoặc chưa gán phương pháp
    /// </summary>
    NotAssigned = 0,

    /// <summary>
    /// Đã giao việc - Tất cả chỉ tiêu đã được giao việc (đã có KTV và phương pháp)
    /// </summary>
    Assigned = 1,

    /// <summary>
    /// Đang thử nghiệm - Có ≥1 chỉ tiêu đang thử nghiệm (KTV đã check-in)
    /// </summary>
    Testing = 2,

    /// <summary>
    /// Chờ duyệt kết quả - Có ≥1 chỉ tiêu KTV đã gửi KQ (chờ QLKT duyệt)
    /// </summary>
    PendingApproval = 3,

    /// <summary>
    /// Đã duyệt - Tất cả chỉ tiêu đã được QLKT duyệt
    /// </summary>
    Approved = 4,

    /// <summary>
    /// Đã gửi QL Chất lượng - Có ≥1 chỉ tiêu đã gửi cho QLCL
    /// </summary>
    SentToQuality = 5
}
