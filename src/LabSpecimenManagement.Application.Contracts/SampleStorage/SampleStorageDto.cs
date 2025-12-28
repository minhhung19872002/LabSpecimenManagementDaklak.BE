using System;

namespace LabSpecimenManagement.SampleStorage;

/// <summary>
/// DTO cho danh sách quản lý mẫu (hiển thị theo HĐ/phiếu yêu cầu)
/// </summary>
public class SampleStorageDto
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
    /// Số mẫu đã hủy
    /// </summary>
    public int CancelledCount { get; set; }
}
