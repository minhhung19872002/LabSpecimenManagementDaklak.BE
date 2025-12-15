using System;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;
using Volo.Abp.Content;

namespace LabSpecimenManagement.Statistics;

public interface IStatisticsAppService : IApplicationService
{
    Task<StatisticsOverviewDto> GetOverviewAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<StatisticsDataDto> GetStatisticsDataAsync(DateTime? fromDate = null, DateTime? toDate = null);
    Task<IRemoteStreamContent> ExportToExcelAsync(DateTime? fromDate = null, DateTime? toDate = null);
}
