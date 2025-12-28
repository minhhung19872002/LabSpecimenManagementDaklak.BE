namespace LabSpecimenManagement.SampleStorage;

/// <summary>
/// Trạng thái quản lý mẫu lưu kho
/// </summary>
public enum SampleStorageStatus
{
    /// <summary>
    /// Đã nhận mẫu (Phiếu YC đã gửi mẫu)
    /// </summary>
    Received = 0,

    /// <summary>
    /// Cần hủy mẫu (Phiếu YC hoàn thành + 5 ngày)
    /// </summary>
    PendingDisposal = 1,

    /// <summary>
    /// Đã hủy (Tất cả mẫu trong HĐ đã hủy)
    /// </summary>
    Disposed = 2
}
