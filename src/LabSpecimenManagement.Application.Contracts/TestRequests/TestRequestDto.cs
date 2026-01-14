using System;
using System.Collections.Generic;
using LabSpecimenManagement.Files;
using LabSpecimenManagement.LaboratoryTasks;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.TestRequests;

public class TestRequestDto : FullAuditedEntityDto<Guid>
{
    #region Liên kết báo giá

    /// <summary>
    /// Mã báo giá (nếu tạo từ báo giá)
    /// </summary>
    public Guid? QuotationId { get; set; }

    #endregion

    #region Thông tin hợp đồng

    public string? ContractNumber { get; set; }
    public DateTime? ContractDate { get; set; }
    public bool SentToAccounting { get; set; }
    public bool PaymentReceived { get; set; }
    public bool SentToDepartment { get; set; }
    public bool ResultReturned { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? ProvinceCode { get; set; }
    public string? Province { get; set; }
    public string? DistrictCode { get; set; }
    public string? District { get; set; }
    public string? WardCode { get; set; }
    public string? Ward { get; set; }
    public string? Address { get; set; }
    public string? TaxCode { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }

    #endregion

    #region Thông tin còn lại

    public int TotalSampleCount { get; set; }

    #endregion

    #region Phương pháp thử nghiệm

    public bool MethodWestHighlandInstitute { get; set; }
    public bool MethodCustomer { get; set; }
    public string? MethodCustomerDetail { get; set; }
    public ResultLanguage ResultLanguage { get; set; }
    public string? ResultCopyCount { get; set; }
    public bool SeparatePassedResults { get; set; }
    public string? ConformityDeclaration { get; set; }
    public string? OtherRequirements { get; set; }
    public string? SubContract { get; set; }
    public SamplingRequirement SamplingRequirement { get; set; }
    public bool DeliveryDirectPickup { get; set; }
    public bool DeliveryPostal { get; set; }
    public bool ReturnStoredSample { get; set; }
    public bool SampleTypeService { get; set; }
    public bool SampleTypeContract { get; set; }
    public bool SampleTypeTask { get; set; }
    public bool SampleTypeSubContract { get; set; }
    public DateTime? ResultDeliveryDate { get; set; }
    public SampleStorageOption SampleStorage { get; set; }
    public decimal TotalCost { get; set; }
    public string? TotalCostInWords { get; set; }
    public string? ReceiptNumber { get; set; }
    public Guid? SupervisorId { get; set; }
    public string? SupervisorName { get; set; }
    public Guid? DirectorId { get; set; }
    public string? DirectorName { get; set; }
    public string? ChangeRequest { get; set; }
    public bool AgreeTerms { get; set; }

    #endregion

    #region Legacy fields

    public string RequestNumber { get; set; } = string.Empty;
    public DateTime RequestDate { get; set; }
    public string? Notes { get; set; }
    public TestRequestStatus Status { get; set; }
    public DateTime? ExpectedCompletionDate { get; set; }
    public string? Subcontractor { get; set; }

    #endregion

    #region Postal Delivery Image

    /// <summary>
    /// Danh sách hình ảnh minh chứng gửi bưu điện (tối đa 5 hình)
    /// </summary>
    public List<PostalDeliveryImageDto> PostalDeliveryImages { get; set; } = new();

    #endregion

    #region Collections

    public List<TestRequestSampleDto> Samples { get; set; } = new();

    #endregion
}

public class TestRequestSampleDto
{
    public Guid Id { get; set; }
    public Guid TestRequestId { get; set; }
    public int SortOrder { get; set; }
    public string SampleCode { get; set; } = string.Empty;
    public string SampleName { get; set; } = string.Empty;
    public string? SamplingAddress { get; set; }
    public string? SampleQuantity { get; set; }
    public SampleStatus Status { get; set; }
    public string? StatusText { get; set; }
    public string? ProcessingStatus { get; set; }
    public string? Notes { get; set; }
    public string? StorageLocation { get; set; }
    public decimal TotalIndicatorCost { get; set; }

    /// <summary>
    /// Đánh dấu mẫu đã bị hủy (dùng cho quản lý mẫu lưu kho)
    /// </summary>
    public bool IsCancelled { get; set; }

    /// <summary>
    /// Đánh dấu mẫu đã bị khách hàng hủy (sau khi đã gửi mẫu)
    /// </summary>
    public bool IsCustomerCancelled { get; set; }

    /// <summary>
    /// Loại mẫu ID
    /// </summary>
    public Guid? SpecimenTypeId { get; set; }

    /// <summary>
    /// Tên loại mẫu (để hiển thị)
    /// </summary>
    public string? SpecimenTypeName { get; set; }

    public List<TestRequestSampleIndicatorDto> Indicators { get; set; } = new();

    /// <summary>
    /// Danh sách hình ảnh đính kèm của mẫu (upload khi tạo/edit TestRequest)
    /// </summary>
    public List<SampleAttachmentDto> Attachments { get; set; } = new();

    /// <summary>
    /// Danh sách hình ảnh trước thử nghiệm (upload bởi QLKT/KTV)
    /// </summary>
    public List<SampleTestingImageDto> BeforeTestingImages { get; set; } = new();

    /// <summary>
    /// Danh sách hình ảnh sau thử nghiệm (upload bởi QLKT/KTV)
    /// </summary>
    public List<SampleTestingImageDto> AfterTestingImages { get; set; } = new();
}

public class TestRequestSampleIndicatorDto
{
    public Guid Id { get; set; }
    public Guid TestRequestSampleId { get; set; }
    public Guid IndicatorId { get; set; }
    public string? IndicatorCode { get; set; }
    public string? IndicatorName { get; set; }
    /// <summary>
    /// Tên chỉ tiêu có định dạng (HTML)
    /// </summary>
    public string? IndicatorFormattedName { get; set; }
    public string? IndicatorGroupName { get; set; }

