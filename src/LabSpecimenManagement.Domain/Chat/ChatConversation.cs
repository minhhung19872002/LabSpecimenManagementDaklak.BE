using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.Chat;

/// <summary>
/// Cuộc hội thoại với khách hàng
/// </summary>
public class ChatConversation : FullAuditedAggregateRoot<Guid>
{
    /// <summary>
    /// ID kênh giao tiếp
    /// </summary>
    public Guid ChannelId { get; set; }

    /// <summary>
    /// ID khách hàng (ChatContact)
    /// </summary>
    public Guid ContactId { get; set; }

    /// <summary>
    /// ID agent được phân công xử lý
    /// </summary>
    public Guid? AssignedAgentId { get; set; }

    /// <summary>
    /// Trạng thái cuộc hội thoại
    /// </summary>
    public ConversationStatus Status { get; set; } = ConversationStatus.Pending;

    /// <summary>
    /// Mức độ ưu tiên
    /// </summary>
    public ConversationPriority Priority { get; set; } = ConversationPriority.Normal;

    /// <summary>
    /// Chủ đề/Tiêu đề cuộc hội thoại
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Thời gian tin nhắn cuối cùng
    /// </summary>
    public DateTime? LastMessageAt { get; set; }

    /// <summary>
    /// Preview tin nhắn cuối cùng
    /// </summary>
    public string? LastMessagePreview { get; set; }

    /// <summary>
    /// Số tin nhắn chưa đọc (từ khách hàng)
    /// </summary>
    public int UnreadCount { get; set; } = 0;

    /// <summary>
    /// Dữ liệu bổ sung dạng JSON
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Navigation property - Kênh giao tiếp
    /// </summary>
    public virtual ChatChannel? Channel { get; set; }

    /// <summary>
    /// Navigation property - Khách hàng
    /// </summary>
    public virtual ChatContact? Contact { get; set; }

    protected ChatConversation()
    {
    }

    public ChatConversation(
        Guid id,
        Guid channelId,
        Guid contactId) : base(id)
    {
        ChannelId = channelId;
        ContactId = contactId;
        Status = ConversationStatus.Pending;
        Priority = ConversationPriority.Normal;
        UnreadCount = 0;
    }

    /// <summary>
    /// Phân công agent xử lý
    /// </summary>
    public void AssignToAgent(Guid agentId)
    {
        AssignedAgentId = agentId;
        if (Status == ConversationStatus.Pending)
        {
            Status = ConversationStatus.Active;
        }
    }

    /// <summary>
    /// Bỏ phân công agent
    /// </summary>
    public void Unassign()
    {
        AssignedAgentId = null;
        Status = ConversationStatus.Pending;
    }

    /// <summary>
    /// Đánh dấu đã giải quyết
    /// </summary>
    public void Resolve()
    {
        Status = ConversationStatus.Resolved;
    }

    /// <summary>
    /// Đóng cuộc hội thoại
    /// </summary>
    public void Close()
    {
        Status = ConversationStatus.Closed;
    }

    /// <summary>
    /// Mở lại cuộc hội thoại
    /// </summary>
    public void Reopen()
    {
        Status = AssignedAgentId.HasValue ? ConversationStatus.Active : ConversationStatus.Pending;
    }

    /// <summary>
    /// Cập nhật tin nhắn cuối
    /// </summary>
    public void UpdateLastMessage(string preview, DateTime messageTime, bool fromContact)
    {
        LastMessagePreview = preview?.Length > ChatConsts.MaxLastMessagePreviewLength
            ? preview.Substring(0, ChatConsts.MaxLastMessagePreviewLength)
            : preview;
        LastMessageAt = messageTime;

        if (fromContact)
        {
            UnreadCount++;
        }

        // Nếu đã closed/resolved mà khách gửi tin mới => reopen
        if (fromContact && (Status == ConversationStatus.Closed || Status == ConversationStatus.Resolved))
        {
            Reopen();
        }
    }

    /// <summary>
    /// Đánh dấu đã đọc tất cả
    /// </summary>
    public void MarkAsRead()
    {
        UnreadCount = 0;
    }
}
