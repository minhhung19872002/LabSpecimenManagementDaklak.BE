using System;
using LabSpecimenManagement.Testing;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// Hình ảnh thử nghiệm của mẫu (trước/sau TN)
/// </summary>
public class SampleTestingImage : CreationAuditedEntity<Guid>
{
    /// <summary>
    /// LaboratoryTask ID (Foreign Key)
    /// </summary>
    public Guid LaboratoryTaskId { get; set; }

    /// <summary>
    /// TestRequestSample ID - Mẫu liên quan
    /// </summary>
    public Guid SampleId { get; set; }

    /// <summary>
    /// File ID từ hệ thống lưu trữ
    /// </summary>
    public string FileId { get; set; } = null!;

    /// <summary>
    /// Tên file gốc
    /// </summary>
    public string FileName { get; set; } = null!;

    /// <summary>
    /// Đường dẫn file
    /// </summary>
    public string FilePath { get; set; } = null!;

    /// <summary>
    /// Kích thước file (bytes)
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Loại hình ảnh (Trước TN / Sau TN)
    /// </summary>
    public SampleTestingImageType ImageType { get; set; }

    /// <summary>
    /// Navigation property to LaboratoryTask
    /// </summary>
    public virtual LaboratoryTask LaboratoryTask { get; set; } = null!;

    protected SampleTestingImage()
    {
    }

    public SampleTestingImage(
        Guid id,
        Guid laboratoryTaskId,
        Guid sampleId,
        string fileId,
        string fileName,
        string filePath,
        long fileSize,
        SampleTestingImageType imageType) : base(id)
    {
        LaboratoryTaskId = laboratoryTaskId;
        SampleId = sampleId;
        FileId = fileId;
        FileName = fileName;
        FilePath = filePath;
        FileSize = fileSize;
        ImageType = imageType;
    }
}
