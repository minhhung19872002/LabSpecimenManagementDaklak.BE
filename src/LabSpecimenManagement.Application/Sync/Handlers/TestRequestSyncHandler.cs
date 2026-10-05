using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LabSpecimenManagement.TestRequests;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.Sync.Handlers;

/// <summary>
/// Sync handler for TestRequest entity
/// </summary>
public class TestRequestSyncHandler : ISyncEntityHandler, ITransientDependency
{
    // Khớp với FE useTestRequestFormData: OFFLINE-YYYYMMDD-XXXX
    private const string OfflineContractNumberPrefix = "OFFLINE-";

    private readonly ITestRequestAppService _testRequestAppService;
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;

    public TestRequestSyncHandler(
        ITestRequestAppService testRequestAppService,
        IRepository<TestRequest, Guid> testRequestRepository)
    {
        _testRequestAppService = testRequestAppService;
        _testRequestRepository = testRequestRepository;
    }

    public SyncEntityType EntityType => SyncEntityType.TestRequest;

    public bool UsesClientIdAsServerId => true;

    public async Task<(Guid ServerId, object ServerData)> CreateAsync(object data)
    {
        var createDto = DeserializeData<CreateTestRequestDto>(data);
        await EnsureUniqueContractNumberAsync(createDto);
        var result = await _testRequestAppService.CreateAsync(createDto);
        return (result.Id, result);
    }

    /// <summary>
    /// Số HĐ của phiếu tạo offline chỉ là số tạm: dạng OFFLINE-... (tạo khi mất mạng) hoặc số đã lấy trước khi
    /// mất mạng nhưng trong lúc offline đã bị phiếu khác dùng → cấp số mới theo dãy tuần tự,
    /// đổi luôn tiền tố mã mẫu "&lt;số cũ&gt;/001" cho khớp số mới.
    /// </summary>
    private async Task EnsureUniqueContractNumberAsync(CreateTestRequestDto dto)
    {
        var oldNumber = dto.ContractNumber?.Trim();
        if (string.IsNullOrEmpty(oldNumber))
        {
            return;
        }

        var isTemporary = oldNumber.StartsWith(OfflineContractNumberPrefix, StringComparison.OrdinalIgnoreCase);
        if (!isTemporary && !await _testRequestAppService.CheckContractNumberExistsAsync(oldNumber))
        {
            return;
        }

        var newNumber = await _testRequestAppService.GenerateContractNumberAsync();
        dto.ContractNumber = newNumber;

        var oldPrefix = oldNumber + "/";
        foreach (var sample in dto.Samples ?? new())
        {
            if (sample.SampleCode != null && sample.SampleCode.StartsWith(oldPrefix, StringComparison.Ordinal))
            {
                sample.SampleCode = newNumber + "/" + sample.SampleCode[oldPrefix.Length..];
            }
        }
    }

    public async Task<object> UpdateAsync(Guid serverId, object data)
    {
        var createDto = DeserializeData<CreateTestRequestDto>(data);
        var updateDto = MapToUpdateDto(createDto);
        var result = await _testRequestAppService.UpdateAsync(serverId, updateDto);
        return result;
    }

    public async Task DeleteAsync(Guid serverId)
    {
        await _testRequestAppService.DeleteAsync(serverId);
    }

    public async Task<object?> GetAsync(Guid serverId)
    {
        try
        {
            return await _testRequestAppService.GetAsync(serverId);
        }
        catch
        {
            return null;
        }
    }

    public async Task<DateTime?> GetLastModificationTimeAsync(Guid serverId)
    {
        var entity = await _testRequestRepository.FindAsync(serverId);
        return entity?.LastModificationTime ?? entity?.CreationTime;
    }

    public async Task<bool> ExistsAsync(Guid serverId)
    {
        var entity = await _testRequestRepository.FindAsync(serverId);
        return entity != null;
    }

    private static T DeserializeData<T>(object data) => SyncDataSerializer.Deserialize<T>(data);

