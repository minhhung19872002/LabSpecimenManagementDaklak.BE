using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.Dashboard;

public interface IDashboardAppService : IApplicationService
{
    Task<DashboardStatisticsDto> GetStatisticsAsync();
    Task<DashboardDataDto> GetDashboardDataAsync();

    // New endpoints matching frontend requirements
    Task<DashboardSummaryDto> GetSummaryAsync();
    Task<ChartDataDto> GetTestRequestsByStatusAsync();
    Task<ChartDataDto> GetSpecimensByStatusAsync();
    Task<ChartDataDto> GetTestsByStatusAsync();
    Task<ChartDataDto> GetMonthlyTestRequestsAsync(int? year);
    Task<List<RecentActivityDto>> GetRecentActivitiesAsync(int? count);
    Task<ChartDataDto> GetTestResultsApprovalStatusAsync();
    Task<ChartDataDto> GetNCRsBySeverityAsync();
}
