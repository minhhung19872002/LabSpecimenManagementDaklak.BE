using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers.MasterData;

[RemoteService(Name = "Default")]
[Route("api/v1/warehouses")]
[Authorize(LabSpecimenManagementPermissions.Warehouses.Default)]
public class WarehouseController : LabSpecimenManagementController
{
    private readonly IWarehouseAppService _warehouseAppService;

    public WarehouseController(IWarehouseAppService warehouseAppService)
    {
        _warehouseAppService = warehouseAppService;
    }

    [HttpGet]
    public virtual async Task<PagedResultDto<WarehouseDto>> GetListAsync([FromQuery] PagedRequestDto input)
    {
        var abpInput = new PagedAndSortedResultRequestDto
        {
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        };
        return await _warehouseAppService.GetListAsync(abpInput);
    }

    [HttpGet("{id}")]
    public virtual Task<WarehouseDto> GetAsync(Guid id)
    {
        return _warehouseAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Create)]
    public virtual Task<WarehouseDto> CreateAsync([FromBody] CreateUpdateWarehouseDto input)
    {
        return _warehouseAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task<WarehouseDto> UpdateAsync(Guid id, [FromBody] CreateUpdateWarehouseDto input)
    {
        return _warehouseAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _warehouseAppService.DeleteAsync(id);
    }

    [HttpGet("active")]
    public virtual Task<ListResultDto<WarehouseDto>> GetActiveWarehousesAsync()
    {
        return _warehouseAppService.GetActiveWarehousesAsync();
    }

    [HttpGet("next-code")]
    public virtual Task<string> GetNextCodeAsync()
    {
        return _warehouseAppService.GetNextCodeAsync();
    }

    [HttpPost("{id}/toggle-active")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task ToggleActiveAsync(Guid id)
    {
        return _warehouseAppService.ToggleActiveAsync(id);
    }

    [HttpPost("batch-delete")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Delete)]
    public virtual Task<int> BatchDeleteAsync([FromBody] BatchIdsDto input)
    {
        return _warehouseAppService.BatchDeleteAsync(input.Ids);
    }

    [HttpPost("batch-toggle-active")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task<int> BatchToggleActiveAsync([FromBody] BatchToggleActiveDto input)
    {
        return _warehouseAppService.BatchToggleActiveAsync(input.Ids, input.IsActive);
    }
}
