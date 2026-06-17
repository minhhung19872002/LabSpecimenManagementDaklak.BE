using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.MasterData.Units;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers.MasterData;

[RemoteService(Name = "Default")]
[Route("api/v1/units")]
[Authorize(LabSpecimenManagementPermissions.Units.Default)]
public class UnitController : LabSpecimenManagementController
{
    private readonly IUnitAppService _unitAppService;

    public UnitController(IUnitAppService unitAppService)
    {
        _unitAppService = unitAppService;
    }

    [HttpGet]
    public virtual async Task<PagedResultDto<UnitDto>> GetListAsync([FromQuery] PagedRequestDto input)
    {
        var abpInput = new PagedAndSortedResultRequestDto
        {
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        };
        return await _unitAppService.GetListAsync(abpInput);
    }

    [HttpGet("{id}")]
    public virtual Task<UnitDto> GetAsync(Guid id)
    {
        return _unitAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Create)]
    public virtual Task<UnitDto> CreateAsync([FromBody] CreateUpdateUnitDto input)
    {
        return _unitAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task<UnitDto> UpdateAsync(Guid id, [FromBody] CreateUpdateUnitDto input)
    {
        return _unitAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _unitAppService.DeleteAsync(id);
    }

    [HttpGet("active")]
    [AllowAnonymous]
    public virtual Task<ListResultDto<UnitDto>> GetActiveUnitsAsync([FromQuery] List<Guid>? includeIds = null)
    {
        return _unitAppService.GetActiveUnitsAsync(includeIds);
    }

    [HttpPost("{id}/toggle-active")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task ToggleActiveAsync(Guid id)
    {
        return _unitAppService.ToggleActiveAsync(id);
    }

    [HttpPost("batch-delete")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Delete)]
    public virtual Task<int> BatchDeleteAsync([FromBody] BatchIdsDto input)
    {
        return _unitAppService.BatchDeleteAsync(input.Ids);
    }

    [HttpPost("batch-toggle-active")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task<int> BatchToggleActiveAsync([FromBody] BatchToggleActiveDto input)
    {
        return _unitAppService.BatchToggleActiveAsync(input.Ids, input.IsActive);
    }
}

/// <summary>
/// DTO cho batch operations
/// </summary>
public class BatchIdsDto
{
    public List<Guid> Ids { get; set; } = new();
}

/// <summary>
/// DTO cho batch toggle active
/// </summary>
public class BatchToggleActiveDto
{
    public List<Guid> Ids { get; set; } = new();
    public bool IsActive { get; set; }
}
