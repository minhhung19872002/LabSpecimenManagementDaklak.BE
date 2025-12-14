using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.Tasks;

public interface ITaskAssignmentAppService : ICrudAppService<
    TaskAssignmentDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateTaskAssignmentDto,
    UpdateTaskAssignmentDto>
{
    Task<PagedResultDto<TaskAssignmentDto>> GetFilteredListAsync(GetTaskAssignmentListInput input);
    Task<TaskAssignmentDto> AcceptAsync(Guid id);
    Task<TaskAssignmentDto> RejectAsync(Guid id, string? reason);
    Task<TaskAssignmentDto> StartAsync(Guid id);
    Task<TaskAssignmentDto> CompleteAsync(Guid id);
    Task<TaskAssignmentDto> UpdateProgressAsync(Guid id, int progress);
    Task<ListResultDto<TaskAssignmentDto>> GetMyTasksAsync();
    Task<ListResultDto<TaskAssignmentDto>> GetByAssigneeAsync(Guid assigneeId);
}
