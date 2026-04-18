using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.TestRequests;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// Helper service for enriching LaboratoryTask DTOs with related data
/// Centralizes data loading logic to reduce duplication in LaboratoryTaskAppService
/// </summary>
public class LaboratoryTaskDtoEnricher : ITransientDependency
{
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;
    private readonly IRepository<Laboratory, Guid> _laboratoryRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IRepository<LaboratoryTaskIndicator, Guid> _taskIndicatorRepository;
    private readonly IRepository<LaboratoryTaskSupporter, Guid> _supporterRepository;
    private readonly IRepository<TestRequestSampleIndicator, Guid> _sampleIndicatorRepository;
    private readonly IRepository<TestRequestSample, Guid> _sampleRepository;
    private readonly IRepository<Indicator, Guid> _indicatorRepository;
    private readonly IRepository<TestMethod, Guid> _testMethodRepository;
    private readonly IRepository<Note, Guid> _noteRepository;
    private readonly IUserNameProvider _userNameProvider;

    public LaboratoryTaskDtoEnricher(
        IRepository<TestRequest, Guid> testRequestRepository,
        IRepository<Laboratory, Guid> laboratoryRepository,
        IRepository<Department, Guid> departmentRepository,
        IRepository<LaboratoryTaskIndicator, Guid> taskIndicatorRepository,
        IRepository<LaboratoryTaskSupporter, Guid> supporterRepository,
        IRepository<TestRequestSampleIndicator, Guid> sampleIndicatorRepository,
        IRepository<TestRequestSample, Guid> sampleRepository,
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<TestMethod, Guid> testMethodRepository,
        IRepository<Note, Guid> noteRepository,
        IUserNameProvider userNameProvider)
    {
        _testRequestRepository = testRequestRepository;
        _laboratoryRepository = laboratoryRepository;
        _departmentRepository = departmentRepository;
        _taskIndicatorRepository = taskIndicatorRepository;
        _supporterRepository = supporterRepository;
        _sampleIndicatorRepository = sampleIndicatorRepository;
        _sampleRepository = sampleRepository;
        _indicatorRepository = indicatorRepository;
        _testMethodRepository = testMethodRepository;
        _noteRepository = noteRepository;
        _userNameProvider = userNameProvider;
    }

    /// <summary>
    /// Enriches a list of LaboratoryTaskDto with related data (batch operation)
    /// </summary>
    public async Task EnrichListAsync(List<LaboratoryTaskDto> dtos, List<LaboratoryTask> entities)
    {
        if (dtos.Count == 0) return;

        // Batch load all related data
        var testRequestIds = entities.Select(e => e.TestRequestId).Distinct().ToList();
        var testRequests = await LoadTestRequestsAsync(testRequestIds);

        var laboratoryIds = entities.Select(e => e.LaboratoryId).Distinct().ToList();
        var laboratories = await LoadLaboratoriesAsync(laboratoryIds);

        // Enrich each DTO
        foreach (var dto in dtos)
        {
            var entity = entities.FirstOrDefault(e => e.Id == dto.Id);
            if (entity == null) continue;

            // Set test request info
            if (testRequests.TryGetValue(entity.TestRequestId, out var testRequest))
            {
                dto.TestRequestNumber = testRequest.RequestNumber;
                dto.CustomerName = testRequest.CustomerName;
            }

            // Set laboratory info
            if (laboratories.TryGetValue(entity.LaboratoryId, out var laboratory))
            {
                dto.LaboratoryName = laboratory.Name;
                dto.LaboratoryCode = laboratory.Code;
            }
        }

        // Enrich with user names
        await _userNameProvider.EnrichWithUserNamesAsync(
            dtos,
            dto => entities.FirstOrDefault(e => e.Id == dto.Id)?.TechnicalManagerId,
            (dto, name) => dto.TechnicalManagerName = name);
    }

