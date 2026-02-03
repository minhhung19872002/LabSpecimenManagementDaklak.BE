using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common.Exceptions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Users;

namespace LabSpecimenManagement.Chat;

public class ConversationAppService : ApplicationService, IConversationAppService
{
    private readonly IRepository<ChatConversation, Guid> _conversationRepository;
    private readonly IRepository<ChatContact, Guid> _contactRepository;
    private readonly IRepository<ChatChannel, Guid> _channelRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IIdentityUserRepository _userRepository;
    private readonly IChatHubService? _chatHubService;

    public ConversationAppService(
        IRepository<ChatConversation, Guid> conversationRepository,
        IRepository<ChatContact, Guid> contactRepository,
        IRepository<ChatChannel, Guid> channelRepository,
        ICurrentUser currentUser,
        IIdentityUserRepository userRepository,
        IChatHubService? chatHubService = null)
    {
        _conversationRepository = conversationRepository;
        _contactRepository = contactRepository;
        _channelRepository = channelRepository;
        _currentUser = currentUser;
        _userRepository = userRepository;
        _chatHubService = chatHubService;
    }

    public async Task<PagedResultDto<ChatConversationDto>> GetListAsync(GetConversationsInput input)
    {
        var currentUserId = _currentUser.Id ?? throw new System.UnauthorizedAccessException("Người dùng chưa đăng nhập");

        var queryable = await _conversationRepository.GetQueryableAsync();

        // Apply filters
        if (input.ChannelId.HasValue)
        {
            queryable = queryable.Where(c => c.ChannelId == input.ChannelId.Value);
        }

        if (input.Status.HasValue)
        {
            queryable = queryable.Where(c => c.Status == input.Status.Value);
        }

        if (input.UnassignedOnly == true)
        {
            queryable = queryable.Where(c => c.AssignedAgentId == null);
        }
        else if (input.MyConversationsOnly == true)
        {
            queryable = queryable.Where(c => c.AssignedAgentId == currentUserId);
        }
        else if (input.AssignedAgentId.HasValue)
        {
            queryable = queryable.Where(c => c.AssignedAgentId == input.AssignedAgentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.SearchText))
        {
            var searchLower = input.SearchText.ToLower();
            queryable = queryable.Where(c =>
                (c.LastMessagePreview != null && c.LastMessagePreview.ToLower().Contains(searchLower)) ||
                (c.Subject != null && c.Subject.ToLower().Contains(searchLower)));
        }

        // Apply sorting
        queryable = input.Sorting?.ToLower() switch
        {
            "creationtime" => queryable.OrderByDescending(c => c.CreationTime),
            "unreadcount" => queryable.OrderByDescending(c => c.UnreadCount).ThenByDescending(c => c.LastMessageAt),
            _ => queryable.OrderByDescending(c => c.LastMessageAt ?? c.CreationTime)
        };

        var totalCount = await AsyncExecuter.CountAsync(queryable);

        var items = await AsyncExecuter.ToListAsync(
            queryable.Skip(input.SkipCount).Take(input.MaxResultCount)
        );

        var dtos = new List<ChatConversationDto>();
        foreach (var item in items)
        {
            var dto = await MapToConversationDtoAsync(item);
            dtos.Add(dto);
        }

        return new PagedResultDto<ChatConversationDto>(totalCount, dtos);
    }

    public async Task<ChatConversationDetailDto> GetAsync(Guid id)
    {
        var conversation = await _conversationRepository.GetAsync(id);
        return await MapToConversationDetailDtoAsync(conversation);
    }

    public async Task<ChatConversationDto> AssignAsync(AssignConversationInput input)
    {
        var conversation = await _conversationRepository.GetAsync(input.ConversationId);

        if (input.AgentId.HasValue)
        {
            conversation.AssignToAgent(input.AgentId.Value);
        }
        else
        {
            conversation.Unassign();
        }

        await _conversationRepository.UpdateAsync(conversation);

        var dto = await MapToConversationDtoAsync(conversation);

        // Notify via SignalR
        if (_chatHubService != null)
        {
            await _chatHubService.NotifyConversationUpdatedAsync(dto);
            if (input.AgentId.HasValue)
            {
                await _chatHubService.NotifyConversationAssignedAsync(input.AgentId.Value, dto);
            }
        }

        return dto;
    }

