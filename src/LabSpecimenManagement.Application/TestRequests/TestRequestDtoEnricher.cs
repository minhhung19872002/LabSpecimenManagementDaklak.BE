using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.MasterData;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.ObjectMapping;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// Helper service for enriching TestRequest DTOs with related data
/// Centralizes data loading logic to reduce duplication in TestRequestAppService
/// </summary>
public class TestRequestDtoEnricher : ITransientDependency
{
    private readonly IRepository<TestRequestSample, Guid> _sampleRepository;
    private readonly IRepository<TestRequestSampleIndicator, Guid> _sampleIndicatorRepository;
    private readonly IRepository<Indicator, Guid> _indicatorRepository;
    private readonly IRepository<IndicatorGroup, Guid> _indicatorGroupRepository;
    private readonly IRepository<Laboratory, Guid> _laboratoryRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IRepository<TestMethod, Guid> _testMethodRepository;
    private readonly IRepository<SpecimenType, Guid> _specimenTypeRepository;
    private readonly IRepository<LaboratoryTaskIndicator, Guid> _laboratoryTaskIndicatorRepository;
    private readonly IRepository<IndicatorImage, Guid> _indicatorImageRepository;
    private readonly IUserNameProvider _userNameProvider;
    private readonly IObjectMapper _objectMapper;

    public TestRequestDtoEnricher(
        IRepository<TestRequestSample, Guid> sampleRepository,
        IRepository<TestRequestSampleIndicator, Guid> sampleIndicatorRepository,
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<IndicatorGroup, Guid> indicatorGroupRepository,
        IRepository<Laboratory, Guid> laboratoryRepository,
        IRepository<Department, Guid> departmentRepository,
        IRepository<TestMethod, Guid> testMethodRepository,
        IRepository<SpecimenType, Guid> specimenTypeRepository,
        IRepository<LaboratoryTaskIndicator, Guid> laboratoryTaskIndicatorRepository,
        IRepository<IndicatorImage, Guid> indicatorImageRepository,
        IUserNameProvider userNameProvider,
        IObjectMapper objectMapper)
    {
        _sampleRepository = sampleRepository;
        _sampleIndicatorRepository = sampleIndicatorRepository;
        _indicatorRepository = indicatorRepository;
        _indicatorGroupRepository = indicatorGroupRepository;
        _laboratoryRepository = laboratoryRepository;
        _departmentRepository = departmentRepository;
        _testMethodRepository = testMethodRepository;
        _specimenTypeRepository = specimenTypeRepository;
        _laboratoryTaskIndicatorRepository = laboratoryTaskIndicatorRepository;
        _indicatorImageRepository = indicatorImageRepository;
        _userNameProvider = userNameProvider;
        _objectMapper = objectMapper;
    }

    /// <summary>
    /// Enriches a TestRequestDto with samples, indicators, and user names
    /// </summary>
    public async Task EnrichAsync(TestRequestDto dto, TestRequest entity)
    {
        // Load samples with indicators
        dto.Samples = await LoadSamplesWithIndicatorsAsync(entity.Id);

        // Calculate total cost
        dto.TotalCost = dto.Samples.Sum(s => s.TotalIndicatorCost);

        // Enrich with user names
        dto.SupervisorName = await _userNameProvider.GetUserNameAsync(dto.SupervisorId);
        dto.DirectorName = await _userNameProvider.GetUserNameAsync(dto.DirectorId);
    }

