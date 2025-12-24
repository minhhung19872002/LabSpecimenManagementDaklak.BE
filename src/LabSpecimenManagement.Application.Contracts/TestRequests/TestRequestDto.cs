using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.TestRequests;

public class TestRequestDto : FullAuditedEntityDto<Guid>
{
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
    public List<TestRequestSampleIndicatorDto> Indicators { get; set; } = new();
}

public class TestRequestSampleIndicatorDto
{
    public Guid Id { get; set; }
    public Guid TestRequestSampleId { get; set; }
    public Guid IndicatorId { get; set; }
    public string? IndicatorCode { get; set; }
    public string? IndicatorName { get; set; }
    public string? IndicatorGroupName { get; set; }
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
