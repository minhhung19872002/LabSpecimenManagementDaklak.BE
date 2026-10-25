using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.Testing;

// TODO: EXTERNAL-CONSUMPTION — remove this entire file after BA confirms
// Endpoint test ẩn (chỉ admin): forward payload tiêu hao sang UAT theo yêu cầu.
public interface IExternalConsumptionTestAppService : IApplicationService
{
    Task<ExternalConsumptionForwardResultDto> ForwardAsync(ExternalConsumptionForwardDto input);
}
