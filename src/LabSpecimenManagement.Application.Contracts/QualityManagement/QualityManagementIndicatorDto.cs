using System;
using System.Collections.Generic;
using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.Testing;

namespace LabSpecimenManagement.QualityManagement;

/// <summary>
/// DTO for Quality Management Indicator
/// </summary>
public class QualityManagementIndicatorDto
{
    /// <summary>
    /// ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// QualityManagementIndicator ID thực tế nếu chỉ tiêu đã vào scope QLCL
    /// </summary>
    public Guid? QualityManagementIndicatorId { get; set; }

    /// <summary>
    /// TestRequestSampleIndicator ID gốc
    /// </summary>
    public Guid TestRequestSampleIndicatorId { get; set; }

    /// <summary>
    /// LaboratoryTaskIndicator ID
    /// </summary>
    public Guid? LaboratoryTaskIndicatorId { get; set; }

    /// <summary>
    /// LaboratoryTask ID hiện tại của chỉ tiêu
    /// </summary>
    public Guid? LaboratoryTaskId { get; set; }

    /// <summary>
    /// Laboratory ID
    /// </summary>
    public Guid LaboratoryId { get; set; }

    /// <summary>
    /// Tên khoa phòng - Laboratory Name
    /// </summary>
    public string? LaboratoryName { get; set; }

    /// <summary>
    /// Sample ID
    /// </summary>
    public Guid SampleId { get; set; }

    /// <summary>
    /// Mã mẫu - Sample Code
    /// </summary>
    public string? SampleCode { get; set; }

    /// <summary>
    /// Tên mẫu - Sample Name
    /// </summary>
    public string? SampleName { get; set; }

    /// <summary>
    /// Loại mẫu - Specimen Type Name
    /// </summary>
    public string? SpecimenTypeName { get; set; }

    /// <summary>
    /// Indicator ID
    /// </summary>
    public Guid IndicatorId { get; set; }

    /// <summary>
    /// Mã chỉ tiêu - Indicator Code
    /// </summary>
    public string? IndicatorCode { get; set; }

    /// <summary>
    /// Tên chỉ tiêu - Indicator Name
    /// </summary>
    public string? IndicatorName { get; set; }

    /// <summary>
    /// Tên chỉ tiêu có định dạng (HTML) - Indicator Formatted Name
    /// </summary>
    public string? IndicatorFormattedName { get; set; }

    /// <summary>
    /// Tên phương pháp - Method Name
    /// </summary>
    public string? MethodName { get; set; }

    /// <summary>
    /// Tên phương pháp có định dạng (HTML) - Method Formatted Name
    /// </summary>
    public string? MethodFormattedName { get; set; }

    /// <summary>
    /// Method ID
    /// </summary>
    public Guid? MethodId { get; set; }

    /// <summary>
    /// Đơn vị tính - Unit
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// Kết quả - Result
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// Ngày gửi KQ từ QLKT - Result Sent Date (from LaboratoryTaskIndicator)
    /// </summary>
    public DateTime? ResultSentDate { get; set; }

    /// <summary>
    /// Ngày QLCL duyệt - Approval Date
    /// </summary>
    public DateTime? ApprovalDate { get; set; }

    /// <summary>
    /// Tiêu chuẩn so sánh - Standard ID
    /// </summary>
    public Guid? StandardId { get; set; }

    /// <summary>
    /// Tên tiêu chuẩn - Standard Name
    /// </summary>
    public string? StandardName { get; set; }

    /// <summary>
    /// Giới hạn tham khảo - Reference Limit (deprecated - use ComparisonOperator + AllowedLimit)
    /// </summary>
    public string? ReferenceLimit { get; set; }

    /// <summary>
    /// Ký hiệu so sánh - Comparison Operator (<=, >=, <, >, =)
    /// </summary>
    public string? ComparisonOperator { get; set; }

    /// <summary>
    /// Giới hạn cho phép - Allowed Limit
    /// </summary>
    public string? AllowedLimit { get; set; }

