using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.Testing;

/// <summary>
/// DTO for saving indicator results (draft)
/// </summary>
public class SaveIndicatorResultDto
{
    /// <summary>
    /// LaboratoryTaskIndicator ID
    /// </summary>
    public Guid IndicatorId { get; set; }

    /// <summary>
    /// Phương pháp - Method ID
    /// </summary>
    public Guid? MethodId { get; set; }

    /// <summary>
    /// Đơn vị tính - Unit
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// Kết quả - Result
    /// </summary>
    public string? Result { get; set; }
}

/// <summary>
/// Input for saving results
/// </summary>
public class SaveResultsInput
{
    /// <summary>
    /// Danh sách kết quả cần lưu
    /// </summary>
    public List<SaveIndicatorResultDto> Results { get; set; } = new();
}

/// <summary>
/// Input for submitting indicators for approval
/// </summary>
public class SubmitForApprovalInput
{
    /// <summary>
    /// Danh sách indicator IDs cần gửi duyệt
    /// </summary>
    public List<Guid> IndicatorIds { get; set; } = new();
}
