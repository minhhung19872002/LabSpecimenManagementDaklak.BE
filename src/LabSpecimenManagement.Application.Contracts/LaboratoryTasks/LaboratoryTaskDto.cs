using System;
using System.Collections.Generic;
using LabSpecimenManagement.Testing;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// DTO for Laboratory Task (Công việc khoa phòng)
/// </summary>
public class LaboratoryTaskDto : FullAuditedEntityDto<Guid>
{
    /// <summary>
    /// TestRequest ID
    /// </summary>
    public Guid TestRequestId { get; set; }

    /// <summary>
    /// Số phiếu yêu cầu - Test Request Number
    /// </summary>
    public string? TestRequestNumber { get; set; }

    /// <summary>
    /// Laboratory ID
    /// </summary>
    public Guid LaboratoryId { get; set; }

    /// <summary>
    /// Mã khoa phòng - Laboratory Code
    /// </summary>
    public string? LaboratoryCode { get; set; }

    /// <summary>
    /// Tên khoa phòng - Laboratory Name
    /// </summary>
    public string? LaboratoryName { get; set; }

    /// <summary>
    /// Số hợp đồng - Contract Number
    /// </summary>
    public string? ContractNumber { get; set; }

    /// <summary>
    /// Tổng số mẫu - Total Sample Count
    /// </summary>
    public int TotalSampleCount { get; set; }

    /// <summary>
    /// ID Quản lý kỹ thuật - Technical Manager ID
    /// </summary>
    public Guid? TechnicalManagerId { get; set; }

    /// <summary>
    /// Tên Quản lý kỹ thuật - Technical Manager Name
    /// </summary>
    public string? TechnicalManagerName { get; set; }

    /// <summary>
    /// Ngày chuyển giao - Transfer Date
    /// </summary>
    public DateTime? TransferDate { get; set; }

    /// <summary>
    /// Ngày yêu cầu hoàn thành - Request Completion Date
    /// </summary>
    public DateTime? RequestCompletionDate { get; set; }

    /// <summary>
    /// Ngày thí nghiệm - Test Date (ngày sớm nhất từ indicators)
    /// </summary>
    public DateTime? TestDate { get; set; }

    /// <summary>
    /// Khoảng ngày thử nghiệm - Test Date Range
    /// Format: "dd/MM/yyyy - dd/MM/yyyy" (Min - Max từ tất cả indicators)
    /// Giống format trên báo cáo
    /// </summary>
    public string? TestDateRange { get; set; }

    /// <summary>
    /// Trạng thái - Status
    /// </summary>
    public LaboratoryTaskStatus Status { get; set; }

    /// <summary>
    /// Ghi chú - Notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Lý do QLCL từ chối kết quả
    /// </summary>
    public string? ResultRejectionReason { get; set; }

    /// <summary>
    /// Tên khách hàng - Customer Name (from TestRequest)
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Danh sách chỉ tiêu - Indicators
    /// </summary>
    public List<LaboratoryTaskIndicatorDto> Indicators { get; set; } = new();

    /// <summary>
    /// Danh sách người hỗ trợ - Supporters
    /// </summary>
    public List<LaboratoryTaskSupporterDto> Supporters { get; set; } = new();

    /// <summary>
    /// Current user là QLKT của task này hay không
    /// </summary>
    public bool IsCurrentUserTechnicalManager { get; set; }

    /// <summary>
    /// Current user là Supporter của task này hay không
    /// </summary>
    public bool IsCurrentUserSupporter { get; set; }

    // ====== Shared Sample Fields ======

    /// <summary>
    /// Task này thuộc về mẫu chung (có chỉ tiêu ở cả Lab Vi Sinh và Lab Hóa)
    /// </summary>
    public bool IsSharedSampleTask { get; set; }

    /// <summary>
    /// Task này đang chờ lab trước hoàn thành (dành cho Lab Hóa chờ Vi Sinh)
    /// </summary>
    public bool IsWaitingForPriorLab { get; set; }

    /// <summary>
    /// Đã gửi mẫu cho lab tiếp theo (dành cho Lab Vi Sinh đã gửi cho Hóa)
    /// </summary>
    public bool SampleSentToNextLab { get; set; }

    /// <summary>
    /// Ngày gửi mẫu cho lab tiếp theo
    /// </summary>
    public DateTime? SampleSentDate { get; set; }

    /// <summary>
    /// Ngày nhận mẫu từ lab trước (dành cho Lab Hóa nhận từ Vi Sinh)
    /// </summary>
    public DateTime? SampleReceivedDate { get; set; }

    /// <summary>
    /// Ghi chú khi nhận mẫu
    /// </summary>
    public string? SampleReceivedNote { get; set; }