    /// <summary>
    /// Loads samples with their indicators for a test request
    /// </summary>
    public async Task<List<TestRequestSampleDto>> LoadSamplesWithIndicatorsAsync(Guid testRequestId)
    {
        var samples = await _sampleRepository.GetListAsync(s => s.TestRequestId == testRequestId);
        var sampleDtos = new List<TestRequestSampleDto>();

        // Pre-load all related data for batch processing
        var sampleIds = samples.Select(s => s.Id).ToList();
        var allIndicatorLinks = await _sampleIndicatorRepository.GetListAsync(i => sampleIds.Contains(i.TestRequestSampleId));

        var indicatorIds = allIndicatorLinks.Select(i => i.IndicatorId).Distinct().ToList();
        var indicators = await LoadIndicatorsAsync(indicatorIds);

        // Load indicator groups to get LaboratoryId for auto-fill
        var indicatorGroupIds = indicators.Values
            .Where(i => i.IndicatorGroupId != Guid.Empty)
            .Select(i => i.IndicatorGroupId)
            .Distinct()
            .ToList();
        var indicatorGroups = await LoadIndicatorGroupsAsync(indicatorGroupIds);

        var laboratoryIds = allIndicatorLinks.Where(i => i.LaboratoryId.HasValue).Select(i => i.LaboratoryId!.Value).Distinct().ToList();
        var laboratories = await LoadLaboratoriesAsync(laboratoryIds);

        var testMethodIds = allIndicatorLinks.Where(i => i.TestMethodId.HasValue).Select(i => i.TestMethodId!.Value).Distinct().ToList();
        var testMethods = await LoadTestMethodsAsync(testMethodIds);

        var specimenTypeIds = samples.Where(s => s.SpecimenTypeId.HasValue).Select(s => s.SpecimenTypeId!.Value).Distinct().ToList();
        var specimenTypes = await LoadSpecimenTypesAsync(specimenTypeIds);

        // Load lab task indicators for approval status
        var indicatorLinkIds = allIndicatorLinks.Select(i => i.Id).ToList();
        var labTaskIndicators = await LoadLabTaskIndicatorsAsync(indicatorLinkIds);

        // Load indicator images (batch load for all lab task indicators)
        var labTaskIndicatorIds = labTaskIndicators.Values.Select(x => x.Id).ToList();
        var indicatorImages = await LoadIndicatorImagesAsync(labTaskIndicatorIds);

        foreach (var sample in samples.OrderBy(s => s.SortOrder))
        {
            var sampleDto = _objectMapper.Map<TestRequestSample, TestRequestSampleDto>(sample);

            // Set specimen type name
            if (sample.SpecimenTypeId.HasValue && specimenTypes.TryGetValue(sample.SpecimenTypeId.Value, out var specimenType))
            {
                sampleDto.SpecimenTypeName = specimenType.Name;
            }

            // Load indicators for this sample
            var sampleIndicatorLinks = allIndicatorLinks.Where(i => i.TestRequestSampleId == sample.Id).ToList();
            sampleDto.Indicators = new List<TestRequestSampleIndicatorDto>();

            foreach (var indicatorLink in sampleIndicatorLinks)
            {
                var labTaskIndicator = labTaskIndicators.GetValueOrDefault(indicatorLink.Id);
                var images = labTaskIndicator != null
                    ? indicatorImages.GetValueOrDefault(labTaskIndicator.Id, new List<IndicatorImage>())
                    : new List<IndicatorImage>();

                var indicator = indicators.GetValueOrDefault(indicatorLink.IndicatorId);
                var indicatorGroup = indicator != null
                    ? indicatorGroups.GetValueOrDefault(indicator.IndicatorGroupId)
                    : null;

                var indicatorDto = CreateIndicatorDto(
                    indicatorLink,
                    indicator,
                    indicatorGroup,
                    indicatorLink.LaboratoryId.HasValue ? laboratories.GetValueOrDefault(indicatorLink.LaboratoryId.Value) : null,
                    indicatorLink.TestMethodId.HasValue ? testMethods.GetValueOrDefault(indicatorLink.TestMethodId.Value) : null,
                    labTaskIndicator,
                    images
                );
                sampleDto.Indicators.Add(indicatorDto);
            }

            sampleDto.TotalIndicatorCost = sampleDto.Indicators.Sum(i => i.UnitPrice);
            sampleDtos.Add(sampleDto);
        }

        return sampleDtos;
    }

    /// <summary>
    /// Calculates total cost for multiple test requests (batch operation)
    /// </summary>
    public async Task EnrichWithTotalCostsAsync(List<TestRequestDto> dtos)
    {
        if (dtos.Count == 0) return;

        var testRequestIds = dtos.Select(d => d.Id).ToList();
        var samples = await _sampleRepository.GetListAsync(s => testRequestIds.Contains(s.TestRequestId));
        var sampleIds = samples.Select(s => s.Id).ToList();
        var indicators = await _sampleIndicatorRepository.GetListAsync(i => sampleIds.Contains(i.TestRequestSampleId));

        // Group costs by test request
        var costsByRequest = samples
            .GroupBy(s => s.TestRequestId)
            .ToDictionary(
                g => g.Key,
                g => indicators.Where(i => g.Any(s => s.Id == i.TestRequestSampleId)).Sum(i => i.UnitPrice)
            );

        foreach (var dto in dtos)
        {
            dto.TotalCost = costsByRequest.GetValueOrDefault(dto.Id, 0);
        }
    }

    #region Private Helper Methods

    private async Task<Dictionary<Guid, Indicator>> LoadIndicatorsAsync(List<Guid> ids)
    {
        if (ids.Count == 0) return new Dictionary<Guid, Indicator>();
        var items = await _indicatorRepository.GetListAsync(x => ids.Contains(x.Id));
        return items.ToDictionary(x => x.Id);
    }

    private async Task<Dictionary<Guid, IndicatorGroup>> LoadIndicatorGroupsAsync(List<Guid> ids)
    {
        if (ids.Count == 0) return new Dictionary<Guid, IndicatorGroup>();
        var items = await _indicatorGroupRepository.GetListAsync(x => ids.Contains(x.Id));
        return items.ToDictionary(x => x.Id);
    }

