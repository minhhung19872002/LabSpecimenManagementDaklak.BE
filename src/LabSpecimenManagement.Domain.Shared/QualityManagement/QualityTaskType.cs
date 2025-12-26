namespace LabSpecimenManagement.QualityManagement;

/// <summary>
/// Loại công việc QLCL
/// </summary>
public enum QualityTaskType
{
    /// <summary>
    /// Phê duyệt kết quả xét nghiệm từ QLKT
    /// </summary>
    ResultApproval = 0,

    /// <summary>
    /// Phê duyệt thay đổi phiếu yêu cầu
    /// </summary>
    ChangeApproval = 1
}
