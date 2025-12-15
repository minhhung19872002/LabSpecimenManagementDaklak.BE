using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.TestResults;

public interface ITestResultAppService : ICrudAppService<
    TestResultDto,
    Guid,
    PagedAndSortedResultRequestDto,
    CreateTestResultDto,
    UpdateTestResultDto>
{
    /// <summary>
    /// KTV submit test result for review
    /// </summary>
    Task<TestResultDto> SubmitForReviewAsync(Guid id, SubmitForReviewDto input);

    /// <summary>
    /// Department head reviews test result (Step 1)
    /// </summary>
    Task<TestResultDto> ReviewAsync(Guid id, ReviewResultDto input);

    /// <summary>
    /// Director approves test result (Step 2)
    /// </summary>
    Task<TestResultDto> ApproveAsync(Guid id, ApproveResultDto input);

    /// <summary>
    /// Get list of test results pending review (for department head)
    /// </summary>
    Task<PagedResultDto<TestResultDto>> GetPendingReviewListAsync(PagedAndSortedResultRequestDto input);

    /// <summary>
    /// Get list of test results pending approval (for director)
    /// </summary>
    Task<PagedResultDto<TestResultDto>> GetPendingApprovalListAsync(PagedAndSortedResultRequestDto input);

    /// <summary>
    /// Get filtered list of test results
    /// </summary>
    Task<PagedResultDto<TestResultDto>> GetFilteredListAsync(GetTestResultListDto input);

    /// <summary>
    /// Reject test result
    /// </summary>
    Task<TestResultDto> RejectAsync(Guid id, RejectResultDto input);

    /// <summary>
    /// Get test results by test ID
    /// </summary>
    Task<ListResultDto<TestResultDto>> GetByTestAsync(Guid testId);
}