    /// <summary>
    /// Loads indicators for a laboratory task with full details
    /// </summary>
    public async Task<List<LaboratoryTaskIndicatorDto>> LoadIndicatorsAsync(Guid taskId)
    {
        var taskIndicators = await _taskIndicatorRepository.GetListAsync(i => i.LaboratoryTaskId == taskId);
        if (taskIndicators.Count == 0) return new List<LaboratoryTaskIndicatorDto>();

        // Batch load related data
        var sampleIndicatorIds = taskIndicators.Select(i => i.TestRequestSampleIndicatorId).ToList();
        var sampleIndicators = await LoadSampleIndicatorsAsync(sampleIndicatorIds);

        var sampleIds = sampleIndicators.Values.Select(si => si.TestRequestSampleId).Distinct().ToList();
        var samples = await LoadSamplesAsync(sampleIds);

        var indicatorIds = sampleIndicators.Values.Select(si => si.IndicatorId).Distinct().ToList();
        var indicators = await LoadIndicatorsAsync(indicatorIds);

        var testMethodIds = taskIndicators.Where(i => i.MethodId.HasValue).Select(i => i.MethodId!.Value).Distinct().ToList();
        var testMethods = await LoadTestMethodsAsync(testMethodIds);

        // Get technician names
        var technicianIds = taskIndicators.Where(i => i.TechnicianId.HasValue).Select(i => i.TechnicianId).ToList();
        var technicianNames = await _userNameProvider.GetUserNamesAsync(technicianIds);

        // Batch load Master Data Notes
        var allMasterDataNoteIds = taskIndicators
            .Where(i => !string.IsNullOrEmpty(i.MasterDataNoteIds))
            .SelectMany(i => ParseNoteIds(i.MasterDataNoteIds))
            .Distinct()
            .ToList();
        var masterDataNotes = await LoadNotesAsync(allMasterDataNoteIds);

        var result = new List<LaboratoryTaskIndicatorDto>();

        foreach (var taskIndicator in taskIndicators)
        {
            var dto = new LaboratoryTaskIndicatorDto
            {
                Id = taskIndicator.Id,
                TestRequestSampleIndicatorId = taskIndicator.TestRequestSampleIndicatorId,
                MethodId = taskIndicator.MethodId,
                TechnicianId = taskIndicator.TechnicianId,
                TestDate = taskIndicator.TestDate,
                Result = taskIndicator.Result,
                FreeTextNote = taskIndicator.FreeTextNote,
                MasterDataNoteIds = taskIndicator.MasterDataNoteIds,
                Unit = taskIndicator.Unit,
                EntryDate = taskIndicator.EntryDate,
                EnteredById = taskIndicator.EnteredById,
                ApprovalStatus = taskIndicator.ApprovalStatus,
                ApprovalDate = taskIndicator.ApprovalDate,
                ApprovedById = taskIndicator.ApprovedById,
                RejectionReason = taskIndicator.RejectionReason,
                IsSentToQuality = taskIndicator.IsSentToQuality,
                StandardId = taskIndicator.StandardId,
                AssignedDate = taskIndicator.AssignedDate,
                IsWaitingForSharedSample = taskIndicator.IsWaitingForSharedSample
            };

            // Set sample indicator info
            if (sampleIndicators.TryGetValue(taskIndicator.TestRequestSampleIndicatorId, out var sampleIndicator))
            {
                dto.UnitPrice = sampleIndicator.UnitPrice;
                dto.IndicatorId = sampleIndicator.IndicatorId;
                dto.SampleId = sampleIndicator.TestRequestSampleId;

                // Set sample info
                if (samples.TryGetValue(sampleIndicator.TestRequestSampleId, out var sample))
                {
                    dto.SampleCode = sample.SampleCode;
                    dto.SampleName = sample.SampleName;
                    dto.SpecimenTypeId = sample.SpecimenTypeId;
                }

                // Set indicator info
                if (indicators.TryGetValue(sampleIndicator.IndicatorId, out var indicator))
                {
                    dto.IndicatorCode = indicator.Code;
                    dto.IndicatorName = indicator.Name;
                    dto.IndicatorFormattedName = indicator.FormattedName;
                    dto.ParentIndicatorId = indicator.ParentIndicatorId;
                }
            }

            // Set test method info
            var isSubContractMethod = !string.IsNullOrWhiteSpace(taskIndicator.SubContractMethodName);
            if (taskIndicator.MethodId.HasValue && testMethods.TryGetValue(taskIndicator.MethodId.Value, out var testMethod))
            {
                if (!isSubContractMethod)
                {
                    dto.MethodName = testMethod.Name;
                    dto.MethodFormattedName = testMethod.FormattedName;
                }

                isSubContractMethod = isSubContractMethod || string.Equals(testMethod.Name?.Trim(), "-", StringComparison.Ordinal);
            }

            if (!string.IsNullOrWhiteSpace(taskIndicator.SubContractMethodName))
            {
                dto.MethodName = taskIndicator.SubContractMethodName;
                dto.MethodFormattedName = null;
            }

            dto.IsSubContractMethod = isSubContractMethod;
            dto.SubContractMethodName = taskIndicator.SubContractMethodName;
            dto.SubContractNumber = taskIndicator.SubContractNumber;
            dto.SubContractCost = taskIndicator.SubContractCost;

            // Set technician name
            if (taskIndicator.TechnicianId.HasValue && technicianNames.TryGetValue(taskIndicator.TechnicianId.Value, out var techName))
            {
                dto.TechnicianName = techName;
            }

            // Set Master Data Note Contents
            if (!string.IsNullOrEmpty(taskIndicator.MasterDataNoteIds))
            {
                var noteIds = ParseNoteIds(taskIndicator.MasterDataNoteIds);
                dto.MasterDataNoteContents = noteIds
                    .Where(id => masterDataNotes.ContainsKey(id))
                    .Select(id => masterDataNotes[id].Content)
                    .ToList();
            }

            result.Add(dto);
        }

        return result;
    }

