using LabSpecimenManagement.Common;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers.MasterData;

[RemoteService(Name = "Default")]
[Route("api/v1/standards")]
[Authorize(LabSpecimenManagementPermissions.Standards.Default)]
public class StandardController : LabSpecimenManagementController
{
    private readonly IStandardAppService _standardAppService;

    public StandardController(IStandardAppService standardAppService)
    {
        _standardAppService = standardAppService;
    }

    [HttpGet]
    public virtual async Task<PagedResultDto<StandardDto>> GetListAsync([FromQuery] PagedRequestDto input)
    {
        var abpInput = new PagedAndSortedResultRequestDto
        {
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        };
        return await _standardAppService.GetListAsync(abpInput);
    }

    [HttpGet("{id}")]
    public virtual Task<StandardDto> GetAsync(Guid id)
    {
        return _standardAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.Standards.Create)]
    public virtual Task<StandardDto> CreateAsync([FromBody] CreateStandardDto input)
    {
        return _standardAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.Standards.Edit)]
    public virtual Task<StandardDto> UpdateAsync(Guid id, [FromBody] UpdateStandardDto input)
    {
        return _standardAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.Standards.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _standardAppService.DeleteAsync(id);
    }

    [HttpGet("active")]
    public virtual Task<ListResultDto<StandardDto>> GetActiveAsync()
    {
        return _standardAppService.GetActiveAsync();
    }

    [HttpPost("{id}/toggle-active")]
    [Authorize(LabSpecimenManagementPermissions.Standards.Edit)]
    public virtual Task<StandardDto> ToggleActiveAsync(Guid id)
    {
        return _standardAppService.ToggleActiveAsync(id);
    }

    [HttpGet("with-indicators")]
    public virtual Task<List<StandardWithIndicatorsDto>> GetAllWithIndicatorsAsync()
    {
        return _standardAppService.GetAllWithIndicatorsAsync();
    }

    [HttpGet("{id}/with-indicators")]
    public virtual Task<StandardWithIndicatorsDto> GetWithIndicatorsAsync(Guid id)
    {
        return _standardAppService.GetWithIndicatorsAsync(id);
    }

    [HttpGet("{id}/indicators")]
    public virtual Task<List<StandardIndicatorDto>> GetIndicatorsAsync(Guid id)
    {
        return _standardAppService.GetIndicatorsAsync(id);
    }

    [HttpPost("{id}/indicators")]
    [Authorize(LabSpecimenManagementPermissions.Standards.Edit)]
    public virtual Task AddIndicatorsAsync(Guid id, [FromBody] AddIndicatorsToStandardDto input)
    {
        return _standardAppService.AddIndicatorsAsync(id, input);
    }

    [HttpPut("{id}/indicators/{indicatorId}")]
    [Authorize(LabSpecimenManagementPermissions.Standards.Edit)]
    public virtual Task<StandardIndicatorDto> UpdateIndicatorAsync(Guid id, Guid indicatorId, [FromBody] UpdateStandardIndicatorDto input)
    {
        return _standardAppService.UpdateIndicatorAsync(id, indicatorId, input);
    }

    [HttpDelete("{id}/indicators/{indicatorId}")]
    [Authorize(LabSpecimenManagementPermissions.Standards.Edit)]
    public virtual Task RemoveIndicatorAsync(Guid id, Guid indicatorId)
    {
        return _standardAppService.RemoveIndicatorAsync(id, indicatorId);
    }

    [HttpPost("{id}/indicators/{indicatorId}/toggle-active")]
    [Authorize(LabSpecimenManagementPermissions.Standards.Edit)]
    public virtual Task<StandardIndicatorDto> ToggleIndicatorActiveAsync(Guid id, Guid indicatorId)
    {
        return _standardAppService.ToggleIndicatorActiveAsync(id, indicatorId);
    }

    /// <summary>
    /// Xóa hàng loạt chỉ tiêu khỏi tiêu chuẩn
    /// </summary>
    [HttpPost("{id}/indicators/batch-remove")]
    [Authorize(LabSpecimenManagementPermissions.Standards.Edit)]
    public virtual Task<int> RemoveIndicatorsBatchAsync(Guid id, [FromBody] BulkIdsDto input)
    {
        return _standardAppService.RemoveIndicatorsBatchAsync(id, input.Ids);
    }

    /// <summary>
    /// Kích hoạt hàng loạt chỉ tiêu trong tiêu chuẩn
    /// </summary>
    [HttpPost("{id}/indicators/batch-activate")]
    [Authorize(LabSpecimenManagementPermissions.Standards.Edit)]
    public virtual Task<int> ActivateIndicatorsBatchAsync(Guid id, [FromBody] BulkIdsDto input)
    {
        return _standardAppService.ActivateIndicatorsBatchAsync(id, input.Ids);
    }

    /// <summary>
    /// Ngừng kích hoạt hàng loạt chỉ tiêu trong tiêu chuẩn
    /// </summary>
    [HttpPost("{id}/indicators/batch-deactivate")]
    [Authorize(LabSpecimenManagementPermissions.Standards.Edit)]
    public virtual Task<int> DeactivateIndicatorsBatchAsync(Guid id, [FromBody] BulkIdsDto input)
    {
        return _standardAppService.DeactivateIndicatorsBatchAsync(id, input.Ids);
    }
}
