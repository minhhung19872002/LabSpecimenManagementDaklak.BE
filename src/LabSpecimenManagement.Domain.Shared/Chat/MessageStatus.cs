namespace LabSpecimenManagement.Chat;

/// <summary>
/// Trạng thái tin nhắn
/// </summary>
public enum MessageStatus
{
    /// <summary>
    /// Đang gửi
    /// </summary>
    Sending = 0,

    /// <summary>
    /// Đã gửi thành công
    /// </summary>
    Sent = 1,

    /// <summary>
    /// Đã giao (bên nhận đã nhận được)
    /// </summary>
    Delivered = 2,

    /// <summary>
    /// Đã đọc
    /// </summary>
    Read = 3,

    /// <summary>
    /// Gửi thất bại
    /// </summary>
    Failed = 4
}
