using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.MasterData;

public interface IManufacturerAppService : ICrudAppService<
    ManufacturerDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateUpdateManufacturerDto>
{
    Task<ListResultDto<ManufacturerDto>> GetActiveAsync();

    Task<string> GetNextCodeAsync();

    Task ToggleActiveAsync(Guid id);

    Task<int> BatchDeleteAsync(List<Guid> ids);

    Task<int> BatchToggleActiveAsync(List<Guid> ids, bool isActive);
}
