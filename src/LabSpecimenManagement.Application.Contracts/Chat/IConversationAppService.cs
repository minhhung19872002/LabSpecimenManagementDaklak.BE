using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.Chat;

/// <summary>
/// Service quản lý cuộc hội thoại
/// </summary>
public interface IConversationAppService : IApplicationService
{
    /// <summary>
    /// Lấy danh sách conversations với filters
    /// </summary>
    Task<PagedResultDto<ChatConversationDto>> GetListAsync(GetConversationsInput input);

    /// <summary>
    /// Lấy chi tiết một conversation
    /// </summary>
    Task<ChatConversationDetailDto> GetAsync(Guid id);

    /// <summary>
    /// Assign conversation cho agent
    /// </summary>
    Task<ChatConversationDto> AssignAsync(AssignConversationInput input);

    /// <summary>
    /// Agent tự nhận conversation
    /// </summary>
    Task<ChatConversationDto> PickupAsync(Guid conversationId);

    /// <summary>
    /// Bỏ assign (trả về queue)
    /// </summary>
    Task<ChatConversationDto> UnassignAsync(Guid conversationId);

    /// <summary>
    /// Đóng conversation
    /// </summary>
    Task<ChatConversationDto> CloseAsync(Guid conversationId);

    /// <summary>
    /// Mở lại conversation
    /// </summary>
    Task<ChatConversationDto> ReopenAsync(Guid conversationId);

    /// <summary>
    /// Đánh dấu đã giải quyết
    /// </summary>
    Task<ChatConversationDto> ResolveAsync(Guid conversationId);

    /// <summary>
    /// Cập nhật priority
    /// </summary>
    Task<ChatConversationDto> UpdatePriorityAsync(UpdateConversationPriorityInput input);

    /// <summary>
    /// Đánh dấu đã đọc tất cả messages trong conversation
    /// </summary>
    Task MarkAsReadAsync(Guid conversationId);

    /// <summary>
    /// Lấy số conversation chưa assign
    /// </summary>
    Task<int> GetUnassignedCountAsync();

    /// <summary>
    /// Lấy số conversation đang active của current agent
    /// </summary>
    Task<int> GetMyActiveCountAsync();
}
