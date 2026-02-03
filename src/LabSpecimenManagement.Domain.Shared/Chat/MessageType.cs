namespace LabSpecimenManagement.Chat;

/// <summary>
/// Loại tin nhắn
/// </summary>
public enum MessageType
{
    /// <summary>
    /// Tin nhắn văn bản
    /// </summary>
    Text = 0,

    /// <summary>
    /// Hình ảnh
    /// </summary>
    Image = 1,

    /// <summary>
    /// File đính kèm
    /// </summary>
    File = 2,

    /// <summary>
    /// Sticker
    /// </summary>
    Sticker = 3,

    /// <summary>
    /// Video
    /// </summary>
    Video = 4,

    /// <summary>
    /// Audio/Voice
    /// </summary>
    Audio = 5,

    /// <summary>
    /// Vị trí
    /// </summary>
    Location = 6,

    /// <summary>
    /// Link/URL
    /// </summary>
    Link = 7
}
