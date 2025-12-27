using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LabSpecimenManagement.Files;

namespace LabSpecimenManagement.TestRequests;

public class CreateTestRequestDto
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

    #endregion

    #region Thông tin còn lại

    public int TotalSampleCount { get; set; }

    #endregion

    #region Phương pháp thử nghiệm

    public bool MethodWestHighlandInstitute { get; set; }

    public bool MethodCustomer { get; set; }

    [StringLength(500)]
    public string? MethodCustomerDetail { get; set; }

    public ResultLanguage ResultLanguage { get; set; } = ResultLanguage.Vietnamese;

    [StringLength(50)]
    public string? ResultCopyCount { get; set; }

    public bool SeparatePassedResults { get; set; }

    [StringLength(500)]
    public string? ConformityDeclaration { get; set; }

    [StringLength(1000)]
    public string? OtherRequirements { get; set; }

    [StringLength(256)]
    public string? SubContract { get; set; }

    public SamplingRequirement SamplingRequirement { get; set; } = SamplingRequirement.CustomerSampling;

    public bool DeliveryDirectPickup { get; set; }

    public bool DeliveryPostal { get; set; }

    public bool ReturnStoredSample { get; set; }

    public bool SampleTypeService { get; set; }

    public bool SampleTypeContract { get; set; }

    public bool SampleTypeTask { get; set; }

    public bool SampleTypeSubContract { get; set; }

    public DateTime? ResultDeliveryDate { get; set; }

    public SampleStorageOption SampleStorage { get; set; } = SampleStorageOption.No;

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

    #region Legacy fields

    [Required]
    public DateTime RequestDate { get; set; } = DateTime.Now;

    [StringLength(2000)]
    public string? Notes { get; set; }

    public DateTime? ExpectedCompletionDate { get; set; }

    [StringLength(256)]
    public string? Subcontractor { get; set; }

    #endregion

    #region Collections

    public List<CreateTestRequestSampleDto> Samples { get; set; } = new();

    #endregion
}

public class CreateTestRequestSampleDto
{
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

    public SampleStatus Status { get; set; } = SampleStatus.Normal;

    [StringLength(256)]
    public string? StatusText { get; set; }

    [StringLength(100)]
    public string? ProcessingStatus { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [StringLength(256)]
    public string? StorageLocation { get; set; }

    public List<CreateTestRequestSampleIndicatorDto> Indicators { get; set; } = new();

    /// <summary>
    /// Danh sách hình ảnh đính kèm (đã upload trước đó)
    /// </summary>
    public List<AttachmentInputDto>? Attachments { get; set; }
}

public class CreateTestRequestSampleIndicatorDto
{
    [Required]
    public Guid IndicatorId { get; set; }

    /// <summary>
    /// Phương pháp thử
    /// </summary>
    public Guid? TestMethodId { get; set; }

    /// <summary>
    /// Khoa phòng thực hiện
    /// </summary>
    public Guid? LaboratoryId { get; set; }

    public decimal UnitPrice { get; set; }

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
}

/// <summary>
/// DTO for deserializing sample changes from JSON in change history
/// </summary>
public class SampleChangeDto
{
    public Guid? Id { get; set; }
    public string? SampleCode { get; set; }
    public string? SampleName { get; set; }
    public string? SamplingAddress { get; set; }
    public string? SampleQuantity { get; set; }
    public string? Notes { get; set; }
    public string? StorageLocation { get; set; }
    public List<IndicatorChangeDto>? Indicators { get; set; }
}

/// <summary>
/// DTO for deserializing indicator changes from JSON in change history
/// </summary>
public class IndicatorChangeDto
{
    public Guid? Id { get; set; }
    public Guid IndicatorId { get; set; }
    public Guid? TestMethodId { get; set; }
    public Guid? LaboratoryId { get; set; }
    public decimal UnitPrice { get; set; }
    public string? Notes { get; set; }
}
