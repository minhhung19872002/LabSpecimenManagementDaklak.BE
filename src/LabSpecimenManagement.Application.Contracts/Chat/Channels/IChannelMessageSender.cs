using System.Threading.Tasks;

namespace LabSpecimenManagement.Chat.Channels;

/// <summary>
/// Interface base cho tất cả channel message senders
/// Thiết kế pluggable - mỗi channel (Zalo, Facebook, Email) implement interface này
/// </summary>
public interface IChannelMessageSender
{
    /// <summary>
    /// Loại channel
    /// </summary>
    ChannelType ChannelType { get; }

    /// <summary>
    /// Gửi tin nhắn text
    /// </summary>
    Task<ChannelSendResult> SendTextAsync(string externalContactId, string content);

    /// <summary>
    /// Gửi tin nhắn hình ảnh
    /// </summary>
    Task<ChannelSendResult> SendImageAsync(string externalContactId, string imageUrl, string? caption = null);

    /// <summary>
    /// Gửi file
    /// </summary>
    Task<ChannelSendResult> SendFileAsync(string externalContactId, string fileUrl, string fileName);
}

/// <summary>
/// Kết quả gửi tin nhắn qua channel
/// </summary>
public class ChannelSendResult
{
    public bool Success { get; set; }
    public string? ExternalMessageId { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    public static ChannelSendResult Ok(string? externalMessageId = null) => new()
    {
        Success = true,
        ExternalMessageId = externalMessageId
    };

    public static ChannelSendResult Fail(string errorMessage, string? errorCode = null) => new()
    {
        Success = false,
        ErrorMessage = errorMessage,
        ErrorCode = errorCode
    };
}
