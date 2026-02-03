using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LabSpecimenManagement.Chat.Channels;
using LabSpecimenManagement.Common.Exceptions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace LabSpecimenManagement.Chat;

public class MessageAppService : ApplicationService, IMessageAppService
{
    private readonly IRepository<ChatMessage, Guid> _messageRepository;
    private readonly IRepository<ChatConversation, Guid> _conversationRepository;
    private readonly IRepository<ChatContact, Guid> _contactRepository;
    private readonly IRepository<ChatChannel, Guid> _channelRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityUserRepository _userRepository;
    private readonly IChatHubService? _chatHubService;
    private readonly IEnumerable<IChannelMessageSender> _channelSenders;

    public MessageAppService(
        IRepository<ChatMessage, Guid> messageRepository,
        IRepository<ChatConversation, Guid> conversationRepository,
        IRepository<ChatContact, Guid> contactRepository,
        IRepository<ChatChannel, Guid> channelRepository,
        ICurrentUser currentUser,
        IIdentityUserRepository userRepository,
        IEnumerable<IChannelMessageSender> channelSenders,
        IChatHubService? chatHubService = null)
    {
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
        _contactRepository = contactRepository;
        _channelRepository = channelRepository;
        _currentUser = currentUser;
        _userRepository = userRepository;
        _channelSenders = channelSenders;
        _chatHubService = chatHubService;
    }

    public async Task<PagedResultDto<ChatMessageDto>> GetListAsync(GetMessagesInput input)
    {
        var queryable = await _messageRepository.GetQueryableAsync();

        queryable = queryable.Where(m => m.ConversationId == input.ConversationId);

        if (input.Before.HasValue)
        {
            queryable = queryable.Where(m => m.CreationTime < input.Before.Value);
        }

        if (input.After.HasValue)
        {
            queryable = queryable.Where(m => m.CreationTime > input.After.Value);
        }

        // Order by creation time descending (newest first for pagination)
        queryable = queryable.OrderByDescending(m => m.CreationTime);

        var totalCount = await AsyncExecuter.CountAsync(queryable);

        var items = await AsyncExecuter.ToListAsync(
            queryable.Skip(input.SkipCount).Take(input.MaxResultCount)
        );

        // Reverse to show oldest first in the result
        items.Reverse();

        var dtos = new List<ChatMessageDto>();
        foreach (var item in items)
        {
            var dto = await MapToMessageDtoAsync(item);
            dtos.Add(dto);
        }

        return new PagedResultDto<ChatMessageDto>(totalCount, dtos);
    }

    public async Task<SendMessageResult> SendAsync(SendMessageInput input)
    {
        var currentUserId = _currentUser.Id ?? throw new System.UnauthorizedAccessException("Người dùng chưa đăng nhập");

        // Get conversation
        var conversation = await _conversationRepository.GetAsync(input.ConversationId);

        // Verify agent is assigned to this conversation
        if (conversation.AssignedAgentId != currentUserId)
        {
            return new SendMessageResult
            {
                Success = false,
                ErrorMessage = "Bạn chưa được phân công xử lý cuộc hội thoại này"
            };
        }

        // Create message in DB first
        var message = new ChatMessage(
            GuidGenerator.Create(),
            conversation.Id,
            MessageSenderType.Agent,
            input.MessageType,
            input.Content,
            currentUserId
        );

        if (input.Attachments != null && input.Attachments.Count > 0)
        {
            message.Attachments = JsonSerializer.Serialize(input.Attachments);
        }

        message.Status = MessageStatus.Sending;
        await _messageRepository.InsertAsync(message);

        // Get contact
        var contact = await _contactRepository.GetAsync(conversation.ContactId);

        // Get channel
        var channel = await _channelRepository.GetAsync(conversation.ChannelId);

        // Find channel sender
        var sender = _channelSenders.FirstOrDefault(s => s.ChannelType == channel.Type);
        if (sender == null)
        {
            message.MarkAsFailed($"Không hỗ trợ gửi tin nhắn qua kênh {channel.Type}");
            await _messageRepository.UpdateAsync(message);

            return new SendMessageResult
            {
                Success = false,
                ErrorMessage = message.ErrorMessage,
                Message = await MapToMessageDtoAsync(message)
            };
        }

        // Send to external channel
        ChannelSendResult result;
        try
        {
            result = input.MessageType switch
            {
                MessageType.Image when input.Attachments?.Count > 0 =>
                    await sender.SendImageAsync(contact.ExternalId, input.Attachments[0].Url),
                MessageType.File when input.Attachments?.Count > 0 =>
                    await sender.SendFileAsync(contact.ExternalId, input.Attachments[0].Url, input.Attachments[0].Name ?? "file"),
                _ => await sender.SendTextAsync(contact.ExternalId, input.Content ?? string.Empty)
            };
        }
        catch (Exception ex)
        {
            result = ChannelSendResult.Fail(ex.Message);
        }

        // Update message status
        if (result.Success)
        {
            message.MarkAsSent(result.ExternalMessageId);
        }
        else
        {
            message.MarkAsFailed(result.ErrorMessage ?? "Gửi tin nhắn thất bại");
        }

        await _messageRepository.UpdateAsync(message);

        // Update conversation
        conversation.UpdateLastMessage(
            GetMessagePreview(message),
            message.CreationTime,
            fromContact: false
        );
        await _conversationRepository.UpdateAsync(conversation);

        var messageDto = await MapToMessageDtoAsync(message);

        // Notify via SignalR
        if (_chatHubService != null)
        {
            if (result.Success)
            {
                await _chatHubService.NotifyMessageSentAsync(conversation.Id, messageDto);
            }
            else
            {
                await _chatHubService.NotifyMessageFailedAsync(conversation.Id, messageDto);
            }
        }

        return new SendMessageResult
        {
            Success = result.Success,
            Message = messageDto,
            ErrorMessage = result.ErrorMessage
        };
    }

