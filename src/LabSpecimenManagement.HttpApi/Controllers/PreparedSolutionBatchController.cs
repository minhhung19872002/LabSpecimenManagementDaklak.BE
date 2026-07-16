using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.PreparedSolutions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Route("api/v1/prepared-solution-batches")]
[Authorize(LabSpecimenManagementPermissions.PreparedSolutions.Default)]
public class PreparedSolutionBatchController : LabSpecimenManagementController
{
    private readonly IPreparedSolutionBatchAppService _appService;

    public PreparedSolutionBatchController(IPreparedSolutionBatchAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public virtual Task<PagedResultDto<PreparedSolutionBatchDto>> GetListAsync([FromQuery] GetPreparedSolutionBatchesInput input)
    {
        return _appService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual Task<PreparedSolutionBatchDto> GetAsync(Guid id)
    {
        return _appService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.PreparedSolutions.Create)]
    public virtual Task<PreparedSolutionBatchDto> CreateAsync([FromBody] CreateUpdatePreparedSolutionBatchDto input)
    {
        return _appService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.PreparedSolutions.Edit)]
    public virtual Task<PreparedSolutionBatchDto> UpdateAsync(Guid id, [FromBody] CreateUpdatePreparedSolutionBatchDto input)
    {
        return _appService.UpdateAsync(id, input);
    }

    [HttpPost("{id}/process")]
    [Authorize(LabSpecimenManagementPermissions.PreparedSolutions.Process)]
    public virtual Task<PreparedSolutionBatchDto> ProcessAsync(Guid id, [FromBody] ProcessPreparedSolutionDto input)
    {
        return _appService.ProcessAsync(id, input);
    }

    [HttpGet("warnings")]
    public virtual Task<List<PreparedSolutionBatchDto>> GetWarningItemsAsync()
    {
        return _appService.GetWarningItemsAsync();
    }

    [HttpGet("warehouse-products")]
    public virtual Task<List<PreparedSolutionWarehouseProductDto>> GetWarehouseProductsAsync(
        [FromQuery] Guid warehouseId,
        [FromQuery] int? productType)
    {
        return _appService.GetWarehouseProductsAsync(warehouseId, productType);
    }
}
