using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LabSpecimenManagement.MasterData.Indicators;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers.MasterData;

[RemoteService(Name = "Default")]
[Route("api/v1/indicators")]
[Authorize(LabSpecimenManagementPermissions.MasterData.Default)]
public class IndicatorController : LabSpecimenManagementController
{
    private readonly IIndicatorAppService _indicatorAppService;

    public IndicatorController(IIndicatorAppService indicatorAppService)
    {
        _indicatorAppService = indicatorAppService;
    }

    [HttpGet]
    public virtual async Task<PagedResultDto<IndicatorDto>> GetListAsync([FromQuery] IndicatorFilterDto input)
    {
        return await _indicatorAppService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual Task<IndicatorDto> GetAsync(Guid id)
    {
        return _indicatorAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Create)]
    public virtual Task<IndicatorDto> CreateAsync([FromBody] CreateIndicatorDto input)
    {
        return _indicatorAppService.CreateAsync(input);
    }

    [HttpPost("bulk")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Create)]
    public virtual Task<List<IndicatorDto>> CreateBulkAsync([FromBody] CreateBulkIndicatorsDto input)
    {
        return _indicatorAppService.CreateBulkAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task<IndicatorDto> UpdateAsync(Guid id, [FromBody] UpdateIndicatorDto input)
    {
        return _indicatorAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _indicatorAppService.DeleteAsync(id);
    }

    [HttpPut("{id}/toggle-active")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task<IndicatorDto> ToggleActiveAsync(Guid id)
    {
        return _indicatorAppService.ToggleActiveAsync(id);
    }

    [HttpGet("by-group/{groupId}")]
    public virtual Task<List<IndicatorDto>> GetByGroupAsync(Guid groupId)
    {
        return _indicatorAppService.GetByGroupAsync(groupId);
    }

    [HttpGet("get-active")]
    public virtual Task<List<IndicatorDto>> GetActiveIndicatorsAsync()
    {
        return _indicatorAppService.GetActiveIndicatorsAsync();
    }

    [HttpGet("can-delete/{id}")]
    public virtual Task<bool> CanDeleteAsync(Guid id)
    {
        return _indicatorAppService.CanDeleteAsync(id);
    }

    [HttpPost("import")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Import)]
    public virtual async Task<ImportIndicatorsResultDto> ImportFromExcelAsync(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        return await _indicatorAppService.ImportFromExcelAsync(stream, file.FileName);
    }

    [HttpGet("template")]
    public virtual async Task<IActionResult> GetExcelTemplateAsync()
    {
        var template = await _indicatorAppService.GetExcelTemplateAsync();
        return File(template, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "indicators-template.xlsx");
    }

    [HttpPost("batch-delete")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Delete)]
    public virtual Task<BatchOperationResultDto> DeleteBatchAsync([FromBody] BatchIndicatorIdsDto input)
    {
        return _indicatorAppService.DeleteBatchAsync(input);
    }

    [HttpPost("batch-deactivate")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task<BatchOperationResultDto> DeactivateBatchAsync([FromBody] BatchIndicatorIdsDto input)
    {
        return _indicatorAppService.DeactivateBatchAsync(input);
    }

    [HttpPost("batch-activate")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task<BatchOperationResultDto> ActivateBatchAsync([FromBody] BatchIndicatorIdsDto input)
    {
        return _indicatorAppService.ActivateBatchAsync(input);
    }

    /// <summary>
    /// Lấy danh sách chỉ tiêu với phương pháp thử
    /// </summary>
    [HttpGet("with-methods")]
    public virtual Task<List<IndicatorWithMethodsDto>> GetAllWithMethodsAsync()
    {
        return _indicatorAppService.GetAllWithMethodsAsync();
    }
}
