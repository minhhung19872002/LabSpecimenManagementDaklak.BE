using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.Statistics;

public interface IStatisticsAppService : IApplicationService
{
    Task<StatisticsOverviewDto> GetOverviewAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<StatisticsDataDto> GetStatisticsDataAsync(DateTime? fromDate = null, DateTime? toDate = null);
}
