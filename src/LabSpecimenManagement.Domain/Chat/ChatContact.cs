using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.Chat;

/// <summary>
/// Thông tin khách hàng từ các kênh giao tiếp
/// </summary>
public class ChatContact : AuditedEntity<Guid>
{
    /// <summary>
    /// ID kênh giao tiếp
    /// </summary>
    public Guid ChannelId { get; set; }

    /// <summary>
    /// ID trên hệ thống ngoài (Zalo User ID, FB PSID, etc.)
    /// </summary>
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>
    /// Tên hiển thị
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// URL avatar
    /// </summary>
    public string? Avatar { get; set; }

    /// <summary>
    /// Số điện thoại (nếu có)
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Email (nếu có)
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Liên kết với Customer trong hệ thống (nếu đã match)
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// Dữ liệu bổ sung từ kênh dạng JSON
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Navigation property - Kênh giao tiếp
    /// </summary>
    public virtual ChatChannel? Channel { get; set; }

    protected ChatContact()
    {
    }

    public ChatContact(
        Guid id,
        Guid channelId,
        string externalId,
        string? displayName = null,
        string? avatar = null) : base(id)
    {
        ChannelId = channelId;
        ExternalId = externalId;
        DisplayName = displayName;
        Avatar = avatar;
    }
}
