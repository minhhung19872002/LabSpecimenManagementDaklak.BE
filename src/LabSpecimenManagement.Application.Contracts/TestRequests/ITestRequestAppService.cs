using System;
using System.Collections.Generic;
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
    /// When user sends sample - update status from Pending to SampleSent
    /// </summary>
    Task SendSampleAsync(Guid id);

    /// <summary>
    /// When QLKT assigns method + technician - update status to InProgress
    /// </summary>
    Task UpdateToInProgressAsync(Guid testRequestId);

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

    /// <summary>
    /// QLCL phê duyệt thay đổi: PendingChangeApproval → Previous status
    /// </summary>
    Task ApproveChangesAsync(Guid id);

    /// <summary>
    /// QLCL từ chối thay đổi: Rollback + PendingChangeApproval → Previous status
    /// </summary>
    Task RejectChangesAsync(Guid id, string reason);

    /// <summary>
    /// Lấy lịch sử thay đổi của phiếu yêu cầu
    /// </summary>
    Task<List<TestRequestChangeHistoryDto>> GetChangeHistoryAsync(Guid id);

    /// <summary>
    /// QLCL từ chối kết quả: PendingApproval → InProgress
    /// Trả lại cho QLKT xem xét
    /// </summary>
    Task RejectResultAsync(Guid id, string reason);

    /// <summary>
    /// Kiểm tra số hợp đồng có tồn tại không
    /// </summary>
    /// <param name="contractNumber">Số hợp đồng cần kiểm tra</param>
    /// <param name="excludeId">ID của phiếu yêu cầu cần loại trừ (dùng khi update)</param>
    /// <returns>True nếu số HĐ đã tồn tại</returns>
    Task<bool> CheckContractNumberExistsAsync(string contractNumber, Guid? excludeId = null);

    /// <summary>
    /// Lấy thông tin tiến trình (timeline) của phiếu yêu cầu
    /// </summary>
    Task<TestRequestTimelineDto> GetTimelineAsync(Guid id);
}
