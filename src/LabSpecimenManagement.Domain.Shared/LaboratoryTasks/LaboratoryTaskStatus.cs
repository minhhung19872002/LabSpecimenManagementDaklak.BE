namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// Trạng thái công việc của khoa phòng
/// </summary>
public enum LaboratoryTaskStatus
{
    /// <summary>
    /// Chưa gán KTV (Phương pháp đã có từ phiếu yêu cầu)
    /// </summary>
    NotAssignedTechnician = 0,

    /// <summary>
    /// Đã gán KTV
    /// </summary>
    AssignedTechnician = 1,

    /// <summary>
    /// Đang thí nghiệm
    /// </summary>
    Testing = 2,

    /// <summary>
    /// Chờ duyệt kết quả
    /// </summary>
    PendingApproval = 3,

    /// <summary>
    /// Đã duyệt
    /// </summary>
    Approved = 4,

    /// <summary>
    /// Đã gửi kết quả
    /// </summary>
    ResultSent = 5,

    /// <summary>
    /// QLCL từ chối kết quả - trả lại cho QLKT xem xét
    /// </summary>
    ResultRejected = 6
}
