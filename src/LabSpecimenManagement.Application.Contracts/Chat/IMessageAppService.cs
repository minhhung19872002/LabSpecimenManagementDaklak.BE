using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.Chat;

/// <summary>
/// Service quản lý tin nhắn
/// </summary>
public interface IMessageAppService : IApplicationService
{
    /// <summary>
    /// Lấy danh sách messages của conversation
    /// </summary>
    Task<PagedResultDto<ChatMessageDto>> GetListAsync(GetMessagesInput input);

    /// <summary>
    /// Agent gửi tin nhắn
    /// </summary>
    Task<SendMessageResult> SendAsync(SendMessageInput input);

    /// <summary>
    /// Retry gửi tin nhắn bị fail
    /// </summary>
    Task<SendMessageResult> RetryAsync(Guid messageId);
}
