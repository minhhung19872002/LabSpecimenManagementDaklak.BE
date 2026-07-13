using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.PreparedSolutions;

public class ProcessPreparedSolutionDto
{
    [MaxLength(1000)]
    public string? Note { get; set; }

    public List<string> ImageUrls { get; set; } = new();
}
