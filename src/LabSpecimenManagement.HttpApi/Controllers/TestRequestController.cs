using LabSpecimenManagement.Common;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.TestRequests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Route("api/v1/test-requests")]
[Authorize(LabSpecimenManagementPermissions.TestRequests.Default)]
public class TestRequestController : LabSpecimenManagementController
{
    private readonly ITestRequestAppService _testRequestAppService;

    public TestRequestController(ITestRequestAppService testRequestAppService)
    {
        _testRequestAppService = testRequestAppService;
    }

    [HttpGet]
    public virtual async Task<PagedResultDto<TestRequestDto>> GetListAsync([FromQuery] GetTestRequestListDto input)
    {
        return await _testRequestAppService.GetFilteredListAsync(input);
    }

    [HttpGet("{id}")]
    public virtual Task<TestRequestDto> GetAsync(Guid id)
    {
        return _testRequestAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Create)]
    public virtual Task<TestRequestDto> CreateAsync([FromBody] CreateTestRequestDto input)
    {
        return _testRequestAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual Task<TestRequestDto> UpdateAsync(Guid id, [FromBody] UpdateTestRequestDto input)
    {
        return _testRequestAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _testRequestAppService.DeleteAsync(id);
    }

    /// <summary>
    /// Print test request form - update status from New to Pending
    /// </summary>
    [HttpPost("{id}/print")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual Task PrintAsync(Guid id)
    {
        return _testRequestAppService.PrintTestRequestAsync(id);
    }

    /// <summary>
    /// Update status to PendingApproval after printing result form
    /// </summary>
    [HttpPost("{id}/pending-approval")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual Task UpdateToPendingApprovalAsync(Guid id)
    {
        return _testRequestAppService.UpdateToPendingApprovalAsync(id);
    }

    /// <summary>
    /// Approve test request - update status to Approved
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual Task ApproveAsync(Guid id)
    {
        return _testRequestAppService.ApproveAsync(id);
    }

    /// <summary>
    /// Reject result - QLCL rejects result, update status back to InProgress
    /// Trả lại cho QLKT xem xét
    /// </summary>
    [HttpPost("{id}/reject-result")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual Task RejectResultAsync(Guid id, [FromBody] RejectChangesDto input)
    {
        return _testRequestAppService.RejectResultAsync(id, input.Reason);
    }

    /// <summary>
    /// Send result to customer - update status to ResultSent
    /// </summary>
    [HttpPost("{id}/send-result")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual Task SendResultAsync(Guid id)
    {
        return _testRequestAppService.SendResultAsync(id);
    }

    /// <summary>
    /// Complete test request - update status to Completed
    /// </summary>
    [HttpPost("{id}/complete")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual Task CompleteAsync(Guid id)
    {
        return _testRequestAppService.CompleteAsync(id);
    }

    /// <summary>
    /// Send sample - update status from Pending to SampleSent
    /// </summary>
    [HttpPost("{id}/send-sample")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual Task SendSampleAsync(Guid id)
    {
        return _testRequestAppService.SendSampleAsync(id);
    }

    /// <summary>
    /// Approve changes - QLCL approves changes, update status back to previous status
    /// </summary>
    [HttpPost("{id}/approve-changes")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual Task ApproveChangesAsync(Guid id)
    {
        return _testRequestAppService.ApproveChangesAsync(id);
    }

    /// <summary>
    /// Reject changes - QLCL rejects changes, rollback data and update status back to previous status
    /// </summary>
    [HttpPost("{id}/reject-changes")]
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public virtual Task RejectChangesAsync(Guid id, [FromBody] RejectChangesDto input)
    {
        return _testRequestAppService.RejectChangesAsync(id, input.Reason);
    }

    /// <summary>
    /// Get change history for a test request
    /// </summary>
    [HttpGet("{id}/change-history")]
    public virtual Task<System.Collections.Generic.List<TestRequestChangeHistoryDto>> GetChangeHistoryAsync(Guid id)
    {
        return _testRequestAppService.GetChangeHistoryAsync(id);
    }
}