    /// <summary>
    /// ID của Task lab trước (dành cho Lab Hóa reference đến Task Vi Sinh)
    /// </summary>
    public Guid? PriorLabTaskId { get; set; }

    /// <summary>
    /// Lab trước đã gửi mẫu chưa (dành cho Lab Hóa kiểm tra Vi Sinh đã gửi mẫu)
    /// True = Vi Sinh đã gửi, có thể nhận mẫu
    /// False = Vi Sinh chưa gửi, chờ đợi
    /// </summary>
    public bool PriorLabSentSample { get; set; }
}

/// <summary>
/// DTO for Laboratory Task Indicator (Chỉ tiêu trong công việc khoa phòng)
/// </summary>
public class LaboratoryTaskIndicatorDto
{
    /// <summary>
    /// ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// TestRequestSampleIndicator ID
    /// </summary>
    public Guid TestRequestSampleIndicatorId { get; set; }

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
    /// Tên nhóm chỉ tiêu - Indicator Group Name
    /// </summary>
    public string? IndicatorGroupName { get; set; }

    /// <summary>
    /// ID chỉ tiêu cha (null nếu là chỉ tiêu gốc) - Parent Indicator ID
    /// </summary>
    public Guid? ParentIndicatorId { get; set; }

    /// <summary>
    /// Đơn giá - Unit Price
    /// </summary>
    public decimal UnitPrice { get; set; }

    // ====== Phase 6 - Workflow Fields ======

    /// <summary>
    /// ID phương pháp thử nghiệm - Method ID
    /// </summary>
    public Guid? MethodId { get; set; }

    /// <summary>
    /// Tên phương pháp - Method Name
    /// </summary>
    public string? MethodName { get; set; }

    /// <summary>
    /// Tên phương pháp có định dạng (HTML) - Method Formatted Name
    /// </summary>
    public string? MethodFormattedName { get; set; }

    /// <summary>
    /// ID Thử nghiệm viên - Technician ID
    /// </summary>
    public Guid? TechnicianId { get; set; }

    /// <summary>
    /// Tên Thử nghiệm viên - Technician Name
    /// </summary>
    public string? TechnicianName { get; set; }

    /// <summary>
    /// Ngày giao việc - Assigned Date
    /// </summary>
    public DateTime? AssignedDate { get; set; }

    /// <summary>
    /// Ngày thử nghiệm - Test Date (ngày KTV bắt đầu thử nghiệm/check-in)
    /// </summary>
    public DateTime? TestDate { get; set; }

    /// <summary>
    /// Kết quả thí nghiệm - Result
    /// </summary>
    public string? Result { get; set; }

    /// <summary>
    /// Ghi chú tự do (free text) - áp dụng cho tất cả chỉ tiêu trong sample
    /// </summary>
    public string? FreeTextNote { get; set; }

    /// <summary>
    /// Danh sách ID của Master Data Notes (comma-separated GUIDs)
    /// </summary>
    public string? MasterDataNoteIds { get; set; }

    /// <summary>
    /// Danh sách nội dung Master Data Notes (để hiển thị trên UI)
    /// </summary>
    public List<string>? MasterDataNoteContents { get; set; }

    /// <summary>
    /// Đơn vị tính - Unit
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// Ngày nhập kết quả - Entry Date
    /// </summary>
    public DateTime? EntryDate { get; set; }

    /// <summary>
    /// ID người nhập - Entered By ID
    /// </summary>
    public Guid? EnteredById { get; set; }

    /// <summary>
    /// Tên người nhập - Entered By Name
    /// </summary>
    public string? EnteredByName { get; set; }

    /// <summary>
    /// Trạng thái duyệt - Approval Status
    /// </summary>
    public IndicatorApprovalStatus ApprovalStatus { get; set; }

    /// <summary>
    /// Ngày duyệt - Approval Date
    /// </summary>
    public DateTime? ApprovalDate { get; set; }

    /// <summary>
    /// ID người duyệt - Approved By ID
    /// </summary>
    public Guid? ApprovedById { get; set; }

    /// <summary>
    /// Tên người duyệt - Approved By Name
    /// </summary>
    public string? ApprovedByName { get; set; }

    /// <summary>
    /// Lý do từ chối - Rejection Reason
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Đã gửi cho QLCL chưa - Is Sent To Quality
    /// </summary>
    public bool IsSentToQuality { get; set; }

    /// <summary>
    /// Chỉ tiêu này đang chờ mẫu chung từ lab khác (Vi Sinh)
    /// </summary>
    public bool IsWaitingForSharedSample { get; set; }

    // ====== Standard Comparison Fields (Phase 7) ======

    /// <summary>
    /// ID tiêu chuẩn so sánh - Standard ID
    /// </summary>
    public Guid? StandardId { get; set; }

