using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using LabSpecimenManagement.SpecimenHistories;

namespace LabSpecimenManagement.Controllers;

[RemoteService]
[Area("app")]
[Route("api/app/specimen-history")]
public class SpecimenHistoryController : LabSpecimenManagementController
{
    private readonly ISpecimenHistoryAppService _specimenHistoryAppService;

    public SpecimenHistoryController(ISpecimenHistoryAppService specimenHistoryAppService)
    {
        _specimenHistoryAppService = specimenHistoryAppService;
    }

    [HttpGet]
    public virtual Task<PagedResultDto<SpecimenHistoryDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        return _specimenHistoryAppService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual Task<SpecimenHistoryDto> GetAsync(Guid id)
    {
        return _specimenHistoryAppService.GetAsync(id);
    }

    [HttpPost]
    public virtual Task<SpecimenHistoryDto> CreateAsync(CreateSpecimenHistoryDto input)
    {
        return _specimenHistoryAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    public virtual Task<SpecimenHistoryDto> UpdateAsync(Guid id, UpdateSpecimenHistoryDto input)
    {
        return _specimenHistoryAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    public virtual Task DeleteAsync(Guid id)
    {
        return _specimenHistoryAppService.DeleteAsync(id);
    }
}
