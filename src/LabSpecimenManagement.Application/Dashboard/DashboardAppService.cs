using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.NCRs;
using LabSpecimenManagement.Reports;
using LabSpecimenManagement.Tasks;
using LabSpecimenManagement.TestRequests;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

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

    public DashboardAppService(
        IRepository<TestRequest, Guid> testRequestRepository,
        IRepository<Specimen, Guid> specimenRepository,
        IRepository<Test, Guid> testRepository,
        IRepository<TestResult, Guid> testResultRepository,
        IRepository<NCR, Guid> ncrRepository,
        IRepository<Report, Guid> reportRepository,
        IRepository<TaskAssignment, Guid> taskRepository)
    {
        _testRequestRepository = testRequestRepository;
        _specimenRepository = specimenRepository;
        _testRepository = testRepository;
        _testResultRepository = testResultRepository;
        _ncrRepository = ncrRepository;
        _reportRepository = reportRepository;
        _taskRepository = taskRepository;
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
            PendingRequests = testRequests.Count(x => x.Status == TestRequestStatus.Pending),
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
            PendingTestRequests = testRequests.Count(x => x.Status == TestRequestStatus.Pending),
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
}
