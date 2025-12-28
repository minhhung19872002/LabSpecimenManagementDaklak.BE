using System;

namespace LabSpecimenManagement.SampleStorage;

/// <summary>
/// DTO cho từng mẫu trong chi tiết quản lý mẫu
/// </summary>
public class SampleStorageItemDto
{
    /// <summary>
    /// Sample ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Mã mẫu
    /// </summary>
    public string SampleCode { get; set; } = string.Empty;

    /// <summary>
    /// Tên mẫu
    /// </summary>
    public string SampleName { get; set; } = string.Empty;

    /// <summary>
    /// Loại mẫu (từ TestRequest SampleType flags)
    /// </summary>
    public string? SampleType { get; set; }

    /// <summary>
    /// Ngày lưu mẫu (từ TestRequest.SampleSentDate)
    /// </summary>
    public DateTime? StorageDate { get; set; }

    /// <summary>
    /// Lượng mẫu
    /// </summary>
    public string? SampleQuantity { get; set; }

    /// <summary>
    /// Tình trạng mẫu (từ StatusText)
    /// </summary>
    public string? StatusText { get; set; }

    /// <summary>
    /// Đã hủy mẫu
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <summary>
    /// Ngày hủy
    /// </summary>
    public DateTime? CancelledDate { get; set; }
}
