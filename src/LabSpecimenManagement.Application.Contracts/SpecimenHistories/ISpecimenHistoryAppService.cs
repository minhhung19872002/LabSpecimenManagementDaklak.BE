using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.SpecimenHistories;

public interface ISpecimenHistoryAppService : ICrudAppService<
    SpecimenHistoryDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateSpecimenHistoryDto,
    UpdateSpecimenHistoryDto>
{
    Task<PagedResultDto<SpecimenHistoryDto>> GetFilteredListAsync(GetSpecimenHistoryListDto input);
    Task<ListResultDto<SpecimenHistoryDto>> GetBySpecimenAsync(Guid specimenId);
}
