using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.MasterData;

[RemoteService(IsEnabled = false)]
public class StorageLocationAppService :
    CrudAppService<
        StorageLocation,
        StorageLocationDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateUpdateStorageLocationDto>,
    IStorageLocationAppService
{
    public StorageLocationAppService(IRepository<StorageLocation, Guid> repository)
        : base(repository)
    {
    }

    public async Task<ListResultDto<StorageLocationDto>> GetActiveStorageLocationsAsync()
    {
        var query = await Repository.GetQueryableAsync();
        var items = query
            .WhereActive()
            .OrderBy(x => x.Name)
            .ToList();

        return new ListResultDto<StorageLocationDto>(
            ObjectMapper.Map<List<StorageLocation>, List<StorageLocationDto>>(items)
        );
    }

    public async Task<ListResultDto<StorageLocationDto>> GetAvailableLocationsAsync()
    {
        var query = await Repository.GetQueryableAsync();
        var items = query
            .WhereActive()
            .Where(x => x.CurrentCount < x.Capacity)
            .OrderBy(x => x.Name)
            .ToList();

        return new ListResultDto<StorageLocationDto>(
            ObjectMapper.Map<List<StorageLocation>, List<StorageLocationDto>>(items)
        );
    }
}