    /// <summary>
    /// Ký hiệu cận dưới (>, >=) - cho khoảng - Lower Operator
    /// </summary>
    public string? LowerOperator { get; set; }

    /// <summary>
    /// Giá trị cận dưới - cho khoảng - Lower Limit
    /// </summary>
    public string? LowerLimit { get; set; }

    /// <summary>
    /// Ký hiệu cận trên (<, <=) - cho khoảng - Upper Operator
    /// </summary>
    public string? UpperOperator { get; set; }

    /// <summary>
    /// Giá trị cận trên - cho khoảng - Upper Limit
    /// </summary>
    public string? UpperLimit { get; set; }

    /// <summary>
    /// Trạng thái phê duyệt - Approval Status
    /// </summary>
    public QualityIndicatorApprovalStatus? ApprovalStatus { get; set; }

    /// <summary>
    /// Lý do từ chối - Rejection Reason
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Đã gửi kết quả cho BPGN - Is Result Sent (DEPRECATED)
    /// </summary>
    public bool IsResultSent { get; set; }

    /// <summary>
    /// Ngày gửi kết quả cho BPGN - Quality Result Sent Date (DEPRECATED)
    /// </summary>
    public DateTime? QualityResultSentDate { get; set; }

    /// <summary>
    /// Đã in kết quả (phát hành) - Is Printed
    /// </summary>
    public bool IsPrinted { get; set; }

    /// <summary>
    /// Ngày in kết quả - Printed Date
    /// </summary>
    public DateTime? PrintedDate { get; set; }

    /// <summary>
    /// ID người duyệt - Approved By ID
    /// </summary>
    public Guid? ApprovedById { get; set; }

    /// <summary>
    /// Tên người duyệt - Approved By Name
    /// </summary>
    public string? ApprovedByName { get; set; }

    /// <summary>
    /// Danh sách hình ảnh của chỉ tiêu (max 5)
    /// </summary>
    public List<IndicatorImageDto> Images { get; set; } = new();

    /// <summary>
    /// Ghi chú tự do từ QLKT - Free Text Note
    /// </summary>
    public string? FreeTextNote { get; set; }

    /// <summary>
    /// Danh sách nội dung Master Data Notes
    /// </summary>
    public List<string> MasterDataNotes { get; set; } = new();

    /// <summary>
    /// ID chỉ tiêu cha (null nếu là chỉ tiêu gốc) - Parent Indicator ID
    /// </summary>
    public Guid? ParentIndicatorId { get; set; }

    /// <summary>
    /// Tên QLKT phụ trách task hiện tại của chỉ tiêu
    /// </summary>
    public string? TechnicalManagerName { get; set; }

    /// <summary>
    /// ID KTV đang được gán cho chỉ tiêu
    /// </summary>
    public Guid? TechnicianId { get; set; }

    /// <summary>
    /// Tên KTV đang được gán cho chỉ tiêu
    /// </summary>
    public string? TechnicianName { get; set; }

    /// <summary>
    /// Ngày QLKT duyệt/chấp nhận chỉ tiêu
    /// </summary>
    public DateTime? QlktApprovalDate { get; set; }

    /// <summary>
    /// Trạng thái workflow phía QLKT/KTV của chỉ tiêu
    /// </summary>
    public IndicatorApprovalStatus? IndicatorApprovalStatus { get; set; }

    /// <summary>
    /// Trạng thái thử nghiệm suy ra từ workflow hiện tại
    /// </summary>
    public TestingStatus? TestingStatus { get; set; }

    /// <summary>
    /// Trạng thái hiển thị tổng hợp cho màn QLCL
    /// </summary>
    public QLCLDisplayStatus? QlclDisplayStatus { get; set; }

    /// <summary>
    /// Chỉ tiêu này hiện có nằm trong scope của task QLCL đang xem hay không
    /// </summary>
    public bool IsInQualityScope { get; set; }
}
