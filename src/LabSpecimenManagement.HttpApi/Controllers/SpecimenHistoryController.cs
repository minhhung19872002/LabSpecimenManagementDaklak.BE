using System;
using System.Threading.Tasks;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.SpecimenHistories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService]
[Route("api/v1/specimen-histories")]
[Authorize(LabSpecimenManagementPermissions.Specimens.Default)]
public class SpecimenHistoryController : LabSpecimenManagementController
{
    private readonly ISpecimenHistoryAppService _specimenHistoryAppService;

    public SpecimenHistoryController(ISpecimenHistoryAppService specimenHistoryAppService)
    {
        _specimenHistoryAppService = specimenHistoryAppService;
    }

    [HttpGet]
    public virtual async Task<PagedResultDto<SpecimenHistoryDto>> GetListAsync([FromQuery] GetSpecimenHistoryListDto input)
    {
        return await _specimenHistoryAppService.GetFilteredListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual Task<SpecimenHistoryDto> GetAsync(Guid id)
    {
        return _specimenHistoryAppService.GetAsync(id);
    }

    [HttpGet("by-specimen/{specimenId}")]
    public virtual Task<ListResultDto<SpecimenHistoryDto>> GetBySpecimenAsync(Guid specimenId)
    {
        return _specimenHistoryAppService.GetBySpecimenAsync(specimenId);
    }

    [HttpPost]
    public virtual Task<SpecimenHistoryDto> CreateAsync([FromBody] CreateSpecimenHistoryDto input)
    {
        return _specimenHistoryAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    public virtual Task<SpecimenHistoryDto> UpdateAsync(Guid id, [FromBody] UpdateSpecimenHistoryDto input)
    {
        return _specimenHistoryAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    public virtual Task DeleteAsync(Guid id)
    {
        return _specimenHistoryAppService.DeleteAsync(id);
    }
}
