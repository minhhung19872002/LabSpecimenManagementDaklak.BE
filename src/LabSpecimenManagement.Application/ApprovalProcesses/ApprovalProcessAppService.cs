using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.ApprovalProcesses;

[Authorize(LabSpecimenManagementPermissions.ApprovalProcesses.Default)]
public class ApprovalProcessAppService : ApplicationService, IApprovalProcessAppService
{
    private readonly IRepository<ApprovalProcess, Guid> _processRepository;
    private readonly IRepository<ApprovalProcessStep, Guid> _stepRepository;
    private readonly IRepository<ApprovalSnapshot, Guid> _snapshotRepository;
    private readonly IRepository<ApprovalSnapshotStep, Guid> _snapshotStepRepository;

    public ApprovalProcessAppService(
        IRepository<ApprovalProcess, Guid> processRepository,
        IRepository<ApprovalProcessStep, Guid> stepRepository,
        IRepository<ApprovalSnapshot, Guid> snapshotRepository,
        IRepository<ApprovalSnapshotStep, Guid> snapshotStepRepository)
    {
        _processRepository = processRepository;
        _stepRepository = stepRepository;
        _snapshotRepository = snapshotRepository;
        _snapshotStepRepository = snapshotStepRepository;
    }

    public async Task<PagedResultDto<ApprovalProcessDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        var query = await _processRepository.GetQueryableAsync();
        var totalCount = query.Count();
        var items = query
            .OrderByDescending(x => x.CreationTime)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        var dtos = ObjectMapper.Map<List<ApprovalProcess>, List<ApprovalProcessDto>>(items);

        var processIds = items.Select(x => x.Id).ToList();
        var stepsQuery = await _stepRepository.GetQueryableAsync();
        var allSteps = stepsQuery
            .Where(s => processIds.Contains(s.ApprovalProcessId))
            .OrderBy(s => s.StepOrder)
            .ToList();

        foreach (var dto in dtos)
        {
            dto.Steps = allSteps
                .Where(s => s.ApprovalProcessId == dto.Id)
                .Select(s => ObjectMapper.Map<ApprovalProcessStep, ApprovalProcessStepDto>(s))
                .ToList();
        }