    public async Task<SendMessageResult> RetryAsync(Guid messageId)
    {
        var message = await _messageRepository.GetAsync(messageId);

        if (message.Status != MessageStatus.Failed)
        {
            return new SendMessageResult
            {
                Success = false,
                ErrorMessage = "Chỉ có thể retry tin nhắn đã gửi thất bại"
            };
        }

        if (message.SenderType != MessageSenderType.Agent)
        {
            return new SendMessageResult
            {
                Success = false,
                ErrorMessage = "Chỉ có thể retry tin nhắn từ agent"
            };
        }

        var conversation = await _conversationRepository.GetAsync(message.ConversationId);
        var contact = await _contactRepository.GetAsync(conversation.ContactId);
        var channel = await _channelRepository.GetAsync(conversation.ChannelId);

        var sender = _channelSenders.FirstOrDefault(s => s.ChannelType == channel.Type);
        if (sender == null)
        {
            return new SendMessageResult
            {
                Success = false,
                ErrorMessage = $"Không hỗ trợ gửi tin nhắn qua kênh {channel.Type}"
            };
        }

        // Retry sending
        message.Status = MessageStatus.Sending;
        await _messageRepository.UpdateAsync(message);

        ChannelSendResult result;
        try
        {
            var attachments = !string.IsNullOrEmpty(message.Attachments)
                ? JsonSerializer.Deserialize<List<MessageAttachmentDto>>(message.Attachments)
                : null;

            result = message.MessageType switch
            {
                MessageType.Image when attachments?.Count > 0 =>
                    await sender.SendImageAsync(contact.ExternalId, attachments[0].Url),
                MessageType.File when attachments?.Count > 0 =>
                    await sender.SendFileAsync(contact.ExternalId, attachments[0].Url, attachments[0].Name ?? "file"),
                _ => await sender.SendTextAsync(contact.ExternalId, message.Content ?? string.Empty)
            };
        }
        catch (Exception ex)
        {
            result = ChannelSendResult.Fail(ex.Message);
        }

        if (result.Success)
        {
            message.MarkAsSent(result.ExternalMessageId);
        }
        else
        {
            message.MarkAsFailed(result.ErrorMessage ?? "Gửi tin nhắn thất bại");
        }

        await _messageRepository.UpdateAsync(message);

        var messageDto = await MapToMessageDtoAsync(message);

        if (_chatHubService != null)
        {
            if (result.Success)
            {
                await _chatHubService.NotifyMessageSentAsync(conversation.Id, messageDto);
            }
            else
            {
                await _chatHubService.NotifyMessageFailedAsync(conversation.Id, messageDto);
            }
        }

        return new SendMessageResult
        {
            Success = result.Success,
            Message = messageDto,
            ErrorMessage = result.ErrorMessage
        };
    }

    #region Private Methods

    private async Task<ChatMessageDto> MapToMessageDtoAsync(ChatMessage entity)
    {
        string? senderName = null;
        string? senderAvatar = null;

        if (entity.SenderType == MessageSenderType.Agent && entity.SenderId.HasValue)
        {
            var user = await _userRepository.FindAsync(entity.SenderId.Value);
            senderName = user?.Name ?? user?.UserName;
        }
        else if (entity.SenderType == MessageSenderType.Contact)
        {
            var conversation = await _conversationRepository.FindAsync(entity.ConversationId);
            if (conversation != null)
            {
                var contact = await _contactRepository.FindAsync(conversation.ContactId);
                senderName = contact?.DisplayName;
                senderAvatar = contact?.Avatar;
            }
        }

        List<MessageAttachmentDto>? attachments = null;
        if (!string.IsNullOrEmpty(entity.Attachments))
        {
            try
            {
                attachments = JsonSerializer.Deserialize<List<MessageAttachmentDto>>(entity.Attachments);
            }
            catch
            {
                // Ignore deserialization errors
            }
        }

        return new ChatMessageDto
        {
            Id = entity.Id,
            ConversationId = entity.ConversationId,
            SenderId = entity.SenderId,
            SenderName = senderName,
            SenderAvatar = senderAvatar,
            SenderType = entity.SenderType,
            MessageType = entity.MessageType,
            Content = entity.Content,
            Attachments = attachments,
            Status = entity.Status,
            ErrorMessage = entity.ErrorMessage,
            CreationTime = entity.CreationTime
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

    #endregion
}
