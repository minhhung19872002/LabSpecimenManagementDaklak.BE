using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.SupplementaryTasks.Dtos;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Data;
using Volo.Abp.Identity;

namespace LabSpecimenManagement.SupplementaryTasks;

[Authorize]
public class SupplementaryTaskAppService : ApplicationService, ISupplementaryTaskAppService
{
    private readonly IRepository<SupplementaryTask, Guid> _repository;
    private readonly IRepository<SupplementaryTaskLog, Guid> _logRepository;
    private readonly IIdentityUserRepository _identityUserRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;
    private bool? _isDirectorCache;

    public SupplementaryTaskAppService(
        IRepository<SupplementaryTask, Guid> repository,
        IRepository<SupplementaryTaskLog, Guid> logRepository,
        IIdentityUserRepository identityUserRepository,
        IRepository<Department, Guid> departmentRepository)
    {
        _repository = repository;
        _logRepository = logRepository;
        _identityUserRepository = identityUserRepository;
        _departmentRepository = departmentRepository;
    }

    public async Task<PagedResultDto<SupplementaryTaskDto>> GetListAsync(GetSupplementaryTaskListInput input)
    {
        var isDirector = await IsDirectorAsync();

        var query = await _repository.GetQueryableAsync();
        if (!isDirector)
            query = query.Where(t => t.AssignedToId == CurrentUser.Id!.Value);

        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var s = input.Search.ToLower();
            query = query.Where(t =>
                t.Title.ToLower().Contains(s) ||
                t.AssignedToName.ToLower().Contains(s) ||
                (t.Description != null && t.Description.ToLower().Contains(s)));
        }
        if (input.Status.HasValue)
        {
            if (input.Status.Value == SupplementaryTaskStatus.Overdue)
            {
                var today = DateTime.Today;
                query = query.Where(t => t.Status == SupplementaryTaskStatus.Overdue
                    || (t.Status == SupplementaryTaskStatus.Pending && t.DueDate.HasValue && t.DueDate.Value < today));
            }
            else if (input.Status.Value == SupplementaryTaskStatus.Pending)
            {
                var today = DateTime.Today;
                query = query.Where(t => t.Status == SupplementaryTaskStatus.Pending
                    && !(t.DueDate.HasValue && t.DueDate.Value < today));
            }
            else
            {
                query = query.Where(t => t.Status == input.Status.Value);
            }
        }
        if (input.Priority.HasValue)
            query = query.Where(t => t.Priority == input.Priority.Value);
        if (input.AssignedToId.HasValue && isDirector)
            query = query.Where(t => t.AssignedToId == input.AssignedToId.Value);

        var totalCount = await AsyncExecuter.CountAsync(query);

        query = input.SortField == "creationTime"
            ? (input.SortDescending
                ? query.OrderByDescending(t => t.CreationTime)
                : query.OrderBy(t => t.CreationTime))
            : (input.SortDescending
                ? query.OrderBy(t => t.DueDate == null ? 0 : 1).ThenByDescending(t => t.DueDate).ThenByDescending(t => t.CreationTime)
                : query.OrderBy(t => t.DueDate == null ? 1 : 0).ThenBy(t => t.DueDate).ThenByDescending(t => t.CreationTime));

