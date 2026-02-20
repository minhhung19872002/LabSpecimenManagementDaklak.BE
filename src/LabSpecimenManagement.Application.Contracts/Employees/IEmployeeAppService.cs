using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LabSpecimenManagement.RoleManagement;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.Employees;

public interface IEmployeeAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeDto>> GetListAsync(GetEmployeesInput input);
    Task<EmployeeDto> GetAsync(Guid id);
    Task<EmployeeDto> CreateAsync(CreateEmployeeDto input);
    Task<EmployeeDto> UpdateAsync(Guid id, UpdateEmployeeDto input);
    Task DeleteAsync(Guid id);
    Task<EmployeeDto> ToggleActiveAsync(Guid id, bool isActive);
    Task<ResetPasswordResultDto> ResetPasswordAsync(Guid id, ResetPasswordDto input);
    Task<int> DeactivateExpiredAccountsAsync();
    Task<bool> IsAccountExpiredAsync(Guid userId);

    #region User Permissions Management

    /// <summary>
    /// Get all available permissions (same as role management)
    /// </summary>
    Task<List<PermissionGroupDto>> GetAllPermissionsAsync();

    /// <summary>
    /// Get user's individual permissions (Quyền User)
    /// </summary>
    Task<List<PermissionGroupDto>> GetUserPermissionsAsync(Guid userId);

    /// <summary>
    /// Update user's individual permissions (Quyền User)
    /// </summary>
    Task UpdateUserPermissionsAsync(Guid userId, UpdateUserPermissionsInput input);

    /// <summary>
    /// Get user's full permission info including role permissions
    /// </summary>
    Task<UserPermissionsDto> GetUserFullPermissionsAsync(Guid userId);

    #endregion

    #region User Roles Management

    /// <summary>
    /// Get all roles with assignment status for a user
    /// </summary>
    Task<List<UserRoleDto>> GetUserRolesAsync(Guid userId);

    /// <summary>
    /// Add a role to user
    /// </summary>
    Task AddRoleToUserAsync(Guid userId, string roleName);

    /// <summary>
    /// Remove a role from user
    /// </summary>
    Task RemoveRoleFromUserAsync(Guid userId, string roleName);

    #endregion
}
