using LabSpecimenManagement.Common;
using LabSpecimenManagement.NCRs;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Route("api/v1/ncrs")]
[Authorize(LabSpecimenManagementPermissions.NCRs.Default)]
public class NCRController : LabSpecimenManagementController
{
    private readonly INCRAppService _ncrAppService;

    public NCRController(INCRAppService ncrAppService)
    {
        _ncrAppService = ncrAppService;
    }

    [HttpGet]
    public virtual async Task<PagedResultDto<NCRDto>> GetListAsync([FromQuery] PagedRequestDto input)
    {
        var abpInput = new PagedAndSortedResultRequestDto
        {
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        };
        return await _ncrAppService.GetListAsync(abpInput);
    }

    [HttpGet("{id}")]
    public virtual Task<NCRDto> GetAsync(Guid id)
    {
        return _ncrAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.NCRs.Create)]
    public virtual Task<NCRDto> CreateAsync([FromBody] CreateNCRDto input)
    {
        return _ncrAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.NCRs.Edit)]
    public virtual Task<NCRDto> UpdateAsync(Guid id, [FromBody] UpdateNCRDto input)
    {
        return _ncrAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.NCRs.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _ncrAppService.DeleteAsync(id);
    }

    [HttpPost("{id}/close")]
    [Authorize(LabSpecimenManagementPermissions.NCRs.Close)]
    public virtual Task<NCRDto> CloseAsync(Guid id)
    {
        return _ncrAppService.CloseAsync(id);
    }

    [HttpGet("open")]
    public virtual Task<ListResultDto<NCRDto>> GetOpenNCRsAsync()
    {
        return _ncrAppService.GetOpenNCRsAsync();
    }

    [HttpPost("{id}/assign")]
    [Authorize(LabSpecimenManagementPermissions.NCRs.Assign)]
    public virtual Task<NCRDto> AssignAsync(Guid id, [FromBody] Guid assigneeId)
    {
        return _ncrAppService.AssignAsync(id, assigneeId);
    }
}
