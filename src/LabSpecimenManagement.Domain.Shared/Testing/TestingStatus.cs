namespace LabSpecimenManagement.Testing;

/// <summary>
/// Trạng thái công việc xét nghiệm (góc nhìn kỹ thuật viên)
/// </summary>
public enum TestingStatus
{
    /// <summary>
    /// Cần làm - Chưa có chỉ tiêu nào được nhập kết quả
    /// </summary>
    Todo = 0,

    /// <summary>
    /// Đang thí nghiệm - Có ít nhất 1 chỉ tiêu đã nhập kết quả
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Đã xong - Tất cả chỉ tiêu đã gửi QL duyệt
    /// </summary>
    Done = 2
}
