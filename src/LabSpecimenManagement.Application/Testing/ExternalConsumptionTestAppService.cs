using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.Testing;

// TODO: EXTERNAL-CONSUMPTION — remove this entire file after BA confirms
// Endpoint test ẩn trên PROD: admin truyền payload, BE forward nguyên sang UAT
// và trả về phản hồi của UAT. Không đụng DB PROD.
[Authorize]
public class ExternalConsumptionTestAppService : ApplicationService, IExternalConsumptionTestAppService
{
    private readonly ExternalConsumptionService _externalConsumptionService;

    public ExternalConsumptionTestAppService(ExternalConsumptionService externalConsumptionService)
    {
        _externalConsumptionService = externalConsumptionService;
    }

    public async Task<ExternalConsumptionForwardResultDto> ForwardAsync(ExternalConsumptionForwardDto input)
    {
        if (!CurrentUser.IsInRole("admin"))
        {
            throw new UserFriendlyException("Chỉ admin được dùng endpoint này");
        }

        if (input.Request == null || string.IsNullOrWhiteSpace(input.Request.LaboratoryCode))
        {
            throw new UserFriendlyException("Thiếu request.laboratoryCode");
        }

        Logger.LogInformation(
            "External consumption TEST forward by {User} -> {Target}, lab {Lab}, {Count} indicator(s)",
            CurrentUser.UserName, input.TargetUrl ?? "(config)", input.Request.LaboratoryCode,
            input.Request.Indicators?.Count ?? 0);

        return await _externalConsumptionService.SendAsync(input.TargetUrl, input.Request);
    }
}
