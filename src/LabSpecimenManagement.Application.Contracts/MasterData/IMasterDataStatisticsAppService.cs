using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.MasterData;

public interface IMasterDataStatisticsAppService : IApplicationService
{
    Task<MasterDataStatisticsDto> GetStatisticsAsync();
}
