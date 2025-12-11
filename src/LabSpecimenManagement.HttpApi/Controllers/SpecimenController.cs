using LabSpecimenManagement.Specimens;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Area("app")]
[Route("api/app/specimen")]
public class SpecimenController : AbpControllerBase
{
    private readonly ISpecimenAppService _specimenAppService;

    public SpecimenController(ISpecimenAppService specimenAppService)
    {
        _specimenAppService = specimenAppService;
    }

    [HttpGet]
    public virtual Task<PagedResultDto<SpecimenDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        return _specimenAppService.GetListAsync(input);
    }

    [HttpGet]
    [Route("{id}")]
    public virtual Task<SpecimenDto> GetAsync(Guid id)
    {
        return _specimenAppService.GetAsync(id);
    }

    [HttpPost]
    public virtual Task<SpecimenDto> CreateAsync(CreateSpecimenDto input)
    {
        return _specimenAppService.CreateAsync(input);
    }

    [HttpPut]
    [Route("{id}")]
    public virtual Task<SpecimenDto> UpdateAsync(Guid id, UpdateSpecimenDto input)
    {
        return _specimenAppService.UpdateAsync(id, input);
    }

    [HttpDelete]
    [Route("{id}")]
    public virtual Task DeleteAsync(Guid id)
    {
        return _specimenAppService.DeleteAsync(id);
    }
}
