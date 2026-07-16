using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.TemporaryAccess;

public class UpdateTemporaryAccessGrantDto
{
    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime ExpiryDate { get; set; }

    [Required, MinLength(1)]
    public List<string> SelectedModules { get; set; } = new();

    public string? Reason { get; set; }
}
