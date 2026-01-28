using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.RoleManagement;

/// <summary>
/// Input for creating a new role
/// </summary>
public class CreateRoleInput
{
    [Required(ErrorMessage = "Tên nhóm quyền là bắt buộc")]
    [StringLength(256, MinimumLength = 1, ErrorMessage = "Tên nhóm quyền phải từ 1-256 ký tự")]
    public string Name { get; set; } = string.Empty;

    public bool IsDefault { get; set; } = false;
    public bool IsPublic { get; set; } = true;
}
