namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// Tình trạng mẫu
/// </summary>
public enum SampleStatus
{
    /// <summary>
    /// Bình thường
    /// </summary>
    Normal = 0,

    /// <summary>
    /// Hư hỏng
    /// </summary>
    Damaged = 1,

    /// <summary>
    /// Không đạt yêu cầu
    /// </summary>
    NotMeetRequirements = 2,

    /// <summary>
    /// Đang xét nghiệm
    /// </summary>
    Testing = 3,

    /// <summary>
    /// Hoàn thành
    /// </summary>
    Completed = 4
}
