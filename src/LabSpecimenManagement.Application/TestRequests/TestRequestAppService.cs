using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Text.Json;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.Common.Exceptions;
using LabSpecimenManagement.Files;
using LabSpecimenManagement.MasterData.Indicators;
using LabSpecimenManagement.Receipts;
using Microsoft.Extensions.Logging;
using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Notifications;
using LabSpecimenManagement.PublishedReports;
using LabSpecimenManagement.QualityManagement;
using LabSpecimenManagement.ResultForms;
using LabSpecimenManagement.Testing;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.Users;
// Alias vì dự án có LabSpecimenManagement.Common.Exceptions.EntityNotFoundException riêng;
// repository.GetAsync throw loại của ABP nên phải trỏ đúng loại đó
using AbpEntityNotFoundException = Volo.Abp.Domain.Entities.EntityNotFoundException;

namespace LabSpecimenManagement.TestRequests;

[RemoteService(IsEnabled = false)]
public partial class TestRequestAppService :
    CrudAppService<
        TestRequest,
        TestRequestDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateTestRequestDto,
        UpdateTestRequestDto>,
    ITestRequestAppService
{
    private const int ContractNumberSeed = 26338;

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
    private readonly IRepository<QualityManagementIndicator, Guid> _qualityIndicatorRepository;
    private readonly IRepository<TestRequestSampleAttachment, Guid> _attachmentRepository;
    private readonly IRepository<SpecimenType, Guid> _specimenTypeRepository;
    private readonly IRepository<SpecimenTypeIndicator, Guid> _specimenTypeIndicatorRepository;
    private readonly IRepository<SampleTestingImage, Guid> _sampleTestingImageRepository;
    private readonly IRepository<IndicatorImage, Guid> _indicatorImageRepository;
    private readonly INotificationAppService _notificationAppService;
    private readonly INotificationCreator _notificationCreator;
    private readonly ICurrentUser _currentUser;
    private readonly ICodeGeneratorService _codeGenerator;
    private readonly IUserNameProvider _userNameProvider;
    private readonly IResultFormAppService _resultFormAppService;
    private readonly IRepository<Receipt, Guid> _receiptRepository;
    private readonly IRepository<MethodChangeHistory, Guid> _methodChangeHistoryRepository;
    private readonly IPublishedReportRepository _publishedReportRepository;
    private readonly IRepository<CostChangeHistory, Guid> _costChangeHistoryRepository;
    private readonly IRepository<IndicatorGroup, Guid> _indicatorGroupRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IRepository<StandardIndicator, Guid> _standardIndicatorRepository;
    private readonly IRepository<TestMethodIndicator, Guid> _testMethodIndicatorRepository;
    private readonly IDataFilter _dataFilter;

    public TestRequestAppService(
        IRepository<TestRequest, Guid> repository,
        IRepository<TestRequestSample, Guid> sampleRepository,
        IRepository<TestRequestSampleIndicator, Guid> sampleIndicatorRepository,
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<Organization, Guid> organizationRepository,
        IRepository<Laboratory, Guid> laboratoryRepository,
        IRepository<Department, Guid> departmentRepository,
        IRepository<TestMethod, Guid> testMethodRepository,
        IIdentityUserRepository userRepository,
        IRepository<LaboratoryTask, Guid> laboratoryTaskRepository,
        IRepository<LaboratoryTaskIndicator, Guid> laboratoryTaskIndicatorRepository,
        IRepository<TestRequestChangeHistory, Guid> changeHistoryRepository,
        IRepository<QualityManagementTask, Guid> qualityTaskRepository,
        IRepository<QualityManagementIndicator, Guid> qualityIndicatorRepository,
        IRepository<TestRequestSampleAttachment, Guid> attachmentRepository,
        IRepository<SpecimenType, Guid> specimenTypeRepository,
        IRepository<SpecimenTypeIndicator, Guid> specimenTypeIndicatorRepository,
        IRepository<SampleTestingImage, Guid> sampleTestingImageRepository,
        IRepository<IndicatorImage, Guid> indicatorImageRepository,
        INotificationAppService notificationAppService,
        INotificationCreator notificationCreator,
        ICurrentUser currentUser,
        ICodeGeneratorService codeGenerator,
        IUserNameProvider userNameProvider,
        IResultFormAppService resultFormAppService,
        IRepository<Receipt, Guid> receiptRepository,
        IRepository<MethodChangeHistory, Guid> methodChangeHistoryRepository,
        IPublishedReportRepository publishedReportRepository,
        IRepository<CostChangeHistory, Guid> costChangeHistoryRepository,
        IRepository<IndicatorGroup, Guid> indicatorGroupRepository,
        IRepository<StandardIndicator, Guid> standardIndicatorRepository,
        IRepository<TestMethodIndicator, Guid> testMethodIndicatorRepository,
        IDataFilter dataFilter)
        : base(repository)
    {
        _sampleRepository = sampleRepository;
        _sampleIndicatorRepository = sampleIndicatorRepository;
        _indicatorRepository = indicatorRepository;
        _organizationRepository = organizationRepository;
        _laboratoryRepository = laboratoryRepository;
        _departmentRepository = departmentRepository;
        _testMethodRepository = testMethodRepository;
        _userRepository = userRepository;
        _codeGenerator = codeGenerator;
        _laboratoryTaskRepository = laboratoryTaskRepository;
        _laboratoryTaskIndicatorRepository = laboratoryTaskIndicatorRepository;
        _changeHistoryRepository = changeHistoryRepository;
        _qualityTaskRepository = qualityTaskRepository;
        _qualityIndicatorRepository = qualityIndicatorRepository;
        _attachmentRepository = attachmentRepository;
        _specimenTypeRepository = specimenTypeRepository;
        _specimenTypeIndicatorRepository = specimenTypeIndicatorRepository;
        _sampleTestingImageRepository = sampleTestingImageRepository;
        _indicatorImageRepository = indicatorImageRepository;
        _notificationAppService = notificationAppService;
        _notificationCreator = notificationCreator;
        _currentUser = currentUser;
        _codeGenerator = codeGenerator;
        _userNameProvider = userNameProvider;
        _resultFormAppService = resultFormAppService;
        _receiptRepository = receiptRepository;
        _methodChangeHistoryRepository = methodChangeHistoryRepository;
        _publishedReportRepository = publishedReportRepository;
        _costChangeHistoryRepository = costChangeHistoryRepository;
        _indicatorGroupRepository = indicatorGroupRepository;
        _standardIndicatorRepository = standardIndicatorRepository;
        _testMethodIndicatorRepository = testMethodIndicatorRepository;
        _dataFilter = dataFilter;
    }

    public override async Task<TestRequestDto> GetAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);
        var dto = ObjectMapper.Map<TestRequest, TestRequestDto>(entity);

        // Map postal delivery images if exists
        if (!string.IsNullOrEmpty(entity.PostalDeliveryImageFileIds))
        {
            try
            {
                var fileIds = JsonSerializer.Deserialize<List<string>>(entity.PostalDeliveryImageFileIds);
                if (fileIds != null)
                {
                    dto.PostalDeliveryImages = fileIds.Select(id => new PostalDeliveryImageDto
                    {
                        FileId = id,
                        Url = $"/api/v1/files/{id}"
                    }).ToList();
                }
            }
            catch
            {
                // Ignore JSON parse errors
            }
        }

        // Load samples with indicators
        var samples = await _sampleRepository.GetListAsync(s => s.TestRequestId == id);
        dto.Samples = new List<TestRequestSampleDto>();

        decimal calculatedTotalCost = 0;

        // Batch load dữ liệu liên quan cho toàn bộ samples/indicators (thay vì ~7 query mỗi indicator)
        var detailSampleIds = samples.Select(s => s.Id).ToList();
        var allSampleIndicators = await _sampleIndicatorRepository.GetListAsync(i => detailSampleIds.Contains(i.TestRequestSampleId));
        var indicatorsBySample = allSampleIndicators.ToLookup(i => i.TestRequestSampleId);

        var detailIndicatorIds = allSampleIndicators.Select(i => i.IndicatorId).Distinct().ToList();
        var indicatorEntityById = (await _indicatorRepository.GetListAsync(i => detailIndicatorIds.Contains(i.Id))).ToDictionary(i => i.Id);

        var detailGroupIds = indicatorEntityById.Values
            .Where(i => i.IndicatorGroupId != Guid.Empty)
            .Select(i => i.IndicatorGroupId).Distinct().ToList();
        var groupById = (await _indicatorGroupRepository.GetListAsync(g => detailGroupIds.Contains(g.Id))).ToDictionary(g => g.Id);

        var detailLabIds = allSampleIndicators.Where(i => i.LaboratoryId.HasValue).Select(i => i.LaboratoryId!.Value).Distinct().ToList();
        var labById = (await _laboratoryRepository.GetListAsync(l => detailLabIds.Contains(l.Id))).ToDictionary(l => l.Id);
        var detailDeptIds = labById.Values.Select(l => l.DepartmentId).Distinct().ToList();
        var deptById = (await _departmentRepository.GetListAsync(d => detailDeptIds.Contains(d.Id))).ToDictionary(d => d.Id);

        var detailMethodIds = allSampleIndicators.Where(i => i.TestMethodId.HasValue).Select(i => i.TestMethodId!.Value).Distinct().ToList();
        var methodById = (await _testMethodRepository.GetListAsync(m => detailMethodIds.Contains(m.Id))).ToDictionary(m => m.Id);

        var detailSpecimenTypeIds = samples.Where(s => s.SpecimenTypeId.HasValue).Select(s => s.SpecimenTypeId!.Value)
            .Concat(allSampleIndicators.Where(i => i.SpecimenTypeId.HasValue).Select(i => i.SpecimenTypeId!.Value))
            .Distinct().ToList();
        var specimenTypeById = (await _specimenTypeRepository.GetListAsync(st => detailSpecimenTypeIds.Contains(st.Id))).ToDictionary(st => st.Id);

        var sampleIndicatorIdList = allSampleIndicators.Select(i => i.Id).ToList();
        var allLabTaskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(
            lti => sampleIndicatorIdList.Contains(lti.TestRequestSampleIndicatorId));
        var labTaskIndicatorsBySampleIndicator = allLabTaskIndicators.ToLookup(lti => lti.TestRequestSampleIndicatorId);

        var labTaskIndicatorIds = allLabTaskIndicators.Select(lti => lti.Id).ToList();
        var imagesByLabTaskIndicator = (await _indicatorImageRepository.GetListAsync(
            x => labTaskIndicatorIds.Contains(x.LaboratoryTaskIndicatorId))).ToLookup(x => x.LaboratoryTaskIndicatorId);

        var attachmentsBySample = (await _attachmentRepository.GetListAsync(a => detailSampleIds.Contains(a.TestRequestSampleId)))
            .ToLookup(a => a.TestRequestSampleId);
        var testingImagesBySample = (await _sampleTestingImageRepository.GetListAsync(img => detailSampleIds.Contains(img.SampleId)))
            .ToLookup(img => img.SampleId);

        foreach (var sample in samples.OrderBy(s => s.SortOrder))
        {
            var sampleDto = ObjectMapper.Map<TestRequestSample, TestRequestSampleDto>(sample);

            // Load specimen type name if assigned
            if (sample.SpecimenTypeId.HasValue)
            {
                sampleDto.SpecimenTypeName = specimenTypeById.GetValueOrDefault(sample.SpecimenTypeId.Value)?.Name;
            }

            // Load indicators for this sample
            var indicators = indicatorsBySample[sample.Id].ToList();
            sampleDto.Indicators = new List<TestRequestSampleIndicatorDto>();

            decimal sampleIndicatorCost = 0;
            foreach (var indicator in indicators)
            {
                var indicatorEntity = indicatorEntityById.GetValueOrDefault(indicator.IndicatorId)
                    ?? throw new AbpEntityNotFoundException(typeof(Indicator), indicator.IndicatorId);

                // Get indicator group info for auto-fill LaboratoryId
                string? indicatorGroupName = null;
                Guid? indicatorGroupLaboratoryId = null;
                if (indicatorEntity.IndicatorGroupId != Guid.Empty)
                {
                    var indicatorGroup = groupById.GetValueOrDefault(indicatorEntity.IndicatorGroupId);
                    if (indicatorGroup != null)
                    {
                        indicatorGroupName = indicatorGroup.Name;
                        indicatorGroupLaboratoryId = indicatorGroup.LaboratoryId;
                    }
                }

                // Get laboratory name if assigned
                string? laboratoryName = null;
                if (indicator.LaboratoryId.HasValue)
                {
                    var laboratory = labById.GetValueOrDefault(indicator.LaboratoryId.Value);
                    if (laboratory != null)
                    {
                        laboratoryName = deptById.GetValueOrDefault(laboratory.DepartmentId)?.Name;
                    }
                }

                // Get test method info if assigned
                string? testMethodCode = null;
                string? testMethodName = null;
                string? testMethodFormattedName = null;
                if (indicator.TestMethodId.HasValue)
                {
                    var testMethod = methodById.GetValueOrDefault(indicator.TestMethodId.Value);
                    testMethodCode = testMethod?.Code;
                    testMethodName = testMethod?.Name;
                    testMethodFormattedName = testMethod?.FormattedName;
                }

                // Get specimen type name if assigned (for indicator-level specimen type)
                string? indicatorSpecimenTypeName = null;
                if (indicator.SpecimenTypeId.HasValue)
                {
                    indicatorSpecimenTypeName = specimenTypeById.GetValueOrDefault(indicator.SpecimenTypeId.Value)?.Name;
                }

                // Get approval status and test date from LaboratoryTaskIndicator if exists
                IndicatorApprovalStatus? approvalStatus = null;
                DateTime? testDate = null;
                Guid? labTaskIndicatorId = null;
                var labTaskIndicator = labTaskIndicatorsBySampleIndicator[indicator.Id].FirstOrDefault();
                if (labTaskIndicator != null)
                {
                    approvalStatus = labTaskIndicator.ApprovalStatus;
                    testDate = labTaskIndicator.TestDate;
                    labTaskIndicatorId = labTaskIndicator.Id;
                }

                // Get indicator images from LaboratoryTaskIndicator
                List<Testing.IndicatorImageDto> indicatorImages = new();
                if (labTaskIndicatorId.HasValue)
                {
                    indicatorImages = imagesByLabTaskIndicator[labTaskIndicatorId.Value]
                        .OrderBy(x => x.CreationTime).Select(x => new Testing.IndicatorImageDto
                    {
                        Id = x.Id,
                        LaboratoryTaskIndicatorId = x.LaboratoryTaskIndicatorId,
                        FileId = x.FileId,
                        FileName = x.FileName,
                        FileSize = x.FileSize,
                        Url = $"/api/v1/files/{x.FileId}",
                        CreationTime = x.CreationTime,
                        CreatorId = x.CreatorId
                    }).ToList();
                }

                sampleDto.Indicators.Add(new TestRequestSampleIndicatorDto
                {
                    Id = indicator.Id,
                    TestRequestSampleId = indicator.TestRequestSampleId,
                    IndicatorId = indicator.IndicatorId,
                    IndicatorCode = indicatorEntity.Code,
                    IndicatorName = indicatorEntity.Name,
                    IndicatorFormattedName = indicatorEntity.FormattedName,
                    IndicatorGroupName = indicatorGroupName,
                    IndicatorGroupLaboratoryId = indicatorGroupLaboratoryId,
                    TestMethodId = indicator.TestMethodId,
                    TestMethodCode = testMethodCode,
                    TestMethodName = testMethodName,
                    TestMethodFormattedName = testMethodFormattedName,
                    SpecimenTypeId = indicator.SpecimenTypeId,
                    SpecimenTypeName = indicatorSpecimenTypeName,
                    UnitPrice = indicator.UnitPrice,
                    OriginalUnitPrice = indicator.OriginalUnitPrice,
                    Notes = indicator.Notes,
                    LaboratoryId = indicator.LaboratoryId,
                    LaboratoryName = laboratoryName,
                    TransferDate = indicator.TransferDate,
                    RequestDate = indicator.RequestDate,
                    SentToDepartment = indicator.SentToDepartment,
                    ApprovalStatus = approvalStatus,
                    TestDate = testDate,
                    ParentIndicatorId = indicatorEntity.ParentIndicatorId,
                    Images = indicatorImages,
                    IsSentToQLKT = labTaskIndicator != null,
                    IsSubcontract = indicator.IsSubcontract
                });
                sampleIndicatorCost += indicator.UnitPrice;
            }

            // Calculate sample total cost from indicators
            sampleDto.TotalIndicatorCost = sampleIndicatorCost;
            calculatedTotalCost += sampleIndicatorCost;

            // Load attachments for this sample
            var attachments = attachmentsBySample[sample.Id].ToList();
            sampleDto.Attachments = attachments.Select(a => new Files.SampleAttachmentDto
            {
                Id = a.Id,
                FileId = a.FileId,
                FileName = a.FileName,
                FileSize = a.FileSize,
                Url = $"/api/v1/files/{a.FileId}"
            }).ToList();

            // Load testing images (before/after) for this sample
            var testingImages = testingImagesBySample[sample.Id].ToList();
            sampleDto.BeforeTestingImages = testingImages
                .Where(img => img.ImageType == SampleTestingImageType.BeforeTesting)
                .Select(img => new SampleTestingImageDto
                {
                    Id = img.Id,
                    FileId = img.FileId,
                    FileName = img.FileName,
                    FileSize = img.FileSize,
                    Url = $"/api/v1/files/{img.FileId}",
                    ImageType = img.ImageType,
                    CreationTime = img.CreationTime
                })
                .OrderBy(img => img.CreationTime)
                .ToList();

            sampleDto.AfterTestingImages = testingImages
                .Where(img => img.ImageType == SampleTestingImageType.AfterTesting)
                .Select(img => new SampleTestingImageDto
                {
                    Id = img.Id,
                    FileId = img.FileId,
                    FileName = img.FileName,
                    FileSize = img.FileSize,
                    Url = $"/api/v1/files/{img.FileId}",
                    ImageType = img.ImageType,
                    CreationTime = img.CreationTime
                })
                .OrderBy(img => img.CreationTime)
                .ToList();

            dto.Samples.Add(sampleDto);
        }

        // Set total cost from calculated value
        dto.TotalCost = calculatedTotalCost;

        // Load user names (batch fetch)
        var userNames = await _userNameProvider.GetUserNamesAsync(new[] { dto.SupervisorId, dto.DirectorId });
        if (dto.SupervisorId.HasValue && userNames.TryGetValue(dto.SupervisorId.Value, out var supervisorName))
        {
            dto.SupervisorName = supervisorName;
        }
        if (dto.DirectorId.HasValue && userNames.TryGetValue(dto.DirectorId.Value, out var directorName))
        {
            dto.DirectorName = directorName;
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
                (x.ContactPerson != null && x.ContactPerson.ToLower().Contains(search)) ||
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
        queryable = input.Sorting?.ToLower() switch
        {
            "expectedcompletiondate asc" => queryable.OrderBy(x => x.ExpectedCompletionDate).ThenByDescending(x => x.CreationTime),
            "expectedcompletiondate desc" => queryable.OrderByDescending(x => x.ExpectedCompletionDate).ThenByDescending(x => x.CreationTime),
            _ => queryable.OrderByDescending(x => x.ContractDate).ThenByDescending(x => x.CreationTime),
        };

        // Apply paging
        queryable = queryable
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount);

        var entities = await AsyncExecuter.ToListAsync(queryable);
        var dtos = ObjectMapper.Map<List<TestRequest>, List<TestRequestDto>>(entities);

        // Batch 2 query cho cả trang thay vì 1+N*M query trong vòng lặp
        var dtoEnricher = LazyServiceProvider.LazyGetRequiredService<TestRequestDtoEnricher>();
        await dtoEnricher.EnrichWithTotalCostsAsync(dtos);

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

        // Quotation reference (when created from quotation)
        entity.QuotationId = createInput.QuotationId;

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

    private static bool HasAnySampleTypeSelection(
        bool sampleTypeService,
        bool sampleTypeContract,
        bool sampleTypeTask,
        bool sampleTypeSubContract)
    {
        return sampleTypeService || sampleTypeContract || sampleTypeTask || sampleTypeSubContract;
    }

    private static bool HasAnySampleTypeSelection(TestRequest entity)
    {
        return HasAnySampleTypeSelection(
            entity.SampleTypeService,
            entity.SampleTypeContract,
            entity.SampleTypeTask,
            entity.SampleTypeSubContract);
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static void NormalizeAndValidateSampleType(CreateTestRequestDto input)
    {
        if (!HasAnySampleTypeSelection(
                input.SampleTypeService,
                input.SampleTypeContract,
                input.SampleTypeTask,
                input.SampleTypeSubContract))
        {
            throw new TestRequestValidationException("sampleType", "Vui lòng chọn ít nhất một loại hình mẫu");
        }

        input.SubContract = input.SampleTypeSubContract
            ? NormalizeOptionalText(input.SubContract)
            : null;

        if (input.SampleTypeSubContract && string.IsNullOrWhiteSpace(input.SubContract))
        {
            throw new TestRequestValidationException("subContract", "Vui lòng nhập hợp đồng phụ");
        }
    }

    private static void NormalizeAndValidateSampleType(UpdateTestRequestDto input, TestRequest existingEntity)
    {
        var hasAnySelectionInInput = HasAnySampleTypeSelection(
            input.SampleTypeService,
            input.SampleTypeContract,
            input.SampleTypeTask,
            input.SampleTypeSubContract);

        // Backward compatibility for old clients/data payloads: keep existing selection
        // when incoming payload sends all flags false but existing record already has a valid selection.
        if (!hasAnySelectionInInput && HasAnySampleTypeSelection(existingEntity))
        {
            input.SampleTypeService = existingEntity.SampleTypeService;
            input.SampleTypeContract = existingEntity.SampleTypeContract;
            input.SampleTypeTask = existingEntity.SampleTypeTask;
            input.SampleTypeSubContract = existingEntity.SampleTypeSubContract;
            hasAnySelectionInInput = true;
        }

        if (!hasAnySelectionInInput)
        {
            throw new TestRequestValidationException("sampleType", "Vui lòng chọn ít nhất một loại hình mẫu");
        }

        input.SubContract = input.SampleTypeSubContract
            ? NormalizeOptionalText(input.SubContract)
            : null;

        if (input.SampleTypeSubContract && string.IsNullOrWhiteSpace(input.SubContract))
        {
            throw new TestRequestValidationException("subContract", "Vui lòng nhập hợp đồng phụ");
        }
    }

    public override async Task<TestRequestDto> CreateAsync(CreateTestRequestDto input)
    {
        NormalizeAndValidateSampleType(input);

        // Contract number must be unique; return validation error when duplicate.
        if (!string.IsNullOrWhiteSpace(input.ContractNumber))
        {
            var contractExists = await CheckContractNumberExistsAsync(input.ContractNumber);
            if (contractExists)
            {
                throw new TestRequestValidationException("contractNumber", "Số hợp đồng đã tồn tại trong hệ thống");
            }
        }

        // Auto-create Organization if customerName doesn't exist
        await EnsureOrganizationExistsAsync(
            input.CustomerName,
            input.ProvinceCode,
            input.WardCode,
            input.Address,
            input.TaxCode,
            input.ContactPerson,
            input.Phone);

        // Validate indicator and test method IDs before inserting
        if (input.Samples != null && input.Samples.Any())
        {
            var allIndicatorIds = input.Samples
                .Where(s => s.Indicators != null)
                .SelectMany(s => s.Indicators)
                .Select(i => i.IndicatorId)
                .Distinct()
                .ToList();

            if (allIndicatorIds.Any())
            {
                var existingIndicators = await _indicatorRepository.GetListAsync(i => allIndicatorIds.Contains(i.Id));
                var existingIndicatorIds = existingIndicators.Select(i => i.Id).ToHashSet();
                var missingIds = allIndicatorIds.Where(id => !existingIndicatorIds.Contains(id)).ToList();
                if (missingIds.Any())
                {
                    throw new Volo.Abp.BusinessException(
                        message: $"Chỉ tiêu không tồn tại trong hệ thống: {string.Join(", ", missingIds)}. Vui lòng tải lại trang và thử lại.");
                }
            }

            var allTestMethodIds = input.Samples
                .Where(s => s.Indicators != null)
                .SelectMany(s => s.Indicators)
                .Where(i => i.TestMethodId.HasValue)
                .Select(i => i.TestMethodId!.Value)
                .Distinct()
                .ToList();

            if (allTestMethodIds.Any())
            {
                var existingMethods = await _testMethodRepository.GetListAsync(m => allTestMethodIds.Contains(m.Id));
                var existingMethodIds = existingMethods.Select(m => m.Id).ToHashSet();
                var missingIds = allTestMethodIds.Where(id => !existingMethodIds.Contains(id)).ToList();
                if (missingIds.Any())
                {
                    throw new Volo.Abp.BusinessException(
                        message: $"Phương pháp thử không tồn tại trong hệ thống: {string.Join(", ", missingIds)}. Vui lòng tải lại trang và thử lại.");
                }
            }
        }

        var entity = await MapToEntityAsync(input);
        try
        {
            await Repository.InsertAsync(entity, autoSave: true);
        }
        catch (Exception ex) when (IsContractNumberDuplicateException(ex))
        {
            throw new TestRequestValidationException("contractNumber", "Số hợp đồng đã tồn tại trong hệ thống");
        }

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
                sample.IsSharedSample = sampleDto.IsSharedSample;
                // Resolve SpecimenTypeName to ID (create new if not exists)
                sample.SpecimenTypeId = await ResolveSpecimenTypeIdAsync(sampleDto.SpecimenTypeName, sampleDto.SpecimenTypeId);

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
                        sampleIndicator.OriginalUnitPrice = indicatorDto.OriginalUnitPrice;
                        sampleIndicator.Notes = indicatorDto.Notes;
                        sampleIndicator.TestMethodId = indicatorDto.TestMethodId;
                        sampleIndicator.SpecimenTypeId = indicatorDto.SpecimenTypeId;
                        sampleIndicator.TransferDate = indicatorDto.TransferDate;
                        sampleIndicator.RequestDate = indicatorDto.RequestDate;
                        sampleIndicator.SentToDepartment = indicatorDto.SentToDepartment;
                        sampleIndicator.LaboratoryId = indicatorDto.LaboratoryId;
                        sampleIndicator.IsSubcontract = indicatorDto.IsSubcontract;

                        await _sampleIndicatorRepository.InsertAsync(sampleIndicator, autoSave: true);
                        sampleIndicatorCost += indicatorDto.UnitPrice;
                    }
                }

                // Update sample total cost
                sample.TotalIndicatorCost = sampleIndicatorCost;
                await _sampleRepository.UpdateAsync(sample, autoSave: true);
                totalCost += sampleIndicatorCost;

                // Create sample attachments
                if (sampleDto.Attachments != null && sampleDto.Attachments.Any())
                {
                    foreach (var attachmentInput in sampleDto.Attachments)
                    {
                        // Check if FileId already exists (avoid duplicate key error)
                        var existingAttachment = await _attachmentRepository.FirstOrDefaultAsync(a => a.FileId == attachmentInput.FileId);
                        if (existingAttachment != null)
                        {
                            // Skip if already exists - file was already uploaded
                            continue;
                        }

                        var attachment = new TestRequestSampleAttachment(
                            GuidGenerator.Create(),
                            sample.Id,
                            attachmentInput.FileId,
                            attachmentInput.FileName,
                            $"uploads/samples/{attachmentInput.FileId}",
                            attachmentInput.FileSize
                        );
                        await _attachmentRepository.InsertAsync(attachment, autoSave: true);
                    }
                }
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
        NormalizeAndValidateSampleType(input, entity);

        // [TEMPORARY] Tạm tắt approval flow
        // var previousStatus = entity.Status;
        // var requiresApproval = CanEditLimitedInfo(entity.Status);

        // Validate contract number is unique (exclude current record)
        if (!string.IsNullOrWhiteSpace(input.ContractNumber))
        {
            var contractExists = await CheckContractNumberExistsAsync(input.ContractNumber, id);
            if (contractExists)
            {
                throw new TestRequestValidationException("contractNumber", "Số hợp đồng đã tồn tại trong hệ thống");
            }
        }

        // Validate edit permission based on status
        if (!CanEditFullInfo(entity.Status))
        {
            throw new Common.Exceptions.InvalidOperationException($"Không thể chỉnh sửa phiếu yêu cầu khi trạng thái là '{GetStatusLabel(entity.Status)}'", "status");
        }

        // [TEMPORARY] Tạm tắt validate locked fields - cho phép edit tất cả fields
        // [OLD LOGIC] Validate limited update - check locked fields
        // if (!CanEditFullInfo(entity.Status))
        // {
        //     ValidateLimitedUpdate(entity, input);
        // }

        // [TEMPORARY] Tạm tắt approval flow - luôn update trực tiếp
        // [OLD LOGIC] If requires approval (SampleSent/InProgress), save change history
        // if (requiresApproval)
        // {
        //     var changeHistoryId = await SaveChangeHistoryAsync(entity, input, previousStatus);
        //     if (changeHistoryId.HasValue)
        //     {
        //         entity.Status = TestRequestStatus.PendingChangeApproval;
        //         await Repository.UpdateAsync(entity, autoSave: true);
        //         await CreateQualityManagementTaskForChangeApprovalAsync(entity, changeHistoryId.Value);
        //     }
        //     return await GetAsync(id);
        // }

        // === Update trực tiếp cho tất cả trạng thái ===

        // Auto-create Organization if customerName doesn't exist
        await EnsureOrganizationExistsAsync(
            input.CustomerName,
            input.ProvinceCode,
            input.WardCode,
            input.Address,
            input.TaxCode,
            input.ContactPerson,
            input.Phone);

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

        try
        {
            await Repository.UpdateAsync(entity, autoSave: true);
        }
        catch (Exception ex) when (IsContractNumberDuplicateException(ex))
        {
            throw new TestRequestValidationException("contractNumber", "Số hợp đồng đã tồn tại trong hệ thống");
        }

        // Sync ContractNumber to related LaboratoryTasks and QualityManagementTasks
        await SyncContractNumberToRelatedTasksAsync(id, entity.ContractNumber);

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
                    sample.IsSharedSample = sampleDto.IsSharedSample;
                    // Resolve SpecimenTypeName to ID (create new if not exists)
                    sample.SpecimenTypeId = await ResolveSpecimenTypeIdAsync(sampleDto.SpecimenTypeName, sampleDto.SpecimenTypeId);
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
                    sample.IsSharedSample = sampleDto.IsSharedSample;
                    // Resolve SpecimenTypeName to ID (create new if not exists)
                    sample.SpecimenTypeId = await ResolveSpecimenTypeIdAsync(sampleDto.SpecimenTypeName, sampleDto.SpecimenTypeId);
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
                                throw new Common.Exceptions.InvalidOperationException("Không thể xóa chỉ tiêu đang được thử nghiệm hoặc đã có kết quả.", "indicator");
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
                            var oldLaboratoryId = indicator.LaboratoryId;
                            indicator.UnitPrice = indicatorDto.UnitPrice;
                            indicator.OriginalUnitPrice = indicatorDto.OriginalUnitPrice;
                            indicator.Notes = indicatorDto.Notes;
                            indicator.LaboratoryId = indicatorDto.LaboratoryId;
                            indicator.TestMethodId = indicatorDto.TestMethodId;
                            indicator.SpecimenTypeId = indicatorDto.SpecimenTypeId;
                            indicator.TransferDate = indicatorDto.TransferDate;
                            indicator.RequestDate = indicatorDto.RequestDate;
                            indicator.SentToDepartment = indicatorDto.SentToDepartment;
                            await _sampleIndicatorRepository.UpdateAsync(indicator, autoSave: true);

                            // Move LaboratoryTaskIndicator when lab changes and status is Pending
                            if (oldLaboratoryId != indicatorDto.LaboratoryId
                                && oldLaboratoryId.HasValue && indicatorDto.LaboratoryId.HasValue)
                            {
                                var labTaskIndicator = await _laboratoryTaskIndicatorRepository.FirstOrDefaultAsync(
                                    lti => lti.TestRequestSampleIndicatorId == indicator.Id);

                                if (labTaskIndicator != null
                                    && labTaskIndicator.ApprovalStatus == IndicatorApprovalStatus.Pending)
                                {
                                    var newLabId = indicatorDto.LaboratoryId.Value;

                                    // Find or create LaboratoryTask for the new lab
                                    var newLabTask = await _laboratoryTaskRepository.FirstOrDefaultAsync(
                                        t => t.TestRequestId == entity.Id && t.LaboratoryId == newLabId);

                                    if (newLabTask == null)
                                    {
                                        var laboratory = await _laboratoryRepository.GetAsync(newLabId);
                                        newLabTask = new LaboratoryTask(
                                            GuidGenerator.Create(),
                                            entity.Id,
                                            newLabId,
                                            entity.ContractNumber,
                                            1,
                                            laboratory.TechnicalManagerId,
                                            indicator.TransferDate,
                                            indicator.RequestDate
                                        );
                                        newLabTask.SampleSentDate = entity.SampleSentDate;
                                        await _laboratoryTaskRepository.InsertAsync(newLabTask, autoSave: true);
                                    }

                                    // Move indicator to new task
                                    labTaskIndicator.LaboratoryTaskId = newLabTask.Id;
                                    await _laboratoryTaskIndicatorRepository.UpdateAsync(labTaskIndicator, autoSave: true);
                                }
                            }
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
                            indicator.OriginalUnitPrice = indicatorDto.OriginalUnitPrice;
                            indicator.Notes = indicatorDto.Notes;
                            indicator.TestMethodId = indicatorDto.TestMethodId;
                            indicator.SpecimenTypeId = indicatorDto.SpecimenTypeId;
                            indicator.TransferDate = indicatorDto.TransferDate;
                            indicator.RequestDate = indicatorDto.RequestDate;
                            indicator.SentToDepartment = indicatorDto.SentToDepartment;
                            indicator.LaboratoryId = indicatorDto.LaboratoryId;
                            indicator.IsSubcontract = indicatorDto.IsSubcontract;

                            await _sampleIndicatorRepository.InsertAsync(indicator, autoSave: true);
                        }
                        sampleIndicatorCost += indicatorDto.UnitPrice;
                    }
                }

                sample.TotalIndicatorCost = sampleIndicatorCost;
                await _sampleRepository.UpdateAsync(sample, autoSave: true);
                totalCost += sampleIndicatorCost;

                // Update sample attachments
                if (sampleDto.Attachments != null)
                {
                    // Get existing attachments
                    var existingAttachments = await _attachmentRepository.GetListAsync(a => a.TestRequestSampleId == sample.Id);
                    var existingFileIds = existingAttachments.Select(a => a.FileId).ToList();
                    var inputFileIds = sampleDto.Attachments.Select(a => a.FileId).ToList();

                    // Delete attachments not in input
                    foreach (var existingAttachment in existingAttachments)
                    {
                        if (!inputFileIds.Contains(existingAttachment.FileId))
                        {
                            await _attachmentRepository.DeleteAsync(existingAttachment);
                        }
                    }

                    // Add new attachments
                    foreach (var attachmentInput in sampleDto.Attachments)
                    {
                        if (!existingFileIds.Contains(attachmentInput.FileId))
                        {
                            // Check if FileId already exists globally (avoid duplicate key error)
                            var globalExisting = await _attachmentRepository.FirstOrDefaultAsync(a => a.FileId == attachmentInput.FileId);
                            if (globalExisting != null)
                            {
                                // Skip if already exists globally
                                continue;
                            }

                            var attachment = new TestRequestSampleAttachment(
                                GuidGenerator.Create(),
                                sample.Id,
                                attachmentInput.FileId,
                                attachmentInput.FileName,
                                $"uploads/samples/{attachmentInput.FileId}",
                                attachmentInput.FileSize
                            );
                            await _attachmentRepository.InsertAsync(attachment, autoSave: true);
                        }
                    }
                }
            }

            // Update total cost if changed
            if (entity.TotalCost != totalCost)
            {
                entity.TotalCost = totalCost;
                await Repository.UpdateAsync(entity, autoSave: true);
            }
        }

        // Migrate mismatched LaboratoryTaskIndicator (SampleIndicator.LaboratoryId != Task.LaboratoryId)
        // Handles edge case: updateIndicatorsLaboratory saved but sendToQLKT didn't run
        var allLabTasks = await _laboratoryTaskRepository.GetListAsync(t => t.TestRequestId == id);
        var hasMigration = false;

        foreach (var labTask in allLabTasks.ToList())
        {
            var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(
                ti => ti.LaboratoryTaskId == labTask.Id);

            foreach (var ti in taskIndicators)
            {
                if (ti.ApprovalStatus != IndicatorApprovalStatus.Pending
                    && ti.ApprovalStatus != IndicatorApprovalStatus.AcceptedForTesting)
                    continue;

                var si = await _sampleIndicatorRepository.FindAsync(ti.TestRequestSampleIndicatorId);
                if (si == null || !si.LaboratoryId.HasValue || si.LaboratoryId.Value == labTask.LaboratoryId)
                    continue;

                var targetLabId = si.LaboratoryId.Value;
                var targetTask = allLabTasks.FirstOrDefault(t => t.LaboratoryId == targetLabId);

                if (targetTask == null)
                {
                    var laboratory = await _laboratoryRepository.GetAsync(targetLabId);
                    targetTask = new LaboratoryTask(
                        GuidGenerator.Create(),
                        id,
                        targetLabId,
                        entity.ContractNumber,
                        1,
                        laboratory.TechnicalManagerId,
                        si.TransferDate,
                        si.RequestDate
                    );
                    targetTask.SampleSentDate = entity.SampleSentDate;
                    await _laboratoryTaskRepository.InsertAsync(targetTask, autoSave: true);
                    allLabTasks.Add(targetTask);
                }

                ti.LaboratoryTaskId = targetTask.Id;
                await _laboratoryTaskIndicatorRepository.UpdateAsync(ti, autoSave: true);
                hasMigration = true;
            }
        }

        // Recalculate TotalSampleCount + delete empty tasks
        if (hasMigration)
        {
            allLabTasks = await _laboratoryTaskRepository.GetListAsync(t => t.TestRequestId == id);
        }
        foreach (var labTask in allLabTasks)
        {
            var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(
                ti => ti.LaboratoryTaskId == labTask.Id);

            if (taskIndicators.Count == 0)
            {
                await _laboratoryTaskRepository.DeleteAsync(labTask, autoSave: true);
                continue;
            }

            var distinctSampleIds = new HashSet<Guid>();
            foreach (var ti in taskIndicators)
            {
                var si = await _sampleIndicatorRepository.FindAsync(ti.TestRequestSampleIndicatorId);
                if (si != null) distinctSampleIds.Add(si.TestRequestSampleId);
            }
            if (labTask.TotalSampleCount != distinctSampleIds.Count)
            {
                labTask.TotalSampleCount = distinctSampleIds.Count;
                await _laboratoryTaskRepository.UpdateAsync(labTask, autoSave: true);
            }
        }

        return await GetAsync(id);
    }

    private async Task<string> GenerateRequestNumberAsync()
    {
        // Disable soft delete filter to include deleted records (avoid duplicate codes)
        using (_dataFilter.Disable<ISoftDelete>())
        {
            var prefix = _codeGenerator.GetTodayPrefix("REQ");
            var count = await Repository.CountAsync(x => x.RequestNumber.StartsWith(prefix));
            return _codeGenerator.GenerateCodeWithDate("REQ", count);
        }
    }

    /// <summary>
    /// Generate next contract number in a global sequential series.
    /// Starts from 26170 and increments by 1 based on persisted database data.
    /// </summary>
    public async Task<string> GenerateContractNumberAsync()
    {
        var query = await Repository.GetQueryableAsync();
        var existingNumbers = await AsyncExecuter.ToListAsync(
            query.Where(x => x.ContractNumber != null)
                 .Select(x => x.ContractNumber));

        var maxNumber = ContractNumberSeed - 1;
        foreach (var contractNumber in existingNumbers)
        {
            if (TryParseSequentialContractNumber(contractNumber, out var parsedNumber) && parsedNumber > maxNumber)
            {
                maxNumber = parsedNumber;
            }
        }

        var nextNumber = maxNumber + 1;
        return nextNumber.ToString();
    }

    private static bool TryParseSequentialContractNumber(string? contractNumber, out int parsedNumber)
    {
        parsedNumber = 0;

        if (string.IsNullOrWhiteSpace(contractNumber))
        {
            return false;
        }

        if (!int.TryParse(contractNumber.Trim(), out var value))
        {
            return false;
        }

        if (value < ContractNumberSeed)
        {
            return false;
        }

        parsedNumber = value;
        return true;
    }

    private static bool IsDuplicateKeyException(Exception ex)
    {
        if (ex.GetType().Name == "DbUpdateException")
        {
            var innerMessage = ex.InnerException?.Message ?? string.Empty;
            if (innerMessage.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) ||
                innerMessage.Contains("23505", StringComparison.OrdinalIgnoreCase) ||
                innerMessage.Contains("unique constraint", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return ex.InnerException != null && IsDuplicateKeyException(ex.InnerException);
    }

    private static bool IsContractNumberDuplicateException(Exception ex)
    {
        if (!IsDuplicateKeyException(ex))
        {
            return false;
        }

        var fullMessage = ex.ToString();
        return fullMessage.Contains("ContractNumber", StringComparison.OrdinalIgnoreCase) ||
               fullMessage.Contains("AppTestRequests", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if Organization exists by name, create if not exists
    /// </summary>
    private async Task EnsureOrganizationExistsAsync(
        string customerName,
        string? provinceCode = null,
        string? wardCode = null,
        string? address = null,
        string? taxCode = null,
        string? contactPerson = null,
        string? phone = null)
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
            organization.ProvinceCode = provinceCode ?? string.Empty;
            organization.WardCode = wardCode ?? string.Empty;
            organization.Address = address ?? string.Empty;
            organization.TaxCode = taxCode;
            organization.ContactPerson = contactPerson ?? string.Empty;
            organization.Phone = phone ?? string.Empty;
            await _organizationRepository.InsertAsync(organization, autoSave: true);
        }
    }

    /// <summary>
    /// Resolve SpecimenType name to ID (case-insensitive)
    /// Creates new SpecimenType if not exists
    /// Priority: specimenTypeName > specimenTypeId (user can change specimen type by entering new name)
    /// </summary>
    /// <param name="specimenTypeName">Name of the specimen type (prioritized if provided)</param>
    /// <param name="existingSpecimenTypeId">Existing specimen type ID (used as fallback if name is empty)</param>
    /// <returns>SpecimenType ID or null if both are empty</returns>
    private async Task<Guid?> ResolveSpecimenTypeIdAsync(string? specimenTypeName, Guid? existingSpecimenTypeId)
    {
        // If specimenTypeName is provided, use it to resolve (allows user to change specimen type)
        if (!string.IsNullOrWhiteSpace(specimenTypeName))
        {
            var normalizedName = specimenTypeName.Trim();

            // Try to find existing SpecimenType by name (case-insensitive)
            // Load all and filter in memory to avoid ToLower() translation issues
            var allSpecimenTypes = await _specimenTypeRepository.GetListAsync();
            var existingSpecimenType = allSpecimenTypes.FirstOrDefault(
                st => st.Name.Equals(normalizedName, StringComparison.OrdinalIgnoreCase));

            if (existingSpecimenType != null)
            {
                return existingSpecimenType.Id;
            }

            // Create new SpecimenType with auto-generated code
            // Use retry logic to handle race condition with unique constraint
            const int maxRetries = 3;
            for (int retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    var newCode = await GenerateSpecimenTypeCodeAsync();
                    var newSpecimenType = new SpecimenType(GuidGenerator.Create(), newCode, normalizedName);
                    await _specimenTypeRepository.InsertAsync(newSpecimenType, autoSave: true);
                    return newSpecimenType.Id;
                }
                catch (Exception ex) when (ex.InnerException?.Message?.Contains("duplicate key") == true ||
                                          ex.InnerException?.Message?.Contains("unique constraint") == true ||
                                          ex.Message?.Contains("duplicate key") == true ||
                                          ex.Message?.Contains("unique constraint") == true)
                {
                    // Duplicate code - retry with new code
                    if (retry == maxRetries - 1)
                    {
                        throw; // Last retry failed, rethrow
                    }
                    await Task.Delay(100 * (retry + 1)); // Small delay before retry
                }
            }
        }

        // Fallback to existing ID if no name provided
        if (existingSpecimenTypeId.HasValue)
        {
            return existingSpecimenTypeId;
        }

        return null;
    }

    /// <summary>
    /// Generate unique code for new SpecimenType (LM001, LM002, etc.)
    /// </summary>
    private async Task<string> GenerateSpecimenTypeCodeAsync()
    {
        const string prefix = "LM";
        var allSpecimenTypes = await _specimenTypeRepository.GetListAsync();
        var maxNumber = 0;

        foreach (var specimenType in allSpecimenTypes)
        {
            if (!string.IsNullOrEmpty(specimenType.Code) && specimenType.Code.StartsWith(prefix))
            {
                var numberPart = specimenType.Code.Substring(prefix.Length);
                if (int.TryParse(numberPart, out var number) && number > maxNumber)
                {
                    maxNumber = number;
                }
            }
        }

        // Add random suffix to avoid race condition
        var timestamp = DateTime.UtcNow.Ticks % 1000;
        return $"{prefix}{(maxNumber + 1):D3}";
    }

    /// <summary>
    /// Auto-create LaboratoryTask records grouped by Laboratory when TestRequest is sent to QLKT
    /// </summary>
    private async Task CreateLaboratoryTasksAsync(TestRequest testRequest)
    {
        // Get all samples for this TestRequest
        var samples = await _sampleRepository.GetListAsync(s => s.TestRequestId == testRequest.Id);

        if (!samples.Any())
        {
            return;
        }

        // Get already-sent indicator IDs (exist in LaboratoryTaskIndicator) to skip them
        var existingTasks = await _laboratoryTaskRepository.GetListAsync(t => t.TestRequestId == testRequest.Id);
        var alreadySentIndicatorIds = new HashSet<Guid>();
        foreach (var task in existingTasks)
        {
            var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(
                ti => ti.LaboratoryTaskId == task.Id);
            foreach (var ti in taskIndicators)
            {
                alreadySentIndicatorIds.Add(ti.TestRequestSampleIndicatorId);
            }
        }

        // Detect and move LaboratoryTaskIndicator records whose SampleIndicator.LaboratoryId has changed
        var tasksNeedingRecalculation = new HashSet<Guid>();
        foreach (var existingTask in existingTasks)
        {
            var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(
                ti => ti.LaboratoryTaskId == existingTask.Id);

            foreach (var ti in taskIndicators)
            {
                var sampleIndicator = await _sampleIndicatorRepository.FindAsync(ti.TestRequestSampleIndicatorId);
                if (sampleIndicator == null || !sampleIndicator.LaboratoryId.HasValue ||
                    sampleIndicator.LaboratoryId.Value == existingTask.LaboratoryId)
                    continue;

                // Lab has changed - move this LaboratoryTaskIndicator to the correct task
                var targetLabId = sampleIndicator.LaboratoryId.Value;
                var targetTask = existingTasks.FirstOrDefault(t => t.LaboratoryId == targetLabId);

                if (targetTask != null)
                {
                    ti.LaboratoryTaskId = targetTask.Id;
                    await _laboratoryTaskIndicatorRepository.UpdateAsync(ti, autoSave: true);
                    tasksNeedingRecalculation.Add(targetTask.Id);
                }
                else
                {
                    // Target task doesn't exist yet - remove from current task and let it be recreated below
                    await _laboratoryTaskIndicatorRepository.DeleteAsync(ti, autoSave: true);
                    alreadySentIndicatorIds.Remove(ti.TestRequestSampleIndicatorId);
                }
                tasksNeedingRecalculation.Add(existingTask.Id);
            }
        }

        // Recalculate TotalSampleCount for affected tasks
        foreach (var taskId in tasksNeedingRecalculation)
        {
            var affectedTask = existingTasks.First(t => t.Id == taskId);
            var currentIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(
                ti => ti.LaboratoryTaskId == taskId);
            var sampleIds = new HashSet<Guid>();
            foreach (var ti in currentIndicators)
            {
                var si = await _sampleIndicatorRepository.FindAsync(ti.TestRequestSampleIndicatorId);
                if (si != null) sampleIds.Add(si.TestRequestSampleId);
            }
            affectedTask.TotalSampleCount = sampleIds.Count;
            await _laboratoryTaskRepository.UpdateAsync(affectedTask, autoSave: true);
        }

        // Group indicators by LaboratoryId
        var indicatorsByLab = new Dictionary<Guid, List<(Guid SampleIndicatorId, Guid SampleId, DateTime? TransferDate, DateTime? RequestDate, Guid? TestMethodId, bool IsSharedSample, Guid IndicatorId, Guid? SpecimenTypeId)>>();

        // Track which samples are shared samples and which labs they have
        var sharedSampleLabs = new Dictionary<Guid, HashSet<Guid>>(); // SampleId -> Set of LabIds

        foreach (var sample in samples)
        {
            var indicators = await _sampleIndicatorRepository.GetListAsync(
                i => i.TestRequestSampleId == sample.Id && i.LaboratoryId.HasValue);

            foreach (var indicator in indicators)
            {
                // Skip indicators that are already sent (exist in LaboratoryTaskIndicator)
                if (alreadySentIndicatorIds.Contains(indicator.Id))
                {
                    continue;
                }

                var labId = indicator.LaboratoryId!.Value;
                if (!indicatorsByLab.ContainsKey(labId))
                {
                    indicatorsByLab[labId] = new List<(Guid, Guid, DateTime?, DateTime?, Guid?, bool, Guid, Guid?)>();
                }

                indicatorsByLab[labId].Add((indicator.Id, sample.Id, indicator.TransferDate, indicator.RequestDate, indicator.TestMethodId, sample.IsSharedSample, indicator.IndicatorId, sample.SpecimenTypeId));

                // Track labs for shared samples
                if (sample.IsSharedSample)
                {
                    if (!sharedSampleLabs.ContainsKey(sample.Id))
                    {
                        sharedSampleLabs[sample.Id] = new HashSet<Guid>();
                    }
                    sharedSampleLabs[sample.Id].Add(labId);
                }
            }
        }

        // Nếu không có indicators mới để gửi (tất cả đã gửi), return early
        if (!indicatorsByLab.Any())
        {
            return;
        }

        // Identify Microbiology Lab (Lab Vi Sinh) - Priority lab for shared samples
        // Check by Code = "LVS" or Name contains "Vi sinh" (case-insensitive)
        Guid? microbiologyLabId = null;
        foreach (var labId in indicatorsByLab.Keys)
        {
            var lab = await _laboratoryRepository.GetAsync(labId);
            var labDept = await _departmentRepository.GetAsync(lab.DepartmentId);
            var labName = labDept.Name;
            if (lab.Code?.Equals("LVS", StringComparison.OrdinalIgnoreCase) == true ||
                labName.Contains("Vi sinh", StringComparison.OrdinalIgnoreCase) ||
                labName.Contains("Vi Sinh", StringComparison.OrdinalIgnoreCase))
            {
                microbiologyLabId = labId;
                break;
            }
        }

        // Validate shared samples: If marked as shared but only has 1 lab, auto-correct
        foreach (var (sampleId, labIds) in sharedSampleLabs)
        {
            if (labIds.Count < 2)
            {
                // Auto-correct: Set IsSharedSample = false
                var sample = samples.First(s => s.Id == sampleId);
                sample.IsSharedSample = false;
                await _sampleRepository.UpdateAsync(sample, autoSave: true);
            }
        }

        // Store created/existing tasks for linking PriorLabTaskId
        var createdTasks = new Dictionary<Guid, LaboratoryTask>(); // LabId -> Task

        // Create or reuse LaboratoryTask for each Laboratory
        foreach (var (labId, indicatorList) in indicatorsByLab)
        {
            var laboratory = await _laboratoryRepository.GetAsync(labId);

            // Check if this task has shared sample indicators
            var hasSharedSample = indicatorList.Any(i => i.IsSharedSample);
            var isSharedSampleTask = hasSharedSample && microbiologyLabId.HasValue;

            // Lab Hóa can work in parallel with Lab Vi Sinh - no blocking
            // PriorLabTaskId is still set for tracking sample transfer

            // Kiểm tra xem đã có LaboratoryTask cho TestRequest + Lab này chưa
            var existingTask = existingTasks.FirstOrDefault(t => t.LaboratoryId == labId);
            LaboratoryTask task;

            if (existingTask != null)
            {
                // Gộp vào task đã có - cập nhật SampleCount
                task = existingTask;
                var allSampleIds = new HashSet<Guid>();

                // Lấy sample IDs từ indicators hiện có trong task
                var currentTaskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(
                    ti => ti.LaboratoryTaskId == task.Id);
                foreach (var ti in currentTaskIndicators)
                {
                    var sampleIndicator = await _sampleIndicatorRepository.FindAsync(ti.TestRequestSampleIndicatorId);
                    if (sampleIndicator != null)
                    {
                        allSampleIds.Add(sampleIndicator.TestRequestSampleId);
                    }
                }

                // Thêm sample IDs từ indicators mới
                foreach (var ind in indicatorList)
                {
                    allSampleIds.Add(ind.SampleId);
                }

                task.TotalSampleCount = allSampleIds.Count;
                // New indicators require QLKT re-evaluation regardless of current task status
                task.Status = LaboratoryTaskStatus.UnderEvaluation;
                await _laboratoryTaskRepository.UpdateAsync(task, autoSave: true);
            }
            else
            {
                // Tạo task mới
                var sampleCount = indicatorList.Select(i => i.SampleId).Distinct().Count();
                var firstIndicator = indicatorList.First();

                task = new LaboratoryTask(
                    GuidGenerator.Create(),
                    testRequest.Id,
                    labId,
                    testRequest.ContractNumber,
                    sampleCount,
                    laboratory.TechnicalManagerId,
                    firstIndicator.TransferDate,
                    firstIndicator.RequestDate
                );

                // Set shared sample flags - Lab Hóa không bị block, có thể làm song song
                task.IsSharedSampleTask = isSharedSampleTask;
                // IsWaitingForPriorLab = false - Lab Hóa làm song song với Vi Sinh
                // Status không cần set WaitingSharedSample - mặc định là UnderEvaluation

                // Set SampleSentDate from TestRequest so task page can display the send date
                task.SampleSentDate = testRequest.SampleSentDate;

                await _laboratoryTaskRepository.InsertAsync(task, autoSave: true);
            }

            createdTasks[labId] = task;

            // Create junction records linking LaboratoryTask to TestRequestSampleIndicator
            foreach (var indicator in indicatorList)
            {
                var taskIndicator = new LaboratoryTaskIndicator(
                    GuidGenerator.Create(),
                    task.Id,
                    indicator.SampleIndicatorId
                );

                // Lab Hóa làm song song - không cần set IsWaitingForSharedSample = true

                // Copy TestMethodId and Unit from TestRequestSampleIndicator if available
                if (indicator.TestMethodId.HasValue)
                {
                    taskIndicator.MethodId = indicator.TestMethodId;
                    var testMethod = await _testMethodRepository.FindAsync(indicator.TestMethodId.Value);
                    if (testMethod != null)
                    {
                        var tmi = await _testMethodIndicatorRepository.FirstOrDefaultAsync(x =>
                            x.TestMethodId == testMethod.Id && x.IndicatorId == indicator.IndicatorId);
                        taskIndicator.Unit = tmi?.FormattedUnit ?? tmi?.Unit
                            ?? testMethod.FormattedUnit ?? testMethod.Unit ?? "";
                    }
                }

                // Auto-set StandardId - lookup first standard for this indicator + method + specimenType
                var standardIndicator = await _standardIndicatorRepository.FirstOrDefaultAsync(
                    si => si.IndicatorId == indicator.IndicatorId
                          && si.IsActive
                          && (!indicator.TestMethodId.HasValue || si.TestMethodId == indicator.TestMethodId.Value)
                          && (!indicator.SpecimenTypeId.HasValue || !si.SpecimenTypeId.HasValue || si.SpecimenTypeId == indicator.SpecimenTypeId.Value));
                if (standardIndicator != null)
                {
                    taskIndicator.StandardId = standardIndicator.StandardId;
                }

                await _laboratoryTaskIndicatorRepository.InsertAsync(taskIndicator, autoSave: true);
            }
        }

        // Link PriorLabTaskId for non-microbiology tasks (for sample transfer tracking)
        if (microbiologyLabId.HasValue && createdTasks.ContainsKey(microbiologyLabId.Value))
        {
            var microbiologyTask = createdTasks[microbiologyLabId.Value];
            foreach (var (labId, task) in createdTasks)
            {
                // Link all non-Vi Sinh shared sample tasks to Vi Sinh task for tracking
                if (task.IsSharedSampleTask && labId != microbiologyLabId && !task.PriorLabTaskId.HasValue)
                {
                    task.PriorLabTaskId = microbiologyTask.Id;
                    await _laboratoryTaskRepository.UpdateAsync(task, autoSave: true);
                }
            }
        }

        // Final safety net: recalculate TotalSampleCount for ALL tasks of this test request
        // Delete tasks that have no indicators left (all moved to another lab)
        var allTasks = await _laboratoryTaskRepository.GetListAsync(t => t.TestRequestId == testRequest.Id);
        foreach (var t in allTasks)
        {
            var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(
                ti => ti.LaboratoryTaskId == t.Id);

            if (taskIndicators.Count == 0)
            {
                await _laboratoryTaskRepository.DeleteAsync(t, autoSave: true);
                continue;
            }

            var distinctSampleIds = new HashSet<Guid>();
            foreach (var ti in taskIndicators)
            {
                var si = await _sampleIndicatorRepository.FindAsync(ti.TestRequestSampleIndicatorId);
                if (si != null) distinctSampleIds.Add(si.TestRequestSampleId);
            }
            if (t.TotalSampleCount != distinctSampleIds.Count)
            {
                t.TotalSampleCount = distinctSampleIds.Count;
                await _laboratoryTaskRepository.UpdateAsync(t, autoSave: true);
            }
        }
    }

    #region Status Validation Helpers

    /// <summary>
    /// Check if full edit is allowed (all fields)
    /// [TEMPORARY] KH yêu cầu cho phép edit tất cả trạng thái trừ Completed & Cancelled
    /// </summary>
    private bool CanEditFullInfo(TestRequestStatus status)
    {
        // [OLD LOGIC] New, PendingPayment, Paid can be fully edited
        // return status == TestRequestStatus.New ||
        //        status == TestRequestStatus.PendingPayment ||
        //        status == TestRequestStatus.Paid;

        // [TEMPORARY] Cho phép edit tất cả trạng thái trừ Completed & Cancelled
        return status != TestRequestStatus.Completed &&
               status != TestRequestStatus.Cancelled;
    }

    /// <summary>
    /// Check if limited edit is allowed (some fields locked)
    /// [TEMPORARY] Tạm tắt - không cần approval, edit trực tiếp
    /// </summary>
    private bool CanEditLimitedInfo(TestRequestStatus status)
    {
        // [OLD LOGIC] For SampleSent and InProgress: limited edit with QLCL approval required
        // return status == TestRequestStatus.SampleSent || status == TestRequestStatus.InProgress;

        // [TEMPORARY] Không cần limited edit nữa
        return false;
    }

    /// <summary>
    /// Check if delete is allowed
    /// New, PendingPayment, Paid can be deleted
    /// </summary>
    private bool CanDelete(TestRequestStatus status)
    {
        return status == TestRequestStatus.New ||
               status == TestRequestStatus.PendingPayment ||
               status == TestRequestStatus.Paid;
    }

    /// <summary>
    /// Get status label for error messages
    /// </summary>
    private string GetStatusLabel(TestRequestStatus status)
    {
        return status switch
        {
            TestRequestStatus.New => "Mới",
            TestRequestStatus.PendingPayment => "Chờ thanh toán",
            TestRequestStatus.Paid => "Đã thanh toán",
            TestRequestStatus.SampleSent => "Đã gửi mẫu",
            TestRequestStatus.InProgress => "Đang thử nghiệm",
            TestRequestStatus.PendingChangeApproval => "Chờ phê duyệt thay đổi",
            TestRequestStatus.PendingApproval => "Chờ phê duyệt",
            TestRequestStatus.Approved => "Đã phê duyệt",
            TestRequestStatus.ResultSent => "Gửi khách hàng",
            TestRequestStatus.Completed => "Hoàn thành",
            TestRequestStatus.Cancelled => "Hủy phiếu yêu cầu",
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
            throw new Common.Exceptions.InvalidOperationException(
                $"Không thể thay đổi các trường sau khi đang thử nghiệm: {string.Join(", ", errors)}", "fields"
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

    /// <summary>
    /// Determine the appropriate TestRequestStatus based on indicator approval statuses.
    /// Returns Completed if all indicators are approved or cancelled.
    /// Otherwise returns status based on priority:
    /// 1. Approved (if any indicator is approved)
    /// 2. PendingApproval (if any indicator is entered/waiting for approval)
    /// 3. InProgress (if any indicator is pending/testing)
    /// 4. SampleSent (fallback)
    /// </summary>
    private async Task<TestRequestStatus> DetermineStatusBasedOnIndicatorsAsync(Guid testRequestId)
    {
        // Get all sample IDs for this test request
        var samples = await _sampleRepository.GetListAsync(s => s.TestRequestId == testRequestId && !s.IsCancelled);
        var sampleIds = samples.Select(s => s.Id).ToList();

        if (!sampleIds.Any())
        {
            // No samples - can complete
            return TestRequestStatus.Completed;
        }

        // Get all sample indicators for these samples
        var sampleIndicatorIds = await _sampleIndicatorRepository.GetListAsync(si => sampleIds.Contains(si.TestRequestSampleId));
        var sampleIndicatorIdList = sampleIndicatorIds.Select(si => si.Id).ToList();

        if (!sampleIndicatorIdList.Any())
        {
            // No indicators - can complete
            return TestRequestStatus.Completed;
        }

        // Get all laboratory task indicators for these sample indicators
        var labTaskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(
            lti => sampleIndicatorIdList.Contains(lti.TestRequestSampleIndicatorId));

        if (!labTaskIndicators.Any())
        {
            // No lab task indicators - can complete (samples haven't been sent to labs yet)
            return TestRequestStatus.Completed;
        }

        // Get all quality management indicators for the lab task indicators that were sent to QLCL
        var labTaskIndicatorIds = labTaskIndicators
            .Where(lti => lti.IsSentToQuality)
            .Select(lti => lti.Id)
            .ToList();

        var qualityIndicators = labTaskIndicatorIds.Any()
            ? await _qualityIndicatorRepository.GetListAsync(qi => labTaskIndicatorIds.Contains(qi.LaboratoryTaskIndicatorId))
            : new List<QualityManagementIndicator>();

        // Create a lookup for quick access to quality indicator status
        var qualityIndicatorLookup = qualityIndicators.ToDictionary(qi => qi.LaboratoryTaskIndicatorId, qi => qi);

        // Check indicator statuses
        var hasApproved = false;
        var hasEntered = false;
        var hasPending = false;
        var allCompletedOrCancelled = true;

        foreach (var indicator in labTaskIndicators)
        {
            switch (indicator.ApprovalStatus)
            {
                case IndicatorApprovalStatus.Approved:
                    // QLKT đã duyệt - kiểm tra đã gửi sang QLCL chưa
                    if (!indicator.IsSentToQuality)
                    {
                        // QLKT duyệt nhưng chưa gửi sang QLCL - chưa hoàn thành
                        allCompletedOrCancelled = false;
                        hasApproved = true;
                        continue;
                    }

                    // Đã gửi sang QLCL - kiểm tra QLCL đã duyệt và gửi kết quả chưa
                    if (qualityIndicatorLookup.TryGetValue(indicator.Id, out var qualityIndicator))
                    {
                        // Kiểm tra QLCL đã duyệt chưa
                        if (qualityIndicator.ApprovalStatus != QualityIndicatorApprovalStatus.Approved)
                        {
                            // QLCL chưa duyệt - chưa hoàn thành
                            allCompletedOrCancelled = false;
                            hasApproved = true;
                            continue;
                        }

                        // QLCL đã duyệt - kiểm tra đã gửi kết quả chưa (kiểm tra ở cấp indicator)
                        if (!qualityIndicator.IsResultSent)
                        {
                            // QLCL duyệt nhưng chưa gửi kết quả - chưa hoàn thành
                            allCompletedOrCancelled = false;
                            hasApproved = true;
                            continue;
                        }
                    }
                    hasApproved = true;
                    break;
                case IndicatorApprovalStatus.Entered:
                    hasEntered = true;
                    allCompletedOrCancelled = false;
                    break;
                case IndicatorApprovalStatus.Pending:
                    hasPending = true;
                    allCompletedOrCancelled = false;
                    break;
                case IndicatorApprovalStatus.Rejected:
                    // Rejected counts as not completed
                    allCompletedOrCancelled = false;
                    break;
                case IndicatorApprovalStatus.CustomerCancelled:
                    // Customer cancelled is considered completed (no action needed)
                    break;
            }
        }

        // If all indicators are approved (including QLCL approval) or cancelled, allow completion
        if (allCompletedOrCancelled)
        {
            return TestRequestStatus.Completed;
        }

        // Determine previous status based on priority
        if (hasApproved)
        {
            return TestRequestStatus.Approved;
        }
        if (hasEntered)
        {
            return TestRequestStatus.PendingApproval;
        }
        if (hasPending)
        {
            return TestRequestStatus.InProgress;
        }

        // Fallback to SampleSent
        return TestRequestStatus.SampleSent;
    }

    #endregion

    #region Status Transition Methods

    /// <summary>
    /// When printing test request form - log and verify status
    /// In new flow: print is allowed when status = Paid
    /// </summary>
    public async Task PrintTestRequestAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);

        Logger.LogInformation("PrintTestRequestAsync: TestRequest {Id}, Status = {Status}, CreatorId = {CreatorId}",
            id, entity.Status, entity.CreatorId);

        // In new flow, printing is available when Paid
        // We don't change status here, just log that printing occurred
        // Status changes: Paid → (gửi mẫu) → SampleSent
    }

    /// <summary>
    /// When user confirms sample sent - update status to SampleSent
    /// Also creates LaboratoryTasks for all samples grouped by Laboratory
    /// </summary>
    public async Task SendSampleAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);

        // Không còn kiểm tra trạng thái Paid - cho phép gửi mẫu từ nhiều trạng thái

        entity.Status = TestRequestStatus.SampleSent;
        entity.SampleSentDate = Clock.Now; // Lưu ngày gửi mẫu (ngày lưu kho)
        entity.SampleSentByUserId = _currentUser.Id;
        entity.SampleSentByUserName = _currentUser.Name ?? _currentUser.UserName;
        await Repository.UpdateAsync(entity);

        // Update SampleSentDate for all existing LaboratoryTasks of this TestRequest
        var existingTasks = await _laboratoryTaskRepository.GetListAsync(t => t.TestRequestId == entity.Id);
        foreach (var t in existingTasks)
        {
            t.SampleSentDate = entity.SampleSentDate;
            await _laboratoryTaskRepository.UpdateAsync(t, autoSave: true);
        }

        // Create LaboratoryTasks grouped by Laboratory when sample is sent
        await CreateLaboratoryTasksAsync(entity);

        // Notify QLKT about sample sent (Case 2)
        await NotifyTechnicalManagersOnSampleSentAsync(entity);
    }

    /// <summary>
    /// Notify technical managers when sample is sent to their labs
    /// </summary>
    private async Task NotifyTechnicalManagersOnSampleSentAsync(TestRequest testRequest)
    {
        // Get all laboratory tasks for this test request
        var labTasks = await _laboratoryTaskRepository.GetListAsync(lt => lt.TestRequestId == testRequest.Id);

        foreach (var labTask in labTasks)
        {
            if (labTask.TechnicalManagerId.HasValue)
            {
                // Count indicators for this lab task
                var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(
                    ti => ti.LaboratoryTaskId == labTask.Id);
                var indicatorCount = taskIndicators.Count;

                await _notificationCreator.NotifySampleSentToLabAsync(
                    labTask.TechnicalManagerId.Value,
                    testRequest.ContractNumber ?? "",
                    labTask.Id,
                    indicatorCount,
                    labTask.RequestCompletionDate
                );
            }
        }
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
            entity.InProgressDate = Clock.Now;
            entity.InProgressByUserId = _currentUser.Id;
            entity.InProgressByUserName = _currentUser.Name ?? _currentUser.UserName;
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
            entity.PendingApprovalDate = Clock.Now;
            entity.PendingApprovalByUserId = _currentUser.Id;
            entity.PendingApprovalByUserName = _currentUser.Name ?? _currentUser.UserName;
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
            throw new Common.Exceptions.InvalidOperationException("Chỉ có thể phê duyệt khi trạng thái là 'Chờ Phê duyệt'", "status");
        }

        entity.Status = TestRequestStatus.Approved;
        entity.ApprovedDate = Clock.Now;
        entity.ApprovedByUserId = _currentUser.Id;
        entity.ApprovedByUserName = _currentUser.Name ?? _currentUser.UserName;
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
            throw new Common.Exceptions.InvalidOperationException("Chỉ có thể từ chối kết quả khi trạng thái là 'Chờ Phê duyệt'", "status");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ValidationException("reason", "Vui lòng nhập lý do từ chối");
        }

        // Update TestRequest status back to InProgress for QLKT to review
        entity.Status = TestRequestStatus.InProgress;
        entity.ResultRejectionReason = reason;
        await Repository.UpdateAsync(entity);

        // Also update all LaboratoryTask status - recalculate based on indicator status
        var laboratoryTasks = await _laboratoryTaskRepository.GetListAsync(t => t.TestRequestId == id);
        foreach (var task in laboratoryTasks)
        {
            // Only update tasks that have sent results (SentToQuality status)
            if (task.Status == LaboratoryTaskStatus.SentToQuality)
            {
                // Set back to Approved since QLCL rejected - QLKT needs to review and resend
                task.Status = LaboratoryTaskStatus.Approved;
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
            throw new Common.Exceptions.InvalidOperationException("Chỉ có thể gửi kết quả khi đã được phê duyệt", "status");
        }

        entity.Status = TestRequestStatus.ResultSent;
        entity.ResultSentDate = Clock.Now;
        entity.ResultSentByUserId = _currentUser.Id;
        entity.ResultSentByUserName = _currentUser.Name ?? _currentUser.UserName;
        await Repository.UpdateAsync(entity);

        // Notify the test request creator - Case 4 SampleReceiver (first notification)
        if (entity.CreatorId.HasValue)
        {
            await _notificationCreator.NotifySampleReturnReminderAsync(
                entity.CreatorId.Value,
                entity.ContractNumber ?? "",
                entity.Id);
        }

        // TODO: Send email to customer if requested
    }

    /// <summary>
    /// Complete the test request - update status to Completed
    /// If not all indicators are approved, update to previous status based on priority:
    /// 1. Approved (if any indicator is approved)
    /// 2. PendingApproval (if any indicator is entered)
    /// 3. InProgress (if any indicator is pending/testing)
    /// 4. SampleSent (fallback)
    /// </summary>
    public async Task CompleteAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);

        if (entity.Status != TestRequestStatus.ResultSent)
        {
            throw new Common.Exceptions.InvalidOperationException("Chỉ có thể hoàn thành khi đã gửi kết quả", "status");
        }

        // Check if all indicators are completed (Approved)
        var newStatus = await DetermineStatusBasedOnIndicatorsAsync(id);

        if (newStatus == TestRequestStatus.Completed)
        {
            // All indicators are approved - complete normally
            entity.Status = TestRequestStatus.Completed;
            entity.CompletedDate = Clock.Now;
            entity.CompletedByUserId = _currentUser.Id;
            entity.CompletedByUserName = _currentUser.Name ?? _currentUser.UserName;
        }
        else
        {
            // Not all indicators completed - revert to previous status
            entity.Status = newStatus;
        }

        await Repository.UpdateAsync(entity);
    }

    /// <summary>
    /// Complete test request with postal delivery images
    /// Used when DeliveryPostal = true
    /// If not all indicators are approved, update to previous status based on priority
    /// </summary>
    public async Task CompleteWithImageAsync(Guid id, CompleteWithImageDto input)
    {
        var entity = await Repository.GetAsync(id);

        if (entity.Status != TestRequestStatus.ResultSent)
        {
            throw new Common.Exceptions.InvalidOperationException("Chỉ có thể hoàn thành khi đã gửi kết quả", "status");
        }

        if (!entity.DeliveryPostal)
        {
            throw new Common.Exceptions.InvalidOperationException("Phiếu yêu cầu này không yêu cầu gửi bưu điện", "deliveryPostal");
        }

        if (input.ImageFileIds == null || input.ImageFileIds.Count == 0)
        {
            throw new ValidationException("imageFileIds", "Phải có ít nhất 1 hình ảnh");
        }

        if (input.ImageFileIds.Count > 5)
        {
            throw new ValidationException("imageFileIds", "Tối đa 5 hình ảnh");
        }

        // Save image file IDs as JSON array
        entity.PostalDeliveryImageFileIds = JsonSerializer.Serialize(input.ImageFileIds);

        // Check if all indicators are completed (Approved)
        var newStatus = await DetermineStatusBasedOnIndicatorsAsync(id);

        if (newStatus == TestRequestStatus.Completed)
        {
            // All indicators are approved - complete normally
            entity.Status = TestRequestStatus.Completed;
            entity.CompletedDate = Clock.Now;
            entity.CompletedByUserId = _currentUser.Id;
            entity.CompletedByUserName = _currentUser.Name ?? _currentUser.UserName;
        }
        else
        {
            // Not all indicators completed - revert to previous status
            entity.Status = newStatus;
        }

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
            throw new Common.Exceptions.InvalidOperationException("Chỉ có thể phê duyệt thay đổi khi trạng thái là 'Chờ phê duyệt thay đổi'", "status");
        }

        // Get pending change history
        var pendingChanges = await _changeHistoryRepository.GetListAsync(
            c => c.TestRequestId == id && c.ApprovalStatus == ChangeApprovalStatus.Pending);

        if (!pendingChanges.Any())
        {
            throw new Common.Exceptions.EntityNotFoundException("ChangeHistory", id);
        }

        var currentUserId = _currentUser.Id ?? throw new Common.Exceptions.UnauthorizedAccessException("Người dùng chưa đăng nhập");
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

        // TODO: QLCL notification will be added later
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

        // Sync ContractNumber if it was changed
        if (changes.Any(c => c.FieldName?.ToLower() == SortingFields.ContractNumber))
        {
            await SyncContractNumberToRelatedTasksAsync(entity.Id, entity.ContractNumber);
        }
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
            case SortingFields.ContractNumber:
                entity.ContractNumber = newValue;
                break;
            case SortingFields.ContractDate:
                entity.ContractDate = string.IsNullOrEmpty(newValue) ? null : DateTime.Parse(newValue);
                break;
            case SortingFields.CustomerName:
                entity.CustomerName = newValue ?? "";
                break;
            case SortingFields.Address:
                entity.Address = newValue;
                break;
            case SortingFields.TaxCode:
                entity.TaxCode = newValue;
                break;
            case SortingFields.ContactPerson:
                entity.ContactPerson = newValue;
                break;
            case SortingFields.Phone:
                entity.Phone = newValue;
                break;
            case SortingFields.ProvinceCode:
                entity.ProvinceCode = newValue;
                break;
            case SortingFields.Province:
                entity.Province = newValue;
                break;
            case SortingFields.DistrictCode:
                entity.DistrictCode = newValue;
                break;
            case SortingFields.District:
                entity.District = newValue;
                break;
            case SortingFields.WardCode:
                entity.WardCode = newValue;
                break;
            case SortingFields.Ward:
                entity.Ward = newValue;
                break;
            case SortingFields.Notes:
                entity.Notes = newValue;
                break;
            case SortingFields.ExpectedCompletionDate:
                entity.ExpectedCompletionDate = string.IsNullOrEmpty(newValue) ? null : DateTime.Parse(newValue);
                break;
            case SortingFields.ResultDeliveryDate:
                entity.ResultDeliveryDate = string.IsNullOrEmpty(newValue) ? null : DateTime.Parse(newValue);
                break;
            case SortingFields.TotalCost:
                entity.TotalCost = string.IsNullOrEmpty(newValue) ? 0 : decimal.Parse(newValue);
                break;
            case SortingFields.TotalCostInWords:
                entity.TotalCostInWords = newValue;
                break;
            case SortingFields.MethodWestHighlandInstitute:
                entity.MethodWestHighlandInstitute = newValue?.ToLower() == "true";
                break;
            case SortingFields.MethodCustomer:
                entity.MethodCustomer = newValue?.ToLower() == "true";
                break;
            case SortingFields.MethodCustomerDetail:
                entity.MethodCustomerDetail = newValue;
                break;
            case SortingFields.ResultLanguage:
                entity.ResultLanguage = string.IsNullOrEmpty(newValue) ? ResultLanguage.Vietnamese : (ResultLanguage)int.Parse(newValue);
                break;
            case SortingFields.ResultCopyCount:
                entity.ResultCopyCount = newValue;
                break;
            case SortingFields.SeparatePassedResults:
                entity.SeparatePassedResults = newValue?.ToLower() == "true";
                break;
            case SortingFields.ConformityDeclaration:
                entity.ConformityDeclaration = newValue;
                break;
            case SortingFields.OtherRequirements:
                entity.OtherRequirements = newValue;
                break;
            case SortingFields.Subcontract:
                entity.SubContract = newValue;
                break;
            case SortingFields.SamplingRequirement:
                entity.SamplingRequirement = string.IsNullOrEmpty(newValue) ? SamplingRequirement.CustomerSampling : (SamplingRequirement)int.Parse(newValue);
                break;
            case SortingFields.DeliveryDirectPickup:
                entity.DeliveryDirectPickup = newValue?.ToLower() == "true";
                break;
            case SortingFields.DeliveryPostal:
                entity.DeliveryPostal = newValue?.ToLower() == "true";
                break;
            case SortingFields.ReturnStoredSample:
                entity.ReturnStoredSample = newValue?.ToLower() == "true";
                break;
            case SortingFields.SampleTypeService:
                entity.SampleTypeService = newValue?.ToLower() == "true";
                break;
            case SortingFields.SampleTypeContract:
                entity.SampleTypeContract = newValue?.ToLower() == "true";
                break;
            case SortingFields.SampleTypeTask:
                entity.SampleTypeTask = newValue?.ToLower() == "true";
                break;
            case SortingFields.SampleTypeSubcontract:
                entity.SampleTypeSubContract = newValue?.ToLower() == "true";
                break;
            case SortingFields.SampleStorage:
                entity.SampleStorage = string.IsNullOrEmpty(newValue) ? SampleStorageOption.No : (SampleStorageOption)int.Parse(newValue);
                break;
            case SortingFields.ReceiptNumber:
                entity.ReceiptNumber = newValue;
                break;
            case SortingFields.SentToAccounting:
                entity.SentToAccounting = newValue?.ToLower() == "true";
                break;
            case SortingFields.PaymentReceived:
                entity.PaymentReceived = newValue?.ToLower() == "true";
                break;
            case SortingFields.SentToDepartment:
                entity.SentToDepartment = newValue?.ToLower() == "true";
                break;
            case SortingFields.ResultReturned:
                entity.ResultReturned = newValue?.ToLower() == "true";
                break;
            case SortingFields.AgreeTerms:
                entity.AgreeTerms = newValue?.ToLower() == "true";
                break;
            case SortingFields.ChangeRequest:
                entity.ChangeRequest = newValue;
                break;
            case SortingFields.SupervisorId:
                entity.SupervisorId = string.IsNullOrEmpty(newValue) ? null : Guid.Parse(newValue);
                break;
            case SortingFields.DirectorId:
                entity.DirectorId = string.IsNullOrEmpty(newValue) ? null : Guid.Parse(newValue);
                break;
            case SortingFields.Subcontractor:
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

        // Track cancelled samples/indicators for notification (Case 8.2)
        var cancelledSampleCodes = new List<string>();
        var cancelledIndicatorCountBySample = new Dictionary<string, int>();

        // Handle samples not in new list - mark as CustomerCancelled instead of deleting
        foreach (var existingSample in existingSamples)
        {
            if (!newSampleIds.Contains(existingSample.Id))
            {
                // Get all indicators for this sample
                var indicators = await _sampleIndicatorRepository.GetListAsync(i => i.TestRequestSampleId == existingSample.Id);
                bool hasLabTask = false;
                int cancelledIndicatorCount = 0;

                foreach (var indicator in indicators)
                {
                    // Check if indicator has LaboratoryTaskIndicator
                    var labTaskIndicator = await _laboratoryTaskIndicatorRepository.FirstOrDefaultAsync(
                        lti => lti.TestRequestSampleIndicatorId == indicator.Id
                    );

                    if (labTaskIndicator != null)
                    {
                        hasLabTask = true;
                        // Không cho phép hủy mẫu có chỉ tiêu đã có kết quả
                        if (labTaskIndicator.ApprovalStatus == IndicatorApprovalStatus.Entered ||
                            labTaskIndicator.ApprovalStatus == IndicatorApprovalStatus.Approved ||
                            labTaskIndicator.ApprovalStatus == IndicatorApprovalStatus.Rejected)
                        {
                            throw new Common.Exceptions.InvalidOperationException("Không thể xóa mẫu có chỉ tiêu đang được thử nghiệm hoặc đã có kết quả.", "sample");
                        }
                        // Đánh dấu CustomerCancelled thay vì xóa
                        labTaskIndicator.ApprovalStatus = IndicatorApprovalStatus.CustomerCancelled;
                        await _laboratoryTaskIndicatorRepository.UpdateAsync(labTaskIndicator, autoSave: true);
                        cancelledIndicatorCount++;
                    }
                    else
                    {
                        // Nếu chưa có LaboratoryTaskIndicator (chưa gửi mẫu) thì xóa hẳn
                        await _sampleIndicatorRepository.DeleteAsync(indicator, autoSave: true);
                    }
                }

                // If sample has LaboratoryTask, keep sample for history (mark as customer cancelled)
                // Only delete sample if no LaboratoryTask exists (sample not yet sent)
                if (!hasLabTask)
                {
                    await _sampleRepository.DeleteAsync(existingSample);
                }
                else
                {
                    // Mark sample as customer cancelled (KH hủy sau khi đã gửi mẫu)
                    existingSample.IsCustomerCancelled = true;
                    existingSample.CustomerCancelledDate = DateTime.UtcNow;
                    await _sampleRepository.UpdateAsync(existingSample, autoSave: true);

                    // Track for notification (Case 8.2)
                    cancelledSampleCodes.Add(existingSample.SampleCode ?? existingSample.SampleName ?? "");
                    cancelledIndicatorCountBySample[existingSample.SampleCode ?? existingSample.SampleName ?? ""] = cancelledIndicatorCount;
                }
            }
        }

        decimal totalCost = 0;
        int sortOrder = 0;

        // Track new samples/indicators for notification (Case 8.1)
        int newSampleCount = 0;
        int newIndicatorCount = 0;

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
                sample.StatusText = sampleDto.StatusText;
                sample.ProcessingStatus = sampleDto.ProcessingStatus;
                sample.Notes = sampleDto.Notes;
                sample.StorageLocation = sampleDto.StorageLocation;
                sample.IsSharedSample = sampleDto.IsSharedSample;
                // Resolve SpecimenTypeName to ID (create new if not exists)
                sample.SpecimenTypeId = await ResolveSpecimenTypeIdAsync(sampleDto.SpecimenTypeName, sampleDto.SpecimenTypeId);
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
                sample.StatusText = sampleDto.StatusText;
                sample.ProcessingStatus = sampleDto.ProcessingStatus;
                sample.Notes = sampleDto.Notes;
                sample.StorageLocation = sampleDto.StorageLocation;
                sample.IsSharedSample = sampleDto.IsSharedSample;
                // Resolve SpecimenTypeName to ID (create new if not exists)
                sample.SpecimenTypeId = await ResolveSpecimenTypeIdAsync(sampleDto.SpecimenTypeName, sampleDto.SpecimenTypeId);
                await _sampleRepository.InsertAsync(sample, autoSave: true);

                // Track for notification (Case 8.1)
                newSampleCount++;
            }

            // Update indicators
            var existingIndicators = await _sampleIndicatorRepository.GetListAsync(i => i.TestRequestSampleId == sample.Id);
            var newIndicatorIds = sampleDto.Indicators?.Where(i => i.Id != null).Select(i => i.Id!.Value).ToList() ?? new List<Guid>();

            // Handle indicators not in new list - mark as CustomerCancelled instead of deleting
            foreach (var existingIndicator in existingIndicators)
            {
                if (!newIndicatorIds.Contains(existingIndicator.Id))
                {
                    var labTaskIndicator = await _laboratoryTaskIndicatorRepository.FirstOrDefaultAsync(
                        lti => lti.TestRequestSampleIndicatorId == existingIndicator.Id
                    );

                    if (labTaskIndicator != null)
                    {
                        // Không cho phép hủy chỉ tiêu đã có kết quả (Entered, Approved, Rejected)
                        if (labTaskIndicator.ApprovalStatus == IndicatorApprovalStatus.Entered ||
                            labTaskIndicator.ApprovalStatus == IndicatorApprovalStatus.Approved ||
                            labTaskIndicator.ApprovalStatus == IndicatorApprovalStatus.Rejected)
                        {
                            throw new Common.Exceptions.InvalidOperationException("Không thể xóa chỉ tiêu đang được thử nghiệm hoặc đã có kết quả.", "indicator");
                        }
                        // Đánh dấu CustomerCancelled thay vì xóa (sau khi đã gửi mẫu)
                        labTaskIndicator.ApprovalStatus = IndicatorApprovalStatus.CustomerCancelled;
                        await _laboratoryTaskIndicatorRepository.UpdateAsync(labTaskIndicator, autoSave: true);
                    }
                    else
                    {
                        // Nếu chưa có LaboratoryTaskIndicator (chưa gửi mẫu) thì xóa hẳn
                        await _sampleIndicatorRepository.DeleteAsync(existingIndicator, autoSave: true);
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
                        indicator.OriginalUnitPrice = indicatorDto.OriginalUnitPrice;
                        indicator.Notes = indicatorDto.Notes;
                        indicator.LaboratoryId = indicatorDto.LaboratoryId;
                        indicator.TestMethodId = indicatorDto.TestMethodId;
                        indicator.SpecimenTypeId = indicatorDto.SpecimenTypeId;
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
                        indicator.OriginalUnitPrice = indicatorDto.OriginalUnitPrice;
                        indicator.Notes = indicatorDto.Notes;
                        indicator.TestMethodId = indicatorDto.TestMethodId;
                        indicator.SpecimenTypeId = indicatorDto.SpecimenTypeId;
                        indicator.LaboratoryId = indicatorDto.LaboratoryId;
                        indicator.IsSubcontract = indicatorDto.IsSubcontract;

                        await _sampleIndicatorRepository.InsertAsync(indicator, autoSave: true);

                        // Collect new indicators for batch task creation
                        // Create LaboratoryTaskIndicator for new indicators when TestRequest is SampleSent or InProgress
                        // (i.e., when the sample has already been sent to laboratory)
                        if ((previousStatus == TestRequestStatus.SampleSent || previousStatus == TestRequestStatus.InProgress)
                            && indicatorDto.LaboratoryId.HasValue)
                        {
                            newIndicatorsForTask.Add(indicator);
                        }

                        // Track for notification (Case 8.1)
                        newIndicatorCount++;
                    }
                    sampleIndicatorCost += indicatorDto.UnitPrice;
                }
            }

            sample.TotalIndicatorCost = sampleIndicatorCost;
            await _sampleRepository.UpdateAsync(sample, autoSave: true);
            totalCost += sampleIndicatorCost;

            // Update attachments for sample
            if (sampleDto.Attachments != null)
            {
                // Get existing attachments
                var existingAttachments = await _attachmentRepository.GetListAsync(a => a.TestRequestSampleId == sample.Id);
                var existingFileIds = existingAttachments.Select(a => a.FileId).ToList();
                var inputFileIds = sampleDto.Attachments.Select(a => a.FileId).ToList();

                // Delete attachments not in input
                foreach (var existingAttachment in existingAttachments)
                {
                    if (!inputFileIds.Contains(existingAttachment.FileId))
                    {
                        await _attachmentRepository.DeleteAsync(existingAttachment);
                    }
                }

                // Add new attachments
                foreach (var attachmentInput in sampleDto.Attachments)
                {
                    if (!existingFileIds.Contains(attachmentInput.FileId))
                    {
                        var attachment = new TestRequestSampleAttachment(
                            GuidGenerator.Create(),
                            sample.Id,
                            attachmentInput.FileId,
                            attachmentInput.FileName,
                            $"/api/v1/files/{attachmentInput.FileId}",
                            attachmentInput.FileSize
                        );
                        await _attachmentRepository.InsertAsync(attachment);
                    }
                }
            }
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

        // Send notifications for sample/indicator changes (only when SampleSent or InProgress)
        if (previousStatus == TestRequestStatus.SampleSent || previousStatus == TestRequestStatus.InProgress)
        {
            await NotifyTechnicalManagersOnSampleChangesAsync(
                testRequest,
                newSampleCount,
                newIndicatorCount,
                cancelledSampleCodes,
                cancelledIndicatorCountBySample
            );
        }
    }

    /// <summary>
    /// Notify technical managers about sample/indicator changes (Case 8.1 and 8.2)
    /// </summary>
    private async Task NotifyTechnicalManagersOnSampleChangesAsync(
        TestRequest testRequest,
        int newSampleCount,
        int newIndicatorCount,
        List<string> cancelledSampleCodes,
        Dictionary<string, int> cancelledIndicatorCountBySample)
    {
        // Get all laboratory tasks for this test request
        var labTasks = await _laboratoryTaskRepository.GetListAsync(lt => lt.TestRequestId == testRequest.Id);

        // Notify each technical manager with their specific task ID
        foreach (var labTask in labTasks.Where(lt => lt.TechnicalManagerId.HasValue))
        {
            var managerId = labTask.TechnicalManagerId!.Value;

            // Case 8.1: Notify about new samples/indicators
            if (newSampleCount > 0 || newIndicatorCount > 0)
            {
                await _notificationCreator.NotifyNewSamplesAddedAsync(
                    managerId,
                    testRequest.ContractNumber ?? "",
                    labTask.Id,
                    newSampleCount,
                    newIndicatorCount
                );
            }

            // Case 8.2: Notify about cancelled samples/indicators
            foreach (var sampleCode in cancelledSampleCodes)
            {
                var indicatorCount = cancelledIndicatorCountBySample.GetValueOrDefault(sampleCode, 0);
                if (indicatorCount > 0)
                {
                    await _notificationCreator.NotifySamplesCancelledAsync(
                        managerId,
                        testRequest.ContractNumber ?? "",
                        labTask.Id,
                        sampleCode,
                        indicatorCount
                    );
                }
            }
        }
    }

    /// <summary>
    /// Create or update LaboratoryTasks for new indicators when TestRequest is SampleSent or InProgress
    /// Groups indicators by laboratory - adds to existing task if one exists, otherwise creates new task
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

            // Check if there's an existing LaboratoryTask for this TestRequest + Laboratory
            var existingTask = await _laboratoryTaskRepository.FirstOrDefaultAsync(
                t => t.TestRequestId == testRequestId && t.LaboratoryId == laboratoryId
            );

            LaboratoryTask task;
            if (existingTask != null)
            {
                // Use existing task - update sample count
                task = existingTask;

                // Recalculate total sample count for this task (existing + new)
                var existingIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(
                    lti => lti.LaboratoryTaskId == task.Id
                );
                var existingSampleIds = new HashSet<Guid>();
                foreach (var lti in existingIndicators)
                {
                    var sampleIndicator = await _sampleIndicatorRepository.FindAsync(lti.TestRequestSampleIndicatorId);
                    if (sampleIndicator != null)
                    {
                        existingSampleIds.Add(sampleIndicator.TestRequestSampleId);
                    }
                }

                // Add new sample IDs
                var newSampleIds = indicators.Select(i => i.TestRequestSampleId).Distinct();
                foreach (var sampleId in newSampleIds)
                {
                    existingSampleIds.Add(sampleId);
                }

                task.TotalSampleCount = existingSampleIds.Count;
                await _laboratoryTaskRepository.UpdateAsync(task, autoSave: true);
            }
            else
            {
                // Create new LaboratoryTask
                var sampleIds = indicators.Select(i => i.TestRequestSampleId).Distinct().ToList();
                var firstIndicator = indicators.First();

                task = new LaboratoryTask(
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
                // Set SampleSentDate from TestRequest so task page can display the send date
                task.SampleSentDate = testRequest.SampleSentDate;

                await _laboratoryTaskRepository.InsertAsync(task, autoSave: true);
            }

            // Create LaboratoryTaskIndicator for each new indicator
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
                        var tmi = await _testMethodIndicatorRepository.FirstOrDefaultAsync(x =>
                            x.TestMethodId == testMethod.Id && x.IndicatorId == indicator.IndicatorId);
                        taskIndicator.Unit = tmi?.FormattedUnit ?? tmi?.Unit
                            ?? testMethod.FormattedUnit ?? testMethod.Unit ?? "";
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
            throw new Common.Exceptions.InvalidOperationException("Chỉ có thể từ chối thay đổi khi trạng thái là 'Chờ phê duyệt thay đổi'", "status");
        }

        // Get pending change history
        var pendingChanges = await _changeHistoryRepository.GetListAsync(
            c => c.TestRequestId == id && c.ApprovalStatus == ChangeApprovalStatus.Pending);

        if (!pendingChanges.Any())
        {
            throw new Common.Exceptions.EntityNotFoundException("ChangeHistory", id);
        }

        var currentUserId = _currentUser.Id ?? throw new Common.Exceptions.UnauthorizedAccessException("Người dùng chưa đăng nhập");
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

        // TODO: QLCL notification will be added later
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

        // TODO: Implement QLCL notification for ChangeApprovalRequest later

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

        // Send notifications to QLKT (CC) - notify all technical managers
        var technicalManagerIds = labTasks
            .Where(lt => lt.TechnicalManagerId.HasValue)
            .Select(lt => lt.TechnicalManagerId!.Value)
            .Distinct()
            .ToList();

        foreach (var managerId in technicalManagerIds)
        {
            await _notificationCreator.NotifyTestRequestChangeRequestedAsync(
                managerId,
                testRequest.ContractNumber ?? "",
                testRequest.Id);
        }

        // Send notification to QLCL - get all quality managers who have tasks for this test request
        var qualityTasks = await _qualityTaskRepository.GetListAsync(
            t => t.TestRequestId == testRequest.Id && t.AssignedQualityManagerId.HasValue);

        var qualityManagerIds = qualityTasks
            .Select(t => t.AssignedQualityManagerId!.Value)
            .Distinct()
            .ToList();

        foreach (var qmId in qualityManagerIds)
        {
            await _notificationCreator.NotifyTestRequestChangePendingApprovalAsync(
                qmId,
                testRequest.ContractNumber ?? "",
                testRequest.Id);
        }

        // Send notification to BP Giao nhận (SampleReceiver) - test request creator
        // Chỉ gửi nếu creator khác với người đang chỉnh sửa (current user)
        if (testRequest.CreatorId.HasValue && testRequest.CreatorId.Value != CurrentUser.Id)
        {
            await _notificationCreator.NotifyTestRequestChangeRequestedAsync(
                testRequest.CreatorId.Value,
                testRequest.ContractNumber ?? "",
                testRequest.Id);
        }
    }

    /// <summary>
    /// Lấy lịch sử thay đổi của phiếu yêu cầu
    /// </summary>
    public async Task<List<TestRequestChangeHistoryDto>> GetChangeHistoryAsync(Guid id)
    {
        var allChanges = await _changeHistoryRepository.GetListAsync(c => c.TestRequestId == id);
        var changes = allChanges.OrderByDescending(c => c.CreationTime).ToList();

        // Batch fetch all user names
        var allUserIds = changes
            .SelectMany(c => new[] { c.CreatorId, c.ApprovedById })
            .ToList();
        var userNames = await _userNameProvider.GetUserNamesAsync(allUserIds);

        return changes.Select(change => new TestRequestChangeHistoryDto
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
            CreatorId = change.CreatorId,
            CreatorName = change.CreatorId.HasValue && userNames.TryGetValue(change.CreatorId.Value, out var cn) ? cn : null,
            ApprovedByName = change.ApprovedById.HasValue && userNames.TryGetValue(change.ApprovedById.Value, out var an) ? an : null
        }).ToList();
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
            throw new Common.Exceptions.InvalidOperationException(
                $"Không thể xóa phiếu yêu cầu khi trạng thái là '{GetStatusLabel(entity.Status)}'", "status"
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

    /// <summary>
    /// Lấy thông tin tiến trình (timeline) của phiếu yêu cầu
    /// Timeline mới theo workflow:
    /// New → PendingPayment/NoPaymentRequired → Paid → UnderReview → RequestApproved
    /// → SampleSent → InProgress → ReportPublished → Completed
    /// </summary>
    public async Task<TestRequestTimelineDto> GetTimelineAsync(Guid id)
    {
        var testRequest = await Repository.GetAsync(id);

        if (testRequest == null)
        {
            throw new Common.Exceptions.EntityNotFoundException("TestRequest", id);
        }

        var result = new TestRequestTimelineDto
        {
            Id = testRequest.Id,
            Status = testRequest.Status,
            Steps = new List<TimelineStepDto>()
        };

        var currentStatus = testRequest.Status;

        // Get laboratories from sample indicators for SampleSent step (batch loading to avoid N+1)
        var samples = await _sampleRepository.GetListAsync(s => s.TestRequestId == id);
        var sampleIds = samples.Select(s => s.Id).ToList();
        var allIndicators = sampleIds.Count > 0
            ? await _sampleIndicatorRepository.GetListAsync(i => sampleIds.Contains(i.TestRequestSampleId))
            : new List<TestRequestSampleIndicator>();
        var labIds = allIndicators
            .Where(i => i.LaboratoryId.HasValue)
            .Select(i => i.LaboratoryId!.Value)
            .Distinct()
            .ToList();
        var labs = labIds.Count > 0
            ? await _laboratoryRepository.GetListAsync(l => labIds.Contains(l.Id))
            : new List<Laboratory>();
        var deptIds = labs.Select(l => l.DepartmentId).Distinct().ToList();
        var depts = deptIds.Count > 0
            ? await _departmentRepository.GetListAsync(d => deptIds.Contains(d.Id))
            : new List<Department>();
        var laboratories = new List<string>();
        foreach (var lab in labs)
        {
            var dept = depts.FirstOrDefault(d => d.Id == lab.DepartmentId);
            var labName = dept?.Name ?? "";
            if (!string.IsNullOrEmpty(labName) && !laboratories.Contains(labName))
            {
                laboratories.Add(labName);
            }
        }

        // Get receipt for payment date info (PendingDate on TestRequest is never set, Receipt.PaidDate is the real payment date)
        var receipt = await _receiptRepository.FirstOrDefaultAsync(r => r.TestRequestId == id);

        // Build steps from newest to oldest (top to bottom in UI)
        // Sử dụng date fields để xác định step nào đã hoàn thành (vì status enum values không theo thứ tự timeline)
        var isCancelled = currentStatus == TestRequestStatus.Cancelled;

        // Get creator name for fallback (used in New step if no specific data)
        var creatorName = await _userNameProvider.GetUserNameAsync(testRequest.CreatorId);

        // === Step 0: Đã hủy (Cancelled) - chỉ hiện khi đã bị hủy ===
        if (isCancelled)
        {
            result.Steps.Add(new TimelineStepDto
            {
                Title = "Đã hủy",
                Status = "cancelled",
                Date = testRequest.CancelledDate,
                UserId = testRequest.CancelledByUserId,
                UserName = testRequest.CancelledByUserName,
                Note = testRequest.CancellationReason
            });
        }

        // === Step 1: Hoàn thành (Completed) ===
        var isCompleted = currentStatus == TestRequestStatus.Completed;
        result.Steps.Add(new TimelineStepDto
        {
            Title = "Hoàn thành",
            Status = isCompleted ? "completed" : "pending",
            Date = testRequest.CompletedDate,
            UserId = testRequest.CompletedByUserId,
            UserName = testRequest.CompletedByUserName
        });

        // === Step 2: Đã phát hành BC (ReportPublished) - replaces old ResultSent ===
        // Check date field hoặc status
        var isReportPublished = isCancelled
            ? testRequest.ReportPublishedDate.HasValue
            : (currentStatus == TestRequestStatus.ReportPublished ||
               currentStatus == TestRequestStatus.Completed ||
               testRequest.ReportPublishedDate.HasValue);
        result.Steps.Add(new TimelineStepDto
        {
            Title = "Đã phát hành BC",
            Status = isReportPublished ? "completed" : "pending",
            Date = testRequest.ReportPublishedDate,
            UserId = testRequest.ReportPublishedByUserId,
            UserName = testRequest.ReportPublishedByUserName
        });

        // === Step 2.5: Đã duyệt KQ (Approved) - QLCL đã duyệt toàn bộ chỉ tiêu ===
        // ApprovedDate được set khi QLCL duyệt tất cả chỉ tiêu (QualityManagementTask -> PendingPublish)
        var isApproved = isCancelled
            ? testRequest.ApprovedDate.HasValue
            : (testRequest.ApprovedDate.HasValue ||
               currentStatus == TestRequestStatus.Approved ||
               currentStatus == TestRequestStatus.PartialResultSent ||
               currentStatus == TestRequestStatus.ReportPublished ||
               currentStatus == TestRequestStatus.Completed);
        result.Steps.Add(new TimelineStepDto
        {
            Title = "Đã duyệt KQ",
            Status = isApproved ? "completed" : "pending",
            Date = testRequest.ApprovedDate,
            UserId = testRequest.ApprovedByUserId,
            UserName = testRequest.ApprovedByUserName
        });

        // === Step 2.6: Chờ phê duyệt KQ (PendingApproval) - KTV gửi KQ chờ QLCL phê duyệt ===
        var isPendingApproval = isCancelled
            ? testRequest.PendingApprovalDate.HasValue
            : (testRequest.PendingApprovalDate.HasValue ||
               currentStatus == TestRequestStatus.PendingApproval ||
               currentStatus == TestRequestStatus.Approved ||
               currentStatus == TestRequestStatus.PartialResultSent ||
               currentStatus == TestRequestStatus.ReportPublished ||
               currentStatus == TestRequestStatus.Completed);
        result.Steps.Add(new TimelineStepDto
        {
            Title = "Chờ phê duyệt KQ",
            Status = isPendingApproval ? "completed" : "pending",
            Date = testRequest.PendingApprovalDate,
            UserId = testRequest.PendingApprovalByUserId,
            UserName = testRequest.PendingApprovalByUserName
        });

        // === Step 3: Đang thử nghiệm (InProgress) ===
        var isInProgress = isCancelled
            ? testRequest.InProgressDate.HasValue
            : (testRequest.InProgressDate.HasValue ||
               currentStatus == TestRequestStatus.InProgress ||
               currentStatus == TestRequestStatus.PendingApproval ||
               currentStatus == TestRequestStatus.Approved ||
               currentStatus == TestRequestStatus.PendingChangeApproval ||
               currentStatus == TestRequestStatus.PartialResultSent ||
               currentStatus == TestRequestStatus.ReportPublished ||
               currentStatus == TestRequestStatus.Completed);
        result.Steps.Add(new TimelineStepDto
        {
            Title = "Đang thử nghiệm",
            Status = isInProgress ? "completed" : "pending",
            Date = testRequest.InProgressDate,
            UserId = testRequest.InProgressByUserId,
            UserName = testRequest.InProgressByUserName
        });

        // === Step 4: Đã gửi mẫu (SampleSent) ===
        var isSampleSent = isCancelled
            ? testRequest.SampleSentDate.HasValue
            : (testRequest.SampleSentDate.HasValue ||
               currentStatus == TestRequestStatus.SampleSent ||
               currentStatus == TestRequestStatus.InProgress ||
               currentStatus == TestRequestStatus.PendingApproval ||
               currentStatus == TestRequestStatus.Approved ||
               currentStatus == TestRequestStatus.PendingChangeApproval ||
               currentStatus == TestRequestStatus.PartialResultSent ||
               currentStatus == TestRequestStatus.ReportPublished ||
               currentStatus == TestRequestStatus.Completed);
        result.Steps.Add(new TimelineStepDto
        {
            Title = "Đã gửi mẫu",
            Status = isSampleSent ? "completed" : "pending",
            Date = testRequest.SampleSentDate,
            UserId = testRequest.SampleSentByUserId,
            UserName = testRequest.SampleSentByUserName,
            ExtraInfo = isSampleSent && laboratories.Count > 0 ? laboratories : null
        });

        // === Step 5: Đã duyệt yêu cầu (RequestApproved) ===
        // Khi tất cả phòng lab đã xét duyệt xong
        var isRequestApproved = isCancelled
            ? testRequest.RequestApprovedDate.HasValue
            : (currentStatus == TestRequestStatus.RequestApproved ||
               currentStatus == TestRequestStatus.SampleSent ||
               currentStatus == TestRequestStatus.InProgress ||
               currentStatus == TestRequestStatus.PendingApproval ||
               currentStatus == TestRequestStatus.Approved ||
               currentStatus == TestRequestStatus.PendingChangeApproval ||
               currentStatus == TestRequestStatus.PartialResultSent ||
               currentStatus == TestRequestStatus.ReportPublished ||
               currentStatus == TestRequestStatus.Completed);
        result.Steps.Add(new TimelineStepDto
        {
            Title = "Đã duyệt yêu cầu",
            Status = isRequestApproved ? "completed" : "pending",
            Date = testRequest.RequestApprovedDate,
            UserId = null,
            UserName = null
        });

        // === Step 6: Đang xem xét YC (UnderReview) ===
        var isUnderReview = isCancelled
            ? testRequest.UnderReviewDate.HasValue
            : (testRequest.UnderReviewDate.HasValue ||
               currentStatus == TestRequestStatus.UnderReview ||
               currentStatus == TestRequestStatus.WaitingCustomerConfirm ||
               currentStatus == TestRequestStatus.RequestApproved ||
               currentStatus == TestRequestStatus.SampleSent ||
               currentStatus == TestRequestStatus.InProgress ||
               currentStatus == TestRequestStatus.PendingApproval ||
               currentStatus == TestRequestStatus.Approved ||
               currentStatus == TestRequestStatus.PendingChangeApproval ||
               currentStatus == TestRequestStatus.PartialResultSent ||
               currentStatus == TestRequestStatus.ReportPublished ||
               currentStatus == TestRequestStatus.Completed);
        result.Steps.Add(new TimelineStepDto
        {
            Title = "Đang xem xét YC",
            Status = isUnderReview ? "completed" : "pending",
            Date = testRequest.UnderReviewDate,
            UserId = testRequest.UnderReviewByUserId,
            UserName = testRequest.UnderReviewByUserName
        });

        // === Step 7: Đã thanh toán (Paid) - CHỈ hiển thị cho mẫu thường (không phải mẫu nhiệm vụ) ===
        // Mẫu nhiệm vụ không cần thanh toán nên bỏ qua step này
        if (!testRequest.SampleTypeTask)
        {
            var isPaid = testRequest.PaymentReceived;
            // Use Receipt.PaidDate as the real payment date (TestRequest.PendingDate is never set)
            var paidByUserName = receipt?.PaidDate.HasValue == true && receipt.LastModifierId.HasValue
                ? await _userNameProvider.GetUserNameAsync(receipt.LastModifierId)
                : null;
            result.Steps.Add(new TimelineStepDto
            {
                Title = "Đã thanh toán",
                Status = isPaid ? "completed" : "pending",
                Date = receipt?.PaidDate,
                UserId = receipt?.PaidDate.HasValue == true ? receipt.LastModifierId : null,
                UserName = paidByUserName
            });
        }

        // === Step 8: Chờ thanh toán / Không cần thanh toán (PendingPayment / NoPaymentRequired) ===
        // Mẫu nhiệm vụ (SampleTypeTask): hiển thị "Không cần thanh toán"
        // Mẫu thường: hiển thị "Chờ thanh toán"
        // Dựa vào flag SentToAccounting hoặc PaymentReceived (nếu đã thanh toán thì chờ thanh toán cũng xanh)
        var isSentToAccounting = testRequest.SentToAccounting ||
               testRequest.PaymentReceived ||
               currentStatus == TestRequestStatus.PendingPayment ||
               currentStatus == TestRequestStatus.NoPaymentRequired ||
               currentStatus == TestRequestStatus.Paid;
        var paymentStepTitle = testRequest.SampleTypeTask ? "Không cần thanh toán" : "Chờ thanh toán";
        // Use Receipt.CreationTime as the actual "sent to accounting" date (more accurate than TestRequest.CreationTime)
        var sentToAccountingDate = receipt?.CreationTime ?? testRequest.CreationTime;
        var sentToAccountingUserId = receipt?.CreatorId ?? testRequest.CreatorId;
        var sentToAccountingUserName = receipt?.CreatorId != null && receipt.CreatorId != testRequest.CreatorId
            ? await _userNameProvider.GetUserNameAsync(receipt.CreatorId)
            : creatorName;
        result.Steps.Add(new TimelineStepDto
        {
            Title = paymentStepTitle,
            Status = isSentToAccounting ? "completed" : "pending",
            Date = isSentToAccounting ? sentToAccountingDate : null,
            UserId = isSentToAccounting ? sentToAccountingUserId : null,
            UserName = isSentToAccounting ? sentToAccountingUserName : null
        });

        // === Step 9: Mới (New) - luôn completed vì đây là bước đầu ===
        result.Steps.Add(new TimelineStepDto
        {
            Title = "Mới",
            Status = "completed",
            Date = testRequest.CreationTime,
            UserId = testRequest.CreatorId,
            UserName = creatorName
        });

        // Post-processing: mark "current" step and clean up cancelled timeline
        if (isCancelled)
        {
            // When cancelled, remove pending steps - only show what actually happened
            result.Steps = result.Steps.Where(s => s.Status != "pending").ToList();
        }
        else if (currentStatus != TestRequestStatus.Completed)
        {
            // Mark the NEXT step to happen as "current" (yellow clock)
            // This is the last pending step in the list (closest to the completed boundary)
            // Steps already reached stay "completed" (blue check + date shown)
            var nextStep = result.Steps.LastOrDefault(s => s.Status == "pending");
            if (nextStep != null)
            {
                nextStep.Status = "current";
            }
        }

        return result;
    }

    /// <summary>
    /// Lấy danh sách công việc cần làm cho SampleReceiver (dùng cho popup reminder)
    /// SampleReceiver là người tạo TestRequest (CreatorId)
    /// </summary>
    public async Task<List<SampleReceiverPendingTaskDto>> GetSampleReceiverPendingTasksAsync()
    {
        var currentUserId = _currentUser.Id;
        if (!currentUserId.HasValue)
        {
            return new List<SampleReceiverPendingTaskDto>();
        }

        // Query TestRequest do SampleReceiver hiện tại tạo (CreatorId = currentUserId)
        // với status = Paid (cần gửi mẫu) hoặc ResultSent (cần trả kết quả)
        var query = await Repository.GetQueryableAsync();
        var testRequests = await AsyncExecuter.ToListAsync(
            query.Where(x =>
                x.CreatorId == currentUserId.Value &&
                (x.Status == TestRequestStatus.Paid || x.Status == TestRequestStatus.ResultSent))
        );

        return testRequests.Select(tr => new SampleReceiverPendingTaskDto
        {
            Id = tr.Id,
            ContractNumber = tr.ContractNumber ?? tr.RequestNumber,
            TaskName = tr.Status == TestRequestStatus.Paid
                ? "Cần chuyển mẫu thử nghiệm"
                : "Cần trả kết quả cho khách hàng",
            PendingDate = tr.PendingDate,
            ExpectedCompletionDate = tr.ExpectedCompletionDate,
            Status = tr.Status
        }).OrderBy(x => x.PendingDate).ToList();
    }

    #endregion

    #region Print Result Methods

    /// <summary>
    /// Lấy danh sách chỉ tiêu đã duyệt của TestRequest (dùng cho In KQ)
    /// </summary>
    public async Task<List<QualityManagementIndicatorDto>> GetApprovedIndicatorsAsync(Guid id)
    {
        var testRequest = await Repository.GetAsync(id);

        // Lấy tất cả QualityManagementTask của TestRequest này
        var qualityTasks = await _qualityTaskRepository.GetListAsync(t => t.TestRequestId == id);
        var qualityTaskIds = qualityTasks.Select(t => t.Id).ToList();

        // Lấy tất cả QualityManagementIndicator đã được duyệt
        var indicators = await _qualityIndicatorRepository.GetListAsync(i =>
            qualityTaskIds.Contains(i.QualityManagementTaskId) &&
            i.ApprovalStatus == QualityIndicatorApprovalStatus.Approved);

        // Map to DTO
        var result = new List<QualityManagementIndicatorDto>();
        foreach (var indicator in indicators)
        {
            // Get LaboratoryTaskIndicator to access sample, indicator, method info
            var labIndicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicator.LaboratoryTaskIndicatorId);
            var sampleIndicator = await _sampleIndicatorRepository.GetAsync(labIndicator.TestRequestSampleIndicatorId);
            var sample = await _sampleRepository.GetAsync(sampleIndicator.TestRequestSampleId);
            var masterIndicator = await _indicatorRepository.GetAsync(sampleIndicator.IndicatorId);

            // Get lab info through LaboratoryTask
            var labTask = await _laboratoryTaskRepository.GetAsync(labIndicator.LaboratoryTaskId);
            var laboratory = await _laboratoryRepository.GetAsync(labTask.LaboratoryId);
            var labDept = await _departmentRepository.GetAsync(laboratory.DepartmentId);

            // Get specimen type name if exists
            string? specimenTypeName = null;
            if (sample.SpecimenTypeId.HasValue)
            {
                var specimenType = await _specimenTypeRepository.FindAsync(sample.SpecimenTypeId.Value);
                specimenTypeName = specimenType?.Name;
            }

            // Get method info
            string? methodName = null;
            string? methodFormattedName = null;
            if (labIndicator.MethodId.HasValue)
            {
                var method = await _testMethodRepository.FindAsync(labIndicator.MethodId.Value);
                methodName = method?.Name;
                methodFormattedName = method?.FormattedName;
            }

            result.Add(new QualityManagementIndicatorDto
            {
                Id = indicator.Id,
                LaboratoryTaskIndicatorId = indicator.LaboratoryTaskIndicatorId,
                LaboratoryId = laboratory.Id,
                LaboratoryName = labDept.Name,
                SampleId = sample.Id,
                SampleCode = sample.SampleCode,
                SampleName = sample.SampleName,
                SpecimenTypeName = specimenTypeName,
                IndicatorId = masterIndicator.Id,
                IndicatorCode = masterIndicator.Code,
                IndicatorName = masterIndicator.Name,
                IndicatorFormattedName = masterIndicator.FormattedName,
                MethodId = labIndicator.MethodId,
                MethodName = methodName,
                MethodFormattedName = methodFormattedName,
                Unit = labIndicator.Unit,
                Result = labIndicator.Result,
                ResultSentDate = labIndicator.EntryDate,
                ApprovalDate = indicator.ApprovalDate,
                StandardId = indicator.StandardId,
                ReferenceLimit = indicator.ReferenceLimit,
                ApprovalStatus = indicator.ApprovalStatus,
                IsResultSent = indicator.IsResultSent
            });
        }

        return result;
    }

    /// <summary>
    /// In kết quả đơn (1 chỉ tiêu)
    /// </summary>
    public async Task<PrintResultOutputDto> PrintSingleResultAsync(Guid id, Guid indicatorId, string? note = null, string? templateName = null, Guid? vicePresidentId = null)
    {
        var testRequest = await Repository.GetAsync(id);

        // Validate indicator exists and is approved
        var indicator = await _qualityIndicatorRepository.GetAsync(indicatorId);
        if (indicator.ApprovalStatus != QualityIndicatorApprovalStatus.Approved)
        {
            throw new Common.Exceptions.InvalidOperationException("Chỉ có thể in kết quả đã được duyệt.", "indicator");
        }

        // Get quality task for this indicator
        var qualityTask = await _qualityTaskRepository.GetAsync(indicator.QualityManagementTaskId);

        // Xác định ngày in: lấy ngày in sớm nhất của BẤT KỲ indicator nào trong task
        var allTaskIndicators = await _qualityIndicatorRepository.GetListAsync(
            x => x.QualityManagementTaskId == qualityTask.Id);
        var printDate = allTaskIndicators
            .Select(i => i.PrintedDate)
            .Where(d => d.HasValue)
            .OrderBy(d => d)
            .FirstOrDefault() ?? DateTime.Now;

        // Generate PDF result form
        var result = await _resultFormAppService.GenerateSingleIndicatorResultFormAsync(new GenerateSingleIndicatorInput
        {
            TaskId = qualityTask.Id,
            IndicatorId = indicatorId,
            Note = note,
            TemplateName = templateName,
            VicePresidentId = vicePresidentId,
            PrintDate = printDate
        });

        // Chỉ set PrintedDate lần đầu
        if (!indicator.PrintedDate.HasValue)
        {
            indicator.PrintedDate = printDate;
            indicator.IsPrinted = true;
            await _qualityIndicatorRepository.UpdateAsync(indicator);
        }

        // Update TestRequest status to PendingApproval if not already past that
        if (testRequest.Status == TestRequestStatus.InProgress)
        {
            testRequest.Status = TestRequestStatus.PendingApproval;
            await Repository.UpdateAsync(testRequest);
        }

        return new PrintResultOutputDto
        {
            FileName = result.FileName
        };
    }

    /// <summary>
    /// In kết quả hỗn hợp (nhiều chỉ tiêu)
    /// </summary>
    public async Task<PrintResultOutputDto> PrintMixedResultAsync(Guid id, List<Guid> indicatorIds, string? note = null, string? templateName = null, Guid? vicePresidentId = null)
    {
        var testRequest = await Repository.GetAsync(id);

        // Validate all indicators are approved
        var indicatorsToPrint = new List<QualityManagementIndicator>();
        foreach (var indicatorId in indicatorIds)
        {
            var indicator = await _qualityIndicatorRepository.GetAsync(indicatorId);
            if (indicator.ApprovalStatus != QualityIndicatorApprovalStatus.Approved)
            {
                throw new Common.Exceptions.InvalidOperationException("Chỉ có thể in kết quả đã được duyệt.", "indicator");
            }
            indicatorsToPrint.Add(indicator);
        }

        // Get first indicator to find quality task
        var firstIndicator = indicatorsToPrint.First();
        var qualityTask = await _qualityTaskRepository.GetAsync(firstIndicator.QualityManagementTaskId);

        // Xác định ngày in: lấy ngày in sớm nhất của BẤT KỲ indicator nào trong task
        var allTaskIndicators = await _qualityIndicatorRepository.GetListAsync(
            x => x.QualityManagementTaskId == qualityTask.Id);
        var printDate = allTaskIndicators
            .Select(i => i.PrintedDate)
            .Where(d => d.HasValue)
            .OrderBy(d => d)
            .FirstOrDefault() ?? DateTime.Now;

        // Generate PDF result form
        var result = await _resultFormAppService.GenerateMixedIndicatorResultFormAsync(new GenerateMixedIndicatorInput
        {
            TaskId = qualityTask.Id,
            IndicatorIds = indicatorIds,
            Note = note,
            TemplateName = templateName,
            VicePresidentId = vicePresidentId,
            PrintDate = printDate
        });

        // Chỉ set PrintedDate lần đầu
        foreach (var indicator in indicatorsToPrint)
        {
            if (!indicator.PrintedDate.HasValue)
            {
                indicator.PrintedDate = printDate;
                indicator.IsPrinted = true;
                await _qualityIndicatorRepository.UpdateAsync(indicator);
            }
        }

        // Update TestRequest status to PendingApproval if not already past that
        if (testRequest.Status == TestRequestStatus.InProgress)
        {
            testRequest.Status = TestRequestStatus.PendingApproval;
            await Repository.UpdateAsync(testRequest);
        }

        return new PrintResultOutputDto
        {
            FileName = result.FileName
        };
    }

    /// <summary>
    /// In kết quả hỗn hợp theo từng mẫu (mỗi mẫu = 1 file riêng)
    /// </summary>
    public async Task<PrintMultiResultOutputDto> PrintMixedPerSampleResultAsync(Guid id, PrintMixedDto input)
    {
        var testRequest = await Repository.GetAsync(id);

        var indicatorsToPrint = new List<QualityManagementIndicator>();
        foreach (var indicatorId in input.IndicatorIds)
        {
            var indicator = await _qualityIndicatorRepository.GetAsync(indicatorId);
            if (indicator.ApprovalStatus != QualityIndicatorApprovalStatus.Approved)
                throw new Common.Exceptions.InvalidOperationException("Chỉ có thể in kết quả đã được duyệt.", "indicator");
            indicatorsToPrint.Add(indicator);
        }

        var firstIndicator = indicatorsToPrint.First();
        var qualityTask = await _qualityTaskRepository.GetAsync(firstIndicator.QualityManagementTaskId);

        // Xác định ngày in: lấy ngày in sớm nhất của BẤT KỲ indicator nào trong task
        var allTaskIndicators = await _qualityIndicatorRepository.GetListAsync(
            x => x.QualityManagementTaskId == qualityTask.Id);
        var printDate = allTaskIndicators
            .Select(i => i.PrintedDate)
            .Where(d => d.HasValue)
            .OrderBy(d => d)
            .FirstOrDefault() ?? DateTime.Now;

        var results = await _resultFormAppService.GenerateMixedPerSampleResultFormAsync(new GenerateMixedIndicatorInput
        {
            TaskId = qualityTask.Id,
            IndicatorIds = input.IndicatorIds,
            Note = input.Note,
            TemplateName = input.TemplateName,
            VicePresidentId = input.VicePresidentId,
            PrintDate = printDate
        });

        // Chỉ set PrintedDate lần đầu
        foreach (var indicator in indicatorsToPrint)
        {
            if (!indicator.PrintedDate.HasValue)
            {
                indicator.PrintedDate = printDate;
                indicator.IsPrinted = true;
                await _qualityIndicatorRepository.UpdateAsync(indicator);
            }
        }

        if (testRequest.Status == TestRequestStatus.InProgress)
        {
            testRequest.Status = TestRequestStatus.PendingApproval;
            await Repository.UpdateAsync(testRequest);
        }

        return new PrintMultiResultOutputDto
        {
            Results = results.Select(r => new PrintResultItemDto
            {
                FileName = r.FileName,
                SampleCode = r.SampleCode ?? "",
                TemplateName = input.TemplateName ?? "QLNN"
            }).ToList()
        };
    }

    /// <summary>
    /// In kết quả đơn với nhiều chỉ tiêu và nhiều templates (batch processing)
    /// Generates (labs × templates) PDFs - mỗi lab với mỗi template = 1 PDF
    /// Indicators của cùng 1 lab sẽ được gộp vào 1 PDF (dùng mixed-style)
    /// </summary>
    public async Task<PrintMultiResultOutputDto> PrintMultiSingleResultAsync(Guid id, PrintMultiSingleDto input)
    {
        if (input.IndicatorIds == null || input.IndicatorIds.Count == 0)
        {
            throw new Common.Exceptions.ValidationException("indicatorIds", "Vui lòng chọn ít nhất một chỉ tiêu");
        }

        if (input.TemplateNames == null || input.TemplateNames.Count == 0)
        {
            throw new Common.Exceptions.ValidationException("templateNames", "Vui lòng chọn ít nhất một biểu mẫu");
        }

        var testRequest = await Repository.GetAsync(id);

        // Load all selected indicators
        var qualityIndicators = await _qualityIndicatorRepository.GetListAsync(
            x => input.IndicatorIds.Contains(x.Id));

        // Validate all indicators are approved
        foreach (var indicator in qualityIndicators)
        {
            if (indicator.ApprovalStatus != QualityIndicatorApprovalStatus.Approved)
            {
                throw new Common.Exceptions.InvalidOperationException("Chỉ có thể in kết quả đã được duyệt.", "indicator");
            }
        }

        // Get first indicator to find quality task
        var firstIndicator = qualityIndicators.First();
        var qualityTask = await _qualityTaskRepository.GetAsync(firstIndicator.QualityManagementTaskId);

        // Xác định ngày in: lấy ngày in sớm nhất của BẤT KỲ indicator nào trong task
        var allTaskIndicators = await _qualityIndicatorRepository.GetListAsync(
            x => x.QualityManagementTaskId == qualityTask.Id);
        var printDate = allTaskIndicators
            .Select(i => i.PrintedDate)
            .Where(d => d.HasValue)
            .OrderBy(d => d)
            .FirstOrDefault() ?? DateTime.Now;

        // Get LaboratoryTaskIndicators to find LaboratoryId
        var labTaskIndicatorIds = qualityIndicators.Select(qi => qi.LaboratoryTaskIndicatorId).ToList();
        var labTaskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(
            x => labTaskIndicatorIds.Contains(x.Id));
        var labTaskIndicatorDict = labTaskIndicators.ToDictionary(lti => lti.Id);

        // Get LaboratoryTasks to find LaboratoryId
        var labTaskIds = labTaskIndicators.Select(lti => lti.LaboratoryTaskId).Distinct().ToList();
        var labTasks = await _laboratoryTaskRepository.GetListAsync(x => labTaskIds.Contains(x.Id));
        var labTaskDict = labTasks.ToDictionary(lt => lt.Id);

        // Get TestRequestSampleIndicators to find SampleId
        var sampleIndicatorIds = labTaskIndicators.Select(lti => lti.TestRequestSampleIndicatorId).Distinct().ToList();
        var sampleIndicators = await _sampleIndicatorRepository.GetListAsync(
            x => sampleIndicatorIds.Contains(x.Id));
        var sampleIndicatorDict = sampleIndicators.ToDictionary(si => si.Id);

        // Get TestRequestSamples to get SampleCode, SampleName
        var sampleIds = sampleIndicators.Select(si => si.TestRequestSampleId).Distinct().ToList();
        var samples = await _sampleRepository.GetListAsync(x => sampleIds.Contains(x.Id));
        var sampleDict = samples.ToDictionary(s => s.Id);

        // Build mapping: QualityManagementIndicator.Id -> (LaboratoryId, SampleId, SampleCode, SampleName)
        var indicatorToLabSample = new Dictionary<Guid, (Guid LabId, Guid SampleId, string SampleCode, string SampleName)>();
        foreach (var qi in qualityIndicators)
        {
            if (labTaskIndicatorDict.TryGetValue(qi.LaboratoryTaskIndicatorId, out var lti))
            {
                if (labTaskDict.TryGetValue(lti.LaboratoryTaskId, out var lt))
                {
                    if (sampleIndicatorDict.TryGetValue(lti.TestRequestSampleIndicatorId, out var si))
                    {
                        if (sampleDict.TryGetValue(si.TestRequestSampleId, out var sample))
                        {
                            indicatorToLabSample[qi.Id] = (lt.LaboratoryId, sample.Id, sample.SampleCode, sample.SampleName);
                        }
                    }
                }
            }
        }

        // Group indicators by (LaboratoryId, SampleId) instead of just LaboratoryId
        var indicatorsByLabSample = qualityIndicators
            .Where(qi => indicatorToLabSample.ContainsKey(qi.Id))
            .GroupBy(qi => (indicatorToLabSample[qi.Id].LabId, indicatorToLabSample[qi.Id].SampleId))
            .ToDictionary(g => g.Key, g => g.ToList());

        // Get laboratory names
        var labIds = indicatorsByLabSample.Keys.Select(k => k.LabId).Distinct().ToList();
        var laboratories = await _laboratoryRepository.GetListAsync(x => labIds.Contains(x.Id));
        var deptIds = laboratories.Select(l => l.DepartmentId).Distinct().ToList();
        var depts = await _departmentRepository.GetListAsync(d => deptIds.Contains(d.Id));
        var deptDict = depts.ToDictionary(d => d.Id, d => d.Name);
        var labDict = laboratories.ToDictionary(l => l.Id, l => deptDict.TryGetValue(l.DepartmentId, out var name) ? name : "");

        var results = new List<PrintResultItemDto>();

        // Generate ((lab-sample combinations) × templates) PDFs
        foreach (var templateName in input.TemplateNames)
        {
            foreach (var labSampleGroup in indicatorsByLabSample)
            {
                var (labId, sampleId) = labSampleGroup.Key;
                var labSampleIndicators = labSampleGroup.Value;
                var labName = labDict.GetValueOrDefault(labId, "Unknown Lab");

                // Get sample info from first indicator in this group
                var firstGroupIndicator = labSampleIndicators.First();
                var (_, _, sampleCode, sampleName) = indicatorToLabSample[firstGroupIndicator.Id];

                GenerateResultFormOutput result;

                if (labSampleIndicators.Count == 1)
                {
                    // Single indicator - use single form
                    result = await _resultFormAppService.GenerateSingleIndicatorResultFormAsync(new GenerateSingleIndicatorInput
                    {
                        TaskId = qualityTask.Id,
                        IndicatorId = labSampleIndicators[0].Id,
                        Note = input.Note,
                        TemplateName = templateName,
                        VicePresidentId = input.VicePresidentId,
                        PrintDate = printDate
                    });
                }
                else
                {
                    // Multiple indicators - use mixed form
                    result = await _resultFormAppService.GenerateMixedIndicatorResultFormAsync(new GenerateMixedIndicatorInput
                    {
                        TaskId = qualityTask.Id,
                        IndicatorIds = labSampleIndicators.Select(qi => qi.Id).ToList(),
                        Note = input.Note,
                        TemplateName = templateName,
                        VicePresidentId = input.VicePresidentId,
                        PrintDate = printDate
                    });
                }

                results.Add(new PrintResultItemDto
                {
                    FileName = result.FileName,
                    LaboratoryId = labId,
                    LaboratoryName = labName,
                    SampleId = sampleId,
                    SampleCode = sampleCode,
                    SampleName = sampleName,
                    TemplateName = templateName
                });
            }
        }

        // Chỉ set PrintedDate lần đầu
        foreach (var indicator in qualityIndicators)
        {
            if (!indicator.PrintedDate.HasValue)
            {
                indicator.PrintedDate = printDate;
                indicator.IsPrinted = true;
                await _qualityIndicatorRepository.UpdateAsync(indicator);
            }
        }

        // Update TestRequest status to PendingApproval if not already past that
        if (testRequest.Status == TestRequestStatus.InProgress)
        {
            testRequest.Status = TestRequestStatus.PendingApproval;
            await Repository.UpdateAsync(testRequest);
        }

        return new PrintMultiResultOutputDto
        {
            Results = results
        };
    }

    #endregion

    #region Payment Flow Methods

    /// <summary>
    /// Gửi kế toán - Tạo Receipt và xử lý theo loại mẫu
    /// - Mẫu nhiệm vụ (SampleTypeTask): Receipt = NoPaymentRequired, TestRequest -> Paid (auto, không chờ thanh toán)
    /// - Mẫu thường: Receipt = PendingPayment, chờ kế toán xác nhận thanh toán
    /// User click "Gửi QLKT" để chuyển sang UnderReview
    /// </summary>
    public async Task<Guid> SendToAccountingAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);

        // Cho phép gửi kế toán khi status là PendingPayment (sau khi In phiếu XN)
        if (entity.Status != TestRequestStatus.PendingPayment)
        {
            throw new Common.Exceptions.InvalidOperationException("Chỉ có thể gửi kế toán khi trạng thái là 'Chờ thanh toán'", "status");
        }

        // Check if Receipt already exists (shouldn't happen, but safety check)
        var existingReceipt = await _receiptRepository.FirstOrDefaultAsync(r => r.TestRequestId == id);
        if (existingReceipt != null)
        {
            throw new Common.Exceptions.InvalidOperationException("Phiếu thu đã tồn tại cho phiếu yêu cầu này", "receipt");
        }

        // Xử lý theo loại mẫu
        if (entity.SampleTypeTask)
        {
            // Mẫu nhiệm vụ: Không cần thanh toán -> chuyển sang NoPaymentRequired
            // User sẽ click "Gửi QLKT" để chuyển sang UnderReview
            var receipt = new Receipt(GuidGenerator.Create(), id, ReceiptStatus.NoPaymentRequired);
            await _receiptRepository.InsertAsync(receipt);

            entity.Status = TestRequestStatus.NoPaymentRequired;
            entity.SentToAccounting = true;
            await Repository.UpdateAsync(entity);

            return receipt.Id;
        }
        else
        {
            // Mẫu thường: Chờ kế toán xác nhận thanh toán
            var receipt = new Receipt(GuidGenerator.Create(), id, ReceiptStatus.PendingPayment);
            await _receiptRepository.InsertAsync(receipt);

            // Giữ nguyên status PendingPayment, chờ kế toán xác nhận
            entity.SentToAccounting = true;
            await Repository.UpdateAsync(entity);

            // TODO: Send notification to Accounting department

            return receipt.Id;
        }
    }

    /// <summary>
    /// Gửi QLKT - Chuyển từ PendingPayment/Paid/NoPaymentRequired sang UnderReview (Đang xem xét YC)
    /// Áp dụng cho cả mẫu thường (PendingPayment/Paid) và mẫu nhiệm vụ (NoPaymentRequired)
    /// Tạo LaboratoryTasks để QLKT có thể đánh giá các chỉ tiêu
    ///
    /// Hỗ trợ gửi partial (một phần):
    /// - Nếu chưa gửi hết tất cả chỉ tiêu → giữ nguyên status (PendingPayment/Paid/NoPaymentRequired)
    /// - Khi TẤT CẢ chỉ tiêu đã gửi → cập nhật status = UnderReview
    /// </summary>
    public async Task SendToQLKTAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);

        // Không cho gửi khi đã hoàn thành hoặc hủy
        if (entity.Status == TestRequestStatus.Completed || entity.Status == TestRequestStatus.Cancelled)
        {
            throw new Common.Exceptions.InvalidOperationException("Không thể gửi QLKT khi phiếu đã hoàn thành hoặc đã hủy", "status");
        }

        // Đếm tổng số indicators và số đã có LaboratoryId
        var samples = await _sampleRepository.GetListAsync(s => s.TestRequestId == id && !s.IsCancelled && !s.IsCustomerCancelled);
        var totalIndicatorCount = 0;
        var indicatorsWithLabCount = 0;

        foreach (var sample in samples)
        {
            var sampleIndicators = await _sampleIndicatorRepository.GetListAsync(
                i => i.TestRequestSampleId == sample.Id);
            totalIndicatorCount += sampleIndicators.Count;
            indicatorsWithLabCount += sampleIndicators.Count(i => i.LaboratoryId.HasValue);
        }

        // Phải có ít nhất 1 indicator có LaboratoryId để gửi
        if (indicatorsWithLabCount == 0)
        {
            throw new Common.Exceptions.InvalidOperationException("Chưa có chỉ tiêu nào được chọn khoa phòng", "laboratoryId");
        }

        // Tạo LaboratoryTasks grouped by Laboratory để QLKT đánh giá
        // Method này sẽ tự skip các indicators đã được gửi trước đó
        await CreateLaboratoryTasksAsync(entity);

        // Lưu thời gian gửi QLKT ngay từ lần đầu tiên (không ghi đè nếu đã có)
        var isInitialSend = entity.Status == TestRequestStatus.New
            || entity.Status == TestRequestStatus.PendingPayment
            || entity.Status == TestRequestStatus.Paid
            || entity.Status == TestRequestStatus.NoPaymentRequired;

        if (isInitialSend)
        {
            if (!entity.UnderReviewDate.HasValue)
            {
                entity.UnderReviewDate = Clock.Now;
                entity.UnderReviewByUserId = _currentUser.Id;
                entity.UnderReviewByUserName = _currentUser.Name ?? _currentUser.UserName;
            }

            if (indicatorsWithLabCount == totalIndicatorCount)
            {
                entity.Status = TestRequestStatus.UnderReview;
            }

            await Repository.UpdateAsync(entity);
        }

        // Thông báo cho QLKT về yêu cầu mới cần đánh giá
        await NotifyTechnicalManagersOnSampleSentAsync(entity);
    }

    /// <summary>
    /// Cập nhật LaboratoryId và RequestDate cho các chỉ tiêu trước khi gửi QLKT
    /// Sử dụng trong modal "Gửi QLKT" để người dùng chọn khoa phòng cho từng chỉ tiêu
    /// </summary>
    public async Task UpdateIndicatorsLaboratoryAsync(Guid id, UpdateIndicatorsLaboratoryInput input)
    {
        var entity = await Repository.GetAsync(id);

        // Không cho cập nhật khi đã hoàn thành hoặc hủy
        if (entity.Status == TestRequestStatus.Completed || entity.Status == TestRequestStatus.Cancelled)
        {
            throw new Common.Exceptions.InvalidOperationException(
                "Không thể cập nhật khoa phòng khi phiếu đã hoàn thành hoặc đã hủy",
                "status");
        }

        // Validate tất cả indicators phải có LaboratoryId
        if (input.Indicators.Any(i => i.LaboratoryId == Guid.Empty))
        {
            throw new Common.Exceptions.InvalidOperationException(
                "Tất cả chỉ tiêu phải được chọn khoa phòng",
                "laboratoryId");
        }

        // Get all sample indicators for validation
        var sampleIndicatorIds = input.Indicators.Select(i => i.SampleIndicatorId).ToList();
        var sampleIndicators = await _sampleIndicatorRepository.GetListAsync(
            si => sampleIndicatorIds.Contains(si.Id));

        // Validate all sample indicators belong to this test request
        var samples = await _sampleRepository.GetListAsync(s => s.TestRequestId == id);
        var sampleIds = samples.Select(s => s.Id).ToHashSet();

        foreach (var si in sampleIndicators)
        {
            if (!sampleIds.Contains(si.TestRequestSampleId))
            {
                throw new Common.Exceptions.InvalidOperationException(
                    "Một số chỉ tiêu không thuộc phiếu yêu cầu này",
                    "sampleIndicatorId");
            }
        }

        // Update each sample indicator
        foreach (var indicatorInput in input.Indicators)
        {
            var sampleIndicator = sampleIndicators.FirstOrDefault(si => si.Id == indicatorInput.SampleIndicatorId);
            if (sampleIndicator == null)
            {
                throw new Common.Exceptions.InvalidOperationException(
                    $"Không tìm thấy chỉ tiêu với ID: {indicatorInput.SampleIndicatorId}",
                    "sampleIndicatorId");
            }

            // Update LaboratoryId
            sampleIndicator.LaboratoryId = indicatorInput.LaboratoryId;

            // Update RequestDate nếu có
            if (indicatorInput.RequestDate.HasValue)
            {
                sampleIndicator.RequestDate = indicatorInput.RequestDate.Value;
            }

            await _sampleIndicatorRepository.UpdateAsync(sampleIndicator, autoSave: true);
        }
    }

    /// <summary>
    /// Hủy yêu cầu thử nghiệm
    /// Cho phép hủy ở tất cả status ngoại trừ Completed, ResultSent và Cancelled
    /// </summary>
    public async Task CancelRequestAsync(Guid id, CancelTestRequestDto input)
    {
        var entity = await Repository.GetAsync(id);

        // Không cho phép hủy khi đã Completed, ResultSent hoặc đã Cancelled
        if (entity.Status == TestRequestStatus.Completed ||
            entity.Status == TestRequestStatus.ResultSent ||
            entity.Status == TestRequestStatus.Cancelled)
        {
            throw new Common.Exceptions.InvalidOperationException(
                "Không thể hủy yêu cầu khi trạng thái là 'Hoàn thành', 'Đã gửi kết quả' hoặc 'Đã hủy'",
                "status");
        }

        // Cancel associated Receipt nếu có và chưa thanh toán
        var receipt = await _receiptRepository.FirstOrDefaultAsync(r => r.TestRequestId == id);
        if (receipt != null && receipt.Status != ReceiptStatus.Paid)
        {
            receipt.Cancel(input.Reason);
            await _receiptRepository.UpdateAsync(receipt);
        }

        // Get current user info
        var currentUserName = await _userNameProvider.GetUserNameAsync(_currentUser.Id);

        // Update TestRequest status to Cancelled and save cancellation info
        entity.Status = TestRequestStatus.Cancelled;
        entity.CancelledDate = Clock.Now;
        entity.CancelledByUserId = _currentUser.Id;
        entity.CancelledByUserName = currentUserName;
        entity.CancellationReason = input.Reason;

        await Repository.UpdateAsync(entity);
    }

    #endregion

    #region BPGN - Confirm Delivery to Customer

    /// <summary>
    /// BPGN xác nhận đã bàn giao kết quả cho khách hàng
    /// Status tự động tính dựa trên chỉ tiêu đã xuất bên QLCL
    /// </summary>
    public async Task ConfirmDeliveryToCustomerAsync(Guid id, ConfirmDeliveryInput? input = null)
    {
        var testRequest = await Repository.GetAsync(id);

        // Validate status - chỉ cho phép khi đang ở ReportPublished hoặc PartialResultSent
        if (testRequest.Status != TestRequestStatus.ReportPublished &&
            testRequest.Status != TestRequestStatus.PartialResultSent)
        {
            throw new Common.Exceptions.InvalidOperationException(
                "Không thể xác nhận bàn giao ở trạng thái này. Yêu cầu phải ở trạng thái 'Đã phát hành báo cáo'.",
                "status");
        }

        // Lấy tất cả quality tasks của test request
        var qualityTasks = await _qualityTaskRepository.GetListAsync(t => t.TestRequestId == id);

        // Lấy tất cả chỉ tiêu đã duyệt từ QualityManagementIndicator
        var allApprovedIndicators = new List<QualityManagementIndicator>();
        foreach (var task in qualityTasks)
        {
            var indicators = await _qualityIndicatorRepository.GetListAsync(i =>
                i.QualityManagementTaskId == task.Id &&
                i.ApprovalStatus == QualityIndicatorApprovalStatus.Approved);
            allApprovedIndicators.AddRange(indicators);
        }

        var totalApproved = allApprovedIndicators.Count;
        var printedCount = allApprovedIndicators.Count(i => i.IsPrinted);

        // Validate: phải có ít nhất 1 chỉ tiêu đã xuất
        if (printedCount == 0)
        {
            throw new Common.Exceptions.InvalidOperationException(
                "Chưa có chỉ tiêu nào được xuất báo cáo.",
                "indicators");
        }

        // Tính status dựa trên chỉ tiêu đã xuất
        if (printedCount < totalApproved)
        {
            // Còn chỉ tiêu chưa xuất → PartialResultSent
            testRequest.Status = TestRequestStatus.PartialResultSent;
        }
        else
        {
            // Xuất hết → Completed
            testRequest.Status = TestRequestStatus.Completed;
            testRequest.CompletedDate = Clock.Now;
        }

        // Ghi chú nếu có
        if (!string.IsNullOrWhiteSpace(input?.Note))
        {
            testRequest.DeliveryNote = input.Note;
        }

        testRequest.DeliveredToCustomerDate = Clock.Now;
        testRequest.DeliveredToCustomerByUserId = _currentUser.Id;
        testRequest.DeliveredToCustomerByUserName = await _userNameProvider.GetUserNameAsync(_currentUser.Id);

        await Repository.UpdateAsync(testRequest);
    }

    /// <summary>
    /// Lấy danh sách báo cáo đã xuất của TestRequest (để xem lịch sử)
    /// </summary>
    public async Task<List<PublishedReportDto>> GetPublishedReportsAsync(Guid id)
    {
        var reports = await _publishedReportRepository.GetByTestRequestIdAsync(id);
        var result = new List<PublishedReportDto>();

        foreach (var report in reports)
        {
            var creatorName = await _userNameProvider.GetUserNameAsync(report.CreatorId);

            // Parse indicator IDs from JSON
            var indicatorIds = new List<Guid>();
            try
            {
                indicatorIds = JsonSerializer.Deserialize<List<Guid>>(report.IndicatorIds) ?? new List<Guid>();
            }
            catch
            {
                // Ignore parse errors
            }

            // Get indicator names
            var indicatorNames = new List<string>();
            foreach (var indicatorId in indicatorIds)
            {
                var qualityIndicator = await _qualityIndicatorRepository.FindAsync(indicatorId);
                if (qualityIndicator != null)
                {
                    var labIndicator = await _laboratoryTaskIndicatorRepository.FindAsync(qualityIndicator.LaboratoryTaskIndicatorId);
                    if (labIndicator != null)
                    {
                        var sampleIndicator = await _sampleIndicatorRepository.FindAsync(labIndicator.TestRequestSampleIndicatorId);
                        if (sampleIndicator != null)
                        {
                            var masterIndicator = await _indicatorRepository.FindAsync(sampleIndicator.IndicatorId);
                            if (masterIndicator != null)
                            {
                                indicatorNames.Add(masterIndicator.Name);
                            }
                        }
                    }
                }
            }

            result.Add(new PublishedReportDto
            {
                Id = report.Id,
                TestRequestId = report.TestRequestId,
                QualityManagementTaskId = report.QualityManagementTaskId,
                FileName = report.FileName,
                ReportType = report.ReportType,
                PrintNote = report.PrintNote,
                IndicatorIds = indicatorIds,
                IndicatorNames = indicatorNames,
                CreationTime = report.CreationTime,
                CreatorName = creatorName
            });
        }

        return result;
    }

    #endregion

    #region Sync Denormalized Fields

    /// <summary>
    /// Đồng bộ ContractNumber từ TestRequest sang LaboratoryTask và QualityManagementTask
    /// </summary>
    private async Task SyncContractNumberToRelatedTasksAsync(Guid testRequestId, string? contractNumber)
    {
        var labTasks = await _laboratoryTaskRepository.GetListAsync(t => t.TestRequestId == testRequestId);
        foreach (var labTask in labTasks)
        {
            if (labTask.ContractNumber != contractNumber)
            {
                labTask.ContractNumber = contractNumber;
                await _laboratoryTaskRepository.UpdateAsync(labTask, autoSave: true);
            }
        }

        var qualityTasks = await _qualityTaskRepository.GetListAsync(t => t.TestRequestId == testRequestId);
        foreach (var qualityTask in qualityTasks)
        {
            if (qualityTask.ContractNumber != contractNumber)
            {
                qualityTask.ContractNumber = contractNumber ?? "";
                await _qualityTaskRepository.UpdateAsync(qualityTask, autoSave: true);
            }
        }
    }

    #endregion

    #region Cost Change History

    /// <summary>
    /// Lấy lịch sử thay đổi tiền của phiếu yêu cầu
    /// </summary>
    public async Task<List<CostChangeHistoryDto>> GetCostChangeHistoryAsync(Guid id)
    {
        var histories = await _costChangeHistoryRepository.GetListAsync(h => h.TestRequestId == id);
        var result = new List<CostChangeHistoryDto>();

        foreach (var history in histories.OrderByDescending(h => h.CreationTime))
        {
            var creatorName = await _userNameProvider.GetUserNameAsync(history.CreatorId);

            result.Add(new CostChangeHistoryDto
            {
                Id = history.Id,
                TestRequestId = history.TestRequestId,
                ChangeType = (int)history.ChangeType,
                ChangeTypeLabel = GetCostChangeTypeLabel(history.ChangeType),
                OldTotalCost = history.OldTotalCost,
                NewTotalCost = history.NewTotalCost,
                DifferenceAmount = history.DifferenceAmount,
                AffectedIndicatorId = history.AffectedIndicatorId,
                AffectedIndicatorName = history.AffectedIndicatorName,
                IndicatorUnitPrice = history.IndicatorUnitPrice,
                Reason = history.Reason,
                CreationTime = history.CreationTime,
                CreatorName = creatorName
            });
        }

        return result;
    }

    /// <summary>
    /// Get label for CostChangeType enum
    /// </summary>
    private static string GetCostChangeTypeLabel(CostChangeType changeType)
    {
        return changeType switch
        {
            CostChangeType.IndicatorCancelled => "KH hủy chỉ tiêu",
            CostChangeType.MethodChanged => "Thay đổi phương pháp",
            CostChangeType.SampleCancelled => "KH hủy mẫu",
            CostChangeType.ManualAdjustment => "Điều chỉnh thủ công",
            _ => "Không xác định"
        };
    }

    #endregion
}
