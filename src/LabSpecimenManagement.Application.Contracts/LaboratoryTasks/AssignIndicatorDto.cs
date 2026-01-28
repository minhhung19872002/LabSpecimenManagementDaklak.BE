using System;

namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// DTO để gán phương pháp và Thử nghiệm viên cho chỉ tiêu
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
    /// ID Thử nghiệm viên thực hiện (nullable để validate)
    /// </summary>
    public Guid? TechnicianId { get; set; }
}
