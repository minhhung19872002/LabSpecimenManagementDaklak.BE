using System;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.Products;

/// <summary>
/// Tài liệu đính kèm sản phẩm (SDS/MSDS)
/// </summary>
public class ProductAttachment : Entity<Guid>
{
    public Guid ProductId { get; set; }
    public string FileId { get; set; }
    public string FileName { get; set; }
    public string FilePath { get; set; }
    public long FileSize { get; set; }

    /// <summary>
    /// Loại tài liệu (SDS, MSDS, etc.)
    /// </summary>
    public string? FileType { get; set; }

    public DateTime CreationTime { get; set; }

    protected ProductAttachment()
    {
    }

    public ProductAttachment(Guid id, Guid productId, string fileId, string fileName, string filePath, long fileSize, string? fileType = null) : base(id)
    {
        ProductId = productId;
        FileId = fileId;
        FileName = fileName;
        FilePath = filePath;
        FileSize = fileSize;
        FileType = fileType;
        CreationTime = DateTime.UtcNow;
    }
}
