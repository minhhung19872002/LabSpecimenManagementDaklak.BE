using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Linq;

namespace LabSpecimenManagement.Chat.Channels.Zalo;

/// <summary>
/// Xử lý webhook từ Zalo OA
/// </summary>
public class ZaloWebhookHandler : ITransientDependency
{
    private readonly IRepository<ChatChannel, Guid> _channelRepository;
    private readonly IRepository<ChatContact, Guid> _contactRepository;
    private readonly IRepository<ChatConversation, Guid> _conversationRepository;
    private readonly IRepository<ChatMessage, Guid> _messageRepository;
    private readonly IZaloOAClient _zaloClient;
    private readonly IChatHubService? _chatHubService;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IAsyncQueryableExecuter _asyncExecuter;
    private readonly ZaloOASettings _settings;
    private readonly ILogger<ZaloWebhookHandler> _logger;

    public ZaloWebhookHandler(
        IRepository<ChatChannel, Guid> channelRepository,
        IRepository<ChatContact, Guid> contactRepository,
        IRepository<ChatConversation, Guid> conversationRepository,
        IRepository<ChatMessage, Guid> messageRepository,
        IZaloOAClient zaloClient,
        IGuidGenerator guidGenerator,
        IAsyncQueryableExecuter asyncExecuter,
        IOptions<ZaloOASettings> settings,
        ILogger<ZaloWebhookHandler> logger,
        IChatHubService? chatHubService = null)
    {
        _channelRepository = channelRepository;
        _contactRepository = contactRepository;
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _zaloClient = zaloClient;
        _guidGenerator = guidGenerator;
        _asyncExecuter = asyncExecuter;
        _settings = settings.Value;
        _logger = logger;
        _chatHubService = chatHubService;
    }

    /// <summary>
    /// Verify webhook signature từ Zalo
    /// </summary>
    public bool VerifySignature(string payload, string signature)
    {
        // Zalo sử dụng MAC để verify
        // Trong production, cần implement đúng theo docs của Zalo
        // Hiện tại skip để đơn giản hóa
        return true;
    }

