using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.RoleManagement;

/// <summary>
/// Input for updating a role
/// </summary>
public class UpdateRoleInput
{
    [Required(ErrorMessage = "Tên nhóm quyền là bắt buộc")]
    [StringLength(256, MinimumLength = 1, ErrorMessage = "Tên nhóm quyền phải từ 1-256 ký tự")]
    public string Name { get; set; } = string.Empty;

    public bool IsDefault { get; set; }
    public bool IsPublic { get; set; }

    public string? ConcurrencyStamp { get; set; }
}