    /// <summary>
    /// Tên tiêu chuẩn - Standard Name
    /// </summary>
    public string? StandardName { get; set; }

    /// <summary>
    /// Ký hiệu so sánh - Comparison Operator (<=, >=, <, >, =)
    /// </summary>
    public string? ComparisonOperator { get; set; }

    /// <summary>
    /// Giới hạn cho phép - Allowed Limit
    /// </summary>
    public string? AllowedLimit { get; set; }

    /// <summary>
    /// Ký hiệu so sánh cận dưới (dùng cho khoảng): >, >=
    /// </summary>
    public string? LowerOperator { get; set; }

    /// <summary>
    /// Giá trị cận dưới (dùng cho khoảng)
    /// </summary>
    public string? LowerLimit { get; set; }

    /// <summary>
    /// Ký hiệu so sánh cận trên (dùng cho khoảng): <, <=
    /// </summary>
    public string? UpperOperator { get; set; }

    /// <summary>
    /// Giá trị cận trên (dùng cho khoảng)
    /// </summary>
    public string? UpperLimit { get; set; }

    /// <summary>
    /// Danh sách hình ảnh của chỉ tiêu (max 5)
    /// </summary>
    public List<IndicatorImageDto> Images { get; set; } = new();

    // ====== Workflow Redesign - Testing Rejection Fields ======

    /// <summary>
    /// Lý do từ chối thử nghiệm (QLKT từ chối)
    /// </summary>
    public string? TestingRejectionReason { get; set; }

    /// <summary>
    /// Ngày QLKT từ chối thử nghiệm
    /// </summary>
    public DateTime? TestingRejectionDate { get; set; }

    /// <summary>
    /// Người từ chối thử nghiệm - QLKT User ID
    /// </summary>
    public Guid? TestingRejectedById { get; set; }

    /// <summary>
    /// Tên người từ chối thử nghiệm
    /// </summary>
    public string? TestingRejectedByName { get; set; }

    // ====== Workflow Redesign - Customer Confirmation Fields ======

    /// <summary>
    /// Ngày KH xác nhận
    /// </summary>
    public DateTime? CustomerConfirmationDate { get; set; }

    /// <summary>
    /// Người xác nhận KH - BPGN User ID
    /// </summary>
    public Guid? CustomerConfirmedById { get; set; }

    /// <summary>
    /// Tên người xác nhận KH
    /// </summary>
    public string? CustomerConfirmedByName { get; set; }

    /// <summary>
    /// Lịch sử yêu cầu đổi phương pháp
    /// </summary>
    public List<MethodChangeHistoryDto> MethodChangeHistories { get; set; } = new();

    // ====== Workflow Redesign - Supplementary Request Fields ======

    /// <summary>
    /// Nội dung yêu cầu bổ sung (QLKT yêu cầu KH cung cấp)
    /// </summary>
    public string? SupplementaryRequestReason { get; set; }

    /// <summary>
    /// Ngày QLKT yêu cầu bổ sung
    /// </summary>
    public DateTime? SupplementaryRequestDate { get; set; }

    /// <summary>
    /// ID người yêu cầu bổ sung - QLKT User ID
    /// </summary>
    public Guid? SupplementaryRequestedById { get; set; }

    /// <summary>
    /// Tên người yêu cầu bổ sung
    /// </summary>
    public string? SupplementaryRequestedByName { get; set; }
}

/// <summary>
/// DTO for Laboratory Task Supporter
/// </summary>
public class LaboratoryTaskSupporterDto
{
    /// <summary>
    /// Supporter ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// LaboratoryTask ID
    /// </summary>
    public Guid LaboratoryTaskId { get; set; }

    /// <summary>
    /// User ID của người hỗ trợ
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Tên người hỗ trợ
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Tên đầy đủ của người hỗ trợ
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Email
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Thời gian thêm
    /// </summary>
    public DateTime CreationTime { get; set; }
}

/// <summary>
/// DTO for adding/removing supporters
/// </summary>
public class ManageSupportersDto
{
    /// <summary>
    /// List of User IDs to add as supporters
    /// </summary>
    public List<Guid> UserIds { get; set; } = new();
}

/// <summary>
/// DTO for bulk approve/reject indicators
/// </summary>
public class BulkApproveIndicatorsDto
{
    /// <summary>
    /// List of indicator IDs to approve/reject
    /// </summary>
    public List<Guid> IndicatorIds { get; set; } = new();

    /// <summary>
    /// True = Approve, False = Reject
    /// </summary>
    public bool Approve { get; set; }

    /// <summary>
    /// Rejection reason (required when Approve = false)
    /// </summary>
    public string? RejectionReason { get; set; }

