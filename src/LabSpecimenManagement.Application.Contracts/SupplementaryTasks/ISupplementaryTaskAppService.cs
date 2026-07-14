using System;
using System.Threading.Tasks;
using LabSpecimenManagement.SupplementaryTasks.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.SupplementaryTasks;

public interface ISupplementaryTaskAppService : IApplicationService
{
    Task<PagedResultDto<SupplementaryTaskDto>> GetListAsync(GetSupplementaryTaskListInput input);
    Task<SupplementaryTaskStatsDto> GetStatsAsync();
    Task<SupplementaryTaskDto> CreateAsync(CreateSupplementaryTaskDto input);
    Task<SupplementaryTaskDto> UpdateAsync(Guid id, UpdateSupplementaryTaskDto input);
    Task CancelAsync(Guid id);
    Task CompleteAsync(Guid id, CompleteSupplementaryTaskDto input);
}