    /// <summary>
    /// Khoa phòng thực hiện mặc định của nhóm chỉ tiêu (để auto-fill khi gửi QLKT)
    /// </summary>
    public Guid? IndicatorGroupLaboratoryId { get; set; }

    public decimal UnitPrice { get; set; }
    public string? Notes { get; set; }

    /// <summary>
    /// Khoa phòng thực hiện
    /// </summary>
    public Guid? LaboratoryId { get; set; }

    /// <summary>
    /// Tên khoa phòng thực hiện
    /// </summary>
    public string? LaboratoryName { get; set; }

    /// <summary>
    /// Phương pháp thử
    /// </summary>
    public Guid? TestMethodId { get; set; }

    /// <summary>
    /// Mã phương pháp thử
    /// </summary>
    public string? TestMethodCode { get; set; }

    /// <summary>
    /// Tên phương pháp thử
    /// </summary>
    public string? TestMethodName { get; set; }

    /// <summary>
    /// Tên phương pháp thử có định dạng (HTML)
    /// </summary>
    public string? TestMethodFormattedName { get; set; }

    /// <summary>
    /// Ngày chuyển giao
    /// </summary>
    public DateTime? TransferDate { get; set; }

    /// <summary>
    /// Ngày yêu cầu hoàn thành
    /// </summary>
    public DateTime? RequestDate { get; set; }

    /// <summary>
    /// Đã gửi khoa phòng
    /// </summary>
    public bool SentToDepartment { get; set; }

    /// <summary>
    /// Trạng thái duyệt của chỉ tiêu trong LaboratoryTaskIndicator
    /// null nếu chưa gửi mẫu, có giá trị nếu đã gửi mẫu
    /// </summary>
    public IndicatorApprovalStatus? ApprovalStatus { get; set; }

    /// <summary>
    /// Ngày thử nghiệm (ngày KTV bắt đầu thử nghiệm/check-in)
    /// null nếu chưa bắt đầu thử nghiệm
    /// </summary>
    public DateTime? TestDate { get; set; }

    /// <summary>
    /// Hình ảnh của chỉ tiêu (từ IndicatorImage)
    /// </summary>
    public List<Testing.IndicatorImageDto>? Images { get; set; }
}

// ============= Workflow Redesign - BPGN Confirmation DTOs =============

/// <summary>
/// DTO để lấy danh sách chỉ tiêu chờ KH xác nhận
/// </summary>
public class IndicatorPendingConfirmationDto
{
    /// <summary>
    /// LaboratoryTaskIndicator ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Mã mẫu
    /// </summary>
    public string? SampleCode { get; set; }

    /// <summary>
    /// Tên mẫu
    /// </summary>
    public string? SampleName { get; set; }

    /// <summary>
    /// Mã chỉ tiêu
    /// </summary>
    public string? IndicatorCode { get; set; }

    /// <summary>
    /// Tên chỉ tiêu
    /// </summary>
    public string? IndicatorName { get; set; }

    /// <summary>
    /// Tên chỉ tiêu có định dạng (HTML)
    /// </summary>
    public string? IndicatorFormattedName { get; set; }

    /// <summary>
    /// Tên khoa phòng
    /// </summary>
    public string? LaboratoryName { get; set; }

    /// <summary>
    /// Trạng thái hiện tại
    /// </summary>
    public IndicatorApprovalStatus ApprovalStatus { get; set; }

    /// <summary>
    /// Lý do từ chối thử nghiệm (nếu có)
    /// </summary>
    public string? TestingRejectionReason { get; set; }

    /// <summary>
    /// Phương pháp cũ (nếu yêu cầu đổi PP)
    /// </summary>
    public string? OldMethodName { get; set; }

    /// <summary>
    /// Phương pháp mới được đề xuất (nếu yêu cầu đổi PP)
    /// </summary>
    public string? NewMethodName { get; set; }

    /// <summary>
    /// Lý do yêu cầu đổi PP (nếu có)
    /// </summary>
    public string? MethodChangeReason { get; set; }

    /// <summary>
    /// Người yêu cầu đổi PP / từ chối TN
    /// </summary>
    public string? RequestedByName { get; set; }

    /// <summary>
    /// Ngày yêu cầu
    /// </summary>
    public DateTime? RequestedDate { get; set; }

    // ====== Supplementary Request Fields ======

    /// <summary>
    /// Nội dung yêu cầu bổ sung (nếu có)
    /// </summary>
    public string? SupplementaryRequestReason { get; set; }

    /// <summary>
    /// Ngày yêu cầu bổ sung
    /// </summary>
    public DateTime? SupplementaryRequestDate { get; set; }

    /// <summary>
    /// Người yêu cầu bổ sung
    /// </summary>
    public string? SupplementaryRequestedByName { get; set; }
}

/// <summary>
/// DTO cho BPGN xác nhận từng chỉ tiêu (KH xác nhận)
/// </summary>
public class ConfirmIndicatorDto
{
    /// <summary>
    /// True = KH xác nhận đồng ý, False = KH không đồng ý
    /// </summary>
    public bool Confirmed { get; set; }
}

/// <summary>
/// DTO cho BPGN xác nhận nhiều chỉ tiêu cùng lúc
/// </summary>
public class BulkConfirmIndicatorsDto
{
    /// <summary>
    /// Danh sách LaboratoryTaskIndicator IDs
    /// </summary>
    public List<Guid> IndicatorIds { get; set; } = new();

    /// <summary>
    /// True = KH xác nhận đồng ý, False = KH không đồng ý
    /// </summary>
    public bool Confirmed { get; set; }
}
