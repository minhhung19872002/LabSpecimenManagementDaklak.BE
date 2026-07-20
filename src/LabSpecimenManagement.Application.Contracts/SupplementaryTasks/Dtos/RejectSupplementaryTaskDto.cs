using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.SupplementaryTasks.Dtos;

public class RejectSupplementaryTaskDto
{
    [Required]
    [MaxLength(1000)]
    public string Reason { get; set; }
}
