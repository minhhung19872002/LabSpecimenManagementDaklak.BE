using System;
using System.Threading.Tasks;
using LabSpecimenManagement.ApprovalProcesses;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Route("api/v1/approval-processes")]
[Authorize(LabSpecimenManagementPermissions.ApprovalProcesses.Default)]
public class ApprovalProcessController : LabSpecimenManagementController
{
    private readonly IApprovalProcessAppService _appService;

    public ApprovalProcessController(IApprovalProcessAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public virtual Task<PagedResultDto<ApprovalProcessDto>> GetListAsync([FromQuery] PagedAndSortedResultRequestDto input)
    {
        return _appService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual Task<ApprovalProcessDto> GetAsync(Guid id)
    {
        return _appService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.ApprovalProcesses.Create)]
    public virtual Task<ApprovalProcessDto> CreateAsync([FromBody] CreateUpdateApprovalProcessDto input)
    {
        return _appService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.ApprovalProcesses.Edit)]
    public virtual Task<ApprovalProcessDto> UpdateAsync(Guid id, [FromBody] CreateUpdateApprovalProcessDto input)
    {
        return _appService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.ApprovalProcesses.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _appService.DeleteAsync(id);
    }

    [HttpPost("{id}/toggle-active")]
    [Authorize(LabSpecimenManagementPermissions.ApprovalProcesses.Edit)]
    public virtual Task ToggleActiveAsync(Guid id)
    {
        return _appService.ToggleActiveAsync(id);
    }

    [HttpGet("by-type/{processType}")]
    public virtual Task<ApprovalProcessDto?> GetByProcessTypeAsync(ApprovalProcessType processType)
    {
        return _appService.GetByProcessTypeAsync(processType);
    }

    [HttpGet("snapshot/{referenceId}")]
    public virtual Task<ApprovalSnapshotDto?> GetSnapshotAsync(Guid referenceId)
    {
        return _appService.GetSnapshotAsync(referenceId);
    }
}
