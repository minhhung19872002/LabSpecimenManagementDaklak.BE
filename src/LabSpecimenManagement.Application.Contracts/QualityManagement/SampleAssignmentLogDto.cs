using System;
using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.TestRequests;
using LabSpecimenManagement.Testing;

namespace LabSpecimenManagement.QualityManagement;

/// <summary>
/// DTO for Sample Assignment Log - Flat indicator list for "Sổ nhận mẫu giao việc" tab
/// </summary>
public class SampleAssignmentLogDto
{
    #region TestRequest Info

    /// <summary>
    /// TestRequest ID - used for navigation
    /// </summary>
    public Guid TestRequestId { get; set; }

    /// <summary>
    /// Quality Management Task ID - used for detail navigation (QLCL page)
    /// Nullable: chưa có nếu phiếu chưa gửi QLKT
    /// </summary>
    public Guid? QualityTaskId { get; set; }

    /// <summary>
    /// Laboratory Task ID - used for detail navigation (Testing page)
    /// Nullable: chưa có nếu phiếu chưa gửi KTN
    /// </summary>
    public Guid? LaboratoryTaskId { get; set; }

    /// <summary>
    /// Số hợp đồng - Contract Number
    /// </summary>
    public string? ContractNumber { get; set; }

    /// <summary>
    /// Trạng thái phiếu TN - TestRequest Status
    /// </summary>
    public TestRequestStatus TestRequestStatus { get; set; }

    /// <summary>
    /// Ngày YC xong - Request Completion Date
    /// </summary>
    public DateTime? RequestCompletionDate { get; set; }

    #endregion

    #region Sample Info

    /// <summary>
    /// Sample ID - used for row spanning
    /// </summary>
    public Guid SampleId { get; set; }

    /// <summary>
    /// Mã số mẫu - Sample Code
    /// </summary>
    public string? SampleCode { get; set; }

    /// <summary>
    /// Tên mẫu - Sample Name
    /// </summary>
    public string? SampleName { get; set; }

    /// <summary>
    /// Lượng mẫu gửi - Sample Quantity
    /// </summary>
    public string? SampleQuantity { get; set; }

    /// <summary>
    /// Tình trạng mẫu (free text) - Sample Status Text
    /// </summary>
    public string? SampleStatusText { get; set; }

    #endregion

    #region Indicator Info

    /// <summary>
    /// TestRequestSampleIndicator ID - ID chính của chỉ tiêu trong phiếu yêu cầu
    /// </summary>
    public Guid TestRequestSampleIndicatorId { get; set; }

    /// <summary>
    /// LaboratoryTaskIndicator ID
    /// Nullable: chưa có nếu phiếu chưa gửi KTN
    /// </summary>
    public Guid? LaboratoryTaskIndicatorId { get; set; }

    /// <summary>
    /// Indicator ID - ID của chỉ tiêu master data
    /// </summary>
    public Guid IndicatorId { get; set; }

    /// <summary>
    /// Tên chỉ tiêu - Indicator Name
    /// </summary>
    public string? IndicatorName { get; set; }

    /// <summary>
    /// Tên chỉ tiêu có định dạng (HTML)
    /// </summary>
    public string? IndicatorFormattedName { get; set; }

    /// <summary>
    /// Phương pháp - Method Name
    /// </summary>
    public string? MethodName { get; set; }

    /// <summary>
    /// Phương pháp có định dạng (HTML)
    /// </summary>
    public string? MethodFormattedName { get; set; }

    /// <summary>
    /// ĐVT - Unit
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// Ngày chuyển - Transfer Date
    /// </summary>
    public DateTime? TransferDate { get; set; }

    /// <summary>
    /// Thời gian TN - Test Date
    /// </summary>
    public DateTime? TestDate { get; set; }

    /// <summary>
    /// Kết quả - Result
    /// </summary>
    public string? Result { get; set; }

    #endregion

    #region Assignment Info

    /// <summary>
    /// QL kỹ thuật - Technical Manager Name
    /// </summary>
    public string? TechnicalManagerName { get; set; }

    /// <summary>
    /// Kỹ thuật viên - Technician Name
    /// </summary>
    public string? TechnicianName { get; set; }

    /// <summary>
    /// Ngày nhập (KQ) - Entry Date
    /// </summary>
    public DateTime? EntryDate { get; set; }

    /// <summary>
    /// Ngày QL duyệt (QLKT duyệt) - QLKT Approval Date
    /// </summary>
    public DateTime? QlktApprovalDate { get; set; }

    #endregion

    #region Status for Highlighting

    /// <summary>
    /// Trạng thái duyệt chỉ tiêu (QLKT) - Indicator Approval Status
    /// Nullable: chưa có nếu phiếu chưa gửi KTN
    /// </summary>
    public IndicatorApprovalStatus? IndicatorApprovalStatus { get; set; }

    /// <summary>
    /// Trạng thái testing của chỉ tiêu - Testing Status
    /// </summary>
    public TestingStatus? TestingStatus { get; set; }

    /// <summary>
    /// Trạng thái duyệt QLCL - Quality Approval Status
    /// </summary>
    public QualityIndicatorApprovalStatus? QualityApprovalStatus { get; set; }

    /// <summary>
    /// Đã phát hành báo cáo cho chỉ tiêu này - Is Report Published
    /// </summary>
    public bool IsReportPublished { get; set; }

    #endregion
}