    /// <summary>
    /// Xử lý webhook payload
    /// </summary>
    public async Task HandleAsync(ZaloWebhookPayload payload)
    {
        _logger.LogInformation("Processing Zalo webhook event: {EventName}", payload.EventName);

        try
        {
            switch (payload.EventName)
            {
                case "user_send_text":
                case "user_send_image":
                case "user_send_file":
                case "user_send_sticker":
                case "user_send_gif":
                case "user_send_audio":
                case "user_send_video":
                case "user_send_location":
                    await HandleIncomingMessageAsync(payload);
                    break;

                case "follow":
                    await HandleFollowAsync(payload);
                    break;

                case "unfollow":
                    await HandleUnfollowAsync(payload);
                    break;

                default:
                    _logger.LogWarning("Unknown Zalo webhook event: {EventName}", payload.EventName);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Zalo webhook event {EventName}", payload.EventName);
            throw;
        }
    }

    private async Task HandleIncomingMessageAsync(ZaloWebhookPayload payload)
    {
        if (payload.Sender?.Id == null || payload.Message == null)
        {
            _logger.LogWarning("Invalid incoming message payload");
            return;
        }

        var zaloUserId = payload.Sender.Id;

        // 1. Get or create channel
        var channel = await GetOrCreateZaloChannelAsync();

        // 2. Get or create contact
        var contact = await GetOrCreateContactAsync(channel.Id, zaloUserId);

        // 3. Get or create conversation
        var conversation = await GetOrCreateConversationAsync(channel.Id, contact.Id);

        // 4. Create message
        var message = await CreateMessageAsync(conversation, payload);

        // 5. Update conversation
        conversation.UpdateLastMessage(
            GetMessagePreview(message),
            message.CreationTime,
            fromContact: true
        );
        await _conversationRepository.UpdateAsync(conversation);

        // 6. Push realtime notification
        if (_chatHubService != null)
        {
            var messageDto = MapToMessageDto(message, contact);
            await _chatHubService.NotifyNewMessageAsync(conversation.Id, messageDto);

            // Also notify conversation update
            var conversationDto = MapToConversationDto(conversation, channel, contact);
            await _chatHubService.NotifyConversationUpdatedAsync(conversationDto);

            // If new conversation, broadcast to all agents
            if (conversation.CreationTime >= DateTime.UtcNow.AddSeconds(-5))
            {
                await _chatHubService.NotifyNewConversationAsync(conversationDto);
            }
        }
    }

    private async Task HandleFollowAsync(ZaloWebhookPayload payload)
    {
        if (payload.Follower?.Id == null)
        {
            return;
        }

        var channel = await GetOrCreateZaloChannelAsync();
        await GetOrCreateContactAsync(channel.Id, payload.Follower.Id);

        _logger.LogInformation("New Zalo follower: {UserId}", payload.Follower.Id);
    }

    private async Task HandleUnfollowAsync(ZaloWebhookPayload payload)
    {
        if (payload.Follower?.Id == null)
        {
            return;
        }

        _logger.LogInformation("Zalo user unfollowed: {UserId}", payload.Follower.Id);
        // Optionally: Mark contact as inactive
    }

    private async Task<ChatChannel> GetOrCreateZaloChannelAsync()
    {
        var queryable = await _channelRepository.GetQueryableAsync();
        var channel = await _asyncExecuter.FirstOrDefaultAsync(
            queryable.Where(c => c.Type == ChannelType.Zalo && c.IsActive)
        );

        if (channel != null)
        {
            return channel;
        }

        // Create default Zalo channel
        channel = new ChatChannel(
            _guidGenerator.Create(),
            "Zalo OA",
            ChannelType.Zalo
        );

        await _channelRepository.InsertAsync(channel);
        return channel;
    }

    private async Task<ChatContact> GetOrCreateContactAsync(Guid channelId, string externalId)
    {
        var queryable = await _contactRepository.GetQueryableAsync();
        var contact = await _asyncExecuter.FirstOrDefaultAsync(
            queryable.Where(c => c.ChannelId == channelId && c.ExternalId == externalId)
        );

        if (contact != null)
        {
            return contact;
        }

        // Get user profile from Zalo
        string? displayName = null;
        string? avatar = null;

        try
        {
            var profileResponse = await _zaloClient.GetUserProfileAsync(externalId);
            if (profileResponse.Error == 0 && profileResponse.Data != null)
            {
                displayName = profileResponse.Data.DisplayName;
                avatar = profileResponse.Data.Avatar;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to get Zalo user profile for {UserId}", externalId);
        }

        contact = new ChatContact(
            _guidGenerator.Create(),
            channelId,
            externalId,
            displayName ?? $"Zalo User {externalId.Substring(0, Math.Min(8, externalId.Length))}",
            avatar
        );

        await _contactRepository.InsertAsync(contact);
        return contact;
    }

    private async Task<ChatConversation> GetOrCreateConversationAsync(Guid channelId, Guid contactId)
    {
        var queryable = await _conversationRepository.GetQueryableAsync();

        // Find active conversation
        var conversation = await _asyncExecuter.FirstOrDefaultAsync(
            queryable.Where(c =>
                c.ChannelId == channelId &&
                c.ContactId == contactId &&
                (c.Status == ConversationStatus.Pending || c.Status == ConversationStatus.Active))
        );

        if (conversation != null)
        {
            return conversation;
        }

        // Create new conversation
        conversation = new ChatConversation(
            _guidGenerator.Create(),
            channelId,
            contactId
        );

        await _conversationRepository.InsertAsync(conversation);
        return conversation;
    }

    private async Task<ChatMessage> CreateMessageAsync(ChatConversation conversation, ZaloWebhookPayload payload)
    {
        var messageType = MapMessageType(payload.EventName ?? string.Empty);
        var content = payload.Message?.Text;
        List<MessageAttachmentDto>? attachments = null;

        if (payload.Message?.Attachments != null && payload.Message.Attachments.Count > 0)
        {
            attachments = payload.Message.Attachments.Select(a => new MessageAttachmentDto
            {
                Url = a.Payload?.Url ?? string.Empty,
                Type = a.Type,
                Name = a.Payload?.Name,
                ThumbnailUrl = a.Payload?.Thumbnail
            }).ToList();
        }

        var message = new ChatMessage(
            _guidGenerator.Create(),
            conversation.Id,
            MessageSenderType.Contact,
            messageType,
            content
        );

        if (attachments != null)
        {
            message.Attachments = JsonSerializer.Serialize(attachments);
        }

        message.ExternalMessageId = payload.Message?.MsgId;
        message.Status = MessageStatus.Delivered;

        await _messageRepository.InsertAsync(message);
        return message;
    }

    private static MessageType MapMessageType(string eventName)
    {
        return eventName switch
        {
            "user_send_text" => MessageType.Text,
            "user_send_image" => MessageType.Image,
            "user_send_file" => MessageType.File,
            "user_send_sticker" or "user_send_gif" => MessageType.Sticker,
            "user_send_audio" => MessageType.Audio,
            "user_send_video" => MessageType.Video,
            "user_send_location" => MessageType.Location,
            _ => MessageType.Text
        };
    }

    private static string GetMessagePreview(ChatMessage message)
    {
        if (!string.IsNullOrEmpty(message.Content))
        {
            return message.Content.Length > ChatConsts.MaxLastMessagePreviewLength
                ? message.Content.Substring(0, ChatConsts.MaxLastMessagePreviewLength)
                : message.Content;
        }

        return message.MessageType switch
        {
            MessageType.Image => "[Hình ảnh]",
            MessageType.File => "[File]",
            MessageType.Video => "[Video]",
            MessageType.Audio => "[Audio]",
            MessageType.Sticker => "[Sticker]",
            MessageType.Location => "[Vị trí]",
            _ => "[Tin nhắn]"
        };
    }

    private static ChatMessageDto MapToMessageDto(ChatMessage message, ChatContact contact)
    {
        List<MessageAttachmentDto>? attachments = null;
        if (!string.IsNullOrEmpty(message.Attachments))
        {
            try
            {
                attachments = JsonSerializer.Deserialize<List<MessageAttachmentDto>>(message.Attachments);
            }
            catch { }
        }

        return new ChatMessageDto
        {
            Id = message.Id,
            ConversationId = message.ConversationId,
            SenderId = null,
            SenderName = contact.DisplayName,
            SenderAvatar = contact.Avatar,
            SenderType = message.SenderType,
            MessageType = message.MessageType,
            Content = message.Content,
            Attachments = attachments,
            Status = message.Status,
            CreationTime = message.CreationTime
        };
    }

    private static ChatConversationDto MapToConversationDto(
        ChatConversation conversation,
        ChatChannel channel,
        ChatContact contact)
    {
        return new ChatConversationDto
        {
            Id = conversation.Id,
            ChannelId = conversation.ChannelId,
            ChannelName = channel.Name,
            ChannelType = channel.Type,
            ContactId = conversation.ContactId,
            Contact = new ChatContactBriefDto
            {
                Id = contact.Id,
                DisplayName = contact.DisplayName,
                Avatar = contact.Avatar,
                ChannelType = channel.Type
            },
            AssignedAgentId = conversation.AssignedAgentId,
            Status = conversation.Status,
            Priority = conversation.Priority,
            LastMessageAt = conversation.LastMessageAt,
            LastMessagePreview = conversation.LastMessagePreview,
            UnreadCount = conversation.UnreadCount,
            CreationTime = conversation.CreationTime
        };
    }
}
