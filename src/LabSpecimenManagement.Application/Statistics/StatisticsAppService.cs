using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Reports;
using LabSpecimenManagement.Tasks;
using LabSpecimenManagement.TestRequests;
using MiniExcelLibs;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Content;
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
    private readonly IRepository<SpecimenType, Guid> _specimenTypeRepository;

    public StatisticsAppService(
        IRepository<TestRequest, Guid> testRequestRepository,
        IRepository<Specimen, Guid> specimenRepository,
        IRepository<Test, Guid> testRepository,
        IRepository<Report, Guid> reportRepository,
        IRepository<TaskAssignment, Guid> taskRepository,
        IRepository<SpecimenType, Guid> specimenTypeRepository)
    {
        _testRequestRepository = testRequestRepository;
        _specimenRepository = specimenRepository;
        _testRepository = testRepository;
        _reportRepository = reportRepository;
        _taskRepository = taskRepository;
        _specimenTypeRepository = specimenTypeRepository;
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

        // Get all queryables
        var specimens = await _specimenRepository.GetQueryableAsync();
        var specimenTypes = await _specimenTypeRepository.GetQueryableAsync();
        var tests = await _testRepository.GetQueryableAsync();
        var reports = await _reportRepository.GetQueryableAsync();
        var testRequests = await _testRequestRepository.GetQueryableAsync();
        var tasks = await _taskRepository.GetQueryableAsync();

        // Sample Matrix - group specimens by type
        var sampleMatrix = specimens
                          .GroupBy(s => s.Type ?? "Không xác định")
                          .Select(g => new SampleMatrixDto
                          {
                              SpecimenType = g.Key,
                              TotalCount = g.Count(),
                              ReceivedCount = g.Count(x => x.Status == SpecimenStatus.Received),
                              TestingCount = g.Count(x => x.Status == SpecimenStatus.Testing),
                              CompletedCount = g.Count(x => x.Status == SpecimenStatus.Storage)
                          })
                          .ToList();

        // Workload by Department
        var workloadByDept = tasks
            .Where(x => !string.IsNullOrEmpty(x.Department))
            .GroupBy(x => x.Department)
            .Select(g => new WorkloadByDepartmentDto
            {
                Department = g.Key,
                TotalTasks = g.Count(),
                PendingTasks = g.Count(x => x.Status == TaskAssignmentStatus.Pending),
                InProgressTasks = g.Count(x => x.Status == TaskAssignmentStatus.InProgress),
                CompletedTasks = g.Count(x => x.Status == TaskAssignmentStatus.Completed),
                CompletionRate = g.Count() > 0
                    ? Math.Round((decimal)g.Count(x => x.Status == TaskAssignmentStatus.Completed) / g.Count() * 100, 1)
                    : 0
            })
            .OrderByDescending(x => x.TotalTasks)
            .ToList();

        // TAT Compliance - calculate by week for last 4 weeks
        var tatCompliance = CalculateTATCompliance(testRequests);

        // Monthly trends for last 12 months
        var monthlyTrends = Enumerable.Range(0, 12)
            .Select(i => DateTime.Now.AddMonths(-i))
            .Select(date => new MonthlyTrendDto
            {
                Month = $"T{date.Month}/{date.Year}",
                TestRequests = testRequests.Count(x =>
                    x.RequestDate.Year == date.Year && x.RequestDate.Month == date.Month),
                Specimens = specimens.Count(x =>
                    x.CreationTime.Year == date.Year && x.CreationTime.Month == date.Month),
                Tests = tests.Count(x =>
                    x.CreationTime.Year == date.Year && x.CreationTime.Month == date.Month),
                Reports = reports.Count(x =>
                    x.CreationTime.Year == date.Year && x.CreationTime.Month == date.Month)
            })
            .Reverse()
            .ToList();

        return new StatisticsDataDto
        {
            Overview = overview,
            SampleMatrix = sampleMatrix,
            TATCompliance = tatCompliance,
            WorkloadByDepartment = workloadByDept,
            MonthlyTrends = monthlyTrends
        };
    }

    private List<TATComplianceDto> CalculateTATCompliance(IQueryable<TestRequest> testRequests)
    {
        var result = new List<TATComplianceDto>();

        // Calculate for last 4 weeks
        for (int i = 0; i < 4; i++)
        {
            var weekStart = DateTime.Now.Date.AddDays(-7 * (i + 1));
            var weekEnd = DateTime.Now.Date.AddDays(-7 * i);

            var weekRequests = testRequests
                .Where(x => x.RequestDate >= weekStart && x.RequestDate < weekEnd)
                .ToList();

            var totalRequests = weekRequests.Count;
            var requestsWithDeadline = weekRequests
                .Where(x => x.ExpectedCompletionDate.HasValue)
                .ToList();

            var onTimeRequests = requestsWithDeadline
                .Count(x => x.Status == TestRequestStatus.Completed &&
                           x.LastModificationTime <= x.ExpectedCompletionDate);

            var delayedRequests = requestsWithDeadline
                .Count(x => x.Status == TestRequestStatus.Completed &&
                           x.LastModificationTime > x.ExpectedCompletionDate);

            result.Add(new TATComplianceDto
            {
                Period = $"Tuần {4 - i} ({weekStart:dd/MM} - {weekEnd.AddDays(-1):dd/MM})",
                TotalRequests = totalRequests,
                OnTimeRequests = onTimeRequests,
                DelayedRequests = delayedRequests,
                ComplianceRate = requestsWithDeadline.Count > 0
                    ? Math.Round((decimal)onTimeRequests / requestsWithDeadline.Count * 100, 1)
                    : 0
            });
        }

        // Add monthly summary
        var monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var monthRequests = testRequests
            .Where(x => x.RequestDate >= monthStart)
            .ToList();

        var monthWithDeadline = monthRequests
            .Where(x => x.ExpectedCompletionDate.HasValue)
            .ToList();

        var monthOnTime = monthWithDeadline
            .Count(x => x.Status == TestRequestStatus.Completed &&
                       x.LastModificationTime <= x.ExpectedCompletionDate);

        result.Insert(0, new TATComplianceDto
        {
            Period = $"Tháng {DateTime.Now.Month}/{DateTime.Now.Year}",
            TotalRequests = monthRequests.Count,
            OnTimeRequests = monthOnTime,
            DelayedRequests = monthWithDeadline.Count(x =>
                x.Status == TestRequestStatus.Completed &&
                x.LastModificationTime > x.ExpectedCompletionDate),
            ComplianceRate = monthWithDeadline.Count > 0
                ? Math.Round((decimal)monthOnTime / monthWithDeadline.Count * 100, 1)
                : 0
        });

        return result;
    }

    public async Task<IRemoteStreamContent> ExportToExcelAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var data = await GetStatisticsDataAsync(fromDate, toDate);

        var memoryStream = new MemoryStream();

        // Prepare sheets data
        var sheets = new Dictionary<string, object>
        {
            ["Tổng quan"] = new[]
            {
                new { Chỉ_số = "Tổng yêu cầu thử nghiệm", Giá_trị = data.Overview.TotalTestRequests },
                new { Chỉ_số = "Tổng mẫu thử", Giá_trị = data.Overview.TotalSpecimens },
                new { Chỉ_số = "Tổng phép thử", Giá_trị = data.Overview.TotalTests },
                new { Chỉ_số = "Tổng báo cáo", Giá_trị = data.Overview.TotalReports },
                new { Chỉ_số = "Tỷ lệ hoàn thành (%)", Giá_trị = (int)data.Overview.CompletionRate },
                new { Chỉ_số = "Tỷ lệ đúng hạn TAT (%)", Giá_trị = (int)data.Overview.TATComplianceRate }
            },
            ["Theo tháng"] = data.MonthlyTrends.Select(m => new
            {
                Tháng = m.Month,
                Yêu_cầu = m.TestRequests,
                Mẫu = m.Specimens,
                Phép_thử = m.Tests,
                Báo_cáo = m.Reports
            }),
            ["Theo loại mẫu"] = data.SampleMatrix.Select(s => new
            {
                Loại_mẫu = s.SpecimenType,
                Tổng = s.TotalCount,
                Đã_nhận = s.ReceivedCount,
                Đang_thử = s.TestingCount,
                Hoàn_thành = s.CompletedCount
            }),
            ["Theo phòng ban"] = data.WorkloadByDepartment.Select(w => new
            {
                Phòng_ban = w.Department,
                Tổng_công_việc = w.TotalTasks,
                Chờ_xử_lý = w.PendingTasks,
                Đang_thực_hiện = w.InProgressTasks,
                Hoàn_thành = w.CompletedTasks,
                Hiệu_suất = $"{w.CompletionRate:F1}%"
            }),
            ["TAT Compliance"] = data.TATCompliance.Select(t => new
            {
                Kỳ = t.Period,
                Tổng_yêu_cầu = t.TotalRequests,
                Đúng_hạn = t.OnTimeRequests,
                Trễ_hạn = t.DelayedRequests,
                Tỷ_lệ = $"{t.ComplianceRate:F1}%"
            })
        };

        await memoryStream.SaveAsAsync(sheets);
        memoryStream.Position = 0;

        var fileName = fromDate.HasValue && toDate.HasValue
            ? $"ThongKe_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx"
            : $"ThongKe_{DateTime.Now:yyyyMMdd}.xlsx";

        return new RemoteStreamContent(memoryStream, fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }
}
