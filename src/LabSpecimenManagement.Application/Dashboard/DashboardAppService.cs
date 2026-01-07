using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.NCRs;
using LabSpecimenManagement.QualityManagement;
using LabSpecimenManagement.Reports;
using LabSpecimenManagement.Tasks;
using LabSpecimenManagement.TestRequests;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using System.Linq.Dynamic.Core;

namespace LabSpecimenManagement.Dashboard;

[RemoteService(IsEnabled = false)]
public class DashboardAppService : ApplicationService, IDashboardAppService
{
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;
    private readonly IRepository<Specimen, Guid> _specimenRepository;
    private readonly IRepository<Test, Guid> _testRepository;
    private readonly IRepository<TestResult, Guid> _testResultRepository;
    private readonly IRepository<NCR, Guid> _ncrRepository;
    private readonly IRepository<Report, Guid> _reportRepository;
    private readonly IRepository<TaskAssignment, Guid> _taskRepository;
    private readonly IRepository<TestRequestSample, Guid> _sampleRepository;
    private readonly IRepository<TestRequestSampleIndicator, Guid> _indicatorRepository;
    private readonly IRepository<QualityManagementTask, Guid> _qualityManagementTaskRepository;

    private static readonly string[] MonthNames = { "Th1", "Th2", "Th3", "Th4", "Th5", "Th6", "Th7", "Th8", "Th9", "Th10", "Th11", "Th12" };

    public DashboardAppService(
        IRepository<TestRequest, Guid> testRequestRepository,
        IRepository<Specimen, Guid> specimenRepository,
        IRepository<Test, Guid> testRepository,
        IRepository<TestResult, Guid> testResultRepository,
        IRepository<NCR, Guid> ncrRepository,
        IRepository<Report, Guid> reportRepository,
        IRepository<TaskAssignment, Guid> taskRepository,
        IRepository<TestRequestSample, Guid> sampleRepository,
        IRepository<TestRequestSampleIndicator, Guid> indicatorRepository,
        IRepository<QualityManagementTask, Guid> qualityManagementTaskRepository)
    {
        _testRequestRepository = testRequestRepository;
        _specimenRepository = specimenRepository;
        _testRepository = testRepository;
        _testResultRepository = testResultRepository;
        _ncrRepository = ncrRepository;
        _reportRepository = reportRepository;
        _taskRepository = taskRepository;
        _sampleRepository = sampleRepository;
        _indicatorRepository = indicatorRepository;
        _qualityManagementTaskRepository = qualityManagementTaskRepository;
    }

    public async Task<DashboardStatisticsDto> GetStatisticsAsync()
    {
        var testRequests = await _testRequestRepository.GetQueryableAsync();
        var specimens = await _specimenRepository.GetQueryableAsync();
        var tests = await _testRepository.GetQueryableAsync();
        var testResults = await _testResultRepository.GetQueryableAsync();
        var ncrs = await _ncrRepository.GetQueryableAsync();
        var reports = await _reportRepository.GetQueryableAsync();

        return new DashboardStatisticsDto
        {
            TotalRequests = testRequests.Count(),
            PendingRequests = testRequests.Count(x => x.Status == TestRequestStatus.PendingPayment || x.Status == TestRequestStatus.Paid),
            InProgressRequests = testRequests.Count(x => x.Status == TestRequestStatus.InProgress),
            CompletedRequests = testRequests.Count(x => x.Status == TestRequestStatus.Completed),
            TotalSpecimens = specimens.Count(),
            TotalTests = tests.Count(),
            PendingTests = tests.Count(x => x.Status == TestStatus.Pending || x.Status == TestStatus.Assigned),
            InProgressTests = tests.Count(x => x.Status == TestStatus.InProgress),
            CompletedTests = tests.Count(x => x.Status >= TestStatus.Completed),
            OpenNCRs = ncrs.Count(x => x.Status == NCRStatus.Open || x.Status == NCRStatus.InProgress),
            PendingApprovalResults = testResults.Count(x =>
                x.ApprovalStatus == ResultApprovalStatus.Submitted ||
                x.ApprovalStatus == ResultApprovalStatus.Step1Reviewed),
            TotalReports = reports.Count()
        };
    }

