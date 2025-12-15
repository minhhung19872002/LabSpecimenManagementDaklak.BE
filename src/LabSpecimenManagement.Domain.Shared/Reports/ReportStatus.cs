namespace LabSpecimenManagement.Reports;

/// <summary>
/// Trạng thái báo cáo kết quả
/// </summary>
public enum ReportStatus
{
    Draft = 0,           // Bản nháp
    PendingApproval = 1, // Chờ duyệt
    Approved = 2,        // Đã duyệt
    Rejected = 3,        // Từ chối
    Issued = 4           // Đã phát hành
}
