using System;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.Employees;

public class UpdateEmployeeDto
{
    [StringLength(256)]
    public string? FullName { get; set; }

    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; set; }

    [StringLength(24)]
    public string? Phone { get; set; }

    // Note: Role field removed - quản lý quyền thông qua "Quản lý phân quyền"
    // Nhân viên được gán quyền bằng cách thêm vào nhóm quyền trong Role Management

    public Guid? DepartmentId { get; set; }

    [StringLength(128)]
    public string? Position { get; set; }

    public bool? IsActive { get; set; }

    /// <summary>
    /// If true, this is a temporary account with expiration date.
    /// If false, remove expiration (make permanent).
    /// </summary>
    public bool? IsTemporaryAccount { get; set; }

    /// <summary>
    /// Account expiration date (required when IsTemporaryAccount = true).
    /// After this date, the account will be automatically deactivated.
    /// </summary>
    public DateTime? AccountExpirationDate { get; set; }
}
