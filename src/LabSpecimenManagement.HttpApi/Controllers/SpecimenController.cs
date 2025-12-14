using LabSpecimenManagement.Common;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.Specimens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Route("api/v1/specimens")]
[Authorize(LabSpecimenManagementPermissions.Specimens.Default)]
public class SpecimenController : LabSpecimenManagementController
{
    private readonly ISpecimenAppService _specimenAppService;

    public SpecimenController(ISpecimenAppService specimenAppService)
    {
        _specimenAppService = specimenAppService;
    }

    [HttpGet]
    public virtual async Task<PagedResultDto<SpecimenDto>> GetListAsync([FromQuery] GetSpecimenListDto input)
    {
        return await _specimenAppService.GetFilteredListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual Task<SpecimenDto> GetAsync(Guid id)
    {
        return _specimenAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.Specimens.Create)]
    public virtual Task<SpecimenDto> CreateAsync([FromBody] CreateSpecimenDto input)
    {
        return _specimenAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.Specimens.Edit)]
    public virtual Task<SpecimenDto> UpdateAsync(Guid id, [FromBody] UpdateSpecimenDto input)
    {
        return _specimenAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.Specimens.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _specimenAppService.DeleteAsync(id);
    }

    [HttpGet("by-test-request/{testRequestId}")]
    public virtual Task<ListResultDto<SpecimenDto>> GetByTestRequestAsync(Guid testRequestId)
    {
        return _specimenAppService.GetByTestRequestAsync(testRequestId);
    }

    /// <summary>
    /// Tạo nhiều mẫu cùng lúc cho một phiếu yêu cầu
    /// </summary>
    [HttpPost("bulk")]
    [Authorize(LabSpecimenManagementPermissions.Specimens.Create)]
    public virtual Task<CreateManySpecimensResultDto> CreateManyAsync([FromBody] CreateManySpecimensDto input)
    {
        return _specimenAppService.CreateManyAsync(input);
    }
}
