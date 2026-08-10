using System;
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

    #region Dashboard Overview APIs

    /// <summary>
    /// Get dashboard overview statistics with date filter
    /// </summary>
    Task<DashboardOverviewDto> GetOverviewAsync(DateTime fromDate, DateTime toDate);

    /// <summary>
    /// Get recent requests for dashboard with optional customer filter and paging
    /// </summary>
    Task<DashboardRecentRequestPagedResultDto> GetDashboardRecentRequestsAsync(
        string? customerName,
        int skipCount,
        int maxResultCount);

    /// <summary>
    /// Get revenue grouped by customer
    /// </summary>
    Task<RevenueByCustomerPagedResultDto> GetRevenueByCustomerAsync(int skipCount, int maxResultCount);

    /// <summary>
    /// Get monthly revenue for a specific customer
    /// </summary>
    Task<List<MonthlyRevenueDto>> GetMonthlyRevenueAsync(string customerName, int year);

    /// <summary>
    /// Get all unique customer names for filter dropdown
    /// </summary>
    Task<List<string>> GetCustomerNamesAsync();

    /// <summary>
    /// Get revenue grouped by contact person
    /// </summary>
    Task<RevenueByContactPagedResultDto> GetRevenueByContactAsync(int skipCount, int maxResultCount);

    /// <summary>
    /// Get monthly revenue for a specific contact person
    /// </summary>
    Task<List<MonthlyRevenueDto>> GetMonthlyRevenueByContactAsync(string contactPerson, int year);

    /// <summary>
    /// Get all unique contact person names for filter dropdown
    /// </summary>
    Task<List<string>> GetContactPersonNamesAsync();

    #endregion
}
