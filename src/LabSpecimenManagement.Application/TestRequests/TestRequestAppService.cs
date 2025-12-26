using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text.Json;
using System.Threading.Tasks;
using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Notifications;
using LabSpecimenManagement.QualityManagement;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Users;

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
    private readonly IRepository<Organization, Guid> _organizationRepository;
    private readonly IRepository<Laboratory, Guid> _laboratoryRepository;
    private readonly IRepository<TestMethod, Guid> _testMethodRepository;
    private readonly IIdentityUserRepository _userRepository;
    private readonly IRepository<LaboratoryTask, Guid> _laboratoryTaskRepository;
    private readonly IRepository<LaboratoryTaskIndicator, Guid> _laboratoryTaskIndicatorRepository;
    private readonly IRepository<TestRequestChangeHistory, Guid> _changeHistoryRepository;
    private readonly IRepository<QualityManagementTask, Guid> _qualityTaskRepository;
    private readonly INotificationAppService _notificationAppService;
    private readonly ICurrentUser _currentUser;

    public TestRequestAppService(
        IRepository<TestRequest, Guid> repository,
        IRepository<TestRequestSample, Guid> sampleRepository,
        IRepository<TestRequestSampleIndicator, Guid> sampleIndicatorRepository,
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<Organization, Guid> organizationRepository,
        IRepository<Laboratory, Guid> laboratoryRepository,
        IRepository<TestMethod, Guid> testMethodRepository,
        IIdentityUserRepository userRepository,
        IRepository<LaboratoryTask, Guid> laboratoryTaskRepository,
        IRepository<LaboratoryTaskIndicator, Guid> laboratoryTaskIndicatorRepository,
        IRepository<TestRequestChangeHistory, Guid> changeHistoryRepository,
        IRepository<QualityManagementTask, Guid> qualityTaskRepository,
        INotificationAppService notificationAppService,
        ICurrentUser currentUser)
        : base(repository)
    {
        _sampleRepository = sampleRepository;
        _sampleIndicatorRepository = sampleIndicatorRepository;
        _indicatorRepository = indicatorRepository;
        _organizationRepository = organizationRepository;
        _laboratoryRepository = laboratoryRepository;
        _testMethodRepository = testMethodRepository;
        _userRepository = userRepository;
        _laboratoryTaskRepository = laboratoryTaskRepository;
        _laboratoryTaskIndicatorRepository = laboratoryTaskIndicatorRepository;
        _changeHistoryRepository = changeHistoryRepository;
        _qualityTaskRepository = qualityTaskRepository;
        _notificationAppService = notificationAppService;
        _currentUser = currentUser;
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

                // Get laboratory name if assigned
                string? laboratoryName = null;
                if (indicator.LaboratoryId.HasValue)
                {
                    var laboratory = await _laboratoryRepository.FindAsync(indicator.LaboratoryId.Value);
                    laboratoryName = laboratory?.Name;
                }

                // Get test method info if assigned
                string? testMethodCode = null;
                string? testMethodName = null;
                if (indicator.TestMethodId.HasValue)
                {
                    var testMethod = await _testMethodRepository.FindAsync(indicator.TestMethodId.Value);
                    testMethodCode = testMethod?.Code;
                    testMethodName = testMethod?.Name;
                }

                // Get approval status from LaboratoryTaskIndicator if exists
                IndicatorApprovalStatus? approvalStatus = null;
                var labTaskIndicator = await _laboratoryTaskIndicatorRepository.FirstOrDefaultAsync(
                    lti => lti.TestRequestSampleIndicatorId == indicator.Id
                );
                if (labTaskIndicator != null)
                {
                    approvalStatus = labTaskIndicator.ApprovalStatus;
                }

                sampleDto.Indicators.Add(new TestRequestSampleIndicatorDto
                {
                    Id = indicator.Id,
                    TestRequestSampleId = indicator.TestRequestSampleId,
                    IndicatorId = indicator.IndicatorId,
                    IndicatorCode = indicatorEntity.Code,
                    IndicatorName = indicatorEntity.Name,
                    TestMethodId = indicator.TestMethodId,
                    TestMethodCode = testMethodCode,
                    TestMethodName = testMethodName,
                    UnitPrice = indicator.UnitPrice,
                    Notes = indicator.Notes,
                    LaboratoryId = indicator.LaboratoryId,
                    LaboratoryName = laboratoryName,
                    TransferDate = indicator.TransferDate,
                    RequestDate = indicator.RequestDate,
                    SentToDepartment = indicator.SentToDepartment,
                    ApprovalStatus = approvalStatus
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

        // Apply sorting - default sort by CreationTime descending (newest first)
        if (!string.IsNullOrWhiteSpace(input.Sorting))
        {
            queryable = queryable.OrderBy(input.Sorting);
        }
        else
        {
            queryable = queryable.OrderByDescending(x => x.CreationTime);
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
        entity.ProvinceCode = createInput.ProvinceCode;
        entity.Province = createInput.Province;
        entity.DistrictCode = createInput.DistrictCode;
        entity.District = createInput.District;
        entity.WardCode = createInput.WardCode;
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
        entity.ChangeRequest = createInput.ChangeRequest;
        entity.AgreeTerms = createInput.AgreeTerms;

        // Legacy fields
        entity.Notes = createInput.Notes;
        entity.ExpectedCompletionDate = createInput.ExpectedCompletionDate;
        entity.Subcontractor = createInput.Subcontractor;

        return entity;
    }

    public override async Task<TestRequestDto> CreateAsync(CreateTestRequestDto input)
    {
        // Auto-create Organization if customerName doesn't exist
        await EnsureOrganizationExistsAsync(input.CustomerName);

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
                            indicatorDto.IndicatorId
                        );
                        sampleIndicator.UnitPrice = indicatorDto.UnitPrice;
                        sampleIndicator.Notes = indicatorDto.Notes;
                        sampleIndicator.LaboratoryId = indicatorDto.LaboratoryId;
                        sampleIndicator.TestMethodId = indicatorDto.TestMethodId;
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

        // LaboratoryTasks are now created when user clicks "Gửi mẫu" (SendSampleAsync)
        // instead of when creating the test request

        return await GetAsync(entity.Id);
    }

    public override async Task<TestRequestDto> UpdateAsync(Guid id, UpdateTestRequestDto input)
    {
        var entity = await Repository.GetAsync(id);
        var previousStatus = entity.Status;
        var requiresApproval = CanEditLimitedInfo(entity.Status); // SampleSent or InProgress

        // Validate edit permission based on status
        if (!CanEditFullInfo(entity.Status) && !CanEditLimitedInfo(entity.Status))
        {
            throw new UserFriendlyException($"Không thể chỉnh sửa phiếu yêu cầu khi trạng thái là '{GetStatusLabel(entity.Status)}'");
        }

        // Validate limited update - check locked fields
        if (!CanEditFullInfo(entity.Status))
        {
            ValidateLimitedUpdate(entity, input);
        }

        // If requires approval (SampleSent/InProgress), only save change history and change status
        // DO NOT apply changes to entity - changes will be applied when approved
        if (requiresApproval)
        {
            var changeHistoryId = await SaveChangeHistoryAsync(entity, input, previousStatus);
            if (changeHistoryId.HasValue)
            {
                // Only change status to PendingChangeApproval, don't update entity data
                entity.Status = TestRequestStatus.PendingChangeApproval;
                await Repository.UpdateAsync(entity, autoSave: true);

                // Create QualityManagementTask for ChangeApproval
                await CreateQualityManagementTaskForChangeApprovalAsync(entity, changeHistoryId.Value);
            }
            return await GetAsync(id);
        }

        // === Below code only runs when NOT requiring approval (New/Pending status) ===

        // Auto-create Organization if customerName doesn't exist
        await EnsureOrganizationExistsAsync(input.CustomerName);

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
        entity.ExpectedCompletionDate = input.ExpectedCompletionDate;
        entity.Subcontractor = input.Subcontractor;

        // Note: When not requiring approval (New/Pending status), keep current status
        // Status transitions are handled by dedicated methods like PrintTestRequestAsync, SendSampleAsync, etc.

        await Repository.UpdateAsync(entity, autoSave: true);

        // Update samples
        if (input.Samples != null)
        {
            // Get existing samples
            var existingSamples = await _sampleRepository.GetListAsync(s => s.TestRequestId == id);
            var inputSampleIds = input.Samples.Where(s => s.Id.HasValue).Select(s => s.Id!.Value).ToList();

            // Handle samples not in input - delete or mark as CustomerCancelled
            foreach (var existingSample in existingSamples)
            {
                if (!inputSampleIds.Contains(existingSample.Id))
                {
                    var indicators = await _sampleIndicatorRepository.GetListAsync(i => i.TestRequestSampleId == existingSample.Id);
                    bool hasLabTask = false;

                    foreach (var indicator in indicators)
                    {
                        // Check if indicator has LaboratoryTaskIndicator (shouldn't happen for New/Pending status, but check for safety)
                        var labTaskIndicator = await _laboratoryTaskIndicatorRepository.FirstOrDefaultAsync(
                            lti => lti.TestRequestSampleIndicatorId == indicator.Id
                        );

                        if (labTaskIndicator != null)
                        {
                            hasLabTask = true;
                            // Mark as CustomerCancelled
                            labTaskIndicator.ApprovalStatus = IndicatorApprovalStatus.CustomerCancelled;
                            await _laboratoryTaskIndicatorRepository.UpdateAsync(labTaskIndicator, autoSave: true);
                        }
                    }

                    // Only delete sample if no LaboratoryTask exists
                    if (!hasLabTask)
                    {
                        foreach (var indicator in indicators)
                        {
                            await _sampleIndicatorRepository.DeleteAsync(indicator);
                        }
                        await _sampleRepository.DeleteAsync(existingSample);
                    }
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

                // Handle indicators not in input - cancel or throw error based on testing status
                foreach (var existingIndicator in existingIndicators)
                {
                    if (!inputIndicatorIds.Contains(existingIndicator.Id))
                    {
                        // Check if indicator is being tested in laboratory
                        var labTaskIndicator = await _laboratoryTaskIndicatorRepository.FirstOrDefaultAsync(
                            lti => lti.TestRequestSampleIndicatorId == existingIndicator.Id
                        );

                        if (labTaskIndicator != null)
                        {
                            // Check if indicator is being tested or already has results
                            // Cannot delete if: Entered (has result), Approved (already approved), Rejected (already reviewed)
                            if (labTaskIndicator.ApprovalStatus == IndicatorApprovalStatus.Entered ||
                                labTaskIndicator.ApprovalStatus == IndicatorApprovalStatus.Approved ||
                                labTaskIndicator.ApprovalStatus == IndicatorApprovalStatus.Rejected)
                            {
                                throw new UserFriendlyException($"Không thể xóa chỉ tiêu đang được thử nghiệm hoặc đã có kết quả.");
                            }

                            // If status is Pending (not yet tested), mark as CustomerCancelled
                            labTaskIndicator.ApprovalStatus = IndicatorApprovalStatus.CustomerCancelled;
                            await _laboratoryTaskIndicatorRepository.UpdateAsync(labTaskIndicator, autoSave: true);
                        }

                        // Don't delete from TestRequestSampleIndicator, keep for history
                        // Only delete if no lab task exists (sample not yet sent)
                        if (labTaskIndicator == null)
                        {
                            await _sampleIndicatorRepository.DeleteAsync(existingIndicator);
                        }
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
                            indicator.LaboratoryId = indicatorDto.LaboratoryId;
                            indicator.TestMethodId = indicatorDto.TestMethodId;
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
                                indicatorDto.IndicatorId
                            );
                            indicator.UnitPrice = indicatorDto.UnitPrice;
                            indicator.Notes = indicatorDto.Notes;
                            indicator.LaboratoryId = indicatorDto.LaboratoryId;
                            indicator.TestMethodId = indicatorDto.TestMethodId;
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

    /// <summary>
    /// Check if Organization exists by name, create if not exists
    /// </summary>
    private async Task EnsureOrganizationExistsAsync(string customerName)
    {
        if (string.IsNullOrWhiteSpace(customerName))
        {
            return;
        }

        var normalizedName = customerName.Trim();
        var exists = await _organizationRepository.AnyAsync(o => o.Name.ToLower() == normalizedName.ToLower());

        if (!exists)
        {
            var organization = new Organization(GuidGenerator.Create(), normalizedName);
            await _organizationRepository.InsertAsync(organization, autoSave: true);
        }
    }

    /// <summary>
    /// Auto-create LaboratoryTask records grouped by Laboratory when TestRequest is created
    /// </summary>
    private async Task CreateLaboratoryTasksAsync(TestRequest testRequest)
    {
        // Get all samples for this TestRequest
        var samples = await _sampleRepository.GetListAsync(s => s.TestRequestId == testRequest.Id);

        if (!samples.Any())
        {
            return;
        }

        // Group indicators by LaboratoryId
        var indicatorsByLab = new Dictionary<Guid, List<(Guid SampleIndicatorId, Guid SampleId, DateTime? TransferDate, DateTime? RequestDate, Guid? TestMethodId)>>();

        foreach (var sample in samples)
        {
            var indicators = await _sampleIndicatorRepository.GetListAsync(
                i => i.TestRequestSampleId == sample.Id && i.LaboratoryId.HasValue);

            foreach (var indicator in indicators)
            {
                var labId = indicator.LaboratoryId!.Value;
                if (!indicatorsByLab.ContainsKey(labId))
                {
                    indicatorsByLab[labId] = new List<(Guid, Guid, DateTime?, DateTime?, Guid?)>();
                }

                indicatorsByLab[labId].Add((indicator.Id, sample.Id, indicator.TransferDate, indicator.RequestDate, indicator.TestMethodId));
            }
        }

        // Create LaboratoryTask for each Laboratory
        foreach (var (labId, indicatorList) in indicatorsByLab)
        {
            var laboratory = await _laboratoryRepository.GetAsync(labId);
            var sampleCount = indicatorList.Select(i => i.SampleId).Distinct().Count();

            // Get TransferDate and RequestDate from first indicator (they should be the same for the group)
            var firstIndicator = indicatorList.First();

            var task = new LaboratoryTask(
                GuidGenerator.Create(),
                testRequest.Id,
                labId,
                testRequest.ContractNumber,
                sampleCount,
                laboratory.TechnicalManagerId,
                firstIndicator.TransferDate,
                firstIndicator.RequestDate
            );

            await _laboratoryTaskRepository.InsertAsync(task, autoSave: true);

            // Create junction records linking LaboratoryTask to TestRequestSampleIndicator
            foreach (var indicator in indicatorList)
            {
                var taskIndicator = new LaboratoryTaskIndicator(
                    GuidGenerator.Create(),
                    task.Id,
                    indicator.SampleIndicatorId
                );
                // Copy TestMethodId and Unit from TestRequestSampleIndicator if available
                if (indicator.TestMethodId.HasValue)
                {
                    taskIndicator.MethodId = indicator.TestMethodId;
                    var testMethod = await _testMethodRepository.FindAsync(indicator.TestMethodId.Value);
                    if (testMethod != null)
                    {
                        taskIndicator.Unit = testMethod.Unit;
                    }
                }
                await _laboratoryTaskIndicatorRepository.InsertAsync(taskIndicator, autoSave: true);
            }
        }
    }

    #region Status Validation Helpers

    /// <summary>
    /// Check if full edit is allowed (all fields)
    /// </summary>
    private bool CanEditFullInfo(TestRequestStatus status)
    {
        return status == TestRequestStatus.New || status == TestRequestStatus.Pending;
    }

    /// <summary>
    /// Check if limited edit is allowed (some fields locked)
    /// For SampleSent and InProgress: limited edit with QLCL approval required
    /// </summary>
    private bool CanEditLimitedInfo(TestRequestStatus status)
    {
        return status == TestRequestStatus.SampleSent || status == TestRequestStatus.InProgress;
    }

    /// <summary>
    /// Check if delete is allowed
    /// </summary>
    private bool CanDelete(TestRequestStatus status)
    {
        return status == TestRequestStatus.New || status == TestRequestStatus.Pending;
    }

    /// <summary>
    /// Get status label for error messages
    /// </summary>
    private string GetStatusLabel(TestRequestStatus status)
    {
        return status switch
        {
            TestRequestStatus.New => "Mới",
            TestRequestStatus.Pending => "Chờ xử lý",
            TestRequestStatus.SampleSent => "Đã gửi mẫu",
            TestRequestStatus.InProgress => "Đang thử nghiệm",
            TestRequestStatus.PendingChangeApproval => "Chờ phê duyệt thay đổi",
            TestRequestStatus.PendingApproval => "Chờ phê duyệt",
            TestRequestStatus.Approved => "Đã phê duyệt",
            TestRequestStatus.ResultSent => "Gửi khách hàng",
            TestRequestStatus.Completed => "Hoàn thành",
            _ => status.ToString()
        };
    }

    /// <summary>
    /// Validate that locked fields are not changed when in limited edit mode
    /// </summary>
    private void ValidateLimitedUpdate(TestRequest entity, UpdateTestRequestDto input)
    {
        var errors = new List<string>();

        // Check locked fields
        if (!StringEquals(entity.ContractNumber, input.ContractNumber))
            errors.Add("Số hợp đồng");
        if (!DateEquals(entity.ContractDate, input.ContractDate))
            errors.Add("Ngày hợp đồng");
        if (!StringEquals(entity.CustomerName, input.CustomerName))
            errors.Add("Tên khách hàng");
        if (!StringEquals(entity.Address, input.Address))
            errors.Add("Địa chỉ");
        if (!StringEquals(entity.TaxCode, input.TaxCode))
            errors.Add("Mã số thuế");
        if (!StringEquals(entity.ContactPerson, input.ContactPerson))
            errors.Add("Người liên hệ");
        if (!StringEquals(entity.Phone, input.Phone))
            errors.Add("Số điện thoại");
        if (!StringEquals(entity.ProvinceCode, input.ProvinceCode))
            errors.Add("Tỉnh/Thành phố");
        if (!StringEquals(entity.DistrictCode, input.DistrictCode))
            errors.Add("Quận/Huyện");
        if (!StringEquals(entity.WardCode, input.WardCode))
            errors.Add("Phường/Xã");

        if (errors.Any())
        {
            throw new UserFriendlyException(
                $"Không thể thay đổi các trường sau khi đang thử nghiệm: {string.Join(", ", errors)}"
            );
        }
    }

    /// <summary>
    /// Compare two nullable strings safely (null == "" == null)
    /// </summary>
    private static bool StringEquals(string? a, string? b)
    {
        return string.Equals(a ?? "", b ?? "", StringComparison.Ordinal);
    }

    /// <summary>
    /// Compare two nullable DateTimes by date only (ignoring time and timezone)
    /// </summary>
    private static bool DateEquals(DateTime? a, DateTime? b)
    {
        if (a == null && b == null) return true;
        if (a == null || b == null) return false;
        // Compare dates only (year, month, day)
        return a.Value.Date == b.Value.Date;
    }

    #endregion

    #region Status Transition Methods

    /// <summary>
    /// When printing test request form - update status from New to Pending
    /// </summary>
    public async Task PrintTestRequestAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);

        if (entity.Status == TestRequestStatus.New)
        {
            entity.Status = TestRequestStatus.Pending;
            await Repository.UpdateAsync(entity);
        }
        // If already Pending, don't change status (allow re-print)
    }

    /// <summary>
    /// When user confirms sample sent - update status from Pending to SampleSent
    /// Also creates LaboratoryTasks for all samples grouped by Laboratory
    /// </summary>
    public async Task SendSampleAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);

        if (entity.Status != TestRequestStatus.Pending)
        {
            throw new UserFriendlyException("Chỉ có thể gửi mẫu khi trạng thái là 'Chờ xử lý'");
        }

        entity.Status = TestRequestStatus.SampleSent;
        await Repository.UpdateAsync(entity);

        // Create LaboratoryTasks grouped by Laboratory when sample is sent
        await CreateLaboratoryTasksAsync(entity);
    }

    /// <summary>
    /// When QLKT assigns method + technician - update status to InProgress
    /// Called from LaboratoryTaskAppService.AssignSampleIndicatorsAsync
    /// Now transitions from SampleSent (instead of Pending) to InProgress
    /// </summary>
    public async Task UpdateToInProgressAsync(Guid testRequestId)
    {
        var entity = await Repository.GetAsync(testRequestId);

        // In new flow: SampleSent → InProgress when QLKT assigns PP + KTV
        if (entity.Status == TestRequestStatus.SampleSent)
        {
            entity.Status = TestRequestStatus.InProgress;
            await Repository.UpdateAsync(entity);
        }
    }

    /// <summary>
    /// When printing test result form - update status to PendingApproval
    /// In new flow, this is triggered directly from InProgress status
    /// </summary>
    public async Task UpdateToPendingApprovalAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);

        if (entity.Status == TestRequestStatus.InProgress)
        {
            entity.Status = TestRequestStatus.PendingApproval;
            await Repository.UpdateAsync(entity);
        }
    }

    /// <summary>
    /// When leader approves - update status to Approved
    /// </summary>
    public async Task ApproveAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);

        if (entity.Status != TestRequestStatus.PendingApproval)
        {
            throw new UserFriendlyException("Chỉ có thể phê duyệt khi trạng thái là 'Chờ Phê duyệt'");
        }

        entity.Status = TestRequestStatus.Approved;
        await Repository.UpdateAsync(entity);
    }

    /// <summary>
    /// QLCL từ chối kết quả: PendingApproval → InProgress
    /// Trả lại cho QLKT xem xét
    /// </summary>
    public async Task RejectResultAsync(Guid id, string reason)
    {
        var entity = await Repository.GetAsync(id);

        if (entity.Status != TestRequestStatus.PendingApproval)
        {
            throw new UserFriendlyException("Chỉ có thể từ chối kết quả khi trạng thái là 'Chờ Phê duyệt'");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new UserFriendlyException("Vui lòng nhập lý do từ chối");
        }

        // Update TestRequest status back to InProgress for QLKT to review
        entity.Status = TestRequestStatus.InProgress;
        entity.ResultRejectionReason = reason;
        await Repository.UpdateAsync(entity);

        // Also update all LaboratoryTask status to ResultRejected
        var laboratoryTasks = await _laboratoryTaskRepository.GetListAsync(t => t.TestRequestId == id);
        foreach (var task in laboratoryTasks)
        {
            // Only update tasks that have sent results (ResultSent status)
            if (task.Status == LaboratoryTaskStatus.ResultSent)
            {
                task.Status = LaboratoryTaskStatus.ResultRejected;
                task.ResultRejectionReason = reason;
                await _laboratoryTaskRepository.UpdateAsync(task);
            }
        }
    }

    /// <summary>
    /// When sending result to customer - update status to ResultSent
    /// </summary>
    public async Task SendResultAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);

        if (entity.Status != TestRequestStatus.Approved)
        {
            throw new UserFriendlyException("Chỉ có thể gửi kết quả khi đã được phê duyệt");
        }

        entity.Status = TestRequestStatus.ResultSent;
        await Repository.UpdateAsync(entity);

        // TODO: Send email to customer if requested
    }

    /// <summary>
    /// Complete the test request - update status to Completed
    /// </summary>
    public async Task CompleteAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);

        if (entity.Status != TestRequestStatus.ResultSent)
        {
            throw new UserFriendlyException("Chỉ có thể hoàn thành khi đã gửi kết quả");
        }

        entity.Status = TestRequestStatus.Completed;
        await Repository.UpdateAsync(entity);
    }

    #endregion

    #region Change Approval Flow

    /// <summary>
    /// QLCL phê duyệt thay đổi: Apply changes from newValue and restore previous status
    /// </summary>
    public async Task ApproveChangesAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);

        if (entity.Status != TestRequestStatus.PendingChangeApproval)
        {
            throw new UserFriendlyException("Chỉ có thể phê duyệt thay đổi khi trạng thái là 'Chờ phê duyệt thay đổi'");
        }

        // Get pending change history
        var pendingChanges = await _changeHistoryRepository.GetListAsync(
            c => c.TestRequestId == id && c.ApprovalStatus == ChangeApprovalStatus.Pending);

        if (!pendingChanges.Any())
        {
            throw new UserFriendlyException("Không tìm thấy thay đổi nào đang chờ phê duyệt");
        }

        var currentUserId = _currentUser.Id ?? throw new UserFriendlyException("Người dùng chưa đăng nhập");
        var previousStatus = pendingChanges.First().PreviousStatus ?? TestRequestStatus.InProgress;

        // Apply changes from newValue in change history (also restores previous status)
        await ApplyChangesFromHistoryAsync(entity, pendingChanges, previousStatus);

        // Update all pending changes to approved
        foreach (var change in pendingChanges)
        {
            change.ApprovalStatus = ChangeApprovalStatus.Approved;
            change.ApprovedById = currentUserId;
            change.ApprovalDate = DateTime.UtcNow;
        }
        await _changeHistoryRepository.UpdateManyAsync(pendingChanges);

        // Create notification for the person who made the change
        var creatorId = pendingChanges.First().CreatorId;
        if (creatorId.HasValue)
        {
            await _notificationAppService.CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = creatorId.Value,
                Title = "Thay đổi đã được phê duyệt",
                Message = $"Thay đổi trên phiếu yêu cầu {entity.RequestNumber} đã được phê duyệt",
                Type = NotificationType.ChangeApproved,
                ReferenceId = id.ToString(),
                ReferenceType = "TestRequest"
            });
        }
    }

    /// <summary>
    /// Apply changes from change history newValue to entity
    /// </summary>
    private async Task ApplyChangesFromHistoryAsync(TestRequest entity, List<TestRequestChangeHistory> changes, TestRequestStatus previousStatus)
    {
        foreach (var change in changes)
        {
            if (string.IsNullOrEmpty(change.FieldName)) continue;

            // Skip snapshot (no longer used)
            if (change.FieldName == "Snapshot") continue;

            // Handle Samples separately
            if (change.FieldName == "Samples")
            {
                await ApplySampleChangesAsync(entity.Id, change.NewValue, previousStatus);
                continue;
            }

            // Apply simple field changes
            ApplyFieldChange(entity, change.FieldName, change.NewValue);
        }

        // Restore previous status
        entity.Status = previousStatus;

        await Repository.UpdateAsync(entity, autoSave: true);
    }

    /// <summary>
    /// Apply a single field change to entity
    /// </summary>
    private void ApplyFieldChange(TestRequest entity, string fieldName, string? newValue)
    {
        // Map field name (case insensitive) to entity property
        var lowerField = fieldName.ToLower();

        switch (lowerField)
        {
            case "contractnumber":
                entity.ContractNumber = newValue;
                break;
            case "contractdate":
                entity.ContractDate = string.IsNullOrEmpty(newValue) ? null : DateTime.Parse(newValue);
                break;
            case "customername":
                entity.CustomerName = newValue ?? "";
                break;
            case "address":
                entity.Address = newValue;
                break;
            case "taxcode":
                entity.TaxCode = newValue;
                break;
            case "contactperson":
                entity.ContactPerson = newValue;
                break;
            case "phone":
                entity.Phone = newValue;
                break;
            case "provincecode":
                entity.ProvinceCode = newValue;
                break;
            case "province":
                entity.Province = newValue;
                break;
            case "districtcode":
                entity.DistrictCode = newValue;
                break;
            case "district":
                entity.District = newValue;
                break;
            case "wardcode":
                entity.WardCode = newValue;
                break;
            case "ward":
                entity.Ward = newValue;
                break;
            case "notes":
                entity.Notes = newValue;
                break;
            case "expectedcompletiondate":
                entity.ExpectedCompletionDate = string.IsNullOrEmpty(newValue) ? null : DateTime.Parse(newValue);
                break;
            case "resultdeliverydate":
                entity.ResultDeliveryDate = string.IsNullOrEmpty(newValue) ? null : DateTime.Parse(newValue);
                break;
            case "totalcost":
                entity.TotalCost = string.IsNullOrEmpty(newValue) ? 0 : decimal.Parse(newValue);
                break;
            case "totalcostinwords":
                entity.TotalCostInWords = newValue;
                break;
            case "methodwesthighlandinstitute":
                entity.MethodWestHighlandInstitute = newValue?.ToLower() == "true";
                break;
            case "methodcustomer":
                entity.MethodCustomer = newValue?.ToLower() == "true";
                break;
            case "methodcustomerdetail":
                entity.MethodCustomerDetail = newValue;
                break;
            case "resultlanguage":
                entity.ResultLanguage = string.IsNullOrEmpty(newValue) ? ResultLanguage.Vietnamese : (ResultLanguage)int.Parse(newValue);
                break;
            case "resultcopycount":
                entity.ResultCopyCount = newValue;
                break;
            case "separatepassedresults":
                entity.SeparatePassedResults = newValue?.ToLower() == "true";
                break;
            case "conformitydeclaration":
                entity.ConformityDeclaration = newValue;
                break;
            case "otherrequirements":
                entity.OtherRequirements = newValue;
                break;
            case "subcontract":
                entity.SubContract = newValue;
                break;
            case "samplingrequirement":
                entity.SamplingRequirement = string.IsNullOrEmpty(newValue) ? SamplingRequirement.CustomerSampling : (SamplingRequirement)int.Parse(newValue);
                break;
            case "deliverydirectpickup":
                entity.DeliveryDirectPickup = newValue?.ToLower() == "true";
                break;
            case "deliverypostal":
                entity.DeliveryPostal = newValue?.ToLower() == "true";
                break;
            case "returnstoredsample":
                entity.ReturnStoredSample = newValue?.ToLower() == "true";
                break;
            case "sampletypeservice":
                entity.SampleTypeService = newValue?.ToLower() == "true";
                break;
            case "sampletypecontract":
                entity.SampleTypeContract = newValue?.ToLower() == "true";
                break;
            case "sampletypetask":
                entity.SampleTypeTask = newValue?.ToLower() == "true";
                break;
            case "sampletypesubcontract":
                entity.SampleTypeSubContract = newValue?.ToLower() == "true";
                break;
            case "samplestorage":
                entity.SampleStorage = string.IsNullOrEmpty(newValue) ? SampleStorageOption.No : (SampleStorageOption)int.Parse(newValue);
                break;
            case "receiptnumber":
                entity.ReceiptNumber = newValue;
                break;
            case "senttoaccounting":
                entity.SentToAccounting = newValue?.ToLower() == "true";
                break;
            case "paymentreceived":
                entity.PaymentReceived = newValue?.ToLower() == "true";
                break;
            case "senttodepartment":
                entity.SentToDepartment = newValue?.ToLower() == "true";
                break;
            case "resultreturned":
                entity.ResultReturned = newValue?.ToLower() == "true";
                break;
            case "agreeterms":
                entity.AgreeTerms = newValue?.ToLower() == "true";
                break;
            case "changerequest":
                entity.ChangeRequest = newValue;
                break;
            case "supervisorid":
                entity.SupervisorId = string.IsNullOrEmpty(newValue) ? null : Guid.Parse(newValue);
                break;
            case "directorid":
                entity.DirectorId = string.IsNullOrEmpty(newValue) ? null : Guid.Parse(newValue);
                break;
            case "subcontractor":
                entity.Subcontractor = newValue;
                break;
        }
    }

    /// <summary>
    /// Apply sample changes from JSON newValue
    /// </summary>
    private async Task ApplySampleChangesAsync(Guid testRequestId, string? newSamplesJson, TestRequestStatus previousStatus = TestRequestStatus.New)
    {
        if (string.IsNullOrEmpty(newSamplesJson)) return;

        var newSamples = JsonSerializer.Deserialize<List<SampleChangeDto>>(newSamplesJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (newSamples == null) return;

        // Get existing samples
        var existingSamples = await _sampleRepository.GetListAsync(s => s.TestRequestId == testRequestId);
        var newSampleIds = newSamples.Where(s => s.Id != null).Select(s => s.Id!.Value).ToList();

        // Handle samples not in new list - mark as CustomerCancelled instead of deleting
        foreach (var existingSample in existingSamples)
        {
            if (!newSampleIds.Contains(existingSample.Id))
            {
                // Get all indicators for this sample
                var indicators = await _sampleIndicatorRepository.GetListAsync(i => i.TestRequestSampleId == existingSample.Id);
                bool hasLabTask = false;

                foreach (var indicator in indicators)
                {
                    // Check if indicator has LaboratoryTaskIndicator
                    var labTaskIndicator = await _laboratoryTaskIndicatorRepository.FirstOrDefaultAsync(
                        lti => lti.TestRequestSampleIndicatorId == indicator.Id
                    );

                    if (labTaskIndicator != null)
                    {
                        hasLabTask = true;
                        // Check if indicator is being tested or already has results
                        if (labTaskIndicator.ApprovalStatus == IndicatorApprovalStatus.Entered ||
                            labTaskIndicator.ApprovalStatus == IndicatorApprovalStatus.Approved ||
                            labTaskIndicator.ApprovalStatus == IndicatorApprovalStatus.Rejected)
                        {
                            throw new UserFriendlyException($"Không thể xóa mẫu có chỉ tiêu đang được thử nghiệm hoặc đã có kết quả.");
                        }
                        // Mark as CustomerCancelled
                        labTaskIndicator.ApprovalStatus = IndicatorApprovalStatus.CustomerCancelled;
                        await _laboratoryTaskIndicatorRepository.UpdateAsync(labTaskIndicator, autoSave: true);
                    }
                }

                // If sample has LaboratoryTask, keep sample for history (don't delete)
                // Only delete sample if no LaboratoryTask exists (sample not yet sent)
                if (!hasLabTask)
                {
                    foreach (var indicator in indicators)
                    {
                        await _sampleIndicatorRepository.DeleteAsync(indicator);
                    }
                    await _sampleRepository.DeleteAsync(existingSample);
                }
                // Note: If hasLabTask, sample is kept but all its indicators are marked as CustomerCancelled
            }
        }

        decimal totalCost = 0;
        int sortOrder = 0;

        // Collect new indicators for batch LaboratoryTask creation (grouped by laboratory)
        var newIndicatorsForTask = new List<TestRequestSampleIndicator>();

        // Update or create samples
        foreach (var sampleDto in newSamples)
        {
            sortOrder++;
            TestRequestSample sample;

            if (sampleDto.Id.HasValue && existingSamples.Any(s => s.Id == sampleDto.Id.Value))
            {
                // Update existing sample
                sample = await _sampleRepository.GetAsync(sampleDto.Id.Value);
                sample.SortOrder = sortOrder;
                sample.SampleCode = sampleDto.SampleCode ?? sample.SampleCode;
                sample.SampleName = sampleDto.SampleName ?? sample.SampleName;
                sample.SamplingAddress = sampleDto.SamplingAddress;
                sample.SampleQuantity = sampleDto.SampleQuantity;
                sample.Notes = sampleDto.Notes;
                sample.StorageLocation = sampleDto.StorageLocation;
            }
            else
            {
                // Create new sample
                sample = new TestRequestSample(
                    GuidGenerator.Create(),
                    testRequestId,
                    sampleDto.SampleCode ?? "",
                    sampleDto.SampleName ?? ""
                );
                sample.SortOrder = sortOrder;
                sample.SamplingAddress = sampleDto.SamplingAddress;
                sample.SampleQuantity = sampleDto.SampleQuantity;
                sample.Notes = sampleDto.Notes;
                sample.StorageLocation = sampleDto.StorageLocation;
                await _sampleRepository.InsertAsync(sample, autoSave: true);
            }

            // Update indicators
            var existingIndicators = await _sampleIndicatorRepository.GetListAsync(i => i.TestRequestSampleId == sample.Id);
            var newIndicatorIds = sampleDto.Indicators?.Where(i => i.Id != null).Select(i => i.Id!.Value).ToList() ?? new List<Guid>();

            // Delete indicators not in new list
            foreach (var existingIndicator in existingIndicators)
            {
                if (!newIndicatorIds.Contains(existingIndicator.Id))
                {
                    var labTaskIndicator = await _laboratoryTaskIndicatorRepository.FirstOrDefaultAsync(
                        lti => lti.TestRequestSampleIndicatorId == existingIndicator.Id
                    );

                    if (labTaskIndicator != null)
                    {
                        if (labTaskIndicator.ApprovalStatus == IndicatorApprovalStatus.Entered ||
                            labTaskIndicator.ApprovalStatus == IndicatorApprovalStatus.Approved ||
                            labTaskIndicator.ApprovalStatus == IndicatorApprovalStatus.Rejected)
                        {
                            throw new UserFriendlyException($"Không thể xóa chỉ tiêu đang được thử nghiệm hoặc đã có kết quả.");
                        }
                        labTaskIndicator.ApprovalStatus = IndicatorApprovalStatus.CustomerCancelled;
                        await _laboratoryTaskIndicatorRepository.UpdateAsync(labTaskIndicator, autoSave: true);
                    }
                    else
                    {
                        await _sampleIndicatorRepository.DeleteAsync(existingIndicator);
                    }
                }
            }

            decimal sampleIndicatorCost = 0;
            if (sampleDto.Indicators != null)
            {
                foreach (var indicatorDto in sampleDto.Indicators)
                {
                    if (indicatorDto.Id.HasValue && existingIndicators.Any(i => i.Id == indicatorDto.Id.Value))
                    {
                        // Update existing indicator
                        var indicator = await _sampleIndicatorRepository.GetAsync(indicatorDto.Id.Value);
                        indicator.UnitPrice = indicatorDto.UnitPrice;
                        indicator.Notes = indicatorDto.Notes;
                        indicator.LaboratoryId = indicatorDto.LaboratoryId;
                        indicator.TestMethodId = indicatorDto.TestMethodId;
                        await _sampleIndicatorRepository.UpdateAsync(indicator, autoSave: true);
                    }
                    else
                    {
                        // Create new indicator
                        var indicator = new TestRequestSampleIndicator(
                            GuidGenerator.Create(),
                            sample.Id,
                            indicatorDto.IndicatorId
                        );
                        indicator.UnitPrice = indicatorDto.UnitPrice;
                        indicator.Notes = indicatorDto.Notes;
                        indicator.LaboratoryId = indicatorDto.LaboratoryId;
                        indicator.TestMethodId = indicatorDto.TestMethodId;
                        await _sampleIndicatorRepository.InsertAsync(indicator, autoSave: true);

                        // Collect new indicators for batch task creation
                        if (previousStatus == TestRequestStatus.InProgress && indicatorDto.LaboratoryId.HasValue)
                        {
                            newIndicatorsForTask.Add(indicator);
                        }
                    }
                    sampleIndicatorCost += indicatorDto.UnitPrice;
                }
            }

            sample.TotalIndicatorCost = sampleIndicatorCost;
            await _sampleRepository.UpdateAsync(sample, autoSave: true);
            totalCost += sampleIndicatorCost;
        }

        // Create LaboratoryTasks for new indicators - grouped by laboratory
        if (newIndicatorsForTask.Any())
        {
            await CreateLaboratoryTasksForNewIndicatorsAsync(testRequestId, newIndicatorsForTask);
        }

        // Update entity total cost
        var testRequest = await Repository.GetAsync(testRequestId);
        testRequest.TotalCost = totalCost;
        testRequest.TotalSampleCount = newSamples.Count;
        await Repository.UpdateAsync(testRequest, autoSave: true);
    }

    /// <summary>
    /// Create LaboratoryTasks for new indicators when TestRequest is InProgress
    /// Groups indicators by laboratory - one task per laboratory (like initial SendSample)
    /// </summary>
    private async Task CreateLaboratoryTasksForNewIndicatorsAsync(
        Guid testRequestId,
        List<TestRequestSampleIndicator> newIndicators)
    {
        if (!newIndicators.Any()) return;

        var testRequest = await Repository.GetAsync(testRequestId);

        // Group indicators by LaboratoryId
        var indicatorsByLab = newIndicators
            .Where(i => i.LaboratoryId.HasValue)
            .GroupBy(i => i.LaboratoryId!.Value)
            .ToList();

        foreach (var labGroup in indicatorsByLab)
        {
            var laboratoryId = labGroup.Key;
            var indicators = labGroup.ToList();
            var laboratory = await _laboratoryRepository.GetAsync(laboratoryId);

            // Get sample count for this laboratory's indicators
            var sampleIds = indicators.Select(i => i.TestRequestSampleId).Distinct().ToList();

            // Use first indicator's dates for the task
            var firstIndicator = indicators.First();

            // Create one LaboratoryTask for all indicators in this laboratory
            var task = new LaboratoryTask(
                GuidGenerator.Create(),
                testRequestId,
                laboratoryId,
                testRequest.ContractNumber,
                sampleIds.Count,
                laboratory.TechnicalManagerId,
                firstIndicator.TransferDate,
                firstIndicator.RequestDate
            );

            // Task từ QLCL phê duyệt cũng cần QLKT gán KTV, nên status = NotAssignedTechnician (mặc định)

            await _laboratoryTaskRepository.InsertAsync(task, autoSave: true);

            // Create LaboratoryTaskIndicator for each indicator in this group
            foreach (var indicator in indicators)
            {
                var taskIndicator = new LaboratoryTaskIndicator(
                    GuidGenerator.Create(),
                    task.Id,
                    indicator.Id
                );

                // Copy TestMethodId and Unit if available
                if (indicator.TestMethodId.HasValue)
                {
                    taskIndicator.MethodId = indicator.TestMethodId;
                    var testMethod = await _testMethodRepository.FindAsync(indicator.TestMethodId.Value);
                    if (testMethod != null)
                    {
                        taskIndicator.Unit = testMethod.Unit;
                    }
                }

                await _laboratoryTaskIndicatorRepository.InsertAsync(taskIndicator, autoSave: true);
            }
        }
    }

    /// <summary>
    /// QLCL từ chối thay đổi: No rollback needed (entity unchanged), just restore previous status
    /// </summary>
    public async Task RejectChangesAsync(Guid id, string reason)
    {
        var entity = await Repository.GetAsync(id);

        if (entity.Status != TestRequestStatus.PendingChangeApproval)
        {
            throw new UserFriendlyException("Chỉ có thể từ chối thay đổi khi trạng thái là 'Chờ phê duyệt thay đổi'");
        }

        // Get pending change history
        var pendingChanges = await _changeHistoryRepository.GetListAsync(
            c => c.TestRequestId == id && c.ApprovalStatus == ChangeApprovalStatus.Pending);

        if (!pendingChanges.Any())
        {
            throw new UserFriendlyException("Không tìm thấy thay đổi nào đang chờ phê duyệt");
        }

        var currentUserId = _currentUser.Id ?? throw new UserFriendlyException("Người dùng chưa đăng nhập");
        var previousStatus = pendingChanges.First().PreviousStatus ?? TestRequestStatus.InProgress;

        // No rollback needed - entity was never changed, only status was changed to PendingChangeApproval
        // The changes are stored in change history's newValue, not applied to entity

        // Update all pending changes to rejected
        foreach (var change in pendingChanges)
        {
            change.ApprovalStatus = ChangeApprovalStatus.Rejected;
            change.ApprovedById = currentUserId;
            change.ApprovalDate = DateTime.UtcNow;
            change.RejectionReason = reason;
        }
        await _changeHistoryRepository.UpdateManyAsync(pendingChanges);

        // Restore previous status (entity data is already unchanged)
        entity.Status = previousStatus;
        await Repository.UpdateAsync(entity);

        // Create notification for the person who made the change
        var creatorId = pendingChanges.First().CreatorId;
        if (creatorId.HasValue)
        {
            await _notificationAppService.CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = creatorId.Value,
                Title = "Thay đổi bị từ chối",
                Message = $"Thay đổi trên phiếu yêu cầu {entity.RequestNumber} bị từ chối. Lý do: {reason}",
                Type = NotificationType.ChangeRejected,
                ReferenceId = id.ToString(),
                ReferenceType = "TestRequest"
            });
        }
    }

    /// <summary>
    /// Save change history for approval flow
    /// Called when editing TestRequest in SampleSent or InProgress status
    ///
    /// NEW: Sử dụng ChangeHistory từ frontend thay vì tự tính toán
    /// Frontend so sánh originalData (từ API response khi load form) với newData (khi submit)
    /// và gửi danh sách các thay đổi lên backend
    /// Returns the first change history ID if there are changes, null otherwise
    /// </summary>
    private async Task<Guid?> SaveChangeHistoryAsync(TestRequest entity, UpdateTestRequestDto input, TestRequestStatus previousStatus)
    {
        var changes = new List<TestRequestChangeHistory>();

        // NEW: Sử dụng ChangeHistory từ frontend nếu có
        if (input.ChangeHistory != null && input.ChangeHistory.Any())
        {
            foreach (var change in input.ChangeHistory)
            {
                changes.Add(new TestRequestChangeHistory(
                    GuidGenerator.Create(),
                    entity.Id,
                    change.FieldName,
                    change.OldValue,
                    change.NewValue,
                    previousStatus
                ));
            }
        }

        // If there are no changes, return null
        if (!changes.Any())
        {
            return null;
        }

        // No need to save snapshot anymore since we don't apply changes immediately
        // The original data is preserved in the entity itself
        // When approved, we apply newValue from change history
        // When rejected, we just delete change history (entity unchanged)

        await _changeHistoryRepository.InsertManyAsync(changes);

        // Send notification to QLCL users
        // Get all users with QLCL role
        var qlclUsers = await _userRepository.GetListAsync();
        var adminUsers = qlclUsers.Where(u => u.UserName == "admin").ToList();

        foreach (var user in adminUsers)
        {
            await _notificationAppService.CreateNotificationAsync(new CreateNotificationDto
            {
                UserId = user.Id,
                Title = "Yêu cầu phê duyệt thay đổi",
                Message = $"Phiếu yêu cầu {entity.RequestNumber} có thay đổi cần phê duyệt",
                Type = NotificationType.ChangeApprovalRequest,
                ReferenceId = entity.Id.ToString(),
                ReferenceType = "TestRequest"
            });
        }

        // Return the first change history ID for linking to QualityManagementTask
        return changes.First().Id;
    }

    /// <summary>
    /// Tạo QualityManagementTask cho việc phê duyệt thay đổi phiếu yêu cầu
    /// </summary>
    private async Task CreateQualityManagementTaskForChangeApprovalAsync(TestRequest testRequest, Guid changeHistoryId)
    {
        // Get sample and indicator counts
        var samples = await _sampleRepository.GetListAsync(s => s.TestRequestId == testRequest.Id);
        var sampleIds = samples.Select(s => s.Id).ToList();
        var indicators = await _sampleIndicatorRepository.GetListAsync(i => sampleIds.Contains(i.TestRequestSampleId));

        // Get transfer date and farthest RequestCompletionDate from LaboratoryTasks
        var labTasks = await _laboratoryTaskRepository.GetListAsync(lt => lt.TestRequestId == testRequest.Id);

        DateTime? transferDate = labTasks
            .Where(lt => lt.TransferDate.HasValue)
            .Select(lt => lt.TransferDate)
            .OrderBy(d => d)
            .FirstOrDefault();

        DateTime? requestCompletionDate = labTasks
            .Where(lt => lt.RequestCompletionDate.HasValue)
            .Select(lt => lt.RequestCompletionDate)
            .OrderByDescending(d => d)
            .FirstOrDefault();

        // Check if QualityManagementTask (ChangeApproval) already exists for this TestRequest
        var existingTask = await _qualityTaskRepository.FirstOrDefaultAsync(
            t => t.TestRequestId == testRequest.Id && t.TaskType == QualityTaskType.ChangeApproval);

        if (existingTask != null)
        {
            // Update existing task
            existingTask.TotalSampleCount = samples.Count;
            existingTask.TotalIndicatorCount = indicators.Count;
            existingTask.TransferDate = transferDate;
            existingTask.RequestCompletionDate = requestCompletionDate;
            existingTask.ChangeHistoryId = changeHistoryId;
            existingTask.Status = QualityManagementStatus.PendingApproval; // Reset to pending
            await _qualityTaskRepository.UpdateAsync(existingTask);
        }
        else
        {
            // Create new QualityManagementTask
            var task = new QualityManagementTask(
                GuidGenerator.Create(),
                testRequest.Id,
                testRequest.ContractNumber ?? "",
                QualityTaskType.ChangeApproval,
                samples.Count,
                indicators.Count,
                transferDate,
                requestCompletionDate
            )
            {
                ChangeHistoryId = changeHistoryId
            };

            await _qualityTaskRepository.InsertAsync(task);
        }
    }

    /// <summary>
    /// Lấy lịch sử thay đổi của phiếu yêu cầu
    /// </summary>
    public async Task<List<TestRequestChangeHistoryDto>> GetChangeHistoryAsync(Guid id)
    {
        var allChanges = await _changeHistoryRepository.GetListAsync(c => c.TestRequestId == id);
        var changes = allChanges.OrderByDescending(c => c.CreationTime).ToList();

        var result = new List<TestRequestChangeHistoryDto>();
        foreach (var change in changes)
        {
            var dto = new TestRequestChangeHistoryDto
            {
                Id = change.Id,
                TestRequestId = change.TestRequestId,
                FieldName = change.FieldName,
                OldValue = change.OldValue,
                NewValue = change.NewValue,
                ApprovalStatus = change.ApprovalStatus,
                PreviousStatus = change.PreviousStatus,
                ApprovedById = change.ApprovedById,
                ApprovalDate = change.ApprovalDate,
                RejectionReason = change.RejectionReason,
                CreationTime = change.CreationTime,
                CreatorId = change.CreatorId
            };

            // Get creator name
            if (change.CreatorId.HasValue)
            {
                var creator = await _userRepository.FindAsync(change.CreatorId.Value);
                dto.CreatorName = creator?.Name ?? creator?.UserName;
            }

            // Get approver name
            if (change.ApprovedById.HasValue)
            {
                var approver = await _userRepository.FindAsync(change.ApprovedById.Value);
                dto.ApprovedByName = approver?.Name ?? approver?.UserName;
            }

            result.Add(dto);
        }

        return result;
    }

    /// <summary>
    /// Rollback entity từ snapshot
    /// </summary>
    private void RollbackFromSnapshot(TestRequest entity, TestRequestSnapshot snapshot)
    {
        // === Thông tin hợp đồng ===
        entity.ContractNumber = snapshot.ContractNumber;
        entity.ContractDate = snapshot.ContractDate;
        entity.SentToAccounting = snapshot.SentToAccounting;
        entity.PaymentReceived = snapshot.PaymentReceived;
        entity.SentToDepartment = snapshot.SentToDepartment;
        entity.ResultReturned = snapshot.ResultReturned;

        // === Thông tin khách hàng ===
        entity.CustomerName = snapshot.CustomerName;
        entity.Province = snapshot.Province;
        entity.District = snapshot.District;
        entity.Ward = snapshot.Ward;
        entity.Address = snapshot.Address;
        entity.TaxCode = snapshot.TaxCode;
        entity.ContactPerson = snapshot.ContactPerson;
        entity.Phone = snapshot.Phone;

        // === Loại hình mẫu ===
        entity.SampleTypeService = snapshot.SampleTypeService;
        entity.SampleTypeContract = snapshot.SampleTypeContract;
        entity.SampleTypeTask = snapshot.SampleTypeTask;
        entity.SampleTypeSubContract = snapshot.SampleTypeSubContract;

        // === Yêu cầu về phương pháp ===
        entity.MethodWestHighlandInstitute = snapshot.MethodWestHighlandInstitute;
        entity.MethodCustomer = snapshot.MethodCustomer;
        entity.MethodCustomerDetail = snapshot.MethodCustomerDetail;
        entity.ResultLanguage = snapshot.ResultLanguage;
        entity.ResultCopyCount = snapshot.ResultCopyCount;
        entity.SeparatePassedResults = snapshot.SeparatePassedResults;
        entity.ConformityDeclaration = snapshot.ConformityDeclaration;
        entity.OtherRequirements = snapshot.OtherRequirements;
        entity.SubContract = snapshot.SubContract;

        // === Thông tin chi phí ===
        entity.TotalCost = snapshot.TotalCost;
        entity.TotalCostInWords = snapshot.TotalCostInWords;
        entity.ReceiptNumber = snapshot.ReceiptNumber;

        // === Người phụ trách ===
        entity.SupervisorId = snapshot.SupervisorId;
        entity.DirectorId = snapshot.DirectorId;

        // === Yêu cầu về lấy mẫu & giao trả ===
        entity.SamplingRequirement = snapshot.SamplingRequirement;
        entity.DeliveryDirectPickup = snapshot.DeliveryDirectPickup;
        entity.DeliveryPostal = snapshot.DeliveryPostal;
        entity.ReturnStoredSample = snapshot.ReturnStoredSample;
        entity.SampleStorage = snapshot.SampleStorage;
        entity.ResultDeliveryDate = snapshot.ResultDeliveryDate;

        // === Thông tin khác ===
        entity.Notes = snapshot.Notes;
        entity.ExpectedCompletionDate = snapshot.ExpectedCompletionDate;
        entity.ChangeRequest = snapshot.ChangeRequest;
        entity.AgreeTerms = snapshot.AgreeTerms;
    }

    #endregion

    #region Override Delete with Validation

    public override async Task DeleteAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);

        if (!CanDelete(entity.Status))
        {
            throw new UserFriendlyException(
                $"Không thể xóa phiếu yêu cầu khi trạng thái là '{GetStatusLabel(entity.Status)}'"
            );
        }

        // Delete samples and indicators first
        var samples = await _sampleRepository.GetListAsync(s => s.TestRequestId == id);
        foreach (var sample in samples)
        {
            var indicators = await _sampleIndicatorRepository.GetListAsync(i => i.TestRequestSampleId == sample.Id);
            foreach (var indicator in indicators)
            {
                await _sampleIndicatorRepository.DeleteAsync(indicator);
            }
            await _sampleRepository.DeleteAsync(sample);
        }

        // Delete laboratory tasks
        var tasks = await _laboratoryTaskRepository.GetListAsync(t => t.TestRequestId == id);
        foreach (var task in tasks)
        {
            var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(ti => ti.LaboratoryTaskId == task.Id);
            foreach (var taskIndicator in taskIndicators)
            {
                await _laboratoryTaskIndicatorRepository.DeleteAsync(taskIndicator);
            }
            await _laboratoryTaskRepository.DeleteAsync(task);
        }

        await Repository.DeleteAsync(id);
    }

    #endregion

    #region Validation

    /// <summary>
    /// Kiểm tra số hợp đồng có tồn tại không
    /// </summary>
    public async Task<bool> CheckContractNumberExistsAsync(string contractNumber, Guid? excludeId = null)
    {
        if (string.IsNullOrWhiteSpace(contractNumber))
        {
            return false;
        }

        var query = await Repository.GetQueryableAsync();

        if (excludeId.HasValue)
        {
            // Exclude current record when updating
            return query.Any(x => x.ContractNumber == contractNumber && x.Id != excludeId.Value);
        }

        return query.Any(x => x.ContractNumber == contractNumber);
    }

    #endregion
}
