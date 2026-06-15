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
[Route("api/v1/manufacturers")]
[Authorize(LabSpecimenManagementPermissions.Manufacturers.Default)]
public class ManufacturerController : LabSpecimenManagementController
{
    private readonly IManufacturerAppService _manufacturerAppService;

    public ManufacturerController(IManufacturerAppService manufacturerAppService)
    {
        _manufacturerAppService = manufacturerAppService;
    }

    [HttpGet]
    public virtual async Task<PagedResultDto<ManufacturerDto>> GetListAsync([FromQuery] PagedRequestDto input)
    {
        var abpInput = new PagedAndSortedResultRequestDto
        {
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        };
        return await _manufacturerAppService.GetListAsync(abpInput);
    }

    [HttpGet("{id}")]
    public virtual Task<ManufacturerDto> GetAsync(Guid id)
    {
        return _manufacturerAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Create)]
    public virtual Task<ManufacturerDto> CreateAsync([FromBody] CreateUpdateManufacturerDto input)
    {
        return _manufacturerAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task<ManufacturerDto> UpdateAsync(Guid id, [FromBody] CreateUpdateManufacturerDto input)
    {
        return _manufacturerAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _manufacturerAppService.DeleteAsync(id);
    }

    [HttpGet("active")]
    public virtual Task<ListResultDto<ManufacturerDto>> GetActiveAsync()
    {
        return _manufacturerAppService.GetActiveAsync();
    }

    [HttpGet("next-code")]
    public virtual Task<string> GetNextCodeAsync()
    {
        return _manufacturerAppService.GetNextCodeAsync();
    }

    [HttpPost("{id}/toggle-active")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task ToggleActiveAsync(Guid id)
    {
        return _manufacturerAppService.ToggleActiveAsync(id);
    }

    [HttpPost("batch-delete")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Delete)]
    public virtual Task<int> BatchDeleteAsync([FromBody] BatchIdsDto input)
    {
        return _manufacturerAppService.BatchDeleteAsync(input.Ids);
    }

    [HttpPost("batch-toggle-active")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task<int> BatchToggleActiveAsync([FromBody] BatchToggleActiveDto input)
    {
        return _manufacturerAppService.BatchToggleActiveAsync(input.Ids, input.IsActive);
    }
}
