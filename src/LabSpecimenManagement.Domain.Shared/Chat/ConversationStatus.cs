namespace LabSpecimenManagement.Chat;

/// <summary>
/// Trạng thái cuộc hội thoại
/// </summary>
public enum ConversationStatus
{
    /// <summary>
    /// Chờ xử lý - chưa có agent nào nhận
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Đang xử lý - đã có agent nhận
    /// </summary>
    Active = 1,

    /// <summary>
    /// Đã giải quyết - agent đánh dấu hoàn thành
    /// </summary>
    Resolved = 2,

    /// <summary>
    /// Đã đóng - kết thúc hoàn toàn
    /// </summary>
    Closed = 3
}
