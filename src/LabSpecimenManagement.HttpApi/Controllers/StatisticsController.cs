using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.Statistics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Volo.Abp;

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
}
