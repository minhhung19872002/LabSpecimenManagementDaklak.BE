using LabSpecimenManagement.Common;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Content;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Route("api/v1/reports")]
[Authorize(LabSpecimenManagementPermissions.Reports.Default)]
public class ReportController : LabSpecimenManagementController
{
    private readonly IReportAppService _reportAppService;

    public ReportController(IReportAppService reportAppService)
    {
        _reportAppService = reportAppService;
    }

    [HttpGet]
    public virtual async Task<PagedResultDto<ReportDto>> GetListAsync([FromQuery] PagedRequestDto input)
    {
        var abpInput = new PagedAndSortedResultRequestDto
        {
            SkipCount = input.SkipCount,
            MaxResultCount = input.MaxResultCount,
            Sorting = input.Sorting
        };
        return await _reportAppService.GetListAsync(abpInput);
    }

    [HttpGet("{id}")]
    public virtual Task<ReportDto> GetAsync(Guid id)
    {
        return _reportAppService.GetAsync(id);
    }

    [HttpPost]
    [Authorize(LabSpecimenManagementPermissions.Reports.Create)]
    public virtual Task<ReportDto> CreateAsync([FromBody] CreateReportDto input)
    {
        return _reportAppService.CreateAsync(input);
    }

    [HttpPut("{id}")]
    [Authorize(LabSpecimenManagementPermissions.Reports.Edit)]
    public virtual Task<ReportDto> UpdateAsync(Guid id, [FromBody] UpdateReportDto input)
    {
        return _reportAppService.UpdateAsync(id, input);
    }

    [HttpDelete("{id}")]
    [Authorize(LabSpecimenManagementPermissions.Reports.Delete)]
    public virtual Task DeleteAsync(Guid id)
    {
        return _reportAppService.DeleteAsync(id);
    }

    [HttpPost("{id}/submit")]
    [Authorize(LabSpecimenManagementPermissions.Reports.Create)]
    public virtual Task<ReportDto> SubmitForApprovalAsync(Guid id)
    {
        return _reportAppService.SubmitForApprovalAsync(id);
    }

    [HttpPost("{id}/approve")]
    [Authorize(LabSpecimenManagementPermissions.Reports.Approve)]
    public virtual Task<ReportDto> ApproveAsync(Guid id)
    {
        return _reportAppService.ApproveAsync(id);
    }

    [HttpPost("{id}/reject")]
    [Authorize(LabSpecimenManagementPermissions.Reports.Approve)]
    public virtual Task<ReportDto> RejectAsync(Guid id, [FromBody] string reason)
    {
        return _reportAppService.RejectAsync(id, reason);
    }

    [HttpPost("{id}/issue")]
    [Authorize(LabSpecimenManagementPermissions.Reports.Approve)]
    public virtual Task<ReportDto> IssueAsync(Guid id)
    {
        return _reportAppService.IssueAsync(id);
    }

    [HttpPost("{id}/send-email")]
    [Authorize(LabSpecimenManagementPermissions.Reports.Send)]
    public virtual Task SendEmailAsync(Guid id)
    {
        return _reportAppService.SendEmailAsync(id);
    }

    [HttpGet("by-test-request/{testRequestId}")]
    public virtual Task<ListResultDto<ReportDto>> GetByTestRequestAsync(Guid testRequestId)
    {
        return _reportAppService.GetByTestRequestAsync(testRequestId);
    }

    [HttpGet("{id}/pdf")]
    [Authorize(LabSpecimenManagementPermissions.Reports.Export)]
    public virtual async Task<IActionResult> ExportPdfAsync(Guid id)
    {
        var result = await _reportAppService.ExportPdfAsync(id);
        return File(result.GetStream(), result.ContentType, result.FileName);
    }

    [HttpGet("verify/{reportNumber}")]
    [AllowAnonymous]
    public virtual Task<ReportVerifyResultDto> VerifyReportAsync(string reportNumber)
    {
        return _reportAppService.VerifyReportAsync(reportNumber);
    }
}