    public async Task<ChatConversationDto> PickupAsync(Guid conversationId)
    {
        var currentUserId = _currentUser.Id ?? throw new System.UnauthorizedAccessException("Người dùng chưa đăng nhập");

        var conversation = await _conversationRepository.GetAsync(conversationId);

        if (conversation.AssignedAgentId.HasValue && conversation.AssignedAgentId != currentUserId)
        {
            throw new ForbiddenAccessException("Cuộc hội thoại này đã được agent khác nhận");
        }

        conversation.AssignToAgent(currentUserId);
        await _conversationRepository.UpdateAsync(conversation);

        var dto = await MapToConversationDtoAsync(conversation);

        if (_chatHubService != null)
        {
            await _chatHubService.NotifyConversationUpdatedAsync(dto);
        }

        return dto;
    }

    public async Task<ChatConversationDto> UnassignAsync(Guid conversationId)
    {
        var currentUserId = _currentUser.Id ?? throw new System.UnauthorizedAccessException("Người dùng chưa đăng nhập");

        var conversation = await _conversationRepository.GetAsync(conversationId);

        // Chỉ agent đang được assign hoặc admin mới có quyền unassign
        if (conversation.AssignedAgentId.HasValue && conversation.AssignedAgentId != currentUserId)
        {
            // TODO: Check if current user is admin
            throw new ForbiddenAccessException("Bạn không có quyền bỏ nhận cuộc hội thoại này");
        }

        conversation.Unassign();
        await _conversationRepository.UpdateAsync(conversation);

        var dto = await MapToConversationDtoAsync(conversation);

        if (_chatHubService != null)
        {
            await _chatHubService.NotifyConversationUpdatedAsync(dto);
        }

        return dto;
    }

    public async Task<ChatConversationDto> CloseAsync(Guid conversationId)
    {
        var conversation = await _conversationRepository.GetAsync(conversationId);
        conversation.Close();
        await _conversationRepository.UpdateAsync(conversation);

        var dto = await MapToConversationDtoAsync(conversation);

        if (_chatHubService != null)
        {
            await _chatHubService.NotifyConversationUpdatedAsync(dto);
        }

        return dto;
    }

    public async Task<ChatConversationDto> ReopenAsync(Guid conversationId)
    {
        var conversation = await _conversationRepository.GetAsync(conversationId);
        conversation.Reopen();
        await _conversationRepository.UpdateAsync(conversation);

        var dto = await MapToConversationDtoAsync(conversation);

        if (_chatHubService != null)
        {
            await _chatHubService.NotifyConversationUpdatedAsync(dto);
        }

        return dto;
    }

    public async Task<ChatConversationDto> ResolveAsync(Guid conversationId)
    {
        var conversation = await _conversationRepository.GetAsync(conversationId);
        conversation.Resolve();
        await _conversationRepository.UpdateAsync(conversation);

        var dto = await MapToConversationDtoAsync(conversation);

        if (_chatHubService != null)
        {
            await _chatHubService.NotifyConversationUpdatedAsync(dto);
        }

        return dto;
    }

    public async Task<ChatConversationDto> UpdatePriorityAsync(UpdateConversationPriorityInput input)
    {
        var conversation = await _conversationRepository.GetAsync(input.ConversationId);
        conversation.Priority = input.Priority;
        await _conversationRepository.UpdateAsync(conversation);

        var dto = await MapToConversationDtoAsync(conversation);

        if (_chatHubService != null)
        {
            await _chatHubService.NotifyConversationUpdatedAsync(dto);
        }

        return dto;
    }

    public async Task MarkAsReadAsync(Guid conversationId)
    {
        var conversation = await _conversationRepository.GetAsync(conversationId);
        conversation.MarkAsRead();
        await _conversationRepository.UpdateAsync(conversation);
    }

