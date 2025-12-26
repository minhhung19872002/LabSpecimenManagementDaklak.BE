namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// Trạng thái phê duyệt thay đổi
/// </summary>
public enum ChangeApprovalStatus
{
    Pending = 0,    // Chờ phê duyệt
    Approved = 1,   // Đã duyệt
    Rejected = 2    // Từ chối
}
