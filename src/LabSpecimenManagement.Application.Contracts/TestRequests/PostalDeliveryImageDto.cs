namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// DTO cho hình ảnh gửi bưu điện
/// </summary>
public class PostalDeliveryImageDto
{
    /// <summary>
    /// FileId của hình ảnh
    /// </summary>
    public string FileId { get; set; } = string.Empty;

    /// <summary>
    /// URL để hiển thị hình ảnh
    /// </summary>
    public string Url { get; set; } = string.Empty;
}
