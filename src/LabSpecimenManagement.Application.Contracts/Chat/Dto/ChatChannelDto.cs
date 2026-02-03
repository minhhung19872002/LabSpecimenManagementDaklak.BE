using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Chat;

/// <summary>
/// DTO cho ChatChannel
/// </summary>
public class ChatChannelDto : EntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;
    public ChannelType Type { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreationTime { get; set; }
}