    private async Task<Dictionary<Guid, (string Code, string Name)>> LoadLaboratoriesAsync(List<Guid> ids)
    {
        if (ids.Count == 0) return new Dictionary<Guid, (string Code, string Name)>();
        var labs = await _laboratoryRepository.GetListAsync(x => ids.Contains(x.Id));
        var departmentIds = labs.Select(l => l.DepartmentId).Distinct().ToList();
        var departments = await _departmentRepository.GetListAsync(d => departmentIds.Contains(d.Id));
        var deptDict = departments.ToDictionary(d => d.Id, d => d.Name);

        return labs.ToDictionary(
            x => x.Id,
            x => (x.Code, deptDict.TryGetValue(x.DepartmentId, out var name) ? name : ""));
    }

    private async Task<Dictionary<Guid, TestMethod>> LoadTestMethodsAsync(List<Guid> ids)
    {
        if (ids.Count == 0) return new Dictionary<Guid, TestMethod>();
        var items = await _testMethodRepository.GetListAsync(x => ids.Contains(x.Id));
        return items.ToDictionary(x => x.Id);
    }

    private async Task<Dictionary<Guid, SpecimenType>> LoadSpecimenTypesAsync(List<Guid> ids)
    {
        if (ids.Count == 0) return new Dictionary<Guid, SpecimenType>();
        var items = await _specimenTypeRepository.GetListAsync(x => ids.Contains(x.Id));
        return items.ToDictionary(x => x.Id);
    }

    private async Task<Dictionary<Guid, LaboratoryTaskIndicator>> LoadLabTaskIndicatorsAsync(List<Guid> sampleIndicatorIds)
    {
        if (sampleIndicatorIds.Count == 0) return new Dictionary<Guid, LaboratoryTaskIndicator>();
        var items = await _laboratoryTaskIndicatorRepository.GetListAsync(x => sampleIndicatorIds.Contains(x.TestRequestSampleIndicatorId));
        return items.ToDictionary(x => x.TestRequestSampleIndicatorId);
    }

    private async Task<Dictionary<Guid, List<IndicatorImage>>> LoadIndicatorImagesAsync(List<Guid> labTaskIndicatorIds)
    {
        if (labTaskIndicatorIds.Count == 0) return new Dictionary<Guid, List<IndicatorImage>>();
        var images = await _indicatorImageRepository.GetListAsync(x => labTaskIndicatorIds.Contains(x.LaboratoryTaskIndicatorId));
        return images
            .GroupBy(x => x.LaboratoryTaskIndicatorId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.CreationTime).ToList());
    }

    private static TestRequestSampleIndicatorDto CreateIndicatorDto(
        TestRequestSampleIndicator indicatorLink,
        Indicator? indicator,
        IndicatorGroup? indicatorGroup,
        (string Code, string Name)? laboratory,
        TestMethod? testMethod,
        LaboratoryTaskIndicator? labTaskIndicator,
        List<IndicatorImage> images)
    {
        return new TestRequestSampleIndicatorDto
        {
            Id = indicatorLink.Id,
            TestRequestSampleId = indicatorLink.TestRequestSampleId,
            IndicatorId = indicatorLink.IndicatorId,
            IndicatorCode = indicator?.Code ?? string.Empty,
            IndicatorName = indicator?.Name ?? string.Empty,
            IndicatorFormattedName = indicator?.FormattedName,
            IndicatorGroupName = indicatorGroup?.Name,
            IndicatorGroupLaboratoryId = indicatorGroup?.LaboratoryId,
            TestMethodId = indicatorLink.TestMethodId,
            TestMethodCode = testMethod?.Code,
            TestMethodName = testMethod?.Name,
            TestMethodFormattedName = testMethod?.FormattedName,
            UnitPrice = indicatorLink.UnitPrice,
            OriginalUnitPrice = indicatorLink.OriginalUnitPrice,
            Notes = indicatorLink.Notes,
            LaboratoryId = indicatorLink.LaboratoryId,
            LaboratoryName = laboratory?.Name,
            TransferDate = indicatorLink.TransferDate,
            RequestDate = indicatorLink.RequestDate,
            SentToDepartment = indicatorLink.SentToDepartment,
            ApprovalStatus = labTaskIndicator?.ApprovalStatus,
            TestDate = labTaskIndicator?.TestDate,
            ParentIndicatorId = indicator?.ParentIndicatorId,
            IsSentToQLKT = labTaskIndicator != null,
            IsSubcontract = indicatorLink.IsSubcontract,
            Images = images.Select(x => new Testing.IndicatorImageDto
            {
                Id = x.Id,
                LaboratoryTaskIndicatorId = x.LaboratoryTaskIndicatorId,
                FileId = x.FileId,
                FileName = x.FileName,
                FileSize = x.FileSize,
                Url = $"/api/v1/files/{x.FileId}",
                CreationTime = x.CreationTime,
                CreatorId = x.CreatorId
            }).ToList()
        };
    }

    #endregion
}
