using System;
using LabSpecimenManagement.Testing;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// DTO cho hình ảnh thử nghiệm (trước/sau TN) trong context TestRequest
/// </summary>
public class SampleTestingImageDto
{
    public Guid Id { get; set; }

    public string FileId { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public long FileSize { get; set; }

    /// <summary>
    /// URL để hiển thị hình ảnh
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Loại hình: BeforeTesting = 0, AfterTesting = 1
    /// </summary>
    public SampleTestingImageType ImageType { get; set; }

    public DateTime CreationTime { get; set; }
}
