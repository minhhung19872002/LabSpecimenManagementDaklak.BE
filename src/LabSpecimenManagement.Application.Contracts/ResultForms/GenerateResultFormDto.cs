using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.ResultForms;

/// <summary>
/// Input DTO for generating result form
/// </summary>
public class GenerateResultFormInput
{
    /// <summary>
    /// TestRequest ID
    /// </summary>
    [Required]
    public Guid TestRequestId { get; set; }

    /// <summary>
    /// Sample ID
    /// </summary>
    [Required]
    public Guid SampleId { get; set; }
}

/// <summary>
/// Output DTO for generating result form
/// </summary>
public class GenerateResultFormOutput
{
    /// <summary>
    /// Generated file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Sample code for reference
    /// </summary>
    public string SampleCode { get; set; } = string.Empty;
}

/// <summary>
/// Input DTO for generating BM01 result form (single sample)
/// </summary>
public class GenerateBM01ResultFormInput
{
    /// <summary>
    /// TestRequest ID
    /// </summary>
    [Required]
    public Guid TestRequestId { get; set; }

    /// <summary>
    /// Sample ID
    /// </summary>
    [Required]
    public Guid SampleId { get; set; }

    /// <summary>
    /// Optional note to include in the result form (will be placed in {{Note}} placeholder)
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Template name (without .template.docx extension)
    /// If null, uses default template (BM01)
    /// </summary>
    public string? TemplateName { get; set; }
}

/// <summary>
/// Input DTO for generating test request form (BM05 - phiếu yêu cầu XN)
/// </summary>
public class GenerateTestRequestFormInput
{
    /// <summary>
    /// TestRequest ID
    /// </summary>
    [Required]
    public Guid TestRequestId { get; set; }
}

/// <summary>
/// Output DTO for generating test request form
/// </summary>
public class GenerateTestRequestFormOutput
{
    /// <summary>
    /// Generated file name
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Request number for reference
    /// </summary>
    public string RequestNumber { get; set; } = string.Empty;

    /// <summary>
    /// Contract number for reference
    /// </summary>
    public string ContractNumber { get; set; } = string.Empty;
}

/// <summary>
/// Input DTO for generating single indicator result form (In KQ Đơn)
/// </summary>
public class GenerateSingleIndicatorInput
{
    /// <summary>
    /// QualityManagementTask ID
    /// </summary>
    [Required]
    public Guid TaskId { get; set; }

    /// <summary>
    /// QualityManagementIndicator ID
    /// </summary>
    [Required]
    public Guid IndicatorId { get; set; }

    /// <summary>
    /// Optional note to include in the result form
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Template name (without .template.docx extension)
    /// If null, uses default template
    /// </summary>
    public string? TemplateName { get; set; }

    /// <summary>
    /// Optional Vice President user ID - to replace {{VicePresident}} placeholder in PDF
    /// </summary>
    public Guid? VicePresidentId { get; set; }
}

/// <summary>
/// Input DTO for generating mixed indicator result form (In KQ Hỗn hợp)
/// </summary>
public class GenerateMixedIndicatorInput
{
    /// <summary>
    /// QualityManagementTask ID
    /// </summary>
    [Required]
    public Guid TaskId { get; set; }

    /// <summary>
    /// List of QualityManagementIndicator IDs
    /// </summary>
    [Required]
    public List<Guid> IndicatorIds { get; set; } = new();

    /// <summary>
    /// Optional note to include in the result form
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Template name (without .template.docx extension)
    /// If null, uses default template
    /// </summary>
    public string? TemplateName { get; set; }

    /// <summary>
    /// Optional Vice President user ID - to replace {{VicePresident}} placeholder in PDF
    /// </summary>
    public Guid? VicePresidentId { get; set; }
}

/// <summary>
/// Input DTO for generating mixed samples result form (In KQ Hỗn hợp cho nhiều mẫu)
/// Used from TestRequestDetail page
/// </summary>
public class GenerateMixedSamplesResultFormInput
{
    /// <summary>
    /// TestRequest ID
    /// </summary>
    [Required]
    public Guid TestRequestId { get; set; }

    /// <summary>
    /// List of Sample IDs to include in the result form
    /// </summary>
    [Required]
    public List<Guid> SampleIds { get; set; } = new();

    /// <summary>
    /// Optional note to include in the result form
    /// </summary>
    public string? Note { get; set; }
}

/// <summary>
/// DTO for template information
/// </summary>
public class TemplateDto
{
    /// <summary>
    /// Template name (without .template.docx extension) - used as value
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Display name (same as Name, for UI display)
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Full file name with extension
    /// </summary>
    public string FileName { get; set; } = string.Empty;
}

/// <summary>
/// Input DTO for previewing test request form with form data (no DB save required)
/// Used when creating new test request or editing unsaved changes
/// </summary>
public class PreviewTestRequestFormInput
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

    public TestRequests.ResultLanguage ResultLanguage { get; set; } = TestRequests.ResultLanguage.Vietnamese;

    [StringLength(50)]
    public string? ResultCopyCount { get; set; }

    public bool SeparatePassedResults { get; set; }

    [StringLength(500)]
    public string? ConformityDeclaration { get; set; }

    [StringLength(1000)]
    public string? OtherRequirements { get; set; }

    [StringLength(256)]
    public string? SubContract { get; set; }

    public TestRequests.SamplingRequirement SamplingRequirement { get; set; } = TestRequests.SamplingRequirement.CustomerSampling;

    public bool DeliveryDirectPickup { get; set; }

    public bool DeliveryPostal { get; set; }

    public bool ReturnStoredSample { get; set; }

    public bool SampleTypeService { get; set; }

    public bool SampleTypeContract { get; set; }

    public bool SampleTypeTask { get; set; }

    public bool SampleTypeSubContract { get; set; }

    public DateTime? ResultDeliveryDate { get; set; }

    public TestRequests.SampleStorageOption SampleStorage { get; set; } = TestRequests.SampleStorageOption.No;

    public decimal TotalCost { get; set; }

    [StringLength(500)]
    public string? TotalCostInWords { get; set; }

    [StringLength(100)]
    public string? ReceiptNumber { get; set; }

    public bool AgreeTerms { get; set; }

    #endregion

    #region Legacy fields

    [Required]
    public DateTime RequestDate { get; set; } = DateTime.Now;

    [StringLength(2000)]
    public string? Notes { get; set; }

    #endregion

    #region Collections

    public List<PreviewTestRequestSampleDto> Samples { get; set; } = new();

    #endregion
}

/// <summary>
/// Sample DTO for preview (contains indicator names directly, no DB lookup needed)
/// </summary>
public class PreviewTestRequestSampleDto
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

    [StringLength(256)]
    public string? StatusText { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Indicator data for the sample (for preview only)
    /// </summary>
    public List<PreviewSampleIndicatorDto> Indicators { get; set; } = new();
}

/// <summary>
/// Indicator DTO for preview (contains name and price, no ID lookup needed)
/// </summary>
public class PreviewSampleIndicatorDto
{
    /// <summary>
    /// Indicator ID (for price lookup if needed)
    /// </summary>
    public Guid IndicatorId { get; set; }

    /// <summary>
    /// Indicator name (displayed in form)
    /// </summary>
    [StringLength(256)]
    public string IndicatorName { get; set; } = string.Empty;

    /// <summary>
    /// Indicator formatted name (HTML) - for rich text display
    /// </summary>
    [StringLength(1000)]
    public string? IndicatorFormattedName { get; set; }

    /// <summary>
    /// Unit price
    /// </summary>
    public decimal UnitPrice { get; set; }
}
