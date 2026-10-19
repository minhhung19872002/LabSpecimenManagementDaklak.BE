using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LabSpecimenManagement.Files;

namespace LabSpecimenManagement.TestRequests;

public class UpdateTestRequestDto
{
    #region Thông tin hợp đồng

    [StringLength(100)]
    public string? ContractNumber { get; set; }

    public DateTime? ContractDate { get; set; }

    public bool SentToAccounting { get; set; }

    public bool PaymentReceived { get; set; }

    public bool SentToDepartment { get; set; }

    public bool ResultReturned { get; set; }

    [Required]
    [StringLength(256)]
    public string CustomerName { get; set; } = string.Empty;

    [StringLength(20)]
    public string? ProvinceCode { get; set; }

    [StringLength(100)]
    public string? Province { get; set; }

    [StringLength(20)]
    public string? DistrictCode { get; set; }

    [StringLength(100)]
    public string? District { get; set; }

    [StringLength(20)]
    public string? WardCode { get; set; }

    [StringLength(100)]
    public string? Ward { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(50)]
    public string? TaxCode { get; set; }

    [StringLength(256)]
    public string? ContactPerson { get; set; }

    [Phone]
    [StringLength(50)]
    public string? Phone { get; set; }

    [StringLength(256)]
    public string? Email { get; set; }

    #endregion

    #region Thông tin còn lại

    public int TotalSampleCount { get; set; }

    #endregion

    #region Phương pháp thử nghiệm

    public bool MethodWestHighlandInstitute { get; set; }

    public bool MethodCustomer { get; set; }

    [StringLength(500)]
    public string? MethodCustomerDetail { get; set; }

    public ResultLanguage ResultLanguage { get; set; }

    [StringLength(50)]
    public string? ResultCopyCount { get; set; }

    public bool SeparatePassedResults { get; set; }

    [StringLength(500)]
    public string? ConformityDeclaration { get; set; }

    [StringLength(1000)]
    public string? OtherRequirements { get; set; }

    [StringLength(256)]
    public string? SubContract { get; set; }

    public SamplingRequirement SamplingRequirement { get; set; }

    public bool DeliveryDirectPickup { get; set; }

    public bool DeliveryPostal { get; set; }

    public bool ReturnStoredSample { get; set; }

    public bool SampleTypeService { get; set; }

    public bool SampleTypeContract { get; set; }

    public bool SampleTypeTask { get; set; }

    public bool SampleTypeSubContract { get; set; }

    public bool SampleTypeSurveillance { get; set; }

    public DateTime? ResultDeliveryDate { get; set; }

    public SampleStorageOption SampleStorage { get; set; }

    public decimal TotalCost { get; set; }

    [StringLength(500)]
    public string? TotalCostInWords { get; set; }

    [StringLength(100)]
    public string? ReceiptNumber { get; set; }

    public Guid? SupervisorId { get; set; }

    public Guid? DirectorId { get; set; }

    [StringLength(1000)]
    public string? ChangeRequest { get; set; }

    public bool AgreeTerms { get; set; }

    #endregion

    #region Legacy/Status fields

    [Required]
    public DateTime RequestDate { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    public TestRequestStatus Status { get; set; }

    public DateTime? ExpectedCompletionDate { get; set; }

    [StringLength(256)]
    public string? Subcontractor { get; set; }

    #endregion

    #region Collections

    public List<UpdateTestRequestSampleDto> Samples { get; set; } = new();

    /// <summary>
    /// Change history từ frontend (so sánh originalData vs newData)
    /// Chỉ gửi khi cần phê duyệt (SampleSent/InProgress status)
    /// </summary>
    public List<ChangeHistoryItemDto>? ChangeHistory { get; set; }

    #endregion
}

/// <summary>
/// DTO cho một mục thay đổi từ frontend
/// </summary>
public class ChangeHistoryItemDto
{
    public string FieldName { get; set; } = string.Empty;
    public string OldValue { get; set; } = string.Empty;
    public string NewValue { get; set; } = string.Empty;
}

public class UpdateTestRequestSampleDto
{
    public Guid? Id { get; set; } // null for new samples

    public int SortOrder { get; set; }

    [Required]
    [StringLength(50)]
    public string SampleCode { get; set; } = string.Empty;

    [Required]
    [StringLength(256)]
    public string SampleName { get; set; } = string.Empty;

    [StringLength(500)]
    public string? SamplingAddress { get; set; }

    [StringLength(100)]
    public string? SampleQuantity { get; set; }

    public SampleStatus Status { get; set; }

    [StringLength(256)]
    public string? StatusText { get; set; }

    [StringLength(100)]
    public string? ProcessingStatus { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [StringLength(256)]
    public string? StorageLocation { get; set; }

    /// <summary>
    /// Loại mẫu ID
    /// </summary>
    public Guid? SpecimenTypeId { get; set; }

    /// <summary>
    /// Loại mẫu Name - sẽ được resolve thành ID ở backend
    /// Nếu không tồn tại sẽ tạo mới (case-insensitive)
    /// </summary>
    [StringLength(100)]
    public string? SpecimenTypeName { get; set; }

    public List<UpdateTestRequestSampleIndicatorDto> Indicators { get; set; } = new();

    /// <summary>
    /// Danh sách hình ảnh đính kèm (đã upload trước đó)
    /// </summary>
    public List<AttachmentInputDto>? Attachments { get; set; }

    /// <summary>
    /// Mẫu chung - Mẫu có chỉ tiêu ở cả Lab Vi Sinh và Lab Hóa
    /// Khi true, Lab Vi Sinh sẽ xử lý trước, sau đó gửi mẫu cho Lab Hóa
    /// </summary>
    public bool IsSharedSample { get; set; }
}

public class UpdateTestRequestSampleIndicatorDto
{
    public Guid? Id { get; set; } // null for new indicators

    [Required]
    public Guid IndicatorId { get; set; }

    /// <summary>
    /// Phương pháp thử
    /// </summary>
    public Guid? TestMethodId { get; set; }

    /// <summary>
    /// Nền mẫu ID - để phân biệt giá của cùng indicator+method ở các nền mẫu khác nhau
    /// </summary>
    public Guid? SpecimenTypeId { get; set; }

    /// <summary>
    /// Khoa phòng thực hiện
    /// </summary>
    public Guid? LaboratoryId { get; set; }

    public decimal UnitPrice { get; set; }
    public decimal? OriginalUnitPrice { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

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
    /// Hợp đồng phụ (snapshot từ TestMethodIndicator)
    /// </summary>
    public bool IsSubcontract { get; set; }
}
