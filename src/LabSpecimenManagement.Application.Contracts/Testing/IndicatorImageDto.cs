using System;

namespace LabSpecimenManagement.Testing;

/// <summary>
/// DTO cho hình ảnh của chỉ tiêu thử nghiệm
/// </summary>
public class IndicatorImageDto
{
    public Guid Id { get; set; }

    public Guid LaboratoryTaskIndicatorId { get; set; }

    public string FileId { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public long FileSize { get; set; }

    /// <summary>
    /// URL để hiển thị hình ảnh
    /// </summary>
    public string Url { get; set; } = string.Empty;

    public DateTime CreationTime { get; set; }

    public Guid? CreatorId { get; set; }
}
