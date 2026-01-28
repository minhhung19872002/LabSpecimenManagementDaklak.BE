using System;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.MasterData.Indicators;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers.MasterData;

[RemoteService(Name = "Default")]
[Route("api/v1/indicator-groups")]
[Authorize(LabSpecimenManagementPermissions.MasterData.Default)]
public class IndicatorGroupController : LabSpecimenManagementController
{
    private readonly IIndicatorGroupAppService _indicatorGroupAppService;

    public IndicatorGroupController(IIndicatorGroupAppService indicatorGroupAppService)
    {
        _indicatorGroupAppService = indicatorGroupAppService;
    }

    [HttpGet]
    public virtual async Task<PagedResultDto<IndicatorGroupDto>> GetListAsync([FromQuery] PagedRequestDto input)
    {
        var abpInput = new PagedAndSortedResultRequestDto
        {
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        };
        return await _indicatorGroupAppService.GetListAsync(abpInput);
    }

    [HttpGet("{id}")]
    public virtual Task<IndicatorGroupDto> GetAsync(Guid id)
    {
        return _indicatorGroupAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Create)]
    public virtual Task<IndicatorGroupDto> CreateAsync([FromBody] CreateUpdateIndicatorGroupDto input)
    {
        return _indicatorGroupAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task<IndicatorGroupDto> UpdateAsync(Guid id, [FromBody] CreateUpdateIndicatorGroupDto input)
    {
        return _indicatorGroupAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _indicatorGroupAppService.DeleteAsync(id);
    }

    [HttpGet("active")]
    public virtual Task<ListResultDto<IndicatorGroupDto>> GetActiveGroupsAsync()
    {
        return _indicatorGroupAppService.GetActiveGroupsAsync();
    }

    [HttpGet("with-count")]
    public virtual Task<ListResultDto<IndicatorGroupDto>> GetGroupsWithCountAsync()
    {
        return _indicatorGroupAppService.GetGroupsWithCountAsync();
    }

    [HttpPost("import")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Import)]
    public virtual async Task<ImportIndicatorGroupsResultDto> ImportFromExcelAsync(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        return await _indicatorGroupAppService.ImportFromExcelAsync(stream, file.FileName);
    }

    [HttpGet("template")]
    public virtual async Task<IActionResult> GetExcelTemplateAsync()
    {
        var template = await _indicatorGroupAppService.GetExcelTemplateAsync();
        return File(template, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "indicator-groups-template.xlsx");
    }
}
