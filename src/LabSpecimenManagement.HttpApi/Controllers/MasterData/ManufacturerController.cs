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
[Route("api/v1/manufacturers")]
[Authorize(LabSpecimenManagementPermissions.MasterData.Default)]
public class ManufacturerController : LabSpecimenManagementController
{
    private readonly IManufacturerAppService _manufacturerAppService;

    public ManufacturerController(IManufacturerAppService manufacturerAppService)
    {
        _manufacturerAppService = manufacturerAppService;
    }

    [HttpGet]
    public virtual async Task<PagedResultDto<ManufacturerDto>> GetListAsync([FromQuery] PagedAndSortedResultRequestDto input)
    {
        return await _manufacturerAppService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual Task<ManufacturerDto> GetAsync(Guid id)
    {
        return _manufacturerAppService.GetAsync(id);
    }

    [HttpGet("active")]
    public virtual Task<ListResultDto<ManufacturerDto>> GetActiveAsync()
    {
        return _manufacturerAppService.GetActiveAsync();
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Create)]
    public virtual async Task<ManufacturerDto> CreateAsync([FromBody] CreateUpdateManufacturerDto input)
    {
        return await _manufacturerAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual async Task<ManufacturerDto> UpdateAsync(Guid id, [FromBody] CreateUpdateManufacturerDto input)
    {
        return await _manufacturerAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _manufacturerAppService.DeleteAsync(id);
    }
}
