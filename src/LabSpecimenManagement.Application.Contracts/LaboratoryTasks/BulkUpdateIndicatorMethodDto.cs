using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.LaboratoryTasks;

public class BulkUpdateIndicatorMethodDto
{
    [Required]
    public List<Guid> IndicatorIds { get; set; } = new();

    public Guid? MethodId { get; set; }
}
