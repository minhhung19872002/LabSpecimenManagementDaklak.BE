using System;
using System.Threading.Tasks;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.StockAdjustments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Route("api/v1/stock-adjustments")]
[Authorize(LabSpecimenManagementPermissions.StockAdjustments.Default)]
public class StockAdjustmentController : LabSpecimenManagementController
{
    private readonly IStockAdjustmentAppService _appService;

    public StockAdjustmentController(IStockAdjustmentAppService appService)
    {
        _appService = appService;
    }

    [HttpGet]
    public virtual Task<PagedResultDto<StockAdjustmentListDto>> GetListAsync([FromQuery] GetStockAdjustmentListInput input)
    {
        return _appService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual Task<StockAdjustmentDto> GetAsync(Guid id)
    {
        return _appService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.StockAdjustments.Create)]
    public virtual Task<StockAdjustmentDto> CreateAsync([FromBody] CreateUpdateStockAdjustmentDto input)
    {
        return _appService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.StockAdjustments.Edit)]
    public virtual Task<StockAdjustmentDto> UpdateAsync(Guid id, [FromBody] CreateUpdateStockAdjustmentDto input)
    {
        return _appService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.StockAdjustments.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _appService.DeleteAsync(id);
    }

    [HttpPost("{id}/confirm")]
    [Authorize(LabSpecimenManagementPermissions.StockAdjustments.Confirm)]
    public virtual Task<StockAdjustmentDto> ConfirmAsync(Guid id)
    {
        return _appService.ConfirmAsync(id);
    }
}
