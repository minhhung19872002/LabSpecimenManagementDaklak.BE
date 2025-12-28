using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.SampleStorage;

/// <summary>
/// DTO chi tiết quản lý mẫu (HĐ + danh sách mẫu)
/// </summary>
public class SampleStorageDetailDto
{
    /// <summary>
    /// TestRequest ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Số hợp đồng
    /// </summary>
    public string ContractNumber { get; set; } = string.Empty;

    /// <summary>
    /// Tên khách hàng
    /// </summary>
    public string CustomerName { get; set; } = string.Empty;

    /// <summary>
    /// Tổng số mẫu
    /// </summary>
    public int TotalSampleCount { get; set; }

    /// <summary>
    /// Ngày lưu kho (SampleSentDate)
    /// </summary>
    public DateTime? StorageDate { get; set; }

    /// <summary>
    /// Trạng thái quản lý mẫu
    /// </summary>
    public SampleStorageStatus Status { get; set; }

    /// <summary>
    /// Danh sách mẫu
    /// </summary>
    public List<SampleStorageItemDto> Samples { get; set; } = new();
}
