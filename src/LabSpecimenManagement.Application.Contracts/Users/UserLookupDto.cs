using System;

namespace LabSpecimenManagement.Users;

/// <summary>
/// DTO cho dropdown chọn user
/// </summary>
public class UserLookupDto
{
    public Guid Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string? Name { get; set; }

    public string? Surname { get; set; }

    /// <summary>
    /// Tên hiển thị đầy đủ (Họ Tên - Username)
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    public string? Email { get; set; }

    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Chức vụ
    /// </summary>
    public string? Position { get; set; }

    /// <summary>
    /// Phòng ban
    /// </summary>
    public string? DepartmentName { get; set; }

    public bool IsActive { get; set; }
}
