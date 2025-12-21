using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.Testing;

/// <summary>
/// Interface for Testing Application Service
/// </summary>
public interface ITestingAppService : IApplicationService
{
    /// <summary>
    /// Get list of testing tasks with filters
    /// </summary>
    Task<PagedResultDto<TestingListDto>> GetListAsync(GetTestingListInput input);

    /// <summary>
    /// Get testing task detail by LaboratoryTask ID
    /// </summary>
    Task<TestingDetailDto> GetAsync(Guid laboratoryTaskId);

    /// <summary>
    /// Save results (draft) for indicators
    /// </summary>
    Task SaveResultsAsync(Guid laboratoryTaskId, SaveResultsInput input);

    /// <summary>
    /// Submit indicators for approval
    /// </summary>
    Task SubmitForApprovalAsync(Guid laboratoryTaskId, SubmitForApprovalInput input);

    /// <summary>
    /// Get rejection history for an indicator
    /// </summary>
    Task<List<RejectionHistoryDto>> GetRejectionHistoryAsync(Guid indicatorId);
}
