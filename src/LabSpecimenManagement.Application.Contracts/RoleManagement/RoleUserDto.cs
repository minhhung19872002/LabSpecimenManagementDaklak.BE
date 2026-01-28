using System;

namespace LabSpecimenManagement.RoleManagement;

/// <summary>
/// DTO for user in role list
/// </summary>
public class RoleUserDto
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? Position { get; set; }
    public bool IsActive { get; set; }
}
