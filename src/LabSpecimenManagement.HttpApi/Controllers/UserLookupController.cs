using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LabSpecimenManagement.Common.Exceptions;
using LabSpecimenManagement.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

/// <summary>
/// API lấy danh sách users cho dropdown
/// </summary>
[RemoteService]
[Route("api/v1/users")]
[Authorize]
public class UserLookupController : LabSpecimenManagementController
{
    private readonly IUserLookupAppService _userLookupAppService;

    public UserLookupController(IUserLookupAppService userLookupAppService)
    {
        _userLookupAppService = userLookupAppService;
    }

    /// <summary>
    /// Lấy tất cả users đang active cho dropdown
    /// </summary>
    [HttpGet("lookup")]
    public async Task<ListResultDto<UserLookupDto>> GetAllAsync()
    {
        return await _userLookupAppService.GetAllAsync();
    }

    /// <summary>
    /// Tìm kiếm users theo tên hoặc username
    /// </summary>
    [HttpGet("lookup/search")]
    public async Task<ListResultDto<UserLookupDto>> SearchAsync([FromQuery] string? filter)
    {
        return await _userLookupAppService.SearchAsync(filter);
    }

    /// <summary>
    /// Lấy user theo ID
    /// </summary>
    [HttpGet("lookup/{id}")]
    public async Task<UserLookupDto> GetByIdAsync(Guid id)
    {
        var result = await _userLookupAppService.GetByIdAsync(id);
        if (result == null)
        {
            throw new EntityNotFoundException("User", id);
        }
        return result;
    }

    /// <summary>
    /// Lấy users theo danh sách IDs
    /// </summary>
    [HttpPost("lookup/by-ids")]
    public async Task<ListResultDto<UserLookupDto>> GetByIdsAsync([FromBody] List<Guid> ids)
    {
        return await _userLookupAppService.GetByIdsAsync(ids);
    }

    /// <summary>
    /// Lấy danh sách users có chức vụ Phó Giám đốc
    /// </summary>
    [HttpGet("lookup/vice-presidents")]
    public async Task<ListResultDto<UserLookupDto>> GetVicePresidentsAsync()
    {
        return await _userLookupAppService.GetVicePresidentsAsync();
    }
}
