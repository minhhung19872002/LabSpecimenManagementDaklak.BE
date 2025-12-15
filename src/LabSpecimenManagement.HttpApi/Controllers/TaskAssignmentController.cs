using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Route("api/v1/task-assignments")]
[Authorize(LabSpecimenManagementPermissions.TaskAssignments.Default)]
public class TaskAssignmentController : LabSpecimenManagementController
{
    private readonly ITaskAssignmentAppService _taskAssignmentAppService;

    public TaskAssignmentController(ITaskAssignmentAppService taskAssignmentAppService)
    {
        _taskAssignmentAppService = taskAssignmentAppService;
    }

    [HttpGet]
    public virtual async Task<PagedResultDto<TaskAssignmentDto>> GetListAsync([FromQuery] GetTaskAssignmentListInput input)
    {
        return await _taskAssignmentAppService.GetFilteredListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual Task<TaskAssignmentDto> GetAsync(Guid id)
    {
        return _taskAssignmentAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.TaskAssignments.Create)]
    public virtual Task<TaskAssignmentDto> CreateAsync([FromBody] CreateTaskAssignmentDto input)
    {
        return _taskAssignmentAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.TaskAssignments.Edit)]
    public virtual Task<TaskAssignmentDto> UpdateAsync(Guid id, [FromBody] UpdateTaskAssignmentDto input)
    {
        return _taskAssignmentAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.TaskAssignments.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _taskAssignmentAppService.DeleteAsync(id);
    }

    [HttpPost("{id}/accept")]
    [Authorize(LabSpecimenManagementPermissions.TaskAssignments.Accept)]
    public virtual Task<TaskAssignmentDto> AcceptAsync(Guid id)
    {
        return _taskAssignmentAppService.AcceptAsync(id);
    }

    [HttpPost("{id}/reject")]
    [Authorize(LabSpecimenManagementPermissions.TaskAssignments.Reject)]
    public virtual Task<TaskAssignmentDto> RejectAsync(Guid id, [FromBody] string? reason)
    {
        return _taskAssignmentAppService.RejectAsync(id, reason);
    }

    [HttpPost("{id}/start")]
    public virtual Task<TaskAssignmentDto> StartAsync(Guid id)
    {
        return _taskAssignmentAppService.StartAsync(id);
    }

    [HttpPost("{id}/complete")]
    [Authorize(LabSpecimenManagementPermissions.TaskAssignments.Complete)]
    public virtual Task<TaskAssignmentDto> CompleteAsync(Guid id)
    {
        return _taskAssignmentAppService.CompleteAsync(id);
    }

    [HttpPut("{id}/progress")]
    public virtual Task<TaskAssignmentDto> UpdateProgressAsync(Guid id, [FromBody] int progress)
    {
        return _taskAssignmentAppService.UpdateProgressAsync(id, progress);
    }

    [HttpGet("my-tasks")]
    public virtual Task<ListResultDto<TaskAssignmentDto>> GetMyTasksAsync()
    {
        return _taskAssignmentAppService.GetMyTasksAsync();
    }

    [HttpGet("by-assignee/{assigneeId}")]
    public virtual Task<ListResultDto<TaskAssignmentDto>> GetByAssigneeAsync(Guid assigneeId)
    {
        return _taskAssignmentAppService.GetByAssigneeAsync(assigneeId);
    }
}
