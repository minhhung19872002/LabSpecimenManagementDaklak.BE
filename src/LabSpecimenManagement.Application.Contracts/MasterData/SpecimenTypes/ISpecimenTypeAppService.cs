using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.MasterData;

public interface ISpecimenTypeAppService : ICrudAppService<
    SpecimenTypeDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateUpdateSpecimenTypeDto>
{
    Task<ListResultDto<SpecimenTypeDto>> GetActiveSpecimenTypesAsync();
}
