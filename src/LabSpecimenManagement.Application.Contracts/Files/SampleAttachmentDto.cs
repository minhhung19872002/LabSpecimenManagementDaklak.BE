using System;

namespace LabSpecimenManagement.Files;

/// <summary>
/// DTO cho attachment của sample (output)
/// </summary>
public class SampleAttachmentDto
{
    /// <summary>
    /// ID của attachment
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// File ID (unique identifier để lấy file)
    /// </summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// Tên file gốc
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Kích thước file (bytes)
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// URL để lấy file
    /// </summary>
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// DTO cho attachment input (khi tạo/cập nhật sample)
/// </summary>
public class AttachmentInputDto
{
    /// <summary>
    /// File ID (từ API upload)
    /// </summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// Tên file gốc
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Kích thước file (bytes)
    /// </summary>
    public long FileSize { get; set; }
}
