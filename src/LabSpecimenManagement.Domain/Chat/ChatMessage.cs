using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.Chat;

/// <summary>
/// Tin nhắn trong cuộc hội thoại
/// </summary>
public class ChatMessage : CreationAuditedEntity<Guid>
{
    /// <summary>
    /// ID cuộc hội thoại
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// ID người gửi (Agent ID nếu SenderType = Agent, null nếu từ Contact)
    /// </summary>
    public Guid? SenderId { get; set; }

    /// <summary>
    /// Loại người gửi
    /// </summary>
    public MessageSenderType SenderType { get; set; }

    /// <summary>
    /// Loại tin nhắn
    /// </summary>
    public MessageType MessageType { get; set; }

    /// <summary>
    /// Nội dung tin nhắn (text)
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Danh sách file đính kèm dạng JSON [{url, type, name, size}]
    /// </summary>
    public string? Attachments { get; set; }

    /// <summary>
    /// ID tin nhắn trên hệ thống ngoài (Zalo message_id, etc.)
    /// </summary>
    public string? ExternalMessageId { get; set; }

    /// <summary>
    /// Trạng thái tin nhắn
    /// </summary>
    public MessageStatus Status { get; set; } = MessageStatus.Sent;

    /// <summary>
    /// Thông báo lỗi nếu gửi thất bại
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Dữ liệu bổ sung dạng JSON
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Đánh dấu đã xóa (soft delete)
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Navigation property - Cuộc hội thoại
    /// </summary>
    public virtual ChatConversation? Conversation { get; set; }

    protected ChatMessage()
    {
    }

    public ChatMessage(
        Guid id,
        Guid conversationId,
        MessageSenderType senderType,
        MessageType messageType,
        string? content = null,
        Guid? senderId = null) : base(id)
    {
        ConversationId = conversationId;
        SenderType = senderType;
        MessageType = messageType;
        Content = content;
        SenderId = senderId;
        Status = MessageStatus.Sent;
    }

    /// <summary>
    /// Đánh dấu gửi thành công
    /// </summary>
    public void MarkAsSent(string? externalMessageId = null)
    {
        Status = MessageStatus.Sent;
        ExternalMessageId = externalMessageId;
        ErrorMessage = null;
    }

    /// <summary>
    /// Đánh dấu đã giao
    /// </summary>
    public void MarkAsDelivered()
    {
        if (Status < MessageStatus.Delivered)
        {
            Status = MessageStatus.Delivered;
        }
    }

    /// <summary>
    /// Đánh dấu đã đọc
    /// </summary>
    public void MarkAsRead()
    {
        Status = MessageStatus.Read;
    }

    /// <summary>
    /// Đánh dấu gửi thất bại
    /// </summary>
    public void MarkAsFailed(string errorMessage)
    {
        Status = MessageStatus.Failed;
        ErrorMessage = errorMessage?.Length > ChatConsts.MaxErrorMessageLength
            ? errorMessage.Substring(0, ChatConsts.MaxErrorMessageLength)
            : errorMessage;
    }
}
