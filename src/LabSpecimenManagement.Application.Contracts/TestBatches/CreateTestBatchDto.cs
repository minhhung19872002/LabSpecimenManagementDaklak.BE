using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.TestBatches;

public class CreateTestBatchDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = null!;

    [StringLength(2000)]
    public string? Note { get; set; }

    [Required]
    [MinLength(1)]
    public List<Guid> LaboratoryTaskIndicatorIds { get; set; } = new();
}
