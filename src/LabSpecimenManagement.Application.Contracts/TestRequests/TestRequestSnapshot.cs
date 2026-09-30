using System;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// Snapshot dữ liệu phiếu yêu cầu để rollback khi từ chối thay đổi
/// </summary>
public class TestRequestSnapshot
{
    // === Thông tin hợp đồng ===
    public string? ContractNumber { get; set; }
    public DateTime? ContractDate { get; set; }
    public bool SentToAccounting { get; set; }
    public bool PaymentReceived { get; set; }
    public bool SentToDepartment { get; set; }
    public bool ResultReturned { get; set; }

    // === Thông tin khách hàng ===
    public string CustomerName { get; set; } = string.Empty;
    public string? Province { get; set; }
    public string? District { get; set; }
    public string? Ward { get; set; }
    public string? Address { get; set; }
    public string? TaxCode { get; set; }
    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    // === Loại hình mẫu ===
    public bool SampleTypeService { get; set; }
    public bool SampleTypeContract { get; set; }
    public bool SampleTypeTask { get; set; }
    public bool SampleTypeSubContract { get; set; }

    // === Yêu cầu về phương pháp ===
    public bool MethodWestHighlandInstitute { get; set; }
    public bool MethodCustomer { get; set; }
    public string? MethodCustomerDetail { get; set; }
    public ResultLanguage ResultLanguage { get; set; }
    public string? ResultCopyCount { get; set; }
    public bool SeparatePassedResults { get; set; }
    public string? ConformityDeclaration { get; set; }
    public string? OtherRequirements { get; set; }
    public string? SubContract { get; set; }

    // === Thông tin chi phí ===
    public decimal TotalCost { get; set; }
    public string? TotalCostInWords { get; set; }
    public string? ReceiptNumber { get; set; }

    // === Người phụ trách ===
    public Guid? SupervisorId { get; set; }
    public Guid? DirectorId { get; set; }

    // === Yêu cầu về lấy mẫu & giao trả ===
    public SamplingRequirement SamplingRequirement { get; set; }
    public bool DeliveryDirectPickup { get; set; }
    public bool DeliveryPostal { get; set; }
    public bool ReturnStoredSample { get; set; }
    public SampleStorageOption SampleStorage { get; set; }
    public DateTime? ResultDeliveryDate { get; set; }

    // === Thông tin khác ===
    public string? Notes { get; set; }
    public DateTime? ExpectedCompletionDate { get; set; }
    public string? ChangeRequest { get; set; }
    public bool AgreeTerms { get; set; }

    public static TestRequestSnapshot FromEntity(dynamic entity)
    {
        return new TestRequestSnapshot
        {
            // Thông tin hợp đồng
            ContractNumber = entity.ContractNumber,
            ContractDate = entity.ContractDate,
            SentToAccounting = entity.SentToAccounting,
            PaymentReceived = entity.PaymentReceived,
            SentToDepartment = entity.SentToDepartment,
            ResultReturned = entity.ResultReturned,

            // Thông tin khách hàng
            CustomerName = entity.CustomerName,
            Province = entity.Province,
            District = entity.District,
            Ward = entity.Ward,
            Address = entity.Address,
            TaxCode = entity.TaxCode,
            ContactPerson = entity.ContactPerson,
            Phone = entity.Phone,
            Email = entity.Email,

            // Loại hình mẫu
            SampleTypeService = entity.SampleTypeService,
            SampleTypeContract = entity.SampleTypeContract,
            SampleTypeTask = entity.SampleTypeTask,
            SampleTypeSubContract = entity.SampleTypeSubContract,

            // Yêu cầu về phương pháp
            MethodWestHighlandInstitute = entity.MethodWestHighlandInstitute,
            MethodCustomer = entity.MethodCustomer,
            MethodCustomerDetail = entity.MethodCustomerDetail,
            ResultLanguage = entity.ResultLanguage,
            ResultCopyCount = entity.ResultCopyCount,
            SeparatePassedResults = entity.SeparatePassedResults,
            ConformityDeclaration = entity.ConformityDeclaration,
            OtherRequirements = entity.OtherRequirements,
            SubContract = entity.SubContract,

            // Thông tin chi phí
            TotalCost = entity.TotalCost,
            TotalCostInWords = entity.TotalCostInWords,
            ReceiptNumber = entity.ReceiptNumber,

            // Người phụ trách
            SupervisorId = entity.SupervisorId,
            DirectorId = entity.DirectorId,

            // Yêu cầu về lấy mẫu & giao trả
            SamplingRequirement = entity.SamplingRequirement,
            DeliveryDirectPickup = entity.DeliveryDirectPickup,
            DeliveryPostal = entity.DeliveryPostal,
            ReturnStoredSample = entity.ReturnStoredSample,
            SampleStorage = entity.SampleStorage,
            ResultDeliveryDate = entity.ResultDeliveryDate,

            // Thông tin khác
            Notes = entity.Notes,
            ExpectedCompletionDate = entity.ExpectedCompletionDate,
            ChangeRequest = entity.ChangeRequest,
            AgreeTerms = entity.AgreeTerms
        };
    }
}
