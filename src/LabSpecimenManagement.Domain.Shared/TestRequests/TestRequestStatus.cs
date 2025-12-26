namespace LabSpecimenManagement.TestRequests;

public enum TestRequestStatus
{
    New = 0,                      // Mới - vừa tạo, chưa in phiếu
    Pending = 1,                  // Chờ xử lý - đã in phiếu XN
    SampleSent = 2,               // Đã gửi mẫu - người dùng xác nhận đã gửi mẫu
    InProgress = 3,               // Đang thử nghiệm - QLKT đã gán PP + KTV
    PendingChangeApproval = 4,    // Chờ phê duyệt thay đổi - chỉnh sửa cần QLCL duyệt
    PendingApproval = 5,          // Chờ phê duyệt KQ - QLKT đã gửi kết quả
    Approved = 6,                 // Đã phê duyệt - QLCL đã duyệt KQ
    ResultSent = 7,               // Gửi khách hàng - đã nhấn gửi KQ
    Completed = 8                 // Hoàn thành - bộ phận giao trả xác nhận
}
