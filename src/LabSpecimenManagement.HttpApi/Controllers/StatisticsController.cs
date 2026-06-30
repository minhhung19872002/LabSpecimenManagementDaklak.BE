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
[Route("api/v1/statistics")]
[Authorize(LabSpecimenManagementPermissions.Statistics.Default)]
public class StatisticsController : LabSpecimenManagementController
{
    private readonly IStatisticsAppService _statisticsAppService;

    public StatisticsController(IStatisticsAppService statisticsAppService)
    {
        _statisticsAppService = statisticsAppService;
    }

    [HttpGet("overview")]
    public virtual Task<StatisticsOverviewDto> GetOverviewAsync([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        return _statisticsAppService.GetOverviewAsync(fromDate, toDate);
    }

    [HttpGet]
    public virtual Task<StatisticsDataDto> GetStatisticsDataAsync([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        return _statisticsAppService.GetStatisticsDataAsync(fromDate, toDate);
    }

    [HttpGet("export")]
    public virtual async Task<IActionResult> ExportToExcelAsync([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
    {
        var result = await _statisticsAppService.ExportToExcelAsync(fromDate, toDate);
        return File(result.GetStream(), result.ContentType, result.FileName);
    }
}
