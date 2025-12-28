using System;

namespace LabSpecimenManagement.SampleStorage;

/// <summary>
/// Input DTO để cập nhật thông tin mẫu
/// </summary>
public class UpdateSampleDto
{
    /// <summary>
    /// Tên mẫu
    /// </summary>
    public string? SampleName { get; set; }

    /// <summary>
    /// Ngày lưu mẫu
    /// </summary>
    public DateTime? StorageDate { get; set; }

    /// <summary>
    /// Lượng mẫu
    /// </summary>
    public string? SampleQuantity { get; set; }

    /// <summary>
    /// Tình trạng mẫu
    /// </summary>
    public string? StatusText { get; set; }
}
