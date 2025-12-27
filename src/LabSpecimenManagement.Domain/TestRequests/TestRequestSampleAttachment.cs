using System;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// Hình ảnh đính kèm của mẫu xét nghiệm
/// </summary>
public class TestRequestSampleAttachment : Entity<Guid>
{
    /// <summary>
    /// TestRequestSample ID (Foreign Key)
    /// </summary>
    public Guid TestRequestSampleId { get; set; }

    /// <summary>
    /// Unique file ID (dùng để lấy file từ API)
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
    /// Ngày tạo
    /// </summary>
    public DateTime CreationTime { get; set; }

    /// <summary>
    /// Navigation property to TestRequestSample
    /// </summary>
    public virtual TestRequestSample Sample { get; set; } = null!;

    protected TestRequestSampleAttachment()
    {
    }

    public TestRequestSampleAttachment(
        Guid id,
        Guid testRequestSampleId,
        string fileId,
        string fileName,
        string filePath,
        long fileSize) : base(id)
    {
        TestRequestSampleId = testRequestSampleId;
        FileId = fileId;
        FileName = fileName;
        FilePath = filePath;
        FileSize = fileSize;
        CreationTime = DateTime.Now;
    }
}
