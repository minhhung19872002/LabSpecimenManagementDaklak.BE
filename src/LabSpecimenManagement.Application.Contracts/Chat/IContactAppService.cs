using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.Chat;

/// <summary>
/// Service quản lý contacts
/// </summary>
public interface IContactAppService : IApplicationService
{
    /// <summary>
    /// Lấy danh sách contacts
    /// </summary>
    Task<PagedResultDto<ChatContactDto>> GetListAsync(PagedResultRequestDto input);

    /// <summary>
    /// Lấy chi tiết contact
    /// </summary>
    Task<ChatContactDto> GetAsync(Guid id);

    /// <summary>
    /// Link contact với customer
    /// </summary>
    Task<ChatContactDto> LinkToCustomerAsync(LinkContactToCustomerInput input);

    /// <summary>
    /// Unlink contact khỏi customer
    /// </summary>
    Task<ChatContactDto> UnlinkFromCustomerAsync(Guid contactId);
}
