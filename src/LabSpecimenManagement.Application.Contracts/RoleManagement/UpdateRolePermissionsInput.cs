using System.Collections.Generic;

namespace LabSpecimenManagement.RoleManagement;

/// <summary>
/// Input for updating role permissions
/// </summary>
public class UpdateRolePermissionsInput
{
    /// <summary>
    /// List of permission names to grant (permissions not in this list will be revoked)
    /// </summary>
    public List<string> Permissions { get; set; } = new();
}