    public async Task<DashboardDataDto> GetDashboardDataAsync()
    {
        var statistics = await GetStatisticsAsync();

        var testRequestQuery = await _testRequestRepository.GetQueryableAsync();
        var recentRequests = testRequestQuery
            .OrderByDescending(x => x.CreationTime)
            .Take(10)
            .Select(x => new RecentTestRequestDto
            {
                Id = x.Id,
                RequestNumber = x.RequestNumber,
                RequestDate = x.RequestDate,
                CustomerName = x.CustomerName,
                Status = x.Status.ToString(),
                SpecimenCount = x.Specimens.Count
            })
            .ToList();

        var testResultQuery = await _testResultRepository.GetQueryableAsync();
        var pendingApprovals = testResultQuery
            .Where(x => x.ApprovalStatus == ResultApprovalStatus.Submitted ||
                       x.ApprovalStatus == ResultApprovalStatus.Step1Reviewed)
            .OrderByDescending(x => x.CreationTime)
            .Take(10)
            .Select(x => new PendingApprovalDto
            {
                Id = x.Id,
                Code = x.Test.Code,
                Type = "TestResult",
                Title = x.Parameter,
                CreatedAt = x.CreationTime,
                Status = x.ApprovalStatus.ToString()
            })
            .ToList();

        return new DashboardDataDto
        {
            Statistics = statistics,
            RecentRequests = recentRequests,
            PendingApprovals = pendingApprovals,
            RecentActivities = new List<ActivityLogDto>() // TODO: Implement from audit log
        };
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync()
    {
        var testRequests = await _testRequestRepository.GetQueryableAsync();
        var specimens = await _specimenRepository.GetQueryableAsync();
        var tests = await _testRepository.GetQueryableAsync();
        var testResults = await _testResultRepository.GetQueryableAsync();
        var ncrs = await _ncrRepository.GetQueryableAsync();
        var reports = await _reportRepository.GetQueryableAsync();
        var tasks = await _taskRepository.GetQueryableAsync();

        return new DashboardSummaryDto
        {
            TotalTestRequests = testRequests.Count(),
            PendingTestRequests = testRequests.Count(x => x.Status == TestRequestStatus.PendingPayment || x.Status == TestRequestStatus.Paid),
            TotalSpecimens = specimens.Count(),
            SpecimenInTesting = specimens.Count(x => x.Status == SpecimenStatus.Testing),
            TotalTests = tests.Count(),
            PendingTests = tests.Count(x => x.Status == TestStatus.Pending || x.Status == TestStatus.Assigned),
            CompletedTests = tests.Count(x => x.Status >= TestStatus.Completed),
            TotalTestResults = testResults.Count(),
            PendingApprovalResults = testResults.Count(x =>
                x.ApprovalStatus == ResultApprovalStatus.Submitted ||
                x.ApprovalStatus == ResultApprovalStatus.Step1Reviewed),
            OpenNCRs = ncrs.Count(x => x.Status == NCRStatus.Open || x.Status == NCRStatus.InProgress),
            PendingTasks = tasks.Count(x => x.Status == TaskAssignmentStatus.Pending || x.Status == TaskAssignmentStatus.Accepted),
            TotalReports = reports.Count(),
            IssuedReports = reports.Count(x => x.Status == ReportStatus.Issued)
        };
    }

    public async Task<ChartDataDto> GetTestRequestsByStatusAsync()
    {
        var testRequests = await _testRequestRepository.GetQueryableAsync();

        var statusCounts = Enum.GetValues<TestRequestStatus>()
            .Select(status => new
            {
                Status = status.ToString(),
                Count = testRequests.Count(x => x.Status == status)
            })
            .ToList();

        return new ChartDataDto
        {
            Labels = statusCounts.Select(x => x.Status).ToList(),
            Datasets = new List<ChartDatasetDto>
            {
                new()
                {
                    Label = "Yêu cầu thử nghiệm",
                    Data = statusCounts.Select(x => x.Count).ToList()
                }
            }
        };
    }

    public async Task<ChartDataDto> GetSpecimensByStatusAsync()
    {
        var specimens = await _specimenRepository.GetQueryableAsync();

        var statusCounts = Enum.GetValues<SpecimenStatus>()
            .Select(status => new
            {
                Status = status.ToString(),
                Count = specimens.Count(x => x.Status == status)
            })
            .ToList();

        return new ChartDataDto
        {
            Labels = statusCounts.Select(x => x.Status).ToList(),
            Datasets = new List<ChartDatasetDto>
            {
                new()
                {
                    Label = "Mẫu thử",
                    Data = statusCounts.Select(x => x.Count).ToList()
                }
            }
        };
    }

    public async Task<ChartDataDto> GetTestsByStatusAsync()
    {
        var tests = await _testRepository.GetQueryableAsync();

        var statusCounts = Enum.GetValues<TestStatus>()
            .Select(status => new
            {
                Status = status.ToString(),
                Count = tests.Count(x => x.Status == status)
            })
            .ToList();

        return new ChartDataDto
        {
            Labels = statusCounts.Select(x => x.Status).ToList(),
            Datasets = new List<ChartDatasetDto>
            {
                new()
                {
                    Label = "Phép thử",
                    Data = statusCounts.Select(x => x.Count).ToList()
                }
            }
        };
    }

    public async Task<ChartDataDto> GetMonthlyTestRequestsAsync(int? year)
    {
        var targetYear = year ?? DateTime.Now.Year;
        var testRequests = await _testRequestRepository.GetQueryableAsync();

        var monthlyData = Enumerable.Range(1, 12)
            .Select(month => new
            {
                Month = month,
                Count = testRequests.Count(x =>
                    x.RequestDate.Year == targetYear && x.RequestDate.Month == month)
            })
            .ToList();

        var monthNames = new[] { "Th1", "Th2", "Th3", "Th4", "Th5", "Th6", "Th7", "Th8", "Th9", "Th10", "Th11", "Th12" };

        return new ChartDataDto
        {
            Labels = monthNames.ToList(),
            Datasets = new List<ChartDatasetDto>
            {
                new()
                {
                    Label = $"Yêu cầu năm {targetYear}",
                    Data = monthlyData.Select(x => x.Count).ToList()
                }
            }
        };
    }

    public async Task<List<RecentActivityDto>> GetRecentActivitiesAsync(int? count)
    {
        var limit = count ?? 10;
        var activities = new List<RecentActivityDto>();

        // Get recent test requests
        var testRequests = await _testRequestRepository.GetQueryableAsync();
        var recentRequests = testRequests
            .OrderByDescending(x => x.CreationTime)
            .Take(limit)
            .Select(x => new RecentActivityDto
            {
                Id = x.Id,
                Type = "TestRequest",
                Description = $"Yêu cầu thử nghiệm {x.RequestNumber} được tạo",
                EntityType = "TestRequest",
                EntityId = x.Id,
                EntityCode = x.RequestNumber,
                PerformedById = x.CreatorId,
                PerformedAt = x.CreationTime
            })
            .ToList();
        activities.AddRange(recentRequests);

        // Get recent specimens
        var specimens = await _specimenRepository.GetQueryableAsync();
        var recentSpecimens = specimens
            .OrderByDescending(x => x.CreationTime)
            .Take(limit)
            .Select(x => new RecentActivityDto
            {
                Id = x.Id,
                Type = "Specimen",
                Description = $"Mẫu {x.Code} được tạo",
                EntityType = "Specimen",
                EntityId = x.Id,
                EntityCode = x.Code,
                PerformedById = x.CreatorId,
                PerformedAt = x.CreationTime
            })
            .ToList();
        activities.AddRange(recentSpecimens);

        // Get recent tests
        var tests = await _testRepository.GetQueryableAsync();
        var recentTests = tests
            .OrderByDescending(x => x.CreationTime)
            .Take(limit)
            .Select(x => new RecentActivityDto
            {
                Id = x.Id,
                Type = "Test",
                Description = $"Phép thử {x.Code} được tạo",
                EntityType = "Test",
                EntityId = x.Id,
                EntityCode = x.Code,
                PerformedById = x.CreatorId,
                PerformedAt = x.CreationTime
            })
            .ToList();
        activities.AddRange(recentTests);

        // Sort by time and return top N
        return activities
            .OrderByDescending(x => x.PerformedAt)
            .Take(limit)
            .ToList();
    }

    public async Task<ChartDataDto> GetTestResultsApprovalStatusAsync()
    {
        var testResults = await _testResultRepository.GetQueryableAsync();

        var statusCounts = Enum.GetValues<ResultApprovalStatus>()
            .Select(status => new
            {
                Status = status.ToString(),
                Count = testResults.Count(x => x.ApprovalStatus == status)
            })
            .ToList();

        return new ChartDataDto
        {
            Labels = statusCounts.Select(x => x.Status).ToList(),
            Datasets = new List<ChartDatasetDto>
            {
                new()
                {
                    Label = "Trạng thái duyệt kết quả",
                    Data = statusCounts.Select(x => x.Count).ToList()
                }
            }
        };
    }

    public async Task<ChartDataDto> GetNCRsBySeverityAsync()
    {
        var ncrs = await _ncrRepository.GetQueryableAsync();

        var severityCounts = Enum.GetValues<NCRSeverity>()
            .Select(severity => new
            {
                Severity = severity.ToString(),
                Count = ncrs.Count(x => x.Severity == severity)
            })
            .ToList();

        return new ChartDataDto
        {
            Labels = severityCounts.Select(x => x.Severity).ToList(),
            Datasets = new List<ChartDatasetDto>
            {
                new()
                {
                    Label = "NCR theo mức độ",
                    Data = severityCounts.Select(x => x.Count).ToList()
                }
            }
        };
    }

    #region Dashboard Overview APIs

    public async Task<DashboardOverviewDto> GetOverviewAsync(DateTime fromDate, DateTime toDate)
    {
        var testRequestQuery = await _testRequestRepository.GetQueryableAsync();
        var ncrQuery = await _ncrRepository.GetQueryableAsync();
        var sampleQuery = await _sampleRepository.GetQueryableAsync();
        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();

        // Adjust toDate to include the entire day (end of day)
        var adjustedToDate = toDate.Date.AddDays(1);

        // Filter TestRequests by CreationTime within date range
        var filteredRequests = testRequestQuery
            .Where(x => x.CreationTime >= fromDate.Date && x.CreationTime < adjustedToDate)
            .ToList();

        var filteredRequestIds = filteredRequests.Select(x => x.Id).ToList();

        // 1. Total Contracts
        var totalContracts = filteredRequests.Count;

        // 2. Total Samples - Count samples in contracts with status != New
        var validRequestIds = filteredRequests
            .Where(x => x.Status != TestRequestStatus.New)
            .Select(x => x.Id)
            .ToList();

        var totalSamples = sampleQuery
            .Where(s => validRequestIds.Contains(s.TestRequestId))
            .Count();

        // 3. Total Indicators - Similar to samples
        var sampleIds = sampleQuery
            .Where(s => validRequestIds.Contains(s.TestRequestId))
            .Select(s => s.Id)
            .ToList();

        var totalIndicators = indicatorQuery
            .Where(i => sampleIds.Contains(i.TestRequestSampleId))
            .Count();

        // 4. Total NCRs - Count NCRs within date range
        var totalNCRs = ncrQuery
            .Where(n => n.CreationTime >= fromDate.Date && n.CreationTime < adjustedToDate)
            .Count();

        // 5. Total Revenue - Sum TotalCost of Completed contracts
        var totalRevenue = filteredRequests
            .Where(x => x.Status == TestRequestStatus.Completed)
            .Sum(x => x.TotalCost);

        // 6 & 7. On-time vs Late Tests
        // Đếm HĐ có status = PendingApproval (Chờ phê duyệt KQ - QLKT đã gửi kết quả)
        // So sánh PendingApprovalDate (ngày QLKT gửi KQ) với ExpectedCompletionDate (ngày YC xong)
        // Nếu QLCL từ chối và QLKT gửi lại, PendingApprovalDate sẽ được cập nhật
        var pendingApprovalRequests = filteredRequests
            .Where(x => x.Status == TestRequestStatus.PendingApproval)
            .ToList();

        var onTimeTests = pendingApprovalRequests
            .Count(x => x.PendingApprovalDate.HasValue && x.ExpectedCompletionDate.HasValue &&
                        x.PendingApprovalDate.Value <= x.ExpectedCompletionDate.Value);

        var lateTests = pendingApprovalRequests
            .Count(x => x.PendingApprovalDate.HasValue && x.ExpectedCompletionDate.HasValue &&
                        x.PendingApprovalDate.Value > x.ExpectedCompletionDate.Value);

        // 8. Pending Approval - Đếm từ bảng QualityManagementTask
        // Bao gồm: HĐ chờ phê duyệt kết quả (ResultApproval) + HĐ chờ phê duyệt thay đổi (ChangeApproval)
        var qmTaskQuery = await _qualityManagementTaskRepository.GetQueryableAsync();
        var pendingApproval = qmTaskQuery
            .Where(q => q.Status == QualityManagementStatus.PendingApproval)
            .Where(q => q.CreationTime >= fromDate.Date && q.CreationTime < adjustedToDate)
            .Count();

        return new DashboardOverviewDto
        {
            TotalContracts = totalContracts,
            TotalSamples = totalSamples,
            TotalIndicators = totalIndicators,
            TotalNCRs = totalNCRs,
            TotalRevenue = totalRevenue,
            OnTimeTests = onTimeTests,
            LateTests = lateTests,
            PendingApproval = pendingApproval
        };
    }

    public async Task<DashboardRecentRequestPagedResultDto> GetDashboardRecentRequestsAsync(
        string? customerName,
        int skipCount,
        int maxResultCount)
    {
        var testRequestQuery = await _testRequestRepository.GetQueryableAsync();
        var sampleQuery = await _sampleRepository.GetQueryableAsync();
        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();

        var query = testRequestQuery.AsQueryable();

        // Filter by customer name if provided
        if (!string.IsNullOrWhiteSpace(customerName))
        {
            query = query.Where(x => x.CustomerName.Contains(customerName));
        }

        // Order by PendingDate descending (most recent first)
        var allItems = query
            .OrderByDescending(x => x.PendingDate ?? x.CreationTime)
            .ToList();

        var totalCount = allItems.Count;

        var pagedItems = allItems
            .Skip(skipCount)
            .Take(maxResultCount)
            .ToList();

        var items = pagedItems.Select(x => new DashboardRecentRequestDto
        {
            Id = x.Id,
            ContractNumber = x.ContractNumber ?? x.RequestNumber,
            CustomerName = x.CustomerName,
            PendingDate = x.PendingDate,
            TotalCost = x.TotalCost,
            Status = (int)x.Status
        }).ToList();

        // Get sample and indicator counts for each request
        var requestIds = items.Select(x => x.Id).ToList();

        var sampleCounts = sampleQuery
            .Where(s => requestIds.Contains(s.TestRequestId))
            .GroupBy(s => s.TestRequestId)
            .Select(g => new { TestRequestId = g.Key, Count = g.Count() })
            .ToList();

        var sampleIdsInRequests = sampleQuery
            .Where(s => requestIds.Contains(s.TestRequestId))
            .Select(s => new { s.Id, s.TestRequestId })
            .ToList();

        var indicatorCounts = indicatorQuery
            .Where(i => sampleIdsInRequests.Select(s => s.Id).Contains(i.TestRequestSampleId))
            .ToList()
            .GroupBy(i => sampleIdsInRequests.First(s => s.Id == i.TestRequestSampleId).TestRequestId)
            .Select(g => new { TestRequestId = g.Key, Count = g.Count() })
            .ToList();

        foreach (var item in items)
        {
            item.SampleCount = sampleCounts.FirstOrDefault(x => x.TestRequestId == item.Id)?.Count ?? 0;
            item.IndicatorCount = indicatorCounts.FirstOrDefault(x => x.TestRequestId == item.Id)?.Count ?? 0;
        }

        return new DashboardRecentRequestPagedResultDto
        {
            Items = items,
            TotalCount = totalCount
        };
    }

    public async Task<RevenueByCustomerPagedResultDto> GetRevenueByCustomerAsync(int skipCount, int maxResultCount)
    {
        var testRequestQuery = await _testRequestRepository.GetQueryableAsync();
        var sampleQuery = await _sampleRepository.GetQueryableAsync();
        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();

        // Get completed contracts grouped by customer
        var completedRequests = testRequestQuery
            .Where(x => x.Status == TestRequestStatus.Completed)
            .ToList();

        var customerGroups = completedRequests
            .GroupBy(x => x.CustomerName)
            .Select(g => new
            {
                CustomerName = g.Key,
                Revenue = g.Sum(x => x.TotalCost),
                RequestIds = g.Select(x => x.Id).ToList()
            })
            .OrderByDescending(x => x.Revenue)
            .ToList();

        var totalCount = customerGroups.Count;

        var pagedGroups = customerGroups
            .Skip(skipCount)
            .Take(maxResultCount)
            .ToList();

        var items = new List<RevenueByCustomerDto>();

        // Get all sample and indicator data once
        var allRequestIds = pagedGroups.SelectMany(g => g.RequestIds).Distinct().ToList();
        var samples = sampleQuery.Where(s => allRequestIds.Contains(s.TestRequestId)).ToList();
        var sampleIds = samples.Select(s => s.Id).ToList();
        var indicators = indicatorQuery.Where(i => sampleIds.Contains(i.TestRequestSampleId)).ToList();

        foreach (var group in pagedGroups)
        {
            var sampleCount = samples.Count(s => group.RequestIds.Contains(s.TestRequestId));
            var groupSampleIds = samples.Where(s => group.RequestIds.Contains(s.TestRequestId)).Select(s => s.Id).ToList();
            var indicatorCount = indicators.Count(i => groupSampleIds.Contains(i.TestRequestSampleId));

            items.Add(new RevenueByCustomerDto
            {
                CustomerName = group.CustomerName,
                TotalSamples = sampleCount,
                TotalIndicators = indicatorCount,
                Revenue = group.Revenue
            });
        }

        return new RevenueByCustomerPagedResultDto
        {
            Items = items,
            TotalCount = totalCount
        };
    }

    public async Task<List<MonthlyRevenueDto>> GetMonthlyRevenueAsync(string customerName, int year)
    {
        var testRequestQuery = await _testRequestRepository.GetQueryableAsync();
        var sampleQuery = await _sampleRepository.GetQueryableAsync();
        var indicatorQuery = await _indicatorRepository.GetQueryableAsync();

        // Filter by customer and year, only completed contracts
        var allRequests = testRequestQuery
            .Where(x => x.CustomerName == customerName &&
                        x.Status == TestRequestStatus.Completed &&
                        x.CompletedDate.HasValue &&
                        x.CompletedDate.Value.Year == year)
            .ToList();

        // Get all samples and indicators for these requests once
        var allRequestIds = allRequests.Select(x => x.Id).ToList();
        var allSamples = sampleQuery.Where(s => allRequestIds.Contains(s.TestRequestId)).ToList();
        var allSampleIds = allSamples.Select(s => s.Id).ToList();
        var allIndicators = indicatorQuery.Where(i => allSampleIds.Contains(i.TestRequestSampleId)).ToList();

        var result = new List<MonthlyRevenueDto>();

        for (int month = 1; month <= 12; month++)
        {
            var monthRequests = allRequests
                .Where(x => x.CompletedDate!.Value.Month == month)
                .ToList();

            var requestIds = monthRequests.Select(x => x.Id).ToList();

            var sampleCount = allSamples.Count(s => requestIds.Contains(s.TestRequestId));
            var monthSampleIds = allSamples.Where(s => requestIds.Contains(s.TestRequestId)).Select(s => s.Id).ToList();
            var indicatorCount = allIndicators.Count(i => monthSampleIds.Contains(i.TestRequestSampleId));

            result.Add(new MonthlyRevenueDto
            {
                Month = month,
                MonthName = MonthNames[month - 1],
                ContractCount = monthRequests.Count,
                TotalSamples = sampleCount,
                TotalIndicators = indicatorCount,
                Revenue = monthRequests.Sum(x => x.TotalCost)
            });
        }

        return result;
    }

    public async Task<List<string>> GetCustomerNamesAsync()
    {
        var testRequestQuery = await _testRequestRepository.GetQueryableAsync();

        return testRequestQuery
            .Select(x => x.CustomerName)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
    }

    #endregion
}
