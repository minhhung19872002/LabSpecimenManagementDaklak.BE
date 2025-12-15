using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Users;

namespace LabSpecimenManagement.Tasks;

[RemoteService(IsEnabled = false)]
public class TaskAssignmentAppService :
    CrudAppService<
        TaskAssignment,
        TaskAssignmentDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateTaskAssignmentDto,
        UpdateTaskAssignmentDto>,
    ITaskAssignmentAppService
{
    private readonly ICurrentUser _currentUser;
    private readonly IRepository<Volo.Abp.Identity.IdentityUser, Guid> _userRepository;

    public TaskAssignmentAppService(
        IRepository<TaskAssignment, Guid> repository,
        ICurrentUser currentUser,
        IRepository<Volo.Abp.Identity.IdentityUser, Guid> userRepository)
        : base(repository)
    {
        _currentUser = currentUser;
        _userRepository = userRepository;
    }

    protected override async Task<TaskAssignmentDto> MapToGetOutputDtoAsync(TaskAssignment entity)
    {
        var dto = await base.MapToGetOutputDtoAsync(entity);
        await PopulateUserNamesAsync(dto, entity);
        return dto;
    }

    protected override async Task<TaskAssignmentDto> MapToGetListOutputDtoAsync(TaskAssignment entity)
    {
        var dto = await base.MapToGetListOutputDtoAsync(entity);
        await PopulateUserNamesAsync(dto, entity);
        return dto;
    }

    private async Task PopulateUserNamesAsync(TaskAssignmentDto dto, TaskAssignment entity)
    {
        // Populate AssignedToName
        var assignedTo = await _userRepository.FindAsync(entity.AssignedToId);
        if (assignedTo != null)
        {
            dto.AssignedToName = assignedTo.Name ?? assignedTo.UserName;
        }

        // Populate AssignedByName
        var assignedBy = await _userRepository.FindAsync(entity.AssignedById);
        if (assignedBy != null)
        {
            dto.AssignedByName = assignedBy.Name ?? assignedBy.UserName;
        }
    }

    public async Task<PagedResultDto<TaskAssignmentDto>> GetFilteredListAsync(GetTaskAssignmentListInput input)
    {
        var query = await Repository.GetQueryableAsync();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var filter = input.Filter.ToLower();
            query = query.Where(x =>
                x.TaskNumber.ToLower().Contains(filter) ||
                x.Title.ToLower().Contains(filter));
        }

        if (input.Status.HasValue)
        {
            query = query.Where(x => x.Status == input.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.Department))
        {
            query = query.Where(x => x.Department == input.Department);
        }

        if (input.Priority.HasValue)
        {
            query = query.Where(x => x.Priority == input.Priority.Value);
        }

        if (input.Type.HasValue)
        {
            query = query.Where(x => x.Type == input.Type.Value);
        }

        if (input.AssignedToId.HasValue)
        {
            query = query.Where(x => x.AssignedToId == input.AssignedToId.Value);
        }

        if (input.DueDateFrom.HasValue)
        {
            query = query.Where(x => x.DueDate >= input.DueDateFrom.Value);
        }

        if (input.DueDateTo.HasValue)
        {
            query = query.Where(x => x.DueDate <= input.DueDateTo.Value);
        }

        // Get total count before paging
        var totalCount = query.Count();

        // Apply sorting
        if (!string.IsNullOrWhiteSpace(input.Sorting))
        {
            query = query.OrderBy(input.Sorting);
        }
        else
        {
            query = query.OrderByDescending(x => x.CreationTime);
        }

        // Apply paging
        var items = query
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        // Map to DTOs and populate user names
        var dtos = new List<TaskAssignmentDto>();
        foreach (var item in items)
        {
            var dto = await MapToGetListOutputDtoAsync(item);
            dtos.Add(dto);
        }

        return new PagedResultDto<TaskAssignmentDto>(totalCount, dtos);
    }

    protected override async Task<TaskAssignment> MapToEntityAsync(CreateTaskAssignmentDto createInput)
    {
        var taskNumber = await GenerateTaskNumberAsync();

        var entity = new TaskAssignment(
            GuidGenerator.Create(),
            taskNumber,
            createInput.Title,
            createInput.Type,
            createInput.Priority,
            createInput.AssignedToId,
            _currentUser.Id ?? Guid.Empty,
            createInput.Department,
            createInput.DueDate
        );

        entity.Description = createInput.Description;
        entity.RelatedTestRequestIds = createInput.RelatedTestRequestIds;
        entity.RelatedSpecimenIds = createInput.RelatedSpecimenIds;
        entity.RelatedTestIds = createInput.RelatedTestIds;
        entity.Notes = createInput.Notes;

        return entity;
    }

    public async Task<TaskAssignmentDto> AcceptAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);
        entity.Status = TaskAssignmentStatus.Accepted;

        await Repository.UpdateAsync(entity);
        return await MapToGetOutputDtoAsync(entity);
    }

    public async Task<TaskAssignmentDto> RejectAsync(Guid id, string? reason)
    {
        var entity = await Repository.GetAsync(id);
        entity.Status = TaskAssignmentStatus.Rejected;
        if (!string.IsNullOrEmpty(reason))
        {
            entity.Notes = (entity.Notes ?? "") + $"\n[Rejected]: {reason}";
        }

        await Repository.UpdateAsync(entity);
        return await MapToGetOutputDtoAsync(entity);
    }

    public async Task<TaskAssignmentDto> StartAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);
        entity.Status = TaskAssignmentStatus.InProgress;
        entity.StartDate = DateTime.Now;

        await Repository.UpdateAsync(entity);
        return await MapToGetOutputDtoAsync(entity);
    }

    public async Task<TaskAssignmentDto> CompleteAsync(Guid id)
    {
        var entity = await Repository.GetAsync(id);
        entity.Status = TaskAssignmentStatus.Completed;
        entity.CompletedDate = DateTime.Now;
        entity.Progress = 100;

        await Repository.UpdateAsync(entity);
        return await MapToGetOutputDtoAsync(entity);
    }

    public async Task<TaskAssignmentDto> UpdateProgressAsync(Guid id, int progress)
    {
        var entity = await Repository.GetAsync(id);
        entity.Progress = Math.Clamp(progress, 0, 100);

        await Repository.UpdateAsync(entity);
        return await MapToGetOutputDtoAsync(entity);
    }

    public async Task<ListResultDto<TaskAssignmentDto>> GetMyTasksAsync()
    {
        var currentUserId = _currentUser.Id ?? Guid.Empty;
        var query = await Repository.GetQueryableAsync();
        var items = query
            .Where(x => x.AssignedToId == currentUserId)
            .OrderByDescending(x => x.CreationTime)
            .ToList();

        return new ListResultDto<TaskAssignmentDto>(
            ObjectMapper.Map<System.Collections.Generic.List<TaskAssignment>, System.Collections.Generic.List<TaskAssignmentDto>>(items)
        );
    }

    public async Task<ListResultDto<TaskAssignmentDto>> GetByAssigneeAsync(Guid assigneeId)
    {
        var query = await Repository.GetQueryableAsync();
        var items = query
            .Where(x => x.AssignedToId == assigneeId)
            .OrderByDescending(x => x.CreationTime)
            .ToList();

        return new ListResultDto<TaskAssignmentDto>(
            ObjectMapper.Map<System.Collections.Generic.List<TaskAssignment>, System.Collections.Generic.List<TaskAssignmentDto>>(items)
        );
    }

    private async Task<string> GenerateTaskNumberAsync()
    {
        var today = DateTime.Now;
        var prefix = $"TASK-{today:yyyyMMdd}";
        var count = await Repository.CountAsync(x => x.TaskNumber.StartsWith(prefix));
        var sequence = (count + 1).ToString("D4");
        return $"{prefix}-{sequence}";
    }
}
