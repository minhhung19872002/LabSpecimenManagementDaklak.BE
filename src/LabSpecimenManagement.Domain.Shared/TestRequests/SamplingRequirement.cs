namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// Yêu cầu về lấy mẫu
/// </summary>
public enum SamplingRequirement
{
    /// <summary>
    /// TTKN lấy mẫu
    /// </summary>
    LabSampling = 0,

    /// <summary>
    /// Khách hàng tự lấy mẫu
    /// </summary>
    CustomerSampling = 1
}
