using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Reports;
using LabSpecimenManagement.Tasks;
using LabSpecimenManagement.TestRequests;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.Statistics;

[RemoteService(IsEnabled = false)]
public class StatisticsAppService : ApplicationService, IStatisticsAppService
{
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;
    private readonly IRepository<Specimen, Guid> _specimenRepository;
    private readonly IRepository<Test, Guid> _testRepository;
    private readonly IRepository<Report, Guid> _reportRepository;
    private readonly IRepository<TaskAssignment, Guid> _taskRepository;

    public StatisticsAppService(
        IRepository<TestRequest, Guid> testRequestRepository,
        IRepository<Specimen, Guid> specimenRepository,
        IRepository<Test, Guid> testRepository,
        IRepository<Report, Guid> reportRepository,
        IRepository<TaskAssignment, Guid> taskRepository)
    {
        _testRequestRepository = testRequestRepository;
        _specimenRepository = specimenRepository;
        _testRepository = testRepository;
        _reportRepository = reportRepository;
        _taskRepository = taskRepository;
    }

    public async Task<StatisticsOverviewDto> GetOverviewAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var from = fromDate ?? DateTime.Now.AddMonths(-1);
        var to = toDate ?? DateTime.Now;

        var testRequests = await _testRequestRepository.GetQueryableAsync();
        var specimens = await _specimenRepository.GetQueryableAsync();
        var tests = await _testRepository.GetQueryableAsync();
        var reports = await _reportRepository.GetQueryableAsync();

        var filteredRequests = testRequests.Where(x => x.RequestDate >= from && x.RequestDate <= to);
        var totalRequests = filteredRequests.Count();
        var completedRequests = filteredRequests.Count(x => x.Status == TestRequestStatus.Completed);

        // Calculate TAT compliance
        var requestsWithDeadline = filteredRequests.Where(x => x.ExpectedCompletionDate.HasValue);
        var onTimeRequests = requestsWithDeadline.Count(x =>
            x.Status == TestRequestStatus.Completed &&
            x.LastModificationTime <= x.ExpectedCompletionDate);
        var totalWithDeadline = requestsWithDeadline.Count();

        return new StatisticsOverviewDto
        {
            TotalTestRequests = totalRequests,
            TotalSpecimens = specimens.Count(x => x.CreationTime >= from && x.CreationTime <= to),
            TotalTests = tests.Count(x => x.CreationTime >= from && x.CreationTime <= to),
            TotalReports = reports.Count(x => x.CreationTime >= from && x.CreationTime <= to),
            CompletionRate = totalRequests > 0 ? (decimal)completedRequests / totalRequests * 100 : 0,
            TATComplianceRate = totalWithDeadline > 0 ? (decimal)onTimeRequests / totalWithDeadline * 100 : 0
        };
    }

    public async Task<StatisticsDataDto> GetStatisticsDataAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var overview = await GetOverviewAsync(fromDate, toDate);

        var specimens = await _specimenRepository.GetQueryableAsync();
        var sampleMatrix = specimens
            .GroupBy(x => x.Type)
            .Select(g => new SampleMatrixDto
            {
                SpecimenType = g.Key ?? "Unknown",
                TotalCount = g.Count(),
                ReceivedCount = g.Count(x => x.Status == SpecimenStatus.Received),
                TestingCount = g.Count(x => x.Status == SpecimenStatus.Testing),
                CompletedCount = g.Count(x => x.Status == SpecimenStatus.Storage)
            })
            .ToList();

        var tasks = await _taskRepository.GetQueryableAsync();
        var workloadByDept = tasks
            .GroupBy(x => x.Department)
            .Select(g => new WorkloadByDepartmentDto
            {
                Department = g.Key,
                TotalTasks = g.Count(),
                PendingTasks = g.Count(x => x.Status == TaskAssignmentStatus.Pending),
                InProgressTasks = g.Count(x => x.Status == TaskAssignmentStatus.InProgress),
                CompletedTasks = g.Count(x => x.Status == TaskAssignmentStatus.Completed),
                CompletionRate = g.Count() > 0
                    ? (decimal)g.Count(x => x.Status == TaskAssignmentStatus.Completed) / g.Count() * 100
                    : 0
            })
            .ToList();

        // Monthly trends for last 6 months
        var testRequests = await _testRequestRepository.GetQueryableAsync();
        var monthlyTrends = Enumerable.Range(0, 6)
            .Select(i => DateTime.Now.AddMonths(-i))
            .Select(date => new MonthlyTrendDto
            {
                Month = date.ToString("MM/yyyy"),
                TestRequests = testRequests.Count(x =>
                    x.RequestDate.Year == date.Year && x.RequestDate.Month == date.Month),
                Specimens = specimens.Count(x =>
                    x.CreationTime.Year == date.Year && x.CreationTime.Month == date.Month),
                Tests = 0, // Would need test data
                Reports = 0 // Would need report data
            })
            .Reverse()
            .ToList();

        return new StatisticsDataDto
        {
            Overview = overview,
            SampleMatrix = sampleMatrix,
            TATCompliance = new List<TATComplianceDto>(), // TODO: Implement
            WorkloadByDepartment = workloadByDept,
            MonthlyTrends = monthlyTrends
        };
    }
}
