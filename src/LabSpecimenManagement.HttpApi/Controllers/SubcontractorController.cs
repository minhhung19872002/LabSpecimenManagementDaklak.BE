using LabSpecimenManagement.Common;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.Subcontractors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Route("api/v1/subcontractors")]
[Authorize(LabSpecimenManagementPermissions.Subcontractors.Default)]
public class SubcontractorController : LabSpecimenManagementController
{
    private readonly ISubcontractorAppService _subcontractorAppService;

    public SubcontractorController(ISubcontractorAppService subcontractorAppService)
    {
        _subcontractorAppService = subcontractorAppService;
    }

    [HttpGet]
    public virtual async Task<PagedResultDto<SubcontractorDto>> GetListAsync([FromQuery] GetSubcontractorListInput input)
    {
        return await _subcontractorAppService.GetFilteredListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual Task<SubcontractorDto> GetAsync(Guid id)
    {
        return _subcontractorAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.Subcontractors.Create)]
    public virtual Task<SubcontractorDto> CreateAsync([FromBody] CreateUpdateSubcontractorDto input)
    {
        return _subcontractorAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.Subcontractors.Edit)]
    public virtual Task<SubcontractorDto> UpdateAsync(Guid id, [FromBody] CreateUpdateSubcontractorDto input)
    {
        return _subcontractorAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.Subcontractors.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _subcontractorAppService.DeleteAsync(id);
    }

    [HttpGet("active")]
    public virtual Task<ListResultDto<SubcontractorDto>> GetActiveSubcontractorsAsync()
    {
        return _subcontractorAppService.GetActiveSubcontractorsAsync();
    }

    [HttpGet("expiring-certifications")]
    public virtual Task<ListResultDto<SubcontractorCertificationDto>> GetExpiringCertificationsAsync([FromQuery] int daysAhead = 90)
    {
        return _subcontractorAppService.GetExpiringCertificationsAsync(daysAhead);
    }
}
