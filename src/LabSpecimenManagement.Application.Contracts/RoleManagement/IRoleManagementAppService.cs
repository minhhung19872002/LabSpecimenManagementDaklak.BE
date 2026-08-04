using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LabSpecimenManagement.RoleWarehouses;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.RoleManagement;

/// <summary>
/// Application service interface for role management
/// </summary>
public interface IRoleManagementAppService : IApplicationService
{
    #region Role CRUD

    /// <summary>
    /// Get paginated list of roles
    /// </summary>
    Task<PagedResultDto<RoleDto>> GetListAsync(GetRolesInput input);

    /// <summary>
    /// Get role detail by id
    /// </summary>
    Task<RoleDetailDto> GetAsync(Guid id);

    /// <summary>
    /// Create a new role
    /// </summary>
    Task<RoleDto> CreateAsync(CreateRoleInput input);

    /// <summary>
    /// Update a role
    /// </summary>
    Task<RoleDto> UpdateAsync(Guid id, UpdateRoleInput input);

    /// <summary>
    /// Delete a role
    /// </summary>
    Task DeleteAsync(Guid id);

    #endregion

    #region Permissions

    /// <summary>
    /// Get all available permissions grouped by category
    /// </summary>
    Task<List<PermissionGroupDto>> GetAllPermissionsAsync();

    /// <summary>
    /// Get permissions for a specific role
    /// </summary>
    Task<List<PermissionGroupDto>> GetRolePermissionsAsync(string roleName);

    /// <summary>
    /// Update permissions for a role
    /// </summary>
    Task UpdateRolePermissionsAsync(string roleName, UpdateRolePermissionsInput input);

    #endregion

    #region Users

    /// <summary>
    /// Get list of users in a role
    /// </summary>
    Task<List<RoleUserDto>> GetRoleUsersAsync(Guid roleId);

    /// <summary>
    /// Add a user to a role
    /// </summary>
    Task AddUserToRoleAsync(Guid roleId, Guid userId);

    /// <summary>
    /// Remove a user from a role
    /// </summary>
    Task RemoveUserFromRoleAsync(Guid roleId, Guid userId);

    /// <summary>
    /// Get available users that can be added to a role
    /// </summary>
    Task<List<RoleUserDto>> GetAvailableUsersAsync(Guid roleId, string? filter);

    #endregion

    #region Warehouse Assignments

    Task<List<RoleWarehouseDto>> GetRoleWarehousesAsync(Guid roleId);

    Task AssignRoleWarehousesAsync(Guid roleId, AssignRoleWarehousesInput input);

    #endregion
}