    /// <summary>
    /// Mapping giữa IndicatorId và StandardId (optional)
    /// Key = IndicatorId, Value = StandardId
    /// </summary>
    public Dictionary<Guid, Guid?>? StandardIds { get; set; }
}

/// <summary>
/// DTO for sending results to Quality Management
/// </summary>
public class SendToQualityDto
{
    /// <summary>
    /// List of indicator IDs to send to QLCL
    /// </summary>
    public List<Guid> IndicatorIds { get; set; } = new();

    /// <summary>
    /// Quality Manager User ID (QLCL user to receive results)
    /// </summary>
    public Guid QualityManagerId { get; set; }

    /// <summary>
    /// Nội dung ghi chú đã edit (bao gồm ghi chú từ các chỉ tiêu)
    /// </summary>
    public string? NoteContent { get; set; }

    /// <summary>
    /// Danh sách Note IDs từ Master Data
    /// </summary>
    public List<Guid>? SelectedNoteIds { get; set; }
}

// ============= Workflow Redesign - QLKT Evaluation DTOs =============

/// <summary>
/// DTO for QLKT to evaluate a single indicator
/// </summary>
public class EvaluateIndicatorDto
{
    /// <summary>
    /// Hành động đánh giá: Accept, RequestMethodChange, Reject
    /// </summary>
    public IndicatorEvaluationAction Action { get; set; }

    /// <summary>
    /// Phương pháp mới (bắt buộc khi Action = RequestMethodChange)
    /// </summary>
    public Guid? NewMethodId { get; set; }

    /// <summary>
    /// Lý do (bắt buộc khi Action = RequestMethodChange hoặc Reject)
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// Enum for indicator evaluation actions
/// </summary>
public enum IndicatorEvaluationAction
{
    /// <summary>
    /// Chấp nhận thử nghiệm
    /// </summary>
    Accept = 0,

    /// <summary>
    /// Yêu cầu đổi phương pháp
    /// </summary>
    RequestMethodChange = 1,

    /// <summary>
    /// Từ chối thử nghiệm
    /// </summary>
    Reject = 2,

    /// <summary>
    /// Yêu cầu KH cung cấp thông tin bổ sung
    /// </summary>
    RequestSupplementary = 3
}

/// <summary>
/// DTO for QLKT to request method change for multiple indicators
/// </summary>
public class RequestMethodChangeDto
{
    /// <summary>
    /// Danh sách các yêu cầu đổi phương pháp
    /// </summary>
    public List<MethodChangeRequestItem> Items { get; set; } = new();

    /// <summary>
    /// Lý do đổi phương pháp (áp dụng cho tất cả chỉ tiêu)
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Item in method change request
/// </summary>
public class MethodChangeRequestItem
{
    /// <summary>
    /// LaboratoryTaskIndicator ID
    /// </summary>
    public Guid IndicatorId { get; set; }

    /// <summary>
    /// ID phương pháp mới
    /// </summary>
    public Guid NewMethodId { get; set; }
}

/// <summary>
/// DTO for QLKT to reject indicators for testing
/// </summary>
public class RejectIndicatorsForTestingDto
{
    /// <summary>
    /// Danh sách LaboratoryTaskIndicator IDs
    /// </summary>
    public List<Guid> IndicatorIds { get; set; } = new();

    /// <summary>
    /// Lý do từ chối
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// DTO for method change history
/// </summary>
public class MethodChangeHistoryDto
{
    public Guid Id { get; set; }
    public Guid IndicatorId { get; set; }
    public Guid? OldMethodId { get; set; }
    public string? OldMethodName { get; set; }
    public Guid NewMethodId { get; set; }
    public string NewMethodName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime RequestedDate { get; set; }
    public Guid RequestedByUserId { get; set; }
    public string RequestedByUserName { get; set; } = string.Empty;
    public DateTime? ConfirmedDate { get; set; }
    public Guid? ConfirmedByUserId { get; set; }
    public string? ConfirmedByUserName { get; set; }
    public bool? IsApproved { get; set; }
}

/// <summary>
/// DTO cho việc gửi mẫu chung (Lab Vi Sinh gửi cho Lab Hóa)
/// </summary>
public class SendSharedSampleDto
{
    /// <summary>
    /// Ghi chú khi gửi mẫu (optional)
    /// </summary>
    public string? Note { get; set; }
}

/// <summary>
/// DTO cho việc nhận mẫu chung (Lab Hóa nhận từ Lab Vi Sinh)
/// </summary>
public class ReceiveSharedSampleDto
{
    /// <summary>
    /// Ngày nhận mẫu (default: ngày hiện tại)
    /// </summary>
    public DateTime? ReceivedDate { get; set; }

    /// <summary>
    /// Ghi chú khi nhận mẫu
    /// </summary>
    public string? Note { get; set; }
}
