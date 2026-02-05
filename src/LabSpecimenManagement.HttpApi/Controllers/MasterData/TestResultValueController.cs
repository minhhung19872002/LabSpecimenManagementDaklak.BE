using LabSpecimenManagement.Common;
using LabSpecimenManagement.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers.MasterData;

[RemoteService(Name = "Default")]
[Route("api/v1/test-result-values")]
[Authorize]
public class TestResultValueController : LabSpecimenManagementController
{
    private readonly ITestResultValueAppService _testResultValueAppService;

    public TestResultValueController(ITestResultValueAppService testResultValueAppService)
    {
        _testResultValueAppService = testResultValueAppService;
    }

    [HttpGet]
    public virtual async Task<PagedResultDto<TestResultValueDto>> GetListAsync([FromQuery] PagedRequestDto input)
    {
        var abpInput = new PagedAndSortedResultRequestDto
        {
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        };
        return await _testResultValueAppService.GetListAsync(abpInput);
    }

    [HttpGet("{id}")]
    public virtual Task<TestResultValueDto> GetAsync(Guid id)
    {
        return _testResultValueAppService.GetAsync(id);
    }

    [HttpPost]
    public virtual Task<TestResultValueDto> CreateAsync([FromBody] CreateUpdateTestResultValueDto input)
    {
        return _testResultValueAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    public virtual Task<TestResultValueDto> UpdateAsync(Guid id, [FromBody] CreateUpdateTestResultValueDto input)
    {
        return _testResultValueAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    public virtual Task DeleteAsync(Guid id)
    {
        return _testResultValueAppService.DeleteAsync(id);
    }

    [HttpGet("active")]
    public virtual Task<ListResultDto<TestResultValueDto>> GetActiveTestResultValuesAsync()
    {
        return _testResultValueAppService.GetActiveTestResultValuesAsync();
    }

    /// <summary>
    /// Lấy danh sách kết quả recommend cho chỉ tiêu
    /// - Nếu chỉ tiêu có kết quả riêng: chỉ trả về kết quả của chỉ tiêu đó
    /// - Nếu chỉ tiêu không có kết quả riêng: trả về tất cả kết quả không gắn chỉ tiêu
    /// </summary>
    [HttpGet("for-indicator/{indicatorId}")]
    public virtual Task<ListResultDto<TestResultValueDto>> GetResultsForIndicatorAsync(Guid indicatorId)
    {
        return _testResultValueAppService.GetResultsForIndicatorAsync(indicatorId);
    }

    #region Indicator Management

    /// <summary>
    /// Lấy danh sách kết quả kèm theo danh sách chỉ tiêu
    /// </summary>
    [HttpGet("with-indicators")]
    public virtual Task<ListResultDto<TestResultValueDto>> GetWithIndicatorsAsync()
    {
        return _testResultValueAppService.GetWithIndicatorsAsync();
    }

    /// <summary>
    /// Thêm chỉ tiêu vào kết quả thử nghiệm
    /// </summary>
    [HttpPost("{id}/indicators")]
    public virtual Task AddIndicatorsAsync(Guid id, [FromBody] AddIndicatorsToTestResultValueDto input)
    {
        return _testResultValueAppService.AddIndicatorsAsync(id, input);
    }

    /// <summary>
    /// Xóa một chỉ tiêu khỏi kết quả thử nghiệm
    /// </summary>
    [HttpDelete("{id}/indicators/{indicatorId}")]
    public virtual Task RemoveIndicatorAsync(Guid id, Guid indicatorId, [FromQuery] Guid specimenTypeId)
    {
        return _testResultValueAppService.RemoveIndicatorAsync(id, indicatorId, specimenTypeId);
    }

    /// <summary>
    /// Toggle trạng thái active của chỉ tiêu trong kết quả thử nghiệm
    /// </summary>
    [HttpPost("{id}/indicators/{indicatorId}/toggle-active")]
    public virtual Task<TestResultValueIndicatorDto> ToggleIndicatorActiveAsync(Guid id, Guid indicatorId, [FromQuery] Guid specimenTypeId)
    {
        return _testResultValueAppService.ToggleIndicatorActiveAsync(id, indicatorId, specimenTypeId);
    }

    /// <summary>
    /// Xóa nhiều chỉ tiêu khỏi kết quả thử nghiệm (batch)
    /// </summary>
    [HttpPost("{id}/indicators/batch-remove")]
    public virtual Task<int> BatchRemoveIndicatorsAsync(Guid id, [FromBody] BatchRemoveIndicatorsDto input)
    {
        return _testResultValueAppService.BatchRemoveIndicatorsAsync(id, input);
    }

    #endregion
}
