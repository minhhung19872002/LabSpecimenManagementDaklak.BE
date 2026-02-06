using System;
using System.Collections.Generic;
using LabSpecimenManagement.RoleManagement;

namespace LabSpecimenManagement.Employees;

/// <summary>
/// DTO for user permissions including both individual and role-based permissions
/// </summary>
public class UserPermissionsDto
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// User's full name
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Individual permissions granted directly to the user (Quyền User)
    /// </summary>
    public List<PermissionGroupDto> UserPermissions { get; set; } = new();

    /// <summary>
    /// Permissions inherited from roles (Quyền Group) - read only for display
    /// </summary>
    public List<PermissionGroupDto> RolePermissions { get; set; } = new();

    /// <summary>
    /// Combined effective permissions (merged from user + roles)
    /// </summary>
    public List<string> EffectivePermissions { get; set; } = new();
}

/// <summary>
/// Input for updating user individual permissions
/// </summary>
public class UpdateUserPermissionsInput
{
    /// <summary>
    /// List of permission names to grant to the user (permissions not in this list will be revoked)
    /// </summary>
    public List<string> Permissions { get; set; } = new();
}

/// <summary>
/// DTO for user's role assignment
/// </summary>
public class UserRoleDto
{
    /// <summary>
    /// Role ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Role name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Number of permissions in this role
    /// </summary>
    public int PermissionCount { get; set; }

    /// <summary>
    /// Whether the role is assigned to the user
    /// </summary>
    public bool IsAssigned { get; set; }

    /// <summary>
    /// Whether this is a system role (cannot be removed)
    /// </summary>
    public bool IsStatic { get; set; }
}

/// <summary>
/// Input for adding/removing role from user
/// </summary>
public class UpdateUserRoleInput
{
    /// <summary>
    /// Role name to add or remove
    /// </summary>
    public string RoleName { get; set; } = string.Empty;
}
