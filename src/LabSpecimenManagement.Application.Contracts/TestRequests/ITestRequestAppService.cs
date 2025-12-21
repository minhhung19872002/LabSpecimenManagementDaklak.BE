using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// Disable auto API generation - using custom TestRequestController instead
/// </summary>
[RemoteService(IsEnabled = false)]
public interface ITestRequestAppService :
    ICrudAppService<
        TestRequestDto,
        Guid,
        PagedAndSortedResultRequestDto,
        CreateTestRequestDto,
        UpdateTestRequestDto>
{
    /// <summary>
    /// Get filtered and paged list of test requests
    /// </summary>
    Task<PagedResultDto<TestRequestDto>> GetFilteredListAsync(GetTestRequestListDto input);

    /// <summary>
    /// When printing test request form - update status from New to Pending
    /// </summary>
    Task PrintTestRequestAsync(Guid id);

    /// <summary>
    /// When QLKT assigns method + technician - update status to InProgress
    /// </summary>
    Task UpdateToInProgressAsync(Guid testRequestId);

    /// <summary>
    /// When QLKT sends results - update status to TestingCompleted
    /// </summary>
    Task UpdateToTestingCompletedAsync(Guid testRequestId);

    /// <summary>
    /// When printing test result form - update status to PendingApproval
    /// </summary>
    Task UpdateToPendingApprovalAsync(Guid id);

    /// <summary>
    /// When leader approves - update status to Approved
    /// </summary>
    Task ApproveAsync(Guid id);

    /// <summary>
    /// When sending result to customer - update status to ResultSent
    /// </summary>
    Task SendResultAsync(Guid id);

    /// <summary>
    /// When completing the test request - update status to Completed
    /// </summary>
    Task CompleteAsync(Guid id);
}
