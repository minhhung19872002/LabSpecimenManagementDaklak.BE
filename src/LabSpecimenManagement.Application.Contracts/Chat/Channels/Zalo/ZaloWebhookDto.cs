using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace LabSpecimenManagement.Chat.Channels.Zalo;

/// <summary>
/// Payload từ Zalo OA webhook
/// </summary>
public class ZaloWebhookPayload
{
    [JsonPropertyName("app_id")]
    public string? AppId { get; set; }

    [JsonPropertyName("oa_id")]
    public string? OaId { get; set; }

    [JsonPropertyName("user_id_by_app")]
    public string? UserIdByApp { get; set; }

    [JsonPropertyName("event_name")]
    public string? EventName { get; set; }

    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("sender")]
    public ZaloSender? Sender { get; set; }

    [JsonPropertyName("recipient")]
    public ZaloRecipient? Recipient { get; set; }

    [JsonPropertyName("message")]
    public ZaloMessage? Message { get; set; }

    [JsonPropertyName("follower")]
    public ZaloFollower? Follower { get; set; }
}

public class ZaloSender
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

public class ZaloRecipient
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

public class ZaloMessage
{
    [JsonPropertyName("msg_id")]
    public string? MsgId { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("attachments")]
    public List<ZaloAttachment>? Attachments { get; set; }
}

public class ZaloAttachment
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("payload")]
    public ZaloAttachmentPayload? Payload { get; set; }
}

public class ZaloAttachmentPayload
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("size")]
    public string? Size { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }
}

public class ZaloFollower
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}

/// <summary>
/// Response từ Zalo API
/// </summary>
public class ZaloApiResponse<T>
{
    [JsonPropertyName("error")]
    public int Error { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

/// <summary>
/// Data từ API send message
/// </summary>
public class ZaloSendMessageData
{
    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }
}

/// <summary>
/// Request gửi tin nhắn đến Zalo
/// </summary>
public class ZaloSendMessageRequest
{
    [JsonPropertyName("recipient")]
    public ZaloMessageRecipient Recipient { get; set; } = new();

    [JsonPropertyName("message")]
    public ZaloOutboundMessage Message { get; set; } = new();
}

public class ZaloMessageRecipient
{
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;
}

public class ZaloOutboundMessage
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("attachment")]
    public ZaloOutboundAttachment? Attachment { get; set; }
}

public class ZaloOutboundAttachment
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("payload")]
    public ZaloOutboundPayload Payload { get; set; } = new();
}

public class ZaloOutboundPayload
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }
}

/// <summary>
/// User profile từ Zalo
/// </summary>
public class ZaloUserProfile
{
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("avatar")]
    public string? Avatar { get; set; }

    [JsonPropertyName("user_is_follower")]
    public bool UserIsFollower { get; set; }
}
