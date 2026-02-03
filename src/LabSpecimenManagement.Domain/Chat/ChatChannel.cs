using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.Chat;

/// <summary>
/// Kênh giao tiếp (Zalo OA, Facebook, Email, etc.)
/// </summary>
public class ChatChannel : AuditedAggregateRoot<Guid>
{
    /// <summary>
    /// Tên kênh (VD: "Zalo OA - Lab Đắk Lắk")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Loại kênh
    /// </summary>
    public ChannelType Type { get; set; }

    /// <summary>
    /// Cấu hình kênh dạng JSON (AppId, SecretKey, etc.)
    /// </summary>
    public string? Configuration { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; } = true;

    protected ChatChannel()
    {
    }

    public ChatChannel(
        Guid id,
        string name,
        ChannelType type,
        string? configuration = null) : base(id)
    {
        Name = name;
        Type = type;
        Configuration = configuration;
        IsActive = true;
    }
}
