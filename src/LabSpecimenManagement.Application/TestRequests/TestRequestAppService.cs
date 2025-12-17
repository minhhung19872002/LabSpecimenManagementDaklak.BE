using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using LabSpecimenManagement.MasterData;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace LabSpecimenManagement.TestRequests;

[RemoteService(IsEnabled = false)]
public class TestRequestAppService :
    CrudAppService<
        TestRequest,
        TestRequestDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateTestRequestDto,
        UpdateTestRequestDto>,
    ITestRequestAppService
{
    private readonly IRepository<TestRequestSample, Guid> _sampleRepository;
    private readonly IRepository<TestRequestSampleIndicator, Guid> _sampleIndicatorRepository;
    private readonly IRepository<Indicator, Guid> _indicatorRepository;
    private readonly IIdentityUserRepository _userRepository;

    public TestRequestAppService(
        IRepository<TestRequest, Guid> repository,
        IRepository<TestRequestSample, Guid> sampleRepository,
        IRepository<TestRequestSampleIndicator, Guid> sampleIndicatorRepository,
        IRepository<Indicator, Guid> indicatorRepository,
        IIdentityUserRepository userRepository)
        : base(repository)
    {
        _sampleRepository = sampleRepository;
        _sampleIndicatorRepository = sampleIndicatorRepository;
        _indicatorRepository = indicatorRepository;
        _userRepository = userRepository;
    }

    public override async Task<TestRequestDto> GetAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);
        var dto = ObjectMapper.Map<TestRequest, TestRequestDto>(entity);

        // Load samples with indicators
        var samples = await _sampleRepository.GetListAsync(s => s.TestRequestId == id);
        dto.Samples = new List<TestRequestSampleDto>();

        decimal calculatedTotalCost = 0;

        foreach (var sample in samples.OrderBy(s => s.SortOrder))
        {
            var sampleDto = ObjectMapper.Map<TestRequestSample, TestRequestSampleDto>(sample);

            // Load indicators for this sample
            var indicators = await _sampleIndicatorRepository.GetListAsync(i => i.TestRequestSampleId == sample.Id);
            sampleDto.Indicators = new List<TestRequestSampleIndicatorDto>();

            decimal sampleIndicatorCost = 0;
            foreach (var indicator in indicators)
            {
                var indicatorEntity = await _indicatorRepository.GetAsync(indicator.IndicatorId);
                sampleDto.Indicators.Add(new TestRequestSampleIndicatorDto
                {
                    Id = indicator.Id,
                    TestRequestSampleId = indicator.TestRequestSampleId,
                    IndicatorId = indicator.IndicatorId,
                    IndicatorCode = indicatorEntity.Code,
                    IndicatorName = indicatorEntity.Name,
                    UnitPrice = indicator.UnitPrice,
                    Notes = indicator.Notes,
                    TransferDate = indicator.TransferDate,
                    RequestDate = indicator.RequestDate,
                    SentToDepartment = indicator.SentToDepartment
                });
                sampleIndicatorCost += indicator.UnitPrice;
            }

            // Calculate sample total cost from indicators
            sampleDto.TotalIndicatorCost = sampleIndicatorCost;
            calculatedTotalCost += sampleIndicatorCost;

            dto.Samples.Add(sampleDto);
        }

        // Set total cost from calculated value
        dto.TotalCost = calculatedTotalCost;

        // Load user names
        if (dto.SupervisorId.HasValue)
        {
            var supervisor = await _userRepository.FindAsync(dto.SupervisorId.Value);
            dto.SupervisorName = supervisor?.Name ?? supervisor?.UserName;
        }

        if (dto.DirectorId.HasValue)
        {
            var director = await _userRepository.FindAsync(dto.DirectorId.Value);
            dto.DirectorName = director?.Name ?? director?.UserName;
        }

        return dto;
    }

    public async Task<PagedResultDto<TestRequestDto>> GetFilteredListAsync(GetTestRequestListDto input)
    {
        var queryable = await Repository.GetQueryableAsync();

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var search = input.Search.ToLower();
            queryable = queryable.Where(x =>
                x.RequestNumber.ToLower().Contains(search) ||
                x.CustomerName.ToLower().Contains(search) ||
                (x.ContractNumber != null && x.ContractNumber.ToLower().Contains(search)) ||
                (x.Phone != null && x.Phone.ToLower().Contains(search)));
        }

        // Apply status filter
        if (input.Status.HasValue)
        {
            queryable = queryable.Where(x => x.Status == input.Status.Value);
        }

        // Apply date range filter
        if (input.FromDate.HasValue)
        {
            queryable = queryable.Where(x => x.RequestDate >= input.FromDate.Value);
        }

        if (input.ToDate.HasValue)
        {
            queryable = queryable.Where(x => x.RequestDate <= input.ToDate.Value);
        }

        // Apply customer name filter
        if (!string.IsNullOrWhiteSpace(input.CustomerName))
        {
            var customerName = input.CustomerName.ToLower();
            queryable = queryable.Where(x => x.CustomerName.ToLower().Contains(customerName));
        }

        // Apply expected completion date range filter
        if (input.ExpectedCompletionDateFrom.HasValue)
        {
            queryable = queryable.Where(x => x.ExpectedCompletionDate.HasValue && x.ExpectedCompletionDate.Value >= input.ExpectedCompletionDateFrom.Value);
        }

        if (input.ExpectedCompletionDateTo.HasValue)
        {
            queryable = queryable.Where(x => x.ExpectedCompletionDate.HasValue && x.ExpectedCompletionDate.Value <= input.ExpectedCompletionDateTo.Value);
        }

        // Get total count
        var totalCount = await AsyncExecuter.CountAsync(queryable);

        // Apply sorting
        if (!string.IsNullOrWhiteSpace(input.Sorting))
        {
            queryable = queryable.OrderBy(input.Sorting);
        }
        else
        {
            queryable = queryable.OrderByDescending(x => x.RequestDate);
        }

        // Apply paging
        queryable = queryable
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount);

        var entities = await AsyncExecuter.ToListAsync(queryable);
        var dtos = ObjectMapper.Map<List<TestRequest>, List<TestRequestDto>>(entities);

        // Calculate total cost for each test request from samples and indicators
        foreach (var dto in dtos)
        {
            var samples = await _sampleRepository.GetListAsync(s => s.TestRequestId == dto.Id);
            decimal totalCost = 0;

            foreach (var sample in samples)
            {
                var indicators = await _sampleIndicatorRepository.GetListAsync(i => i.TestRequestSampleId == sample.Id);
                totalCost += indicators.Sum(i => i.UnitPrice);
            }

            dto.TotalCost = totalCost;
        }

        return new PagedResultDto<TestRequestDto>(totalCount, dtos);
    }

    protected override async Task<TestRequest> MapToEntityAsync(CreateTestRequestDto createInput)
    {
        // Generate request number automatically
        var requestNumber = await GenerateRequestNumberAsync();

        var entity = new TestRequest(
            GuidGenerator.Create(),
            requestNumber,
            createInput.RequestDate,
            createInput.CustomerName
        );

        // Contract info
        entity.ContractNumber = createInput.ContractNumber;
        entity.ContractDate = createInput.ContractDate;
        entity.SentToAccounting = createInput.SentToAccounting;
        entity.PaymentReceived = createInput.PaymentReceived;
        entity.SentToDepartment = createInput.SentToDepartment;
        entity.ResultReturned = createInput.ResultReturned;
        entity.Province = createInput.Province;
        entity.District = createInput.District;
        entity.Ward = createInput.Ward;
        entity.Address = createInput.Address;
        entity.TaxCode = createInput.TaxCode;
        entity.ContactPerson = createInput.ContactPerson;
        entity.Phone = createInput.Phone;

        // Method info
        entity.TotalSampleCount = createInput.TotalSampleCount;
        entity.MethodWestHighlandInstitute = createInput.MethodWestHighlandInstitute;
        entity.MethodCustomer = createInput.MethodCustomer;
        entity.MethodCustomerDetail = createInput.MethodCustomerDetail;
        entity.ResultLanguage = createInput.ResultLanguage;
        entity.ResultCopyCount = createInput.ResultCopyCount;
        entity.SeparatePassedResults = createInput.SeparatePassedResults;
        entity.ConformityDeclaration = createInput.ConformityDeclaration;
        entity.OtherRequirements = createInput.OtherRequirements;
        entity.SubContract = createInput.SubContract;
        entity.SamplingRequirement = createInput.SamplingRequirement;
        entity.DeliveryDirectPickup = createInput.DeliveryDirectPickup;
        entity.DeliveryPostal = createInput.DeliveryPostal;
        entity.ReturnStoredSample = createInput.ReturnStoredSample;
        entity.SampleTypeService = createInput.SampleTypeService;
        entity.SampleTypeContract = createInput.SampleTypeContract;
        entity.SampleTypeTask = createInput.SampleTypeTask;
        entity.SampleTypeSubContract = createInput.SampleTypeSubContract;
        entity.ResultDeliveryDate = createInput.ResultDeliveryDate;
        entity.SampleStorage = createInput.SampleStorage;
        entity.TotalCost = createInput.TotalCost;
        entity.TotalCostInWords = createInput.TotalCostInWords;
        entity.ReceiptNumber = createInput.ReceiptNumber;
        entity.SupervisorId = createInput.SupervisorId;
        entity.DirectorId = createInput.DirectorId;

        // Legacy fields
        entity.Notes = createInput.Notes;
        entity.ExpectedCompletionDate = createInput.ExpectedCompletionDate;
        entity.Subcontractor = createInput.Subcontractor;

        return entity;
    }

    public override async Task<TestRequestDto> CreateAsync(CreateTestRequestDto input)
    {
        var entity = await MapToEntityAsync(input);
        await Repository.InsertAsync(entity, autoSave: true);

        // Create samples
        if (input.Samples != null && input.Samples.Any())
        {
            decimal totalCost = 0;
            foreach (var sampleDto in input.Samples)
            {
                var sample = new TestRequestSample(
                    GuidGenerator.Create(),
                    entity.Id,
                    sampleDto.SampleCode,
                    sampleDto.SampleName
                );
                sample.SortOrder = sampleDto.SortOrder;
                sample.SamplingAddress = sampleDto.SamplingAddress;
                sample.SampleQuantity = sampleDto.SampleQuantity;
                sample.Status = sampleDto.Status;
                sample.StatusText = sampleDto.StatusText;
                sample.ProcessingStatus = sampleDto.ProcessingStatus;
                sample.Notes = sampleDto.Notes;
                sample.StorageLocation = sampleDto.StorageLocation;

                await _sampleRepository.InsertAsync(sample, autoSave: true);

                // Create sample indicators
                decimal sampleIndicatorCost = 0;
                if (sampleDto.Indicators != null && sampleDto.Indicators.Any())
                {
                    foreach (var indicatorDto in sampleDto.Indicators)
                    {
                        var sampleIndicator = new TestRequestSampleIndicator(
                            GuidGenerator.Create(),
                            sample.Id,
                            indicatorDto.IndicatorId,
                            indicatorDto.UnitPrice
                        );
                        sampleIndicator.Notes = indicatorDto.Notes;
                        sampleIndicator.TransferDate = indicatorDto.TransferDate;
                        sampleIndicator.RequestDate = indicatorDto.RequestDate;
                        sampleIndicator.SentToDepartment = indicatorDto.SentToDepartment;
                        await _sampleIndicatorRepository.InsertAsync(sampleIndicator, autoSave: true);
                        sampleIndicatorCost += indicatorDto.UnitPrice;
                    }
                }

                // Update sample total cost
                sample.TotalIndicatorCost = sampleIndicatorCost;
                await _sampleRepository.UpdateAsync(sample, autoSave: true);
                totalCost += sampleIndicatorCost;
            }

            // Update entity total cost if not set
            if (entity.TotalCost == 0)
            {
                entity.TotalCost = totalCost;
                await Repository.UpdateAsync(entity, autoSave: true);
            }
        }

        return await GetAsync(entity.Id);
    }

    public override async Task<TestRequestDto> UpdateAsync(Guid id, UpdateTestRequestDto input)
    {
        var entity = await Repository.GetAsync(id);

        // Update contract info
        entity.ContractNumber = input.ContractNumber;
        entity.ContractDate = input.ContractDate;
        entity.SentToAccounting = input.SentToAccounting;
        entity.PaymentReceived = input.PaymentReceived;
        entity.SentToDepartment = input.SentToDepartment;
        entity.ResultReturned = input.ResultReturned;
        entity.CustomerName = input.CustomerName;
        entity.ProvinceCode = input.ProvinceCode;
        entity.Province = input.Province;
        entity.DistrictCode = input.DistrictCode;
        entity.District = input.District;
        entity.WardCode = input.WardCode;
        entity.Ward = input.Ward;
        entity.Address = input.Address;
        entity.TaxCode = input.TaxCode;
        entity.ContactPerson = input.ContactPerson;
        entity.Phone = input.Phone;

        // Method info
        entity.TotalSampleCount = input.TotalSampleCount;
        entity.MethodWestHighlandInstitute = input.MethodWestHighlandInstitute;
        entity.MethodCustomer = input.MethodCustomer;
        entity.MethodCustomerDetail = input.MethodCustomerDetail;
        entity.ResultLanguage = input.ResultLanguage;
        entity.ResultCopyCount = input.ResultCopyCount;
        entity.SeparatePassedResults = input.SeparatePassedResults;
        entity.ConformityDeclaration = input.ConformityDeclaration;
        entity.OtherRequirements = input.OtherRequirements;
        entity.SubContract = input.SubContract;
        entity.SamplingRequirement = input.SamplingRequirement;
        entity.DeliveryDirectPickup = input.DeliveryDirectPickup;
        entity.DeliveryPostal = input.DeliveryPostal;
        entity.ReturnStoredSample = input.ReturnStoredSample;
        entity.SampleTypeService = input.SampleTypeService;
        entity.SampleTypeContract = input.SampleTypeContract;
        entity.SampleTypeTask = input.SampleTypeTask;
        entity.SampleTypeSubContract = input.SampleTypeSubContract;
        entity.ResultDeliveryDate = input.ResultDeliveryDate;
        entity.SampleStorage = input.SampleStorage;
        entity.TotalCost = input.TotalCost;
        entity.TotalCostInWords = input.TotalCostInWords;
        entity.ReceiptNumber = input.ReceiptNumber;
        entity.SupervisorId = input.SupervisorId;
        entity.DirectorId = input.DirectorId;
        entity.ChangeRequest = input.ChangeRequest;
        entity.AgreeTerms = input.AgreeTerms;

        // Legacy fields
        entity.RequestDate = input.RequestDate;
        entity.Notes = input.Notes;
        entity.Status = input.Status;
        entity.ExpectedCompletionDate = input.ExpectedCompletionDate;
        entity.Subcontractor = input.Subcontractor;

        await Repository.UpdateAsync(entity, autoSave: true);

        // Update samples
        if (input.Samples != null)
        {
            // Get existing samples
            var existingSamples = await _sampleRepository.GetListAsync(s => s.TestRequestId == id);
            var inputSampleIds = input.Samples.Where(s => s.Id.HasValue).Select(s => s.Id!.Value).ToList();

            // Delete samples not in input
            foreach (var existingSample in existingSamples)
            {
                if (!inputSampleIds.Contains(existingSample.Id))
                {
                    // Delete indicators first
                    var indicators = await _sampleIndicatorRepository.GetListAsync(i => i.TestRequestSampleId == existingSample.Id);
                    foreach (var indicator in indicators)
                    {
                        await _sampleIndicatorRepository.DeleteAsync(indicator);
                    }
                    await _sampleRepository.DeleteAsync(existingSample);
                }
            }

            decimal totalCost = 0;

            // Update or create samples
            foreach (var sampleDto in input.Samples)
            {
                TestRequestSample sample;
                if (sampleDto.Id.HasValue)
                {
                    // Update existing sample
                    sample = await _sampleRepository.GetAsync(sampleDto.Id.Value);
                    sample.SortOrder = sampleDto.SortOrder;
                    sample.SampleCode = sampleDto.SampleCode;
                    sample.SampleName = sampleDto.SampleName;
                    sample.SamplingAddress = sampleDto.SamplingAddress;
                    sample.SampleQuantity = sampleDto.SampleQuantity;
                    sample.Status = sampleDto.Status;
                    sample.StatusText = sampleDto.StatusText;
                    sample.ProcessingStatus = sampleDto.ProcessingStatus;
                    sample.Notes = sampleDto.Notes;
                    sample.StorageLocation = sampleDto.StorageLocation;
                }
                else
                {
                    // Create new sample
                    sample = new TestRequestSample(
                        GuidGenerator.Create(),
                        id,
                        sampleDto.SampleCode,
                        sampleDto.SampleName
                    );
                    sample.SortOrder = sampleDto.SortOrder;
                    sample.SamplingAddress = sampleDto.SamplingAddress;
                    sample.SampleQuantity = sampleDto.SampleQuantity;
                    sample.Status = sampleDto.Status;
                    sample.StatusText = sampleDto.StatusText;
                    sample.ProcessingStatus = sampleDto.ProcessingStatus;
                    sample.Notes = sampleDto.Notes;
                    sample.StorageLocation = sampleDto.StorageLocation;
                    await _sampleRepository.InsertAsync(sample, autoSave: true);
                }

                // Update indicators
                var existingIndicators = await _sampleIndicatorRepository.GetListAsync(i => i.TestRequestSampleId == sample.Id);
                var inputIndicatorIds = sampleDto.Indicators?.Where(i => i.Id.HasValue).Select(i => i.Id!.Value).ToList() ?? new List<Guid>();

                // Delete indicators not in input
                foreach (var existingIndicator in existingIndicators)
                {
                    if (!inputIndicatorIds.Contains(existingIndicator.Id))
                    {
                        await _sampleIndicatorRepository.DeleteAsync(existingIndicator);
                    }
                }

                decimal sampleIndicatorCost = 0;
                if (sampleDto.Indicators != null)
                {
                    foreach (var indicatorDto in sampleDto.Indicators)
                    {
                        if (indicatorDto.Id.HasValue)
                        {
                            // Update existing indicator
                            var indicator = await _sampleIndicatorRepository.GetAsync(indicatorDto.Id.Value);
                            indicator.UnitPrice = indicatorDto.UnitPrice;
                            indicator.Notes = indicatorDto.Notes;
                            indicator.TransferDate = indicatorDto.TransferDate;
                            indicator.RequestDate = indicatorDto.RequestDate;
                            indicator.SentToDepartment = indicatorDto.SentToDepartment;
                            await _sampleIndicatorRepository.UpdateAsync(indicator, autoSave: true);
                        }
                        else
                        {
                            // Create new indicator
                            var indicator = new TestRequestSampleIndicator(
                                GuidGenerator.Create(),
                                sample.Id,
                                indicatorDto.IndicatorId,
                                indicatorDto.UnitPrice
                            );
                            indicator.Notes = indicatorDto.Notes;
                            indicator.TransferDate = indicatorDto.TransferDate;
                            indicator.RequestDate = indicatorDto.RequestDate;
                            indicator.SentToDepartment = indicatorDto.SentToDepartment;
                            await _sampleIndicatorRepository.InsertAsync(indicator, autoSave: true);
                        }
                        sampleIndicatorCost += indicatorDto.UnitPrice;
                    }
                }

                sample.TotalIndicatorCost = sampleIndicatorCost;
                await _sampleRepository.UpdateAsync(sample, autoSave: true);
                totalCost += sampleIndicatorCost;
            }

            // Update total cost if changed
            if (entity.TotalCost != totalCost)
            {
                entity.TotalCost = totalCost;
                await Repository.UpdateAsync(entity, autoSave: true);
            }
        }

        return await GetAsync(id);
    }

    private async Task<string> GenerateRequestNumberAsync()
    {
        // Generate request number format: REQ-YYYYMMDD-XXXX
        var today = DateTime.Now;
        var prefix = $"REQ-{today:yyyyMMdd}";

        var count = await Repository.CountAsync(x => x.RequestNumber.StartsWith(prefix));
        var sequence = (count + 1).ToString("D4");

        return $"{prefix}-{sequence}";
    }
}
