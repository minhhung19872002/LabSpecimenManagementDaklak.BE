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
[Route("api/v1/test-methods")]
[Authorize(LabSpecimenManagementPermissions.MasterData.Default)]
public class TestMethodController : LabSpecimenManagementController
{
    private readonly ITestMethodAppService _testMethodAppService;

    public TestMethodController(ITestMethodAppService testMethodAppService)
    {
        _testMethodAppService = testMethodAppService;
    }

    [HttpGet]
    public virtual async Task<PagedResultDto<TestMethodDto>> GetListAsync([FromQuery] PagedRequestDto input)
    {
        var abpInput = new PagedAndSortedResultRequestDto
        {
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        };
        return await _testMethodAppService.GetListAsync(abpInput);
    }

    [HttpGet("{id}")]
    public virtual Task<TestMethodDto> GetAsync(Guid id)
    {
        return _testMethodAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Create)]
    public virtual Task<TestMethodDto> CreateAsync([FromBody] CreateTestMethodDto input)
    {
        return _testMethodAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task<TestMethodDto> UpdateAsync(Guid id, [FromBody] UpdateTestMethodDto input)
    {
        return _testMethodAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _testMethodAppService.DeleteAsync(id);
    }

    [HttpGet("active")]
    public virtual Task<ListResultDto<TestMethodDto>> GetActiveAsync()
    {
        return _testMethodAppService.GetActiveAsync();
    }

    [HttpGet("all")]
    public virtual Task<ListResultDto<TestMethodDto>> GetAllAsync()
    {
        return _testMethodAppService.GetAllAsync();
    }

    [HttpGet("with-indicators")]
    public virtual Task<List<TestMethodWithIndicatorsDto>> GetAllWithIndicatorsAsync()
    {
        return _testMethodAppService.GetAllWithIndicatorsAsync();
    }

    [HttpGet("{id}/with-indicators")]
    public virtual Task<TestMethodWithIndicatorsDto> GetWithIndicatorsAsync(Guid id)
    {
        return _testMethodAppService.GetWithIndicatorsAsync(id);
    }

    [HttpGet("{id}/indicators")]
    public virtual Task<List<TestMethodIndicatorDto>> GetIndicatorsAsync(Guid id)
    {
        return _testMethodAppService.GetIndicatorsAsync(id);
    }

    [HttpPost("{id}/indicators")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task AddIndicatorsAsync(Guid id, [FromBody] AddIndicatorsToTestMethodDto input)
    {
        return _testMethodAppService.AddIndicatorsAsync(id, input);
    }

    /// <summary>
    /// Cập nhật thông tin chỉ tiêu trong phương pháp thử (đơn giá)
    /// </summary>
    [HttpPut("{id}/indicators/{indicatorId}")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task<TestMethodIndicatorDto> UpdateIndicatorAsync(
        Guid id,
        Guid indicatorId,
        [FromQuery] Guid? specimenTypeId,
        [FromBody] UpdateTestMethodIndicatorDto input)
    {
        return _testMethodAppService.UpdateIndicatorAsync(id, indicatorId, specimenTypeId, input);
    }

    [HttpPut("{id}/indicators/{indicatorId}/toggle-active")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task ToggleIndicatorActiveAsync(Guid id, Guid indicatorId, [FromQuery] Guid? specimenTypeId)
    {
        return _testMethodAppService.ToggleIndicatorActiveAsync(id, indicatorId, specimenTypeId);
    }

    [HttpDelete("{id}/indicators/{indicatorId}")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task RemoveIndicatorAsync(Guid id, Guid indicatorId, [FromQuery] Guid? specimenTypeId = null)
    {
        return _testMethodAppService.RemoveIndicatorAsync(id, indicatorId, specimenTypeId);
    }

    [HttpPost("{id}/toggle-active")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task<TestMethodDto> ToggleActiveAsync(Guid id)
    {
        return _testMethodAppService.ToggleActiveAsync(id);
    }

    /// <summary>
    /// Xóa hàng loạt chỉ tiêu khỏi phương pháp thử
    /// Uses composite key (IndicatorId + SpecimenTypeId) to identify records
    /// </summary>
    [HttpPost("{id}/indicators/batch-remove")]
    [Authorize(LabSpecimenManagementPermissions.MasterData.Edit)]
    public virtual Task<int> RemoveIndicatorsBatchAsync(Guid id, [FromBody] BulkIndicatorKeysDto input)
    {
        return _testMethodAppService.RemoveIndicatorsBatchAsync(id, input.Keys);
    }
}
