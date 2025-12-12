using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using LabSpecimenManagement.TestResults;

namespace LabSpecimenManagement.Controllers;

[RemoteService]
[Area("app")]
[Route("api/app/test-result")]
public class TestResultController : LabSpecimenManagementController
{
    private readonly ITestResultAppService _testResultAppService;

    public TestResultController(ITestResultAppService testResultAppService)
    {
        _testResultAppService = testResultAppService;
    }

    [HttpGet]
    public virtual Task<PagedResultDto<TestResultDto>> GetListAsync(PagedAndSortedResultRequestDto input)
    {
        return _testResultAppService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual Task<TestResultDto> GetAsync(Guid id)
    {
        return _testResultAppService.GetAsync(id);
    }

    [HttpPost]
    public virtual Task<TestResultDto> CreateAsync(CreateTestResultDto input)
    {
        return _testResultAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    public virtual Task<TestResultDto> UpdateAsync(Guid id, UpdateTestResultDto input)
    {
        return _testResultAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    public virtual Task DeleteAsync(Guid id)
    {
        return _testResultAppService.DeleteAsync(id);
    }

    // ==================== WORKFLOW ENDPOINTS ====================

    /// <summary>
    /// KTV submit kết quả để chờ duyệt
    /// POST /api/app/test-result/{id}/submit
    /// </summary>
    [HttpPost("{id}/submit")]
    public virtual Task<TestResultDto> SubmitForReviewAsync(Guid id, SubmitForReviewDto input)
    {
        return _testResultAppService.SubmitForReviewAsync(id, input);
    }

    /// <summary>
    /// Trưởng phòng duyệt kết quả (Step 1)
    /// POST /api/app/test-result/{id}/review
    /// </summary>
    [HttpPost("{id}/review")]
    public virtual Task<TestResultDto> ReviewAsync(Guid id, ReviewResultDto input)
    {
        return _testResultAppService.ReviewAsync(id, input);
    }

    /// <summary>
    /// Giám đốc phê duyệt kết quả (Step 2)
    /// POST /api/app/test-result/{id}/approve
    /// </summary>
    [HttpPost("{id}/approve")]
    public virtual Task<TestResultDto> ApproveAsync(Guid id, ApproveResultDto input)
    {
        return _testResultAppService.ApproveAsync(id, input);
    }

    /// <summary>
    /// Lấy danh sách kết quả chờ duyệt B1 (cho Trưởng phòng)
    /// GET /api/app/test-result/pending-review
    /// </summary>
    [HttpGet("pending-review")]
    public virtual Task<PagedResultDto<TestResultDto>> GetPendingReviewListAsync(PagedAndSortedResultRequestDto input)
    {
        return _testResultAppService.GetPendingReviewListAsync(input);
    }

    /// <summary>
    /// Lấy danh sách kết quả chờ duyệt B2 (cho Giám đốc)
    /// GET /api/app/test-result/pending-approval
    /// </summary>
    [HttpGet("pending-approval")]
    public virtual Task<PagedResultDto<TestResultDto>> GetPendingApprovalListAsync(PagedAndSortedResultRequestDto input)
    {
        return _testResultAppService.GetPendingApprovalListAsync(input);
    }
}
