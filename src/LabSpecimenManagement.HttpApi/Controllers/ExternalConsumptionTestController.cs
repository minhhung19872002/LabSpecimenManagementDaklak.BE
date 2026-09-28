using System.Threading.Tasks;
using LabSpecimenManagement.Testing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;

namespace LabSpecimenManagement.Controllers;

// TODO: EXTERNAL-CONSUMPTION — remove this entire file after BA confirms
// Endpoint test ẩn (không có UI): POST /api/v1/testing/external-consumption/forward
// Body: { "targetUrl": "https://api-uat.tihelab.vn", "request": { "laboratoryCode": "...", "indicators": [...] } }
[RemoteService(Name = "Default")]
[Route("api/v1/testing/external-consumption")]
[Authorize]
public class ExternalConsumptionTestController : LabSpecimenManagementController
{
    private readonly IExternalConsumptionTestAppService _appService;

    public ExternalConsumptionTestController(IExternalConsumptionTestAppService appService)
    {
        _appService = appService;
    }

    [HttpPost("forward")]
    public virtual Task<ExternalConsumptionForwardResultDto> ForwardAsync([FromBody] ExternalConsumptionForwardDto input)
    {
        return _appService.ForwardAsync(input);
    }
}
