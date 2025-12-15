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

    public string? Role { get; set; }

    public Guid? DepartmentId { get; set; }

    [StringLength(128)]
    public string? Position { get; set; }

    public bool? IsActive { get; set; }
}
