namespace LabSpecimenManagement.RoleManagement;

/// <summary>
/// DTO for individual permission
/// </summary>
public class PermissionDto
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsGranted { get; set; }
}
