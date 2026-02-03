using System.Threading.Tasks;

namespace LabSpecimenManagement.Chat.Channels.Zalo;

/// <summary>
/// Client để gọi Zalo OA APIs
/// </summary>
public interface IZaloOAClient
{
    /// <summary>
    /// Gửi tin nhắn text
    /// </summary>
    Task<ZaloApiResponse<ZaloSendMessageData>> SendTextMessageAsync(string userId, string text);

    /// <summary>
    /// Gửi tin nhắn hình ảnh
    /// </summary>
    Task<ZaloApiResponse<ZaloSendMessageData>> SendImageMessageAsync(string userId, string imageUrl);

    /// <summary>
    /// Gửi file
    /// </summary>
    Task<ZaloApiResponse<ZaloSendMessageData>> SendFileMessageAsync(string userId, string fileUrl);

    /// <summary>
    /// Lấy thông tin user
    /// </summary>
    Task<ZaloApiResponse<ZaloUserProfile>> GetUserProfileAsync(string userId);
}

/// <summary>
/// Quản lý token của Zalo OA
/// </summary>
public interface IZaloTokenManager
{
    /// <summary>
    /// Lấy access token (từ cache hoặc refresh nếu cần)
    /// </summary>
    Task<string> GetAccessTokenAsync();

    /// <summary>
    /// Force refresh access token
    /// </summary>
    Task<string> RefreshAccessTokenAsync();
}
