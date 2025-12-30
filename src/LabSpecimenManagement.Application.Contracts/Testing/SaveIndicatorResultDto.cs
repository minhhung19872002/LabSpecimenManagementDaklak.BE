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

    /// <summary>
    /// Ghi chú - Note
    /// </summary>
    public string? Note { get; set; }
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

/// <summary>
/// Input for checking in indicators (bắt đầu thử nghiệm - ấn "Thực hiện")
/// </summary>
public class CheckInIndicatorsInput
{
    /// <summary>
    /// Sample ID - Mẫu cần check-in
    /// </summary>
    public Guid SampleId { get; set; }

    /// <summary>
    /// Danh sách indicator IDs cần check-in
    /// </summary>
    public List<Guid> IndicatorIds { get; set; } = new();
}

/// <summary>
/// Input for submitting results (gửi kết quả cho QLKT)
/// </summary>
public class SubmitResultsInput
{
    /// <summary>
    /// Sample ID - Mẫu cần gửi kết quả
    /// </summary>
    public Guid SampleId { get; set; }

    /// <summary>
    /// Danh sách indicator IDs cần gửi kết quả
    /// </summary>
    public List<Guid> IndicatorIds { get; set; } = new();
}

/// <summary>
/// Input for uploading sample testing image
/// </summary>
public class UploadSampleTestingImageInput
{
    /// <summary>
    /// Sample ID
    /// </summary>
    public Guid SampleId { get; set; }

    /// <summary>
    /// Loại hình ảnh (Before/After)
    /// </summary>
    public SampleTestingImageType ImageType { get; set; }
}
