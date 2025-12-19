using System;

namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// DTO để gán phương pháp và kỹ thuật viên cho chỉ tiêu
/// </summary>
public class AssignIndicatorDto
{
    /// <summary>
    /// ID của LaboratoryTaskIndicator
    /// </summary>
    public Guid IndicatorId { get; set; }

    /// <summary>
    /// ID phương pháp thử nghiệm (nullable để validate)
    /// </summary>
    public Guid? MethodId { get; set; }

    /// <summary>
    /// ID kỹ thuật viên thực hiện (nullable để validate)
    /// </summary>
    public Guid? TechnicianId { get; set; }
}
