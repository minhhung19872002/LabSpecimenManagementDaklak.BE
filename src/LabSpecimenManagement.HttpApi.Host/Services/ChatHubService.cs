using System;
using System.Threading.Tasks;
using LabSpecimenManagement.Chat;
using LabSpecimenManagement.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace LabSpecimenManagement.Services;

/// <summary>
/// Service để push realtime events qua SignalR ChatHub
/// </summary>
public class ChatHubService : IChatHubService
{
    private readonly IHubContext<ChatHub> _hubContext;
    private readonly ILogger<ChatHubService> _logger;

    public ChatHubService(
        IHubContext<ChatHub> hubContext,
        ILogger<ChatHubService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    /// <summary>
    /// Thông báo có conversation mới (broadcast to all agents)
    /// </summary>
    public async Task NotifyNewConversationAsync(ChatConversationDto conversation)
    {
        await _hubContext.Clients
            .Group("all_agents")
            .SendAsync("NewConversation", conversation);

        _logger.LogDebug("Notified all agents about new conversation {ConversationId}", conversation.Id);
    }

    /// <summary>
    /// Thông báo conversation được cập nhật
    /// </summary>
    public async Task NotifyConversationUpdatedAsync(ChatConversationDto conversation)
    {
        await _hubContext.Clients
            .Group("all_agents")
            .SendAsync("ConversationUpdated", conversation);

        _logger.LogDebug("Notified all agents about conversation update {ConversationId}", conversation.Id);
    }

    /// <summary>
    /// Thông báo conversation được assign cho agent
    /// </summary>
    public async Task NotifyConversationAssignedAsync(Guid agentId, ChatConversationDto conversation)
    {
        await _hubContext.Clients
            .Group($"agent_{agentId}")
            .SendAsync("ConversationAssigned", conversation);

        _logger.LogDebug("Notified agent {AgentId} about conversation assignment {ConversationId}",
            agentId, conversation.Id);
    }

    /// <summary>
    /// Thông báo có tin nhắn mới từ customer
    /// </summary>
    public async Task NotifyNewMessageAsync(Guid conversationId, ChatMessageDto message)
    {
        // Notify conversation room
        await _hubContext.Clients
            .Group($"conversation_{conversationId}")
            .SendAsync("NewMessage", message);

        _logger.LogDebug("Notified conversation {ConversationId} about new message {MessageId}",
            conversationId, message.Id);
    }

    /// <summary>
    /// Thông báo agent đã gửi tin nhắn
    /// </summary>
    public async Task NotifyMessageSentAsync(Guid conversationId, ChatMessageDto message)
    {
        await _hubContext.Clients
            .Group($"conversation_{conversationId}")
            .SendAsync("MessageSent", message);

        _logger.LogDebug("Notified conversation {ConversationId} about sent message {MessageId}",
            conversationId, message.Id);
    }

    /// <summary>
    /// Thông báo tin nhắn gửi thất bại
    /// </summary>
    public async Task NotifyMessageFailedAsync(Guid conversationId, ChatMessageDto message)
    {
        await _hubContext.Clients
            .Group($"conversation_{conversationId}")
            .SendAsync("MessageFailed", message);

        _logger.LogDebug("Notified conversation {ConversationId} about failed message {MessageId}",
            conversationId, message.Id);
    }

    /// <summary>
    /// Thông báo status của tin nhắn thay đổi
    /// </summary>
    public async Task NotifyMessageStatusUpdatedAsync(Guid conversationId, Guid messageId, MessageStatus status)
    {
        await _hubContext.Clients
            .Group($"conversation_{conversationId}")
            .SendAsync("MessageStatusUpdated", new
            {
                MessageId = messageId,
                Status = status
            });

        _logger.LogDebug("Notified conversation {ConversationId} about message status update {MessageId} -> {Status}",
            conversationId, messageId, status);
    }
}
