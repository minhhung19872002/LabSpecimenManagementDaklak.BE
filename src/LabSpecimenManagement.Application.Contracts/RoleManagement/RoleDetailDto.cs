using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.RoleManagement;

/// <summary>
/// DTO for role detail view with users and permissions
/// </summary>
public class RoleDetailDto : RoleDto
{
    public List<RoleUserDto> Users { get; set; } = new();
    public List<PermissionGroupDto> PermissionGroups { get; set; } = new();
}
