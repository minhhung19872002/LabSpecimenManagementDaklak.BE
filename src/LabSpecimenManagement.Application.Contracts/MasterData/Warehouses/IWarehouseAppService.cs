using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.MasterData;

public interface IWarehouseAppService : ICrudAppService<
    WarehouseDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateUpdateWarehouseDto>
{
    Task<ListResultDto<WarehouseDto>> GetActiveWarehousesAsync();

    Task<ListResultDto<WarehouseDto>> GetRootWarehousesAsync();

    Task<string> GetNextCodeAsync();

    Task<WarehouseDto?> GetDefaultAsync();

    Task ToggleActiveAsync(Guid id);

    Task<int> BatchDeleteAsync(List<Guid> ids);

    Task<int> BatchToggleActiveAsync(List<Guid> ids, bool isActive);

    Task<ListResultDto<WarehouseDto>> GetActiveWarehousesForCurrentUserAsync();
}
