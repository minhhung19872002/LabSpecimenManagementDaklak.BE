using LabSpecimenManagement.MasterData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp;

namespace LabSpecimenManagement.Controllers.MasterData;

[RemoteService(Name = "Default")]
[Route("api/v1/master-data/statistics")]
[Authorize]
public class MasterDataStatisticsController : LabSpecimenManagementController
{
    private readonly IMasterDataStatisticsAppService _statisticsAppService;

    public MasterDataStatisticsController(IMasterDataStatisticsAppService statisticsAppService)
    {
        _statisticsAppService = statisticsAppService;
    }

    [HttpGet]
    public virtual Task<MasterDataStatisticsDto> GetStatisticsAsync()
    {
        return _statisticsAppService.GetStatisticsAsync();
    }
}
