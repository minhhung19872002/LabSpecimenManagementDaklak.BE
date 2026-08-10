using System;
using System.Collections.Generic;
using LabSpecimenManagement.Dashboard;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Volo.Abp;

namespace LabSpecimenManagement.Controllers;

[RemoteService(Name = "Default")]
[Route("api/v1/dashboard")]
[Authorize(LabSpecimenManagementPermissions.Dashboard.Default)]
public class DashboardController : LabSpecimenManagementController
{
    private readonly IDashboardAppService _dashboardAppService;

    public DashboardController(IDashboardAppService dashboardAppService)
    {
        _dashboardAppService = dashboardAppService;
    }

    [HttpGet("statistics")]
    public virtual Task<DashboardStatisticsDto> GetStatisticsAsync()
    {
        return _dashboardAppService.GetStatisticsAsync();
    }

    [HttpGet]
    public virtual Task<DashboardDataDto> GetDashboardDataAsync()
    {
        return _dashboardAppService.GetDashboardDataAsync();
    }

    [HttpGet("summary")]
    public virtual Task<DashboardSummaryDto> GetSummaryAsync()
    {
        return _dashboardAppService.GetSummaryAsync();
    }

    [HttpGet("test-requests-by-status")]
    public virtual Task<ChartDataDto> GetTestRequestsByStatusAsync()
    {
        return _dashboardAppService.GetTestRequestsByStatusAsync();
    }

    [HttpGet("specimens-by-status")]
    public virtual Task<ChartDataDto> GetSpecimensByStatusAsync()
    {
        return _dashboardAppService.GetSpecimensByStatusAsync();
    }

    [HttpGet("tests-by-status")]
    public virtual Task<ChartDataDto> GetTestsByStatusAsync()
    {
        return _dashboardAppService.GetTestsByStatusAsync();
    }

    [HttpGet("monthly-test-requests")]
    public virtual Task<ChartDataDto> GetMonthlyTestRequestsAsync([FromQuery] int? year)
    {
        return _dashboardAppService.GetMonthlyTestRequestsAsync(year);
    }

    [HttpGet("recent-activities")]
    public virtual Task<List<RecentActivityDto>> GetRecentActivitiesAsync([FromQuery] int? count)
    {
        return _dashboardAppService.GetRecentActivitiesAsync(count);
    }

    [HttpGet("test-results-approval-status")]
    public virtual Task<ChartDataDto> GetTestResultsApprovalStatusAsync()
    {
        return _dashboardAppService.GetTestResultsApprovalStatusAsync();
    }

    [HttpGet("ncrs-by-severity")]
    public virtual Task<ChartDataDto> GetNCRsBySeverityAsync()
    {
        return _dashboardAppService.GetNCRsBySeverityAsync();
    }

    #region Dashboard Overview APIs

    [HttpGet("overview")]
    public virtual Task<DashboardOverviewDto> GetOverviewAsync(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate)
    {
        return _dashboardAppService.GetOverviewAsync(fromDate, toDate);
    }

    [HttpGet("recent-requests")]
    public virtual Task<DashboardRecentRequestPagedResultDto> GetDashboardRecentRequestsAsync(
        [FromQuery] string? customerName,
        [FromQuery] int skipCount = 0,
        [FromQuery] int maxResultCount = 10)
    {
        return _dashboardAppService.GetDashboardRecentRequestsAsync(customerName, skipCount, maxResultCount);
    }

    [HttpGet("revenue-by-customer")]
    public virtual Task<RevenueByCustomerPagedResultDto> GetRevenueByCustomerAsync(
        [FromQuery] int skipCount = 0,
        [FromQuery] int maxResultCount = 10)
    {
        return _dashboardAppService.GetRevenueByCustomerAsync(skipCount, maxResultCount);
    }

    [HttpGet("monthly-revenue")]
    public virtual Task<List<MonthlyRevenueDto>> GetMonthlyRevenueAsync(
        [FromQuery] string customerName,
        [FromQuery] int year)
    {
        return _dashboardAppService.GetMonthlyRevenueAsync(customerName, year);
    }

    [HttpGet("customer-names")]
    public virtual Task<List<string>> GetCustomerNamesAsync()
    {
        return _dashboardAppService.GetCustomerNamesAsync();
    }

    [HttpGet("revenue-by-contact")]
    public virtual Task<RevenueByContactPagedResultDto> GetRevenueByContactAsync(
        [FromQuery] int skipCount = 0,
        [FromQuery] int maxResultCount = 10)
    {
        return _dashboardAppService.GetRevenueByContactAsync(skipCount, maxResultCount);
    }

    [HttpGet("monthly-revenue-by-contact")]
    public virtual Task<List<MonthlyRevenueDto>> GetMonthlyRevenueByContactAsync(
        [FromQuery] string contactPerson,
        [FromQuery] int year)
    {
        return _dashboardAppService.GetMonthlyRevenueByContactAsync(contactPerson, year);
    }

    [HttpGet("contact-person-names")]
    public virtual Task<List<string>> GetContactPersonNamesAsync()
    {
        return _dashboardAppService.GetContactPersonNamesAsync();
    }

    #endregion
}
