using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Chat;

/// <summary>
/// DTO cho ChatMessage
/// </summary>
public class ChatMessageDto : EntityDto<Guid>
{
    public Guid ConversationId { get; set; }
    public Guid? SenderId { get; set; }
    public string? SenderName { get; set; }
    public string? SenderAvatar { get; set; }
    public MessageSenderType SenderType { get; set; }
    public MessageType MessageType { get; set; }
    public string? Content { get; set; }
    public List<MessageAttachmentDto>? Attachments { get; set; }
    public MessageStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreationTime { get; set; }
}

/// <summary>
/// DTO cho attachment
/// </summary>
public class MessageAttachmentDto
{
    public string Url { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Name { get; set; }
    public long? Size { get; set; }
    public string? ThumbnailUrl { get; set; }
}

/// <summary>
/// Input để lấy messages của conversation
/// </summary>
public class GetMessagesInput : PagedResultRequestDto
{
    public Guid ConversationId { get; set; }

    /// <summary>
    /// Lấy messages trước thời điểm này (for infinite scroll)
    /// </summary>
    public DateTime? Before { get; set; }

    /// <summary>
    /// Lấy messages sau thời điểm này (for new messages)
    /// </summary>
    public DateTime? After { get; set; }
}

/// <summary>
/// Input để agent gửi tin nhắn
/// </summary>
public class SendMessageInput
{
    public Guid ConversationId { get; set; }
    public MessageType MessageType { get; set; } = MessageType.Text;
    public string? Content { get; set; }
    public List<MessageAttachmentDto>? Attachments { get; set; }
}

/// <summary>
/// Result sau khi gửi tin nhắn
/// </summary>
public class SendMessageResult
{
    public bool Success { get; set; }
    public ChatMessageDto? Message { get; set; }
    public string? ErrorMessage { get; set; }
}
