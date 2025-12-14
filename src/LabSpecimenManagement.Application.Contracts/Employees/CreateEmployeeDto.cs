using System;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.Employees;

public class CreateEmployeeDto
{
    [Required]
    [StringLength(256)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [StringLength(24)]
    public string? Phone { get; set; }

    [Required]
    public string Role { get; set; } = string.Empty;

    [Required]
    public Guid DepartmentId { get; set; }

    [StringLength(128)]
    public string? Position { get; set; }

    public bool IsActive { get; set; } = true;
}
