using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.Employees;

public class EmployeeDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }

    /// <summary>
    /// @deprecated Use Roles instead
    /// </summary>
    [Obsolete("Use Roles instead. This property is kept for backward compatibility.")]
    public string? Role { get; set; }

    /// <summary>
    /// Danh sách nhóm quyền của nhân viên
    /// </summary>
    public List<string> Roles { get; set; } = new List<string>();

    public string? Department { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? Position { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }

    /// <summary>
    /// Account expiration date. Null means permanent account.
    /// </summary>
    public DateTime? AccountExpirationDate { get; set; }

    /// <summary>
    /// True if account has expiration date set.
    /// </summary>
    public bool IsTemporaryAccount => AccountExpirationDate.HasValue;

    /// <summary>
    /// True if account has expired (past expiration date).
    /// </summary>
    public bool IsExpired => AccountExpirationDate.HasValue && AccountExpirationDate.Value < DateTime.Now;
}