    public async Task<int> GetUnassignedCountAsync()
    {
        var queryable = await _conversationRepository.GetQueryableAsync();
        return await AsyncExecuter.CountAsync(
            queryable.Where(c =>
                c.AssignedAgentId == null &&
                (c.Status == ConversationStatus.Pending || c.Status == ConversationStatus.Active))
        );
    }

    public async Task<int> GetMyActiveCountAsync()
    {
        var currentUserId = _currentUser.Id ?? throw new System.UnauthorizedAccessException("Người dùng chưa đăng nhập");

        var queryable = await _conversationRepository.GetQueryableAsync();
        return await AsyncExecuter.CountAsync(
            queryable.Where(c =>
                c.AssignedAgentId == currentUserId &&
                (c.Status == ConversationStatus.Pending || c.Status == ConversationStatus.Active))
        );
    }

    #region Private Methods

    private async Task<ChatConversationDto> MapToConversationDtoAsync(ChatConversation entity)
    {
        string? agentName = null;
        if (entity.AssignedAgentId.HasValue)
        {
            var agent = await _userRepository.FindAsync(entity.AssignedAgentId.Value);
            agentName = agent?.Name ?? agent?.UserName;
        }

        // Load channel
        var channel = await _channelRepository.FindAsync(entity.ChannelId);

        // Load contact
        var contact = await _contactRepository.FindAsync(entity.ContactId);

        return new ChatConversationDto
        {
            Id = entity.Id,
            ChannelId = entity.ChannelId,
            ChannelName = channel?.Name ?? string.Empty,
            ChannelType = channel?.Type ?? ChannelType.Zalo,
            ContactId = entity.ContactId,
            Contact = contact != null ? new ChatContactBriefDto
            {
                Id = contact.Id,
                DisplayName = contact.DisplayName,
                Avatar = contact.Avatar,
                ChannelType = channel?.Type ?? ChannelType.Zalo
            } : null,
            AssignedAgentId = entity.AssignedAgentId,
            AssignedAgentName = agentName,
            Status = entity.Status,
            Priority = entity.Priority,
            Subject = entity.Subject,
            LastMessageAt = entity.LastMessageAt,
            LastMessagePreview = entity.LastMessagePreview,
            UnreadCount = entity.UnreadCount,
            CreationTime = entity.CreationTime
        };
    }

    private async Task<ChatConversationDetailDto> MapToConversationDetailDtoAsync(ChatConversation entity)
    {
        var baseDto = await MapToConversationDtoAsync(entity);

        // Load contact for detail
        var contact = await _contactRepository.FindAsync(entity.ContactId);
        var channel = await _channelRepository.FindAsync(entity.ChannelId);

        return new ChatConversationDetailDto
        {
            Id = baseDto.Id,
            ChannelId = baseDto.ChannelId,
            ChannelName = baseDto.ChannelName,
            ChannelType = baseDto.ChannelType,
            ContactId = baseDto.ContactId,
            Contact = baseDto.Contact,
            AssignedAgentId = baseDto.AssignedAgentId,
            AssignedAgentName = baseDto.AssignedAgentName,
            Status = baseDto.Status,
            Priority = baseDto.Priority,
            Subject = baseDto.Subject,
            LastMessageAt = baseDto.LastMessageAt,
            LastMessagePreview = baseDto.LastMessagePreview,
            UnreadCount = baseDto.UnreadCount,
            CreationTime = baseDto.CreationTime,
            ContactDetail = contact != null ? new ChatContactDto
            {
                Id = contact.Id,
                ChannelId = contact.ChannelId,
                ChannelName = channel?.Name ?? string.Empty,
                ChannelType = channel?.Type ?? ChannelType.Zalo,
                ExternalId = contact.ExternalId,
                DisplayName = contact.DisplayName,
                Avatar = contact.Avatar,
                PhoneNumber = contact.PhoneNumber,
                Email = contact.Email,
                CustomerId = contact.CustomerId,
                CreationTime = contact.CreationTime
            } : null
        };
    }

    #endregion
}
