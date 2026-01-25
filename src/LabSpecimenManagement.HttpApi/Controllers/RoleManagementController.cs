using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.RoleManagement;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Route("api/app/role-management")]
[Authorize(LabSpecimenManagementPermissions.RoleManagement.Default)]
public class RoleManagementController : LabSpecimenManagementController
{
    private readonly IRoleManagementAppService _roleManagementAppService;

    public RoleManagementController(IRoleManagementAppService roleManagementAppService)
    {
        _roleManagementAppService = roleManagementAppService;
    }

    #region Role CRUD

    /// <summary>
    /// Get paginated list of roles
    /// </summary>
    [HttpGet]
    public virtual Task<PagedResultDto<RoleDto>> GetListAsync([FromQuery] GetRolesInput input)
    {
        return _roleManagementAppService.GetListAsync(input);
    }

    /// <summary>
    /// Get role detail by id
    /// </summary>
    [HttpGet("{id:guid}")]
    public virtual Task<RoleDetailDto> GetAsync(Guid id)
    {
        return _roleManagementAppService.GetAsync(id);
    }

    /// <summary>
    /// Create a new role
    /// </summary>
    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.RoleManagement.Create)]
    public virtual Task<RoleDto> CreateAsync([FromBody] CreateRoleInput input)
    {
        return _roleManagementAppService.CreateAsync(input);
    }

    /// <summary>
    /// Update a role
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(LabSpecimenManagementPermissions.RoleManagement.Edit)]
    public virtual Task<RoleDto> UpdateAsync(Guid id, [FromBody] UpdateRoleInput input)
    {
        return _roleManagementAppService.UpdateAsync(id, input);
    }

    /// <summary>
    /// Delete a role
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(LabSpecimenManagementPermissions.RoleManagement.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _roleManagementAppService.DeleteAsync(id);
    }

    #endregion

    #region Permissions

    /// <summary>
    /// Get all available permissions grouped by category
    /// </summary>
    [HttpGet("permissions/all")]
    public virtual Task<List<PermissionGroupDto>> GetAllPermissionsAsync()
    {
        return _roleManagementAppService.GetAllPermissionsAsync();
    }

    /// <summary>
    /// Get permissions for a specific role
    /// </summary>
    [HttpGet("{roleName}/permissions")]
    public virtual Task<List<PermissionGroupDto>> GetRolePermissionsAsync(string roleName)
    {
        return _roleManagementAppService.GetRolePermissionsAsync(roleName);
    }

    /// <summary>
    /// Update permissions for a role
    /// </summary>
    [HttpPut("{roleName}/permissions")]
    [Authorize(LabSpecimenManagementPermissions.RoleManagement.ManagePermissions)]
    public virtual Task UpdateRolePermissionsAsync(string roleName, [FromBody] UpdateRolePermissionsInput input)
    {
        return _roleManagementAppService.UpdateRolePermissionsAsync(roleName, input);
    }

    #endregion

    #region Users

    /// <summary>
    /// Get list of users in a role
    /// </summary>
    [HttpGet("{roleId:guid}/users")]
    public virtual Task<List<RoleUserDto>> GetRoleUsersAsync(Guid roleId)
    {
        return _roleManagementAppService.GetRoleUsersAsync(roleId);
    }

    /// <summary>
    /// Add a user to a role
    /// </summary>
    [HttpPost("{roleId:guid}/users/{userId:guid}")]
    [Authorize(LabSpecimenManagementPermissions.RoleManagement.ManageUsers)]
    public virtual Task AddUserToRoleAsync(Guid roleId, Guid userId)
    {
        return _roleManagementAppService.AddUserToRoleAsync(roleId, userId);
    }

    /// <summary>
    /// Remove a user from a role
    /// </summary>
    [HttpDelete("{roleId:guid}/users/{userId:guid}")]
    [Authorize(LabSpecimenManagementPermissions.RoleManagement.ManageUsers)]
    public virtual Task RemoveUserFromRoleAsync(Guid roleId, Guid userId)
    {
        return _roleManagementAppService.RemoveUserFromRoleAsync(roleId, userId);
    }

    /// <summary>
    /// Get available users that can be added to a role
    /// </summary>
    [HttpGet("{roleId:guid}/available-users")]
    public virtual Task<List<RoleUserDto>> GetAvailableUsersAsync(Guid roleId, [FromQuery] string? filter)
    {
        return _roleManagementAppService.GetAvailableUsersAsync(roleId, filter);
    }

    #endregion
}
