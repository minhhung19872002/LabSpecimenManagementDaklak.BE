using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.Statistics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Content;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Route("api/v1/lab-report")]
[Authorize(LabSpecimenManagementPermissions.LabReport.Default)]
public class LabReportController : LabSpecimenManagementController
{
    private readonly ILabReportAppService _labReportAppService;

    public LabReportController(ILabReportAppService labReportAppService)
    {
        _labReportAppService = labReportAppService;
    }

    [HttpGet]
    public virtual Task<LabReportDataDto> GetLabReportDataAsync(
        [FromQuery] Guid labId,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate)
    {
        return _labReportAppService.GetLabReportDataAsync(labId, fromDate, toDate);
    }

    [HttpGet("contract/{testRequestId}")]
    public virtual Task<LabContractDetailDto> GetLabContractDetailAsync(
        [FromRoute] Guid testRequestId,
        [FromQuery] Guid labId)
    {
        return _labReportAppService.GetLabContractDetailAsync(testRequestId, labId);
    }

    [HttpGet("export")]
    [Authorize(LabSpecimenManagementPermissions.LabReport.Export)]
    public virtual async Task<IActionResult> ExportLabReportAsync(
        [FromQuery] Guid labId,
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate)
    {
        var result = await _labReportAppService.ExportLabReportAsync(labId, fromDate, toDate);
        return File(result.GetStream(), result.ContentType, result.FileName);
    }
}
