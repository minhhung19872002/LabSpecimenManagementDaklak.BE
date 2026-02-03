using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Chat;

/// <summary>
/// DTO cho ChatContact
/// </summary>
public class ChatContactDto : EntityDto<Guid>
{
    public Guid ChannelId { get; set; }
    public string ChannelName { get; set; } = string.Empty;
    public ChannelType ChannelType { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Avatar { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public DateTime CreationTime { get; set; }
}

/// <summary>
/// DTO tóm tắt ChatContact (dùng trong list)
/// </summary>
public class ChatContactBriefDto : EntityDto<Guid>
{
    public string? DisplayName { get; set; }
    public string? Avatar { get; set; }
    public ChannelType ChannelType { get; set; }
}

/// <summary>
/// Input để link contact với customer
/// </summary>
public class LinkContactToCustomerInput
{
    public Guid ContactId { get; set; }
    public Guid CustomerId { get; set; }
}
