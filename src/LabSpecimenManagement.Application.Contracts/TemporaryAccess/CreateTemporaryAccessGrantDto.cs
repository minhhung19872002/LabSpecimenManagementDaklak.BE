using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.TemporaryAccess;

public class CreateTemporaryAccessGrantDto
{
    [Required]
    public TemporaryEmployeeType EmployeeType { get; set; }

    // Internal employee — required when EmployeeType = Internal
    public Guid? UserId { get; set; }

    // External contractor — required when EmployeeType = External
    public string? ExternalUsername { get; set; }
    public string? ExternalFullName { get; set; }
    public string? ExternalPhone { get; set; }
    public string? ExternalEmail { get; set; }
    public string? ExternalPassword { get; set; }
    public string? ExternalConfirmPassword { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime ExpiryDate { get; set; }

    [Required, MinLength(1)]
    public List<string> SelectedModules { get; set; } = new();

    public string? Reason { get; set; }
}
