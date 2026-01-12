using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.Users;

/// <summary>
/// Service lấy danh sách users cho dropdown
/// </summary>
public interface IUserLookupAppService : IApplicationService
{
    /// <summary>
    /// Lấy tất cả users đang active cho dropdown
    /// </summary>
    Task<ListResultDto<UserLookupDto>> GetAllAsync();

    /// <summary>
    /// Tìm kiếm users theo tên hoặc username
    /// </summary>
    Task<ListResultDto<UserLookupDto>> SearchAsync(string? filter);

    /// <summary>
    /// Lấy user theo ID
    /// </summary>
    Task<UserLookupDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Lấy users theo danh sách IDs
    /// </summary>
    Task<ListResultDto<UserLookupDto>> GetByIdsAsync(List<Guid> ids);

    /// <summary>
    /// Lấy danh sách users có chức vụ Phó Giám đốc
    /// </summary>
    Task<ListResultDto<UserLookupDto>> GetVicePresidentsAsync();
}
