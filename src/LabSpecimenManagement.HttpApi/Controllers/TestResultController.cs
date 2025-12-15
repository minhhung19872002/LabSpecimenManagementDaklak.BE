using System;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.TestRequests;
using LabSpecimenManagement.TestResults;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService]
[Route("api/v1/test-results")]
[Authorize(LabSpecimenManagementPermissions.TestResults.Default)]
public class TestResultController : LabSpecimenManagementController
{
    private readonly ITestResultAppService _testResultAppService;

    public TestResultController(ITestResultAppService testResultAppService)
    {
        _testResultAppService = testResultAppService;
    }

    [HttpGet]
    public virtual async Task<PagedResultDto<TestResultDto>> GetListAsync(
        [FromQuery] int skipCount = 0,
        [FromQuery] int maxResultCount = 50,
        [FromQuery] string? sorting = null,
        [FromQuery] string? search = null,
        [FromQuery] ResultApprovalStatus? approvalStatus = null,
        [FromQuery] bool? isPass = null,
        [FromQuery] Guid? testId = null)
    {
        var input = new GetTestResultListDto
        {
            SkipCount = skipCount,
            MaxResultCount = maxResultCount,
            Sorting = sorting,
            Search = search,
            ApprovalStatus = approvalStatus,
            IsPass = isPass,
            TestId = testId
        };
        return await _testResultAppService.GetFilteredListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual Task<TestResultDto> GetAsync(Guid id)
    {
        return _testResultAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.TestResults.Create)]
    public virtual Task<TestResultDto> CreateAsync([FromBody] CreateTestResultDto input)
    {
        return _testResultAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.TestResults.Edit)]
    public virtual Task<TestResultDto> UpdateAsync(Guid id, [FromBody] UpdateTestResultDto input)
    {
        return _testResultAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.TestResults.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _testResultAppService.DeleteAsync(id);
    }

    // ==================== WORKFLOW ENDPOINTS ====================

    /// <summary>
    /// KTV submit kết quả để chờ duyệt
    /// </summary>
    [HttpPost("{id}/submit")]
    [Authorize(LabSpecimenManagementPermissions.TestResults.Submit)]
    public virtual Task<TestResultDto> SubmitForReviewAsync(Guid id, [FromBody] SubmitForReviewDto input)
    {
        return _testResultAppService.SubmitForReviewAsync(id, input);
    }

    /// <summary>
    /// Trưởng phòng duyệt kết quả (Step 1)
    /// </summary>
    [HttpPost("{id}/review")]
    [Authorize(LabSpecimenManagementPermissions.TestResults.Review)]
    public virtual Task<TestResultDto> ReviewAsync(Guid id, [FromBody] ReviewResultDto input)
    {
        return _testResultAppService.ReviewAsync(id, input);
    }

    /// <summary>
    /// Giám đốc phê duyệt kết quả (Step 2)
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize(LabSpecimenManagementPermissions.TestResults.Approve)]
    public virtual Task<TestResultDto> ApproveAsync(Guid id, [FromBody] ApproveResultDto input)
    {
        return _testResultAppService.ApproveAsync(id, input);
    }

    /// <summary>
    /// Lấy danh sách kết quả chờ duyệt B1 (cho Trưởng phòng)
    /// </summary>
    [HttpGet("pending-review")]
    [Authorize(LabSpecimenManagementPermissions.TestResults.Review)]
    public virtual async Task<PagedResultDto<TestResultDto>> GetPendingReviewListAsync([FromQuery] PagedRequestDto input)
    {
        var abpInput = new PagedAndSortedResultRequestDto
        {
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        };
        return await _testResultAppService.GetPendingReviewListAsync(abpInput);
    }

    /// <summary>
    /// Lấy danh sách kết quả chờ duyệt B2 (cho Giám đốc)
    /// </summary>
    [HttpGet("pending-approval")]
    [Authorize(LabSpecimenManagementPermissions.TestResults.Approve)]
    public virtual async Task<PagedResultDto<TestResultDto>> GetPendingApprovalListAsync([FromQuery] PagedRequestDto input)
    {
        var abpInput = new PagedAndSortedResultRequestDto
        {
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        };
        return await _testResultAppService.GetPendingApprovalListAsync(abpInput);
    }

    /// <summary>
    /// Từ chối kết quả
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize(LabSpecimenManagementPermissions.TestResults.Reject)]
    public virtual Task<TestResultDto> RejectAsync(Guid id, [FromBody] RejectResultDto input)
    {
        return _testResultAppService.RejectAsync(id, input);
    }

    /// <summary>
    /// Lấy kết quả theo phép thử
    /// </summary>
    [HttpGet("by-test/{testId}")]
    public virtual Task<ListResultDto<TestResultDto>> GetByTestAsync(Guid testId)
    {
        return _testResultAppService.GetByTestAsync(testId);
    }
}
