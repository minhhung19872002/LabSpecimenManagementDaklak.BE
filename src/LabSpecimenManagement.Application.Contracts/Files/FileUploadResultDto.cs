namespace LabSpecimenManagement.Files;

/// <summary>
/// Kết quả upload file
/// </summary>
public class FileUploadResultDto
{
    /// <summary>
    /// ID của file (dùng để lấy file sau này)
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
