using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Chat;

/// <summary>
/// DTO cho ChatConversation
/// </summary>
public class ChatConversationDto : EntityDto<Guid>
{
    public Guid ChannelId { get; set; }
    public string ChannelName { get; set; } = string.Empty;
    public ChannelType ChannelType { get; set; }

    public Guid ContactId { get; set; }
    public ChatContactBriefDto? Contact { get; set; }

    public Guid? AssignedAgentId { get; set; }
    public string? AssignedAgentName { get; set; }

    public ConversationStatus Status { get; set; }
    public ConversationPriority Priority { get; set; }
    public string? Subject { get; set; }

    public DateTime? LastMessageAt { get; set; }
    public string? LastMessagePreview { get; set; }
    public int UnreadCount { get; set; }

    public DateTime CreationTime { get; set; }
}

/// <summary>
/// DTO chi tiết ChatConversation (bao gồm messages gần nhất)
/// </summary>
public class ChatConversationDetailDto : ChatConversationDto
{
    public ChatContactDto? ContactDetail { get; set; }
}

/// <summary>
/// Input để filter conversations
/// </summary>
public class GetConversationsInput : PagedResultRequestDto
{
    /// <summary>
    /// Filter theo channel
    /// </summary>
    public Guid? ChannelId { get; set; }

    /// <summary>
    /// Filter theo status
    /// </summary>
    public ConversationStatus? Status { get; set; }

    /// <summary>
    /// Filter theo assigned agent (null = unassigned, Guid = specific agent)
    /// </summary>
    public Guid? AssignedAgentId { get; set; }

    /// <summary>
    /// Chỉ lấy conversations chưa assign
    /// </summary>
    public bool? UnassignedOnly { get; set; }

    /// <summary>
    /// Chỉ lấy conversations của current agent
    /// </summary>
    public bool? MyConversationsOnly { get; set; }

    /// <summary>
    /// Tìm kiếm theo tên contact hoặc nội dung
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Sắp xếp: LastMessageAt (default), CreationTime, UnreadCount
    /// </summary>
    public string? Sorting { get; set; }
}

/// <summary>
/// Input để assign conversation
/// </summary>
public class AssignConversationInput
{
    public Guid ConversationId { get; set; }
    public Guid? AgentId { get; set; }
}

/// <summary>
/// Input để đổi status conversation
/// </summary>
public class UpdateConversationStatusInput
{
    public Guid ConversationId { get; set; }
    public ConversationStatus Status { get; set; }
}

/// <summary>
/// Input để đổi priority
/// </summary>
public class UpdateConversationPriorityInput
{
    public Guid ConversationId { get; set; }
    public ConversationPriority Priority { get; set; }
}
