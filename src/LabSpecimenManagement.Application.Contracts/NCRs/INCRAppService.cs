using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.NCRs;

public interface INCRAppService : ICrudAppService<
    NCRDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateNCRDto,
    UpdateNCRDto>
{
    Task<NCRDto> CloseAsync(Guid id);
    Task<ListResultDto<NCRDto>> GetOpenNCRsAsync();
    Task<NCRDto> AssignAsync(Guid id, Guid assignedToId);
}
