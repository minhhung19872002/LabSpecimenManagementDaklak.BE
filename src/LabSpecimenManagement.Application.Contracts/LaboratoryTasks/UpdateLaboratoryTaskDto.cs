using System;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// DTO for updating Laboratory Task
/// </summary>
public class UpdateLaboratoryTaskDto
{
    /// <summary>
    /// Ngày thử nghiệm - Test Date (entered by Technical Manager)
    /// </summary>
    public DateTime? TestDate { get; set; }

    /// <summary>
    /// Trạng thái - Status
    /// </summary>
    public LaboratoryTaskStatus Status { get; set; }

    /// <summary>
    /// Ghi chú - Notes
    /// </summary>
    [StringLength(2000)]
    public string? Notes { get; set; }
}
