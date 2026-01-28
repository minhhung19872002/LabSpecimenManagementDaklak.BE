using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// Hình ảnh đính kèm trong lịch sử từ chối/báo không TN được
/// </summary>
public class RejectionHistoryImage : CreationAuditedEntity<Guid>
{
    /// <summary>
    /// IndicatorRejectionHistory ID (Foreign Key)
    /// </summary>
    public Guid RejectionHistoryId { get; set; }

    /// <summary>
    /// File ID từ File Storage
    /// </summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// Tên file gốc
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Đường dẫn file trên server
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Kích thước file (bytes)
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Navigation property to IndicatorRejectionHistory
    /// </summary>
    public virtual IndicatorRejectionHistory RejectionHistory { get; set; } = null!;

    protected RejectionHistoryImage()
    {
    }

    public RejectionHistoryImage(
        Guid id,
        Guid rejectionHistoryId,
        string fileId,
        string fileName,
        string filePath,
        long fileSize) : base(id)
    {
        RejectionHistoryId = rejectionHistoryId;
        FileId = fileId;
        FileName = fileName;
        FilePath = filePath;
        FileSize = fileSize;
    }
}
