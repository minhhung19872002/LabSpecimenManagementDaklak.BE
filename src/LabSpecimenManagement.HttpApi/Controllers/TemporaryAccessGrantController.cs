using System;
using System.Threading.Tasks;
using LabSpecimenManagement.TemporaryAccess;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService]
[Area("app")]
[Route("api/v1/temporary-access")]
public class TemporaryAccessGrantController : LabSpecimenManagementController
{
    private readonly ITemporaryAccessGrantAppService _appService;

    public TemporaryAccessGrantController(ITemporaryAccessGrantAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public async Task<PagedResultDto<TemporaryAccessGrantDto>> GetListAsync([FromQuery] TemporaryAccessGrantPagedRequestDto input)
        => await _appService.GetListAsync(input);

    [HttpGet("{id}")]
    public async Task<TemporaryAccessGrantDto> GetAsync(Guid id)
        => await _appService.GetAsync(id);

    [HttpPost]
    public async Task<TemporaryAccessGrantDto> CreateAsync([FromBody] CreateTemporaryAccessGrantDto input)
        => await _appService.CreateAsync(input);

    [HttpPut("{id}")]
    public async Task<TemporaryAccessGrantDto> UpdateAsync(Guid id, [FromBody] UpdateTemporaryAccessGrantDto input)
        => await _appService.UpdateAsync(id, input);

    [HttpPost("{id}/expire")]
    public async Task ExpireAsync(Guid id)
        => await _appService.ExpireAsync(id);

    [HttpGet("stats")]
    public async Task<TemporaryAccessGrantStatsDto> GetStatsAsync()
        => await _appService.GetStatsAsync();
}