        return new PagedResultDto<ApprovalProcessDto>(totalCount, dtos);
    }

    public async Task<ApprovalProcessDto> GetAsync(Guid id)
    {
        var entity = await _processRepository.GetAsync(id);
        var dto = ObjectMapper.Map<ApprovalProcess, ApprovalProcessDto>(entity);

        var stepsQuery = await _stepRepository.GetQueryableAsync();
        dto.Steps = stepsQuery
            .Where(s => s.ApprovalProcessId == id)
            .OrderBy(s => s.StepOrder)
            .ToList()
            .Select(s => ObjectMapper.Map<ApprovalProcessStep, ApprovalProcessStepDto>(s))
            .ToList();

        return dto;
    }

    [Authorize(LabSpecimenManagementPermissions.ApprovalProcesses.Create)]
    public async Task<ApprovalProcessDto> CreateAsync(CreateUpdateApprovalProcessDto input)
    {
        await ValidateDuplicateProcessTypeAsync(input.ProcessType);

        var entity = new ApprovalProcess(
            GuidGenerator.Create(),
            input.ProcessType,
            input.Scope);

        await _processRepository.InsertAsync(entity, autoSave: true);

        var sortedSteps = input.Steps.OrderBy(s => s.StepOrder).ToList();
        for (int i = 0; i < sortedSteps.Count; i++)
        {
            var step = new ApprovalProcessStep(
                GuidGenerator.Create(),
                entity.Id,
                i,
                sortedSteps[i].ApproverId,
                sortedSteps[i].ApproverName);
            await _stepRepository.InsertAsync(step, autoSave: true);
        }

        return await GetAsync(entity.Id);
    }

    [Authorize(LabSpecimenManagementPermissions.ApprovalProcesses.Edit)]
    public async Task<ApprovalProcessDto> UpdateAsync(Guid id, CreateUpdateApprovalProcessDto input)
    {
        var entity = await _processRepository.GetAsync(id);
        await ValidateDuplicateProcessTypeAsync(input.ProcessType, id);

        entity.ProcessType = input.ProcessType;
        entity.Scope = input.Scope;
        await _processRepository.UpdateAsync(entity, autoSave: true);

        var existingSteps = (await _stepRepository.GetQueryableAsync())
            .Where(x => x.ApprovalProcessId == id).ToList();
        foreach (var step in existingSteps)
        {
            await _stepRepository.DeleteAsync(step, autoSave: true);
        }

        var sortedSteps = input.Steps.OrderBy(s => s.StepOrder).ToList();
        for (int i = 0; i < sortedSteps.Count; i++)
        {
            var step = new ApprovalProcessStep(
                GuidGenerator.Create(),
                entity.Id,
                i,
                sortedSteps[i].ApproverId,
                sortedSteps[i].ApproverName);
            await _stepRepository.InsertAsync(step, autoSave: true);
        }

        return await GetAsync(entity.Id);
    }

    [Authorize(LabSpecimenManagementPermissions.ApprovalProcesses.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var existingSteps = (await _stepRepository.GetQueryableAsync())
            .Where(x => x.ApprovalProcessId == id).ToList();
        foreach (var step in existingSteps)
        {
            await _stepRepository.DeleteAsync(step, autoSave: true);
        }

        await _processRepository.DeleteAsync(id, autoSave: true);
    }

    public async Task<ApprovalProcessDto?> GetByProcessTypeAsync(ApprovalProcessType processType)
    {
        var query = await _processRepository.GetQueryableAsync();
        var entity = query.FirstOrDefault(x => x.ProcessType == processType && x.IsActive);

        if (entity == null) return null;

        var dto = ObjectMapper.Map<ApprovalProcess, ApprovalProcessDto>(entity);
        var stepsQuery = await _stepRepository.GetQueryableAsync();
        dto.Steps = stepsQuery
            .Where(s => s.ApprovalProcessId == entity.Id)
            .OrderBy(s => s.StepOrder)
            .ToList()
            .Select(s => ObjectMapper.Map<ApprovalProcessStep, ApprovalProcessStepDto>(s))
            .ToList();

        return dto;
    }

    public async Task<ApprovalSnapshotDto?> GetSnapshotAsync(Guid referenceId)
    {
        var snapshotQuery = await _snapshotRepository.GetQueryableAsync();
        var snapshot = snapshotQuery
            .Where(x => x.ReferenceId == referenceId)
            .OrderByDescending(x => x.CreationTime)
            .FirstOrDefault();

        if (snapshot == null) return null;

        var dto = ObjectMapper.Map<ApprovalSnapshot, ApprovalSnapshotDto>(snapshot);

        var stepsQuery = await _snapshotStepRepository.GetQueryableAsync();
        dto.Steps = stepsQuery
            .Where(s => s.ApprovalSnapshotId == snapshot.Id)
            .OrderBy(s => s.StepOrder)
            .ToList()
            .Select(s => ObjectMapper.Map<ApprovalSnapshotStep, ApprovalSnapshotStepDto>(s))
            .ToList();

        return dto;
    }

    [Authorize(LabSpecimenManagementPermissions.ApprovalProcesses.Edit)]
    public async Task ToggleActiveAsync(Guid id)
    {
        var entity = await _processRepository.GetAsync(id);

        if (!entity.IsActive)
        {
            await ValidateDuplicateProcessTypeAsync(entity.ProcessType, id);
        }

        entity.IsActive = !entity.IsActive;
        await _processRepository.UpdateAsync(entity, autoSave: true);
    }

    private async Task ValidateDuplicateProcessTypeAsync(ApprovalProcessType processType, Guid? excludeId = null)
    {
        var query = await _processRepository.GetQueryableAsync();
        var exists = query.Any(x =>
            x.ProcessType == processType &&
            x.IsActive &&
            (excludeId == null || x.Id != excludeId));

        if (exists)
        {
            throw new UserFriendlyException("Đã có quy trình cùng loại đang hoạt động.");
        }
    }
}
