using System;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.MasterData.SampleConditions;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers.MasterData;

[RemoteService(Name = "Default")]
[Route("api/v1/sample-conditions")]
[Authorize(LabSpecimenManagementPermissions.SampleConditions.Default)]
public class SampleConditionController : LabSpecimenManagementController
{
    private readonly ISampleConditionAppService _sampleConditionAppService;

    public SampleConditionController(ISampleConditionAppService sampleConditionAppService)
    {
        _sampleConditionAppService = sampleConditionAppService;
    }

    [HttpGet]
    public virtual async Task<PagedResultDto<SampleConditionDto>> GetListAsync([FromQuery] PagedRequestDto input)
    {
        var abpInput = new PagedAndSortedResultRequestDto
        {
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        };
        return await _sampleConditionAppService.GetListAsync(abpInput);
    }

    [HttpGet("{id}")]
    public virtual Task<SampleConditionDto> GetAsync(Guid id)
    {
        return _sampleConditionAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.SampleConditions.Create)]
    public virtual Task<SampleConditionDto> CreateAsync([FromBody] CreateUpdateSampleConditionDto input)
    {
        return _sampleConditionAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.SampleConditions.Edit)]
    public virtual Task<SampleConditionDto> UpdateAsync(Guid id, [FromBody] CreateUpdateSampleConditionDto input)
    {
        return _sampleConditionAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.SampleConditions.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _sampleConditionAppService.DeleteAsync(id);
    }

    [HttpGet("active")]
    [AllowAnonymous]
    public virtual Task<ListResultDto<SampleConditionDto>> GetActiveConditionsAsync()
    {
        return _sampleConditionAppService.GetActiveConditionsAsync();
    }
}
