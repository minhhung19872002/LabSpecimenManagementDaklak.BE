namespace LabSpecimenManagement.TestRequests;

public enum TestRequestStatus
{
    New = 0,                // Mới - vừa tạo, chưa in phiếu
    Pending = 1,            // Chờ xử lý - đã in phiếu XN
    InProgress = 2,         // Đang thực hiện - QLKT đã gán PP + KTV
    TestingCompleted = 3,   // TN Hoàn Thành - QLKT đã gửi kết quả
    PendingApproval = 4,    // Chờ Phê duyệt - đã in KQ xét nghiệm
    Approved = 5,           // Đã phê duyệt - lãnh đạo đã duyệt
    ResultSent = 6,         // Đã gửi kết quả - đã nhấn gửi KQ
    Completed = 7           // Hoàn Thành - kết thúc quy trình
}
