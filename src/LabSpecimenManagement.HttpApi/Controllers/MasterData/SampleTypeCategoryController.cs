using System;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.MasterData.SampleTypeCategories;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers.MasterData;

[RemoteService(Name = "Default")]
[Route("api/v1/sample-type-categories")]
[Authorize(LabSpecimenManagementPermissions.SpecimenTypes.Default)]
public class SampleTypeCategoryController : LabSpecimenManagementController
{
    private readonly ISampleTypeCategoryAppService _sampleTypeCategoryAppService;

    public SampleTypeCategoryController(ISampleTypeCategoryAppService sampleTypeCategoryAppService)
    {
        _sampleTypeCategoryAppService = sampleTypeCategoryAppService;
    }

    [HttpGet]
    public virtual async Task<PagedResultDto<SampleTypeCategoryDto>> GetListAsync([FromQuery] PagedRequestDto input)
    {
        var abpInput = new PagedAndSortedResultRequestDto
        {
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        };
        return await _sampleTypeCategoryAppService.GetListAsync(abpInput);
    }

    [HttpGet("{id}")]
    public virtual Task<SampleTypeCategoryDto> GetAsync(Guid id)
    {
        return _sampleTypeCategoryAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.SpecimenTypes.Create)]
    public virtual Task<SampleTypeCategoryDto> CreateAsync([FromBody] CreateUpdateSampleTypeCategoryDto input)
    {
        return _sampleTypeCategoryAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.SpecimenTypes.Edit)]
    public virtual Task<SampleTypeCategoryDto> UpdateAsync(Guid id, [FromBody] CreateUpdateSampleTypeCategoryDto input)
    {
        return _sampleTypeCategoryAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.SpecimenTypes.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _sampleTypeCategoryAppService.DeleteAsync(id);
    }

    [HttpGet("active")]
    public virtual Task<ListResultDto<SampleTypeCategoryDto>> GetActiveCategoriesAsync()
    {
        return _sampleTypeCategoryAppService.GetActiveCategoriesAsync();
    }

    [HttpGet("with-count")]
    public virtual Task<ListResultDto<SampleTypeCategoryDto>> GetCategoriesWithCountAsync()
    {
        return _sampleTypeCategoryAppService.GetCategoriesWithCountAsync();
    }

    [HttpPost("{id}/toggle-active")]
    [Authorize(LabSpecimenManagementPermissions.SpecimenTypes.Edit)]
    public virtual Task ToggleActiveAsync(Guid id)
    {
        return _sampleTypeCategoryAppService.ToggleActiveAsync(id);
    }
}
