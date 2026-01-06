namespace LabSpecimenManagement.TestRequests;

public enum TestRequestStatus
{
    New = 0,                      // Mới - vừa tạo, chưa gửi kế toán
    PendingPayment = 1,           // Chờ thanh toán - đã gửi kế toán, chờ thanh toán
    Paid = 2,                     // Đã thanh toán - kế toán xác nhận đã thanh toán
    SampleSent = 3,               // Đã gửi mẫu - người dùng xác nhận đã gửi mẫu
    InProgress = 4,               // Đang thử nghiệm - QLKT đã gán PP + KTV
    PendingChangeApproval = 5,    // Chờ phê duyệt thay đổi - chỉnh sửa cần QLCL duyệt
    PendingApproval = 6,          // Chờ phê duyệt KQ - QLKT đã gửi kết quả
    Approved = 7,                 // Đã phê duyệt - QLCL đã duyệt KQ
    ResultSent = 8,               // Gửi khách hàng - đã nhấn gửi KQ
    Completed = 9,                // Hoàn thành - bộ phận giao trả xác nhận
    Cancelled = 10,               // Hủy phiếu yêu cầu - BP Giao nhận hủy
    UnderReview = 11              // Đang xem xét YC - đã gửi QLKT xem xét
}
