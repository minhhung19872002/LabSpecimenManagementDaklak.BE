using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// Tệp đính kèm phiếu yêu cầu - Test Request Attachment
/// </summary>
public class TestRequestAttachment : CreationAuditedEntity<Guid>
{
    public Guid TestRequestId { get; set; }

    /// <summary>
    /// Tên tệp - File name
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// Đường dẫn tệp - File path
    /// </summary>
    public string FilePath { get; set; }

    /// <summary>
    /// Loại tệp - File type (PDF, Image, etc.)
    /// </summary>
    public string FileType { get; set; }

    /// <summary>
    /// Kích thước tệp (bytes)
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Mô tả - Description
    /// </summary>
    public string Description { get; set; }

    public virtual TestRequest TestRequest { get; set; }

    protected TestRequestAttachment()
    {
    }

    public TestRequestAttachment(
        Guid id,
        Guid testRequestId,
        string fileName,
        string filePath,
        string fileType,
        long fileSize
    ) : base(id)
    {
        TestRequestId = testRequestId;
        FileName = fileName;
        FilePath = filePath;
        FileType = fileType;
        FileSize = fileSize;
    }
}
