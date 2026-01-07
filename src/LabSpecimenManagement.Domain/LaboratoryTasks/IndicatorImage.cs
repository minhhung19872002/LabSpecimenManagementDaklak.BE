using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// Hình ảnh của chỉ tiêu thử nghiệm
/// </summary>
public class IndicatorImage : CreationAuditedEntity<Guid>
{
    /// <summary>
    /// LaboratoryTaskIndicator ID (Foreign Key)
    /// </summary>
    public Guid LaboratoryTaskIndicatorId { get; set; }

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
    /// Navigation property to LaboratoryTaskIndicator
    /// </summary>
    public virtual LaboratoryTaskIndicator LaboratoryTaskIndicator { get; set; } = null!;

    protected IndicatorImage()
    {
    }

    public IndicatorImage(
        Guid id,
        Guid laboratoryTaskIndicatorId,
        string fileId,
        string fileName,
        string filePath,
        long fileSize) : base(id)
    {
        LaboratoryTaskIndicatorId = laboratoryTaskIndicatorId;
        FileId = fileId;
        FileName = fileName;
        FilePath = filePath;
        FileSize = fileSize;
    }
}
