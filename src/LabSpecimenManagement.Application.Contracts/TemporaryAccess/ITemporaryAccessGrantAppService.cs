using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.TemporaryAccess;

public interface ITemporaryAccessGrantAppService : IApplicationService
{
    Task<PagedResultDto<TemporaryAccessGrantDto>> GetListAsync(TemporaryAccessGrantPagedRequestDto input);
    Task<TemporaryAccessGrantDto> GetAsync(Guid id);
    Task<TemporaryAccessGrantDto> CreateAsync(CreateTemporaryAccessGrantDto input);
    Task<TemporaryAccessGrantDto> UpdateAsync(Guid id, UpdateTemporaryAccessGrantDto input);
    Task ExpireAsync(Guid id);
    Task<TemporaryAccessGrantStatsDto> GetStatsAsync();
}
