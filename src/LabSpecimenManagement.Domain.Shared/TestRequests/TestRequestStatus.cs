namespace LabSpecimenManagement.TestRequests;

public enum TestRequestStatus
{
    New = 0,                      // Mới - vừa tạo, chưa gửi kế toán
    PendingPayment = 1,           // Chờ thanh toán - đã gửi kế toán, chờ thanh toán
    Paid = 2,                     // Đã thanh toán - kế toán xác nhận đã thanh toán
    SampleSent = 3,               // Đã gửi mẫu - người dùng xác nhận đã gửi mẫu
    InProgress = 4,               // Đang thử nghiệm - QLKT đã gán PP + KTV
    PendingChangeApproval = 5,    // Chờ phê duyệt thay đổi - chỉnh sửa cần QLCL duyệt

    // [DEPRECATED - sẽ bỏ trong phase tiếp theo]
    PendingApproval = 6,          // [DEPRECATED] Chờ phê duyệt KQ
    Approved = 7,                 // [DEPRECATED] Đã phê duyệt

    ResultSent = 8,               // [DEPRECATED] Gửi khách hàng - sử dụng ReportPublished thay thế
    Completed = 9,                // Hoàn thành - bộ phận giao trả xác nhận
    Cancelled = 10,               // Hủy phiếu yêu cầu - BP Giao nhận hủy
    UnderReview = 11,             // Đang xem xét YC - đã gửi QLKT xem xét
    WaitingCustomerConfirm = 12,  // Chờ KH xác nhận - QLKT yêu cầu đổi PP hoặc từ chối
    RequestApproved = 13,         // Đã duyệt yêu cầu - Tất cả phòng LAB đã duyệt
    PartialResultSent = 14,       // Đã trả 1 phần KQ - đã trả một phần kết quả cho KH
    ReportPublished = 15,         // Đã phát hành BC - (đổi tên từ ResultSent)
    NoPaymentRequired = 16        // Không cần thanh toán - Mẫu nhiệm vụ (auto sau khi Gửi kế toán)
}
