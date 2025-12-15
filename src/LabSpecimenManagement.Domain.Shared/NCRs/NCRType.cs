namespace LabSpecimenManagement.NCRs;

/// <summary>
/// Loại NCR
/// </summary>
public enum NCRType
{
    TestResult = 0,  // Kết quả thử nghiệm
    Specimen = 1,    // Mẫu thử
    Equipment = 2,   // Thiết bị
    Report = 3,      // Báo cáo
    Process = 4,     // Quy trình
    Other = 5        // Khác
}