    /// <summary>
    /// Loads supporters for a laboratory task
    /// </summary>
    public async Task<List<LaboratoryTaskSupporterDto>> LoadSupportersAsync(Guid taskId)
    {
        var supporters = await _supporterRepository.GetListAsync(s => s.LaboratoryTaskId == taskId);
        if (supporters.Count == 0) return new List<LaboratoryTaskSupporterDto>();

        var userIds = supporters.Select(s => (Guid?)s.UserId).ToList();
        var userNames = await _userNameProvider.GetUserNamesAsync(userIds);

        return supporters.Select(s => new LaboratoryTaskSupporterDto
        {
            Id = s.Id,
            LaboratoryTaskId = s.LaboratoryTaskId,
            UserId = s.UserId,
            UserName = userNames.GetValueOrDefault(s.UserId, string.Empty),
            CreationTime = s.CreationTime
        }).ToList();
    }

    #region Private Helper Methods

    private async Task<Dictionary<Guid, TestRequest>> LoadTestRequestsAsync(List<Guid> ids)
    {
        if (ids.Count == 0) return new Dictionary<Guid, TestRequest>();
        var items = await _testRequestRepository.GetListAsync(x => ids.Contains(x.Id));
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

    private async Task<Dictionary<Guid, TestRequestSampleIndicator>> LoadSampleIndicatorsAsync(List<Guid> ids)
    {
        if (ids.Count == 0) return new Dictionary<Guid, TestRequestSampleIndicator>();
        var items = await _sampleIndicatorRepository.GetListAsync(x => ids.Contains(x.Id));
        return items.ToDictionary(x => x.Id);
    }

    private async Task<Dictionary<Guid, TestRequestSample>> LoadSamplesAsync(List<Guid> ids)
    {
        if (ids.Count == 0) return new Dictionary<Guid, TestRequestSample>();
        var items = await _sampleRepository.GetListAsync(x => ids.Contains(x.Id));
        return items.ToDictionary(x => x.Id);
    }

    private async Task<Dictionary<Guid, Indicator>> LoadIndicatorsAsync(List<Guid> ids)
    {
        if (ids.Count == 0) return new Dictionary<Guid, Indicator>();
        var items = await _indicatorRepository.GetListAsync(x => ids.Contains(x.Id));
        return items.ToDictionary(x => x.Id);
    }

    private async Task<Dictionary<Guid, TestMethod>> LoadTestMethodsAsync(List<Guid> ids)
    {
        if (ids.Count == 0) return new Dictionary<Guid, TestMethod>();
        var items = await _testMethodRepository.GetListAsync(x => ids.Contains(x.Id));
        return items.ToDictionary(x => x.Id);
    }

    private async Task<Dictionary<Guid, Note>> LoadNotesAsync(List<Guid> ids)
    {
        if (ids.Count == 0) return new Dictionary<Guid, Note>();
        var items = await _noteRepository.GetListAsync(x => ids.Contains(x.Id));
        return items.ToDictionary(x => x.Id);
    }

    /// <summary>
    /// Parse comma-separated note IDs string to list of Guids
    /// </summary>
    private static List<Guid> ParseNoteIds(string? noteIdsString)
    {
        if (string.IsNullOrEmpty(noteIdsString))
            return new List<Guid>();

        return noteIdsString
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Guid.TryParse(s.Trim(), out var id) ? id : (Guid?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();
    }

    #endregion
}
