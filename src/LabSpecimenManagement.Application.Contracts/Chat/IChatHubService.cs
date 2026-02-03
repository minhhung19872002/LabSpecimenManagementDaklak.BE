using System;
using System.Threading.Tasks;

namespace LabSpecimenManagement.Chat;

/// <summary>
/// Service để push realtime events qua SignalR
/// </summary>
public interface IChatHubService
{
    /// <summary>
    /// Thông báo có conversation mới (broadcast to all agents)
    /// </summary>
    Task NotifyNewConversationAsync(ChatConversationDto conversation);

    /// <summary>
    /// Thông báo conversation được cập nhật
    /// </summary>
    Task NotifyConversationUpdatedAsync(ChatConversationDto conversation);

    /// <summary>
    /// Thông báo conversation được assign cho agent
    /// </summary>
    Task NotifyConversationAssignedAsync(Guid agentId, ChatConversationDto conversation);

    /// <summary>
    /// Thông báo có tin nhắn mới từ customer
    /// </summary>
    Task NotifyNewMessageAsync(Guid conversationId, ChatMessageDto message);

    /// <summary>
    /// Thông báo agent đã gửi tin nhắn
    /// </summary>
    Task NotifyMessageSentAsync(Guid conversationId, ChatMessageDto message);

    /// <summary>
    /// Thông báo tin nhắn gửi thất bại
    /// </summary>
    Task NotifyMessageFailedAsync(Guid conversationId, ChatMessageDto message);

    /// <summary>
    /// Thông báo status của tin nhắn thay đổi
    /// </summary>
    Task NotifyMessageStatusUpdatedAsync(Guid conversationId, Guid messageId, MessageStatus status);
}
