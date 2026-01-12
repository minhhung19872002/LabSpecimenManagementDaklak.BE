using System;

namespace LabSpecimenManagement.Testing;

/// <summary>
/// DTO cho hình ảnh đính kèm trong lịch sử từ chối
/// </summary>
public class RejectionHistoryImageDto
{
    public Guid Id { get; set; }

    public Guid RejectionHistoryId { get; set; }

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
