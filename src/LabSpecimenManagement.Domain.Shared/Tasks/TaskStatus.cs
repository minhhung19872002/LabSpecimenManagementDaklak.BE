namespace LabSpecimenManagement.Tasks;

/// <summary>
/// Trạng thái công việc được phân công
/// </summary>
public enum TaskAssignmentStatus
{
    Pending = 0,     // Chờ nhận
    Accepted = 1,    // Đã nhận
    InProgress = 2,  // Đang thực hiện
    Completed = 3,   // Hoàn thành
    Rejected = 4     // Từ chối
}