    private static UpdateTestRequestDto MapToUpdateDto(CreateTestRequestDto createDto)
    {
        return new UpdateTestRequestDto
        {
            ContractNumber = createDto.ContractNumber,
            ContractDate = createDto.ContractDate,
            SentToAccounting = createDto.SentToAccounting,
            PaymentReceived = createDto.PaymentReceived,
            SentToDepartment = createDto.SentToDepartment,
            ResultReturned = createDto.ResultReturned,
            CustomerName = createDto.CustomerName,
            ProvinceCode = createDto.ProvinceCode,
            Province = createDto.Province,
            DistrictCode = createDto.DistrictCode,
            District = createDto.District,
            WardCode = createDto.WardCode,
            Ward = createDto.Ward,
            Address = createDto.Address,
            TaxCode = createDto.TaxCode,
            ContactPerson = createDto.ContactPerson,
            Phone = createDto.Phone,
            Email = createDto.Email,
            TotalSampleCount = createDto.TotalSampleCount,
            MethodWestHighlandInstitute = createDto.MethodWestHighlandInstitute,
            MethodCustomer = createDto.MethodCustomer,
            MethodCustomerDetail = createDto.MethodCustomerDetail,
            ResultLanguage = createDto.ResultLanguage,
            ResultCopyCount = createDto.ResultCopyCount,
            SeparatePassedResults = createDto.SeparatePassedResults,
            ConformityDeclaration = createDto.ConformityDeclaration,
            OtherRequirements = createDto.OtherRequirements,
            SubContract = createDto.SubContract,
            SamplingRequirement = createDto.SamplingRequirement,
            DeliveryDirectPickup = createDto.DeliveryDirectPickup,
            DeliveryPostal = createDto.DeliveryPostal,
            ReturnStoredSample = createDto.ReturnStoredSample,
            SampleTypeService = createDto.SampleTypeService,
            SampleTypeContract = createDto.SampleTypeContract,
            SampleTypeTask = createDto.SampleTypeTask,
            SampleTypeSubContract = createDto.SampleTypeSubContract,
            SampleTypeSurveillance = createDto.SampleTypeSurveillance,
            ResultDeliveryDate = createDto.ResultDeliveryDate,
            SampleStorage = createDto.SampleStorage,
            TotalCost = createDto.TotalCost,
            TotalCostInWords = createDto.TotalCostInWords,
            ReceiptNumber = createDto.ReceiptNumber,
            SupervisorId = createDto.SupervisorId,
            DirectorId = createDto.DirectorId,
            ChangeRequest = createDto.ChangeRequest,
            AgreeTerms = createDto.AgreeTerms,
            RequestDate = createDto.RequestDate,
            Notes = createDto.Notes,
            ExpectedCompletionDate = createDto.ExpectedCompletionDate,
            Subcontractor = createDto.Subcontractor,
            Samples = createDto.Samples?.Select(s => new UpdateTestRequestSampleDto
            {
                Id = null,
                SortOrder = s.SortOrder,
                SampleCode = s.SampleCode,
                SampleName = s.SampleName,
                SamplingAddress = s.SamplingAddress,
                SampleQuantity = s.SampleQuantity,
                Status = s.Status,
                StatusText = s.StatusText,
                ProcessingStatus = s.ProcessingStatus,
                Notes = s.Notes,
                StorageLocation = s.StorageLocation,
                SpecimenTypeId = s.SpecimenTypeId,
                SpecimenTypeName = s.SpecimenTypeName,
                Indicators = s.Indicators?.Select(i => new UpdateTestRequestSampleIndicatorDto
                {
                    Id = null,
                    IndicatorId = i.IndicatorId,
                    TestMethodId = i.TestMethodId,
                    LaboratoryId = i.LaboratoryId,
                    UnitPrice = i.UnitPrice,
                    Notes = i.Notes,
                    TransferDate = i.TransferDate,
                    RequestDate = i.RequestDate,
                    SentToDepartment = i.SentToDepartment
                }).ToList() ?? new(),
                Attachments = s.Attachments
            }).ToList() ?? new()
        };
    }
}
