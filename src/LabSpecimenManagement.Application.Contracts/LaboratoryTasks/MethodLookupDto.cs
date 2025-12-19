using System;

namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// DTO cho dropdown chọn phương pháp thử nghiệm
/// </summary>
public class MethodLookupDto
{
    public Guid Id { get; set; }

    /// <summary>
    /// Mã phương pháp
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Tên phương pháp
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Mô tả
    /// </summary>
    public string? Description { get; set; }
}
