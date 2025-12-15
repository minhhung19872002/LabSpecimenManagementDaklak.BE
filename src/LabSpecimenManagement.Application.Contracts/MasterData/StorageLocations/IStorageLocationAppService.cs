using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.MasterData;

public interface IStorageLocationAppService : ICrudAppService<
    StorageLocationDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateUpdateStorageLocationDto>
{
    Task<ListResultDto<StorageLocationDto>> GetActiveStorageLocationsAsync();
    Task<ListResultDto<StorageLocationDto>> GetAvailableLocationsAsync();
}