        var tasks = await AsyncExecuter.ToListAsync(query.Skip(input.SkipCount).Take(input.MaxResultCount));
        return new PagedResultDto<SupplementaryTaskDto>(totalCount, tasks.Select(MapToDto).ToList());
    }

    public async Task<SupplementaryTaskStatsDto> GetStatsAsync()
    {
        var isDirector = await IsDirectorAsync();
        var query = await _repository.GetQueryableAsync();
        if (!isDirector)
            query = query.Where(t => t.AssignedToId == CurrentUser.Id!.Value);

        var today = DateTime.Today;
        return new SupplementaryTaskStatsDto
        {
            Total = await AsyncExecuter.CountAsync(query),
            Pending = await AsyncExecuter.CountAsync(query.Where(t => t.Status == SupplementaryTaskStatus.Pending && !(t.DueDate.HasValue && t.DueDate.Value < today))),
            Overdue = await AsyncExecuter.CountAsync(query.Where(t => t.Status == SupplementaryTaskStatus.Overdue || (t.Status == SupplementaryTaskStatus.Pending && t.DueDate.HasValue && t.DueDate.Value < today))),
            Completed = await AsyncExecuter.CountAsync(query.Where(t => t.Status == SupplementaryTaskStatus.Completed)),
            WaitingForConfirmation = await AsyncExecuter.CountAsync(query.Where(t => t.Status == SupplementaryTaskStatus.WaitingForConfirmation)),
            Rejected = await AsyncExecuter.CountAsync(query.Where(t => t.Status == SupplementaryTaskStatus.Rejected)),
            Cancelled = await AsyncExecuter.CountAsync(query.Where(t => t.Status == SupplementaryTaskStatus.Cancelled)),
            IsDirector = isDirector
        };
    }

    public async Task<SupplementaryTaskDto> CreateAsync(CreateSupplementaryTaskDto input)
    {
        if (!await IsDirectorAsync())
            throw new UserFriendlyException("Chỉ Ban giám đốc mới có thể tạo công việc bổ sung.");

        var assignedUser = await _identityUserRepository.GetAsync(input.AssignedToId);
        var assignedName = $"{assignedUser.Surname} {assignedUser.Name}".Trim();
        if (string.IsNullOrWhiteSpace(assignedName)) assignedName = assignedUser.UserName ?? string.Empty;

        var task = new SupplementaryTask(
            GuidGenerator.Create(),
            input.Title,
            input.AssignedToId,
            assignedName,
            input.Priority,
            input.DueDate
        );
        task.Description = input.Description;
        task.ImageUrls = input.ImageUrls?.Count > 0
            ? JsonSerializer.Serialize(input.ImageUrls)
            : null;

        await _repository.InsertAsync(task, autoSave: true);
        await _logRepository.InsertAsync(new SupplementaryTaskLog(
            GuidGenerator.Create(), task.Id, null, SupplementaryTaskStatus.Pending,
            CurrentUser.Id!.Value, CurrentUser.Name ?? CurrentUser.UserName ?? ""), autoSave: true);

        return MapToDto(task);
    }

    public async Task<SupplementaryTaskDto> UpdateAsync(Guid id, UpdateSupplementaryTaskDto input)
    {
        if (!await IsDirectorAsync())
            throw new UserFriendlyException("Chỉ Ban giám đốc mới có thể chỉnh sửa công việc bổ sung.");

        var task = await _repository.GetAsync(id);
        var oldStatus = task.Status;

        task.Title = input.Title;
        task.Description = input.Description;
        task.DueDate = input.DueDate;
        task.Priority = input.Priority;
        task.ImageUrls = input.ImageUrls?.Count > 0
            ? JsonSerializer.Serialize(input.ImageUrls)
            : null;

        // Handle assignee change
        if (task.AssignedToId != input.AssignedToId)
        {
            var newUser = await _identityUserRepository.GetAsync(input.AssignedToId);
            task.AssignedToId = input.AssignedToId;
            var newName = $"{newUser.Surname} {newUser.Name}".Trim();
            task.AssignedToName = string.IsNullOrWhiteSpace(newName) ? newUser.UserName ?? string.Empty : newName;
        }

        // Handle status change
        if (task.Status != input.Status && input.Status != SupplementaryTaskStatus.Overdue
            && input.Status != SupplementaryTaskStatus.Cancelled)
        {
            task.Status = input.Status;
        }

        await _repository.UpdateAsync(task, autoSave: true);

        if (oldStatus != task.Status)
        {
            await _logRepository.InsertAsync(new SupplementaryTaskLog(
                GuidGenerator.Create(), task.Id, oldStatus, task.Status,
                CurrentUser.Id!.Value, CurrentUser.Name ?? CurrentUser.UserName ?? ""), autoSave: true);
        }

        return MapToDto(task);
    }

    public async Task CancelAsync(Guid id)
    {
        if (!await IsDirectorAsync())
            throw new UserFriendlyException("Chỉ Ban giám đốc mới có thể hủy công việc bổ sung.");

        var task = await _repository.GetAsync(id);

        if (task.Status == SupplementaryTaskStatus.Completed || task.Status == SupplementaryTaskStatus.Cancelled)
            throw new UserFriendlyException("Công việc đã kết thúc, không thể hủy.");

        var oldStatus = task.Status;
        task.Status = SupplementaryTaskStatus.Cancelled;
        await _repository.UpdateAsync(task, autoSave: true);

        await _logRepository.InsertAsync(new SupplementaryTaskLog(
            GuidGenerator.Create(), task.Id, oldStatus, SupplementaryTaskStatus.Cancelled,
            CurrentUser.Id!.Value, CurrentUser.Name ?? CurrentUser.UserName ?? ""), autoSave: true);
    }

    public async Task CompleteAsync(Guid id, CompleteSupplementaryTaskDto input)
    {
        var task = await _repository.GetAsync(id);

        if (task.AssignedToId != CurrentUser.Id!.Value)
            throw new UserFriendlyException("Bạn không có quyền cập nhật công việc này.");

        if (task.Status != SupplementaryTaskStatus.Pending
            && task.Status != SupplementaryTaskStatus.Overdue
            && task.Status != SupplementaryTaskStatus.Rejected)
            throw new UserFriendlyException("Không thể gửi xác nhận hoàn thành ở trạng thái hiện tại.");

        var oldStatus = task.Status;
        task.Status = SupplementaryTaskStatus.WaitingForConfirmation;
        task.CompletionNote = input.Note;
        task.CompletionImageUrls = input.ImageUrls?.Count > 0
            ? JsonSerializer.Serialize(input.ImageUrls)
            : null;
        task.RejectionReason = null;
        await _repository.UpdateAsync(task, autoSave: true);

        await _logRepository.InsertAsync(new SupplementaryTaskLog(
            GuidGenerator.Create(), task.Id, oldStatus, SupplementaryTaskStatus.WaitingForConfirmation,
            CurrentUser.Id!.Value, CurrentUser.Name ?? CurrentUser.UserName ?? "", input.Note,
            task.CompletionImageUrls), autoSave: true);
    }

    public async Task ApproveAsync(Guid id)
    {
        if (!await IsDirectorAsync())
            throw new UserFriendlyException("Chỉ Ban giám đốc mới có thể xác nhận hoàn thành.");

        var task = await _repository.GetAsync(id);

        if (task.Status != SupplementaryTaskStatus.WaitingForConfirmation)
            throw new UserFriendlyException("Chỉ có thể xác nhận công việc ở trạng thái Chờ xác nhận.");

        task.Status = SupplementaryTaskStatus.Completed;
        await _repository.UpdateAsync(task, autoSave: true);

        await _logRepository.InsertAsync(new SupplementaryTaskLog(
            GuidGenerator.Create(), task.Id, SupplementaryTaskStatus.WaitingForConfirmation,
            SupplementaryTaskStatus.Completed,
            CurrentUser.Id!.Value, CurrentUser.Name ?? CurrentUser.UserName ?? ""), autoSave: true);
    }

    public async Task RejectAsync(Guid id, RejectSupplementaryTaskDto input)
    {
        if (!await IsDirectorAsync())
            throw new UserFriendlyException("Chỉ Ban giám đốc mới có thể từ chối.");

        var task = await _repository.GetAsync(id);

        if (task.Status != SupplementaryTaskStatus.WaitingForConfirmation)
            throw new UserFriendlyException("Chỉ có thể từ chối công việc ở trạng thái Chờ xác nhận.");

        task.Status = SupplementaryTaskStatus.Rejected;
        task.RejectionReason = input.Reason;
        await _repository.UpdateAsync(task, autoSave: true);

        await _logRepository.InsertAsync(new SupplementaryTaskLog(
            GuidGenerator.Create(), task.Id, SupplementaryTaskStatus.WaitingForConfirmation,
            SupplementaryTaskStatus.Rejected,
            CurrentUser.Id!.Value, CurrentUser.Name ?? CurrentUser.UserName ?? "", input.Reason), autoSave: true);
    }

    public async Task<List<SupplementaryTaskLogDto>> GetLogsAsync(Guid taskId)
    {
        var query = (await _logRepository.GetQueryableAsync())
            .Where(l => l.TaskId == taskId)
            .OrderBy(l => l.ChangedAt);

        var logs = await AsyncExecuter.ToListAsync(query);
        return logs.Select(l => new SupplementaryTaskLogDto
        {
            Id = l.Id,
            OldStatus = l.OldStatus,
            NewStatus = l.NewStatus,
            ChangedAt = l.ChangedAt,
            ChangedById = l.ChangedById,
            ChangedByName = l.ChangedByName,
            Note = l.Note,
            ImageUrls = SafeDeserializeList(l.ImageUrls),
        }).ToList();
    }

    private static List<string> SafeDeserializeList(string? json)
    {
        if (string.IsNullOrEmpty(json)) return new();
        try { return JsonSerializer.Deserialize<List<string>>(json) ?? new(); }
        catch { return new(); }
    }

    private SupplementaryTaskDto MapToDto(SupplementaryTask task)
    {
        var dto = ObjectMapper.Map<SupplementaryTask, SupplementaryTaskDto>(task);
        dto.ImageUrls = SafeDeserializeList(task.ImageUrls);
        dto.CompletionImageUrls = SafeDeserializeList(task.CompletionImageUrls);
        return dto;
    }

    private async Task<bool> IsDirectorAsync()
    {
        if (_isDirectorCache.HasValue) return _isDirectorCache.Value;

        if (!CurrentUser.IsAuthenticated || !CurrentUser.Id.HasValue)
            return (_isDirectorCache = false).Value;

        if (CurrentUser.IsInRole("admin"))
            return (_isDirectorCache = true).Value;

        var user = await _identityUserRepository.GetAsync(CurrentUser.Id.Value);
        var userDeptId = user.GetProperty<Guid?>(AppConstants.PropertyDepartmentId);
        if (!userDeptId.HasValue)
            return (_isDirectorCache = false).Value;

        var bgdDept = await AsyncExecuter.FirstOrDefaultAsync(
            (await _departmentRepository.GetQueryableAsync()).Where(d => d.Code == "BGD"));
        return (_isDirectorCache = bgdDept != null && userDeptId == bgdDept.Id).Value;
    }
}
