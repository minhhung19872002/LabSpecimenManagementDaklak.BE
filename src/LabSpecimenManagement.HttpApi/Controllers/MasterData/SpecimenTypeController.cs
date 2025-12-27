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
[Route("api/v1/specimen-types")]
[Authorize(LabSpecimenManagementPermissions.SpecimenTypes.Default)]
public class SpecimenTypeController : LabSpecimenManagementController
{
    private readonly ISpecimenTypeAppService _specimenTypeAppService;

    public SpecimenTypeController(ISpecimenTypeAppService specimenTypeAppService)
    {
        _specimenTypeAppService = specimenTypeAppService;
    }

    [HttpGet]
    public virtual async Task<PagedResultDto<SpecimenTypeDto>> GetListAsync([FromQuery] PagedRequestDto input)
    {
        var abpInput = new PagedAndSortedResultRequestDto
        {
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        };
        return await _specimenTypeAppService.GetListAsync(abpInput);
    }

    [HttpGet("{id}")]
    public virtual Task<SpecimenTypeDto> GetAsync(Guid id)
    {
        return _specimenTypeAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.SpecimenTypes.Create)]
    public virtual async Task<IActionResult> CreateAsync([FromBody] CreateUpdateSpecimenTypeDto input)
    {
        try
        {
            var result = await _specimenTypeAppService.CreateAsync(input);
            return Ok(result);
        }
        catch (SpecimenTypeValidationException ex)
        {
            return BadRequest(ex.Errors);
        }
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.SpecimenTypes.Edit)]
    public virtual async Task<IActionResult> UpdateAsync(Guid id, [FromBody] CreateUpdateSpecimenTypeDto input)
    {
        try
        {
            var result = await _specimenTypeAppService.UpdateAsync(id, input);
            return Ok(result);
        }
        catch (SpecimenTypeValidationException ex)
        {
            return BadRequest(ex.Errors);
        }
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.SpecimenTypes.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _specimenTypeAppService.DeleteAsync(id);
    }

    [HttpGet("active")]
    public virtual Task<ListResultDto<SpecimenTypeDto>> GetActiveSpecimenTypesAsync()
    {
        return _specimenTypeAppService.GetActiveSpecimenTypesAsync();
    }
}
