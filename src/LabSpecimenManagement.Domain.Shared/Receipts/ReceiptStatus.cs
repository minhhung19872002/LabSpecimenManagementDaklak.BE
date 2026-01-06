namespace LabSpecimenManagement.Receipts;

/// <summary>
/// Trạng thái phiếu thu
/// </summary>
public enum ReceiptStatus
{
    /// <summary>
    /// Chờ thanh toán - Record mới tạo khi BP Giao nhận in phiếu thu
    /// </summary>
    PendingPayment = 0,

    /// <summary>
    /// Chưa thanh toán - Kế toán đánh dấu chưa thanh toán
    /// </summary>
    Unpaid = 1,

    /// <summary>
    /// Đã thanh toán - Khách hàng đã thanh toán
    /// </summary>
    Paid = 2,

    /// <summary>
    /// Hủy yêu cầu - BP Giao nhận hủy yêu cầu
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// Không cần thanh toán - Mẫu nhiệm vụ, miễn phí
    /// </summary>
    NoPaymentRequired = 4
}
