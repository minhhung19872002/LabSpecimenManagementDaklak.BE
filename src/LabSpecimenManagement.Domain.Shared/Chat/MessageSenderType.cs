namespace LabSpecimenManagement.Chat;

/// <summary>
/// Loại người gửi tin nhắn
/// </summary>
public enum MessageSenderType
{
    /// <summary>
    /// Khách hàng (từ Zalo, Facebook, etc.)
    /// </summary>
    Contact = 0,

    /// <summary>
    /// Agent/CSKH
    /// </summary>
    Agent = 1,

    /// <summary>
    /// Hệ thống (tin nhắn tự động)
    /// </summary>
    System = 2
}
