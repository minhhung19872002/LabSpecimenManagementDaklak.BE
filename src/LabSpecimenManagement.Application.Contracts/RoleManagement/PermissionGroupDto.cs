using System.Collections.Generic;

namespace LabSpecimenManagement.RoleManagement;

/// <summary>
/// DTO for permission group with list of permissions
/// </summary>
public class PermissionGroupDto
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<PermissionDto> Permissions { get; set; } = new();
}
