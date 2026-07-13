using System;
using System.Threading.Tasks;
using LabSpecimenManagement.SupplementaryTasks;
using LabSpecimenManagement.SupplementaryTasks.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Route("api/v1/supplementary-tasks")]
[Authorize]
public class SupplementaryTaskController : LabSpecimenManagementController
{
    private readonly ISupplementaryTaskAppService _appService;

    public SupplementaryTaskController(ISupplementaryTaskAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public virtual Task<PagedResultDto<SupplementaryTaskDto>> GetListAsync([FromQuery] GetSupplementaryTaskListInput input)
    {
        return _appService.GetListAsync(input);
    }

    [HttpGet("stats")]
    public virtual Task<SupplementaryTaskStatsDto> GetStatsAsync()
    {
        return _appService.GetStatsAsync();
    }

    [HttpPost]
    public virtual Task<SupplementaryTaskDto> CreateAsync([FromBody] CreateSupplementaryTaskDto input)
    {
        return _appService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    public virtual Task<SupplementaryTaskDto> UpdateAsync(Guid id, [FromBody] UpdateSupplementaryTaskDto input)
    {
        return _appService.UpdateAsync(id, input);
    }

    [HttpPost("{id}/cancel")]
    public virtual Task CancelAsync(Guid id)
    {
        return _appService.CancelAsync(id);
    }

    [HttpPost("{id}/complete")]
    public virtual Task CompleteAsync(Guid id)
    {
        return _appService.CompleteAsync(id);
    }
}
