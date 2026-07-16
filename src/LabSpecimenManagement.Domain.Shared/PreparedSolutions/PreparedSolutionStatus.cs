namespace LabSpecimenManagement.PreparedSolutions;

public enum PreparedSolutionStatus
{
    Active = 0,        // Còn dùng
    NearlyEmpty = 1,   // Sắp hết (còn lại ≤ 5% TotalVolume)
    NearingExpiry = 2, // Sắp hết hạn (< 14 ngày)
    Expired = 3,       // Hết hạn (today >= ExpiryDate)
    Empty = 4,         // Hết (RemainingVolume <= 0)
    Processed = 5,     // Đã xử lý (user action)
}
