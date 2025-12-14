using LabSpecimenManagement.Common;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers.MasterData;

[RemoteService(Name = "Default")]
[Route("api/v1/storage-locations")]
[Authorize(LabSpecimenManagementPermissions.StorageLocations.Default)]
public class StorageLocationController : LabSpecimenManagementController
{
    private readonly IStorageLocationAppService _storageLocationAppService;

    public StorageLocationController(IStorageLocationAppService storageLocationAppService)
    {
        _storageLocationAppService = storageLocationAppService;
    }

    [HttpGet]
    public virtual async Task<PagedResultDto<StorageLocationDto>> GetListAsync([FromQuery] PagedRequestDto input)
    {
        var abpInput = new PagedAndSortedResultRequestDto
        {
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        };
        return await _storageLocationAppService.GetListAsync(abpInput);
    }

    [HttpGet("{id}")]
    public virtual Task<StorageLocationDto> GetAsync(Guid id)
    {
        return _storageLocationAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.StorageLocations.Create)]
    public virtual Task<StorageLocationDto> CreateAsync([FromBody] CreateUpdateStorageLocationDto input)
    {
        return _storageLocationAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.StorageLocations.Edit)]
    public virtual Task<StorageLocationDto> UpdateAsync(Guid id, [FromBody] CreateUpdateStorageLocationDto input)
    {
        return _storageLocationAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.StorageLocations.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _storageLocationAppService.DeleteAsync(id);
    }

    [HttpGet("active")]
    public virtual Task<ListResultDto<StorageLocationDto>> GetActiveStorageLocationsAsync()
    {
        return _storageLocationAppService.GetActiveStorageLocationsAsync();
    }

    [HttpGet("available")]
    public virtual Task<ListResultDto<StorageLocationDto>> GetAvailableLocationsAsync()
    {
        return _storageLocationAppService.GetAvailableLocationsAsync();
    }
}
