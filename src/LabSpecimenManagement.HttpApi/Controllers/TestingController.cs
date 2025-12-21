using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.Testing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Route("api/v1/testing")]
[Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Default)]
public class TestingController : LabSpecimenManagementController
{
    private readonly ITestingAppService _testingAppService;

    public TestingController(ITestingAppService testingAppService)
    {
        _testingAppService = testingAppService;
    }

    /// <summary>
    /// Get filtered and paged list of testing tasks
    /// </summary>
    [HttpGet]
    public virtual async Task<PagedResultDto<TestingListDto>> GetListAsync([FromQuery] GetTestingListInput input)
    {
        return await _testingAppService.GetListAsync(input);
    }

    /// <summary>
    /// Get testing task detail by LaboratoryTask ID
    /// </summary>
    [HttpGet("{id}")]
    public virtual async Task<TestingDetailDto> GetAsync(Guid id)
    {
        return await _testingAppService.GetAsync(id);
    }

    /// <summary>
    /// Save results (draft) for indicators
    /// </summary>
    [HttpPost("{id}/save-results")]
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public virtual async Task SaveResultsAsync(Guid id, [FromBody] SaveResultsInput input)
    {
        await _testingAppService.SaveResultsAsync(id, input);
    }

    /// <summary>
    /// Submit indicators for approval
    /// </summary>
    [HttpPost("{id}/submit-approval")]
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public virtual async Task SubmitForApprovalAsync(Guid id, [FromBody] SubmitForApprovalInput input)
    {
        await _testingAppService.SubmitForApprovalAsync(id, input);
    }

    /// <summary>
    /// Get rejection history for an indicator
    /// </summary>
    [HttpGet("indicators/{indicatorId}/rejection-history")]
    public virtual async Task<List<RejectionHistoryDto>> GetRejectionHistoryAsync(Guid indicatorId)
    {
        return await _testingAppService.GetRejectionHistoryAsync(indicatorId);
    }
}
