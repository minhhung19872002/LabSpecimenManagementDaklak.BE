using LabSpecimenManagement.TestRequests;

namespace LabSpecimenManagement.Receipts;

/// <summary>
/// DTO chi tiết phiếu thu - bao gồm thông tin TestRequest đầy đủ
/// </summary>
public class ReceiptDetailDto : ReceiptDto
{
    /// <summary>
    /// Thông tin chi tiết phiếu yêu cầu thử nghiệm
    /// </summary>
    public TestRequestDto? TestRequestDetail { get; set; }
}
