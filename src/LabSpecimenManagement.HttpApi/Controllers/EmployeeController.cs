using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.Employees;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.RoleManagement;
using LabSpecimenManagement.UserWarehouses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Route("api/app/employee")]
[Authorize(LabSpecimenManagementPermissions.Users.Default)]
public class EmployeeController : LabSpecimenManagementController
{
    private readonly IEmployeeAppService _employeeAppService;

    public EmployeeController(IEmployeeAppService employeeAppService)
    {
        _employeeAppService = employeeAppService;
    }

    [HttpGet]
    public virtual Task<PagedResultDto<EmployeeDto>> GetListAsync([FromQuery] GetEmployeesInput input)
    {
        return _employeeAppService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual Task<EmployeeDto> GetAsync(Guid id)
    {
        return _employeeAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.Users.Create)]
    public virtual Task<EmployeeDto> CreateAsync([FromBody] CreateEmployeeDto input)
    {
        return _employeeAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.Users.Edit)]
    public virtual Task<EmployeeDto> UpdateAsync(Guid id, [FromBody] UpdateEmployeeDto input)
    {
        return _employeeAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.Users.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _employeeAppService.DeleteAsync(id);
    }

    [HttpPut("{id}/toggle-active")]
    [Authorize(LabSpecimenManagementPermissions.Users.Edit)]
    public virtual Task<EmployeeDto> ToggleActiveAsync(Guid id, [FromBody] ToggleActiveDto input)
    {
        return _employeeAppService.ToggleActiveAsync(id, input.IsActive);
    }

    [HttpPost("{id}/reset-password")]
    [Authorize(LabSpecimenManagementPermissions.Users.Edit)]
    public virtual Task<ResetPasswordResultDto> ResetPasswordAsync(Guid id, [FromBody] ResetPasswordDto input)
    {
        return _employeeAppService.ResetPasswordAsync(id, input);
    }

    #region Permissions Management

    /// <summary>
    /// Get all available permissions
    /// </summary>
    [HttpGet("permissions/all")]
    public virtual Task<List<PermissionGroupDto>> GetAllPermissionsAsync()
    {
        return _employeeAppService.GetAllPermissionsAsync();
    }

    /// <summary>
    /// Get user's individual permissions (Quyền User)
    /// </summary>
    [HttpGet("{id}/permissions")]
    public virtual Task<List<PermissionGroupDto>> GetUserPermissionsAsync(Guid id)
    {
        return _employeeAppService.GetUserPermissionsAsync(id);
    }

    /// <summary>
    /// Update user's individual permissions (Quyền User)
    /// </summary>
    [HttpPut("{id}/permissions")]
    [Authorize(LabSpecimenManagementPermissions.Users.ManageRoles)]
    public virtual Task UpdateUserPermissionsAsync(Guid id, [FromBody] UpdateUserPermissionsInput input)
    {
        return _employeeAppService.UpdateUserPermissionsAsync(id, input);
    }

    /// <summary>
    /// Get user's full permission info including role permissions
    /// </summary>
    [HttpGet("{id}/permissions/full")]
    public virtual Task<UserPermissionsDto> GetUserFullPermissionsAsync(Guid id)
    {
        return _employeeAppService.GetUserFullPermissionsAsync(id);
    }

    #endregion

    #region Roles Management

    /// <summary>
    /// Get all roles with assignment status for a user
    /// </summary>
    [HttpGet("{id}/roles")]
    public virtual Task<List<UserRoleDto>> GetUserRolesAsync(Guid id)
    {
        return _employeeAppService.GetUserRolesAsync(id);
    }

    /// <summary>
    /// Add a role to user
    /// </summary>
    [HttpPost("{id}/roles/{roleName}")]
    [Authorize(LabSpecimenManagementPermissions.Users.ManageRoles)]
    public virtual Task AddRoleToUserAsync(Guid id, string roleName)
    {
        return _employeeAppService.AddRoleToUserAsync(id, roleName);
    }

    /// <summary>
    /// Remove a role from user
    /// </summary>
    [HttpDelete("{id}/roles/{roleName}")]
    [Authorize(LabSpecimenManagementPermissions.Users.ManageRoles)]
    public virtual Task RemoveRoleFromUserAsync(Guid id, string roleName)
    {
        return _employeeAppService.RemoveRoleFromUserAsync(id, roleName);
    }

    #endregion

    #region Warehouse Assignments

    [HttpGet("{id}/warehouses")]
    public virtual Task<List<UserWarehouseDto>> GetUserWarehousesAsync(Guid id)
    {
        return _employeeAppService.GetUserWarehousesAsync(id);
    }

    [HttpPut("{id}/warehouses")]
    [Authorize(LabSpecimenManagementPermissions.Users.ManageRoles)]
    public virtual Task AssignUserWarehousesAsync(Guid id, [FromBody] AssignUserWarehousesInput input)
    {
        return _employeeAppService.AssignUserWarehousesAsync(id, input);
    }

    #endregion
}
