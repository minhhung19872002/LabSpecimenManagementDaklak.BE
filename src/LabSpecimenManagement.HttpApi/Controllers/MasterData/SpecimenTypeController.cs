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
[Route("api/v1/specimen-types")]
[Authorize(LabSpecimenManagementPermissions.SpecimenTypes.Default)]
public class SpecimenTypeController : LabSpecimenManagementController
{
    private readonly ISpecimenTypeAppService _specimenTypeAppService;

    public SpecimenTypeController(ISpecimenTypeAppService specimenTypeAppService)
    {
        _specimenTypeAppService = specimenTypeAppService;
    }

    [HttpGet]
    public virtual async Task<PagedResultDto<SpecimenTypeDto>> GetListAsync([FromQuery] PagedRequestDto input)
    {
        var abpInput = new PagedAndSortedResultRequestDto
        {
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        };
        return await _specimenTypeAppService.GetListAsync(abpInput);
    }

    [HttpGet("{id}")]
    public virtual Task<SpecimenTypeDto> GetAsync(Guid id)
    {
        return _specimenTypeAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.SpecimenTypes.Create)]
    public virtual async Task<SpecimenTypeDto> CreateAsync([FromBody] CreateUpdateSpecimenTypeDto input)
    {
        return await _specimenTypeAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.SpecimenTypes.Edit)]
    public virtual async Task<SpecimenTypeDto> UpdateAsync(Guid id, [FromBody] CreateUpdateSpecimenTypeDto input)
    {
        return await _specimenTypeAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.SpecimenTypes.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _specimenTypeAppService.DeleteAsync(id);
    }

    [HttpGet("active")]
    public virtual Task<ListResultDto<SpecimenTypeDto>> GetActiveSpecimenTypesAsync()
    {
        return _specimenTypeAppService.GetActiveSpecimenTypesAsync();
    }

    #region Indicator Management

    /// <summary>
    /// Lấy tất cả loại mẫu kèm danh sách chỉ tiêu
    /// </summary>
    [HttpGet("with-indicators")]
    public virtual Task<List<SpecimenTypeWithIndicatorsDto>> GetAllWithIndicatorsAsync()
    {
        return _specimenTypeAppService.GetAllWithIndicatorsAsync();
    }

    /// <summary>
    /// Lấy 1 loại mẫu kèm danh sách chỉ tiêu
    /// </summary>
    [HttpGet("{id}/with-indicators")]
    public virtual Task<SpecimenTypeWithIndicatorsDto> GetWithIndicatorsAsync(Guid id)
    {
        return _specimenTypeAppService.GetWithIndicatorsAsync(id);
    }

    /// <summary>
    /// Lấy danh sách chỉ tiêu của loại mẫu
    /// </summary>
    [HttpGet("{id}/indicators")]
    public virtual Task<List<SpecimenTypeIndicatorDto>> GetIndicatorsAsync(Guid id)
    {
        return _specimenTypeAppService.GetIndicatorsAsync(id);
    }

    /// <summary>
    /// Thêm chỉ tiêu vào loại mẫu
    /// </summary>
    [HttpPost("{id}/indicators")]
    [Authorize(LabSpecimenManagementPermissions.SpecimenTypes.Edit)]
    public virtual Task AddIndicatorsAsync(Guid id, [FromBody] AddIndicatorsToSpecimenTypeDto input)
    {
        return _specimenTypeAppService.AddIndicatorsAsync(id, input);
    }

    /// <summary>
    /// Xóa chỉ tiêu khỏi loại mẫu
    /// </summary>
    [HttpDelete("{id}/indicators/{indicatorId}")]
    [Authorize(LabSpecimenManagementPermissions.SpecimenTypes.Edit)]
    public virtual Task RemoveIndicatorAsync(Guid id, Guid indicatorId)
    {
        return _specimenTypeAppService.RemoveIndicatorAsync(id, indicatorId);
    }

    /// <summary>
    /// Bật/tắt trạng thái hoạt động của chỉ tiêu trong loại mẫu
    /// </summary>
    [HttpPost("{id}/indicators/{indicatorId}/toggle-active")]
    [Authorize(LabSpecimenManagementPermissions.SpecimenTypes.Edit)]
    public virtual Task<SpecimenTypeIndicatorDto> ToggleIndicatorActiveAsync(Guid id, Guid indicatorId)
    {
        return _specimenTypeAppService.ToggleIndicatorActiveAsync(id, indicatorId);
    }

    #endregion
}
