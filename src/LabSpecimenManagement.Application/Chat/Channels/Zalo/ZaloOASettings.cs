namespace LabSpecimenManagement.Chat.Channels.Zalo;

/// <summary>
/// Settings cho Zalo OA - map từ appsettings.json
/// </summary>
public class ZaloOASettings
{
    public const string SectionName = "ZaloOA";

    /// <summary>
    /// App ID từ Zalo Developer
    /// </summary>
    public string AppId { get; set; } = string.Empty;

    /// <summary>
    /// Secret Key từ Zalo Developer
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Refresh Token để lấy Access Token
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Token để verify webhook từ Zalo
    /// </summary>
    public string WebhookVerifyToken { get; set; } = string.Empty;

    /// <summary>
    /// OA ID
    /// </summary>
    public string OAId { get; set; } = string.Empty;
}
