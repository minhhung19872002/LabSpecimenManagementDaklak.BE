using System;

namespace LabSpecimenManagement.RoleManagement;

/// <summary>
/// DTO for role list display
/// </summary>
public class RoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public int UserCount { get; set; }
    public bool IsStatic { get; set; }
    public bool IsDefault { get; set; }
    public bool IsPublic { get; set; }
    public string? ConcurrencyStamp { get; set; }
}
