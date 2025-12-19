namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// Trạng thái công việc của khoa phòng
/// </summary>
public enum LaboratoryTaskStatus
{
    /// <summary>
    /// Chưa gán phương pháp
    /// </summary>
    NotAssignedMethod = 0,

    /// <summary>
    /// Đã gán phương pháp
    /// </summary>
    AssignedMethod = 1,

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
    ResultSent = 5
}
