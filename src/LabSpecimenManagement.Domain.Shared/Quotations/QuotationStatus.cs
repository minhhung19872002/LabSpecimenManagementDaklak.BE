namespace LabSpecimenManagement.Quotations;

/// <summary>
/// Trạng thái báo giá
/// </summary>
public enum QuotationStatus
{
    /// <summary>
    /// Nháp - Chưa gửi khách hàng
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Đã gửi - Đã gửi cho khách hàng
    /// </summary>
    Sent = 1,

    /// <summary>
    /// Đã hủy
    /// </summary>
    Cancelled = 2
}
