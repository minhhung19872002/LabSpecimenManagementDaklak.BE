using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Receipts;
using LabSpecimenManagement.TestRequests;
using MiniExcelLibs;
using Volo.Abp.Application.Services;
using Volo.Abp.Content;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Data;
using Volo.Abp.Identity;

namespace LabSpecimenManagement.Statistics;

public class StatisticsAppService : ApplicationService, IStatisticsAppService
{
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;
    private readonly IRepository<TestRequestSample, Guid> _sampleRepository;
    private readonly IRepository<TestRequestSampleIndicator, Guid> _sampleIndicatorRepository;
    private readonly IRepository<LaboratoryTask, Guid> _laboratoryTaskRepository;
    private readonly IRepository<LaboratoryTaskIndicator, Guid> _laboratoryTaskIndicatorRepository;
    private readonly IRepository<Laboratory, Guid> _laboratoryRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IRepository<SpecimenType, Guid> _specimenTypeRepository;
    private readonly IIdentityUserRepository _identityUserRepository;
    private readonly IIdentityRoleRepository _identityRoleRepository;
    private readonly IRepository<Receipt, Guid> _receiptRepository;
    private readonly IRepository<PaymentHistory, Guid> _paymentHistoryRepository;

    public StatisticsAppService(
        IRepository<TestRequest, Guid> testRequestRepository,
        IRepository<TestRequestSample, Guid> sampleRepository,
        IRepository<TestRequestSampleIndicator, Guid> sampleIndicatorRepository,
        IRepository<LaboratoryTask, Guid> laboratoryTaskRepository,
        IRepository<LaboratoryTaskIndicator, Guid> laboratoryTaskIndicatorRepository,
        IRepository<Laboratory, Guid> laboratoryRepository,
        IRepository<Department, Guid> departmentRepository,
        IRepository<SpecimenType, Guid> specimenTypeRepository,
        IIdentityUserRepository identityUserRepository,
        IIdentityRoleRepository identityRoleRepository,
        IRepository<Receipt, Guid> receiptRepository,
        IRepository<PaymentHistory, Guid> paymentHistoryRepository)
    {
        _testRequestRepository = testRequestRepository;
        _sampleRepository = sampleRepository;
        _sampleIndicatorRepository = sampleIndicatorRepository;
        _laboratoryTaskRepository = laboratoryTaskRepository;
        _laboratoryTaskIndicatorRepository = laboratoryTaskIndicatorRepository;
        _laboratoryRepository = laboratoryRepository;
        _departmentRepository = departmentRepository;
        _specimenTypeRepository = specimenTypeRepository;
        _identityUserRepository = identityUserRepository;
        _identityRoleRepository = identityRoleRepository;
        _receiptRepository = receiptRepository;
        _paymentHistoryRepository = paymentHistoryRepository;
    }

    public async Task<StatisticsOverviewDto> GetOverviewAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        // Default date range: last 30 days if not specified
        var from = fromDate?.Date ?? DateTime.Now.AddDays(-30).Date;
        var to = (toDate?.Date ?? DateTime.Now.Date).AddDays(1); // Include the end date

        var testRequestQuery = await _testRequestRepository.GetQueryableAsync();
        var sampleQuery = await _sampleRepository.GetQueryableAsync();
        var indicatorQuery = await _laboratoryTaskIndicatorRepository.GetQueryableAsync();

        // Filter by date range
        var filteredRequests = testRequestQuery.Where(x => x.RequestDate >= from && x.RequestDate < to);

        // Get sample IDs for filtered requests
        var filteredRequestIds = filteredRequests.Select(r => r.Id).ToList();
        var filteredSamples = sampleQuery.Where(s => filteredRequestIds.Contains(s.TestRequestId));
        var filteredSampleIds = filteredSamples.Select(s => s.Id).ToList();

        // Get indicators through sample indicators
        var sampleIndicatorQuery = await _sampleIndicatorRepository.GetQueryableAsync();
        var filteredSampleIndicatorIds = sampleIndicatorQuery
            .Where(si => filteredSampleIds.Contains(si.TestRequestSampleId))
            .Select(si => si.Id).ToList();
        var filteredIndicators = indicatorQuery.Where(i => filteredSampleIndicatorIds.Contains(i.TestRequestSampleIndicatorId));

        // Calculate counts
        var totalRequests = await AsyncExecuter.CountAsync(filteredRequests);
        var completedRequests = await AsyncExecuter.CountAsync(filteredRequests.Where(x => x.Status == TestRequestStatus.Completed));
        var totalSamples = await AsyncExecuter.CountAsync(filteredSamples);
        var totalTests = await AsyncExecuter.CountAsync(filteredIndicators);

        // Count reports (TestRequests with status Completed)
        var totalReports = completedRequests;

        // Calculate TAT compliance
        // On-time = Completed requests where CompletedDate <= ResultDeliveryDate
        var requestsWithDeadline = filteredRequests.Where(x => x.ResultDeliveryDate.HasValue && x.Status == TestRequestStatus.Completed);
        var onTimeRequests = await AsyncExecuter.CountAsync(requestsWithDeadline.Where(x =>
            x.CompletedDate.HasValue && x.CompletedDate <= x.ResultDeliveryDate));
        var totalWithDeadline = await AsyncExecuter.CountAsync(requestsWithDeadline);

        return new StatisticsOverviewDto
        {
            TotalTestRequests = totalRequests,
            TotalSpecimens = totalSamples,
            TotalTests = totalTests,
            TotalReports = totalReports,
            CompletionRate = totalRequests > 0 ? Math.Round((decimal)completedRequests / totalRequests * 100, 1) : 0,
            TATComplianceRate = totalWithDeadline > 0 ? Math.Round((decimal)onTimeRequests / totalWithDeadline * 100, 1) : 100
        };
    }

    public async Task<StatisticsDataDto> GetStatisticsDataAsync(DateTime? fromDate = null, DateTime? toDate = null)
    {
        var overview = await GetOverviewAsync(fromDate, toDate);

        // Sample Matrix - group by SpecimenType
        var sampleMatrix = await BuildSampleMatrixAsync(fromDate, toDate);

        // Workload by Laboratory (Department)
        var workloadByDepartment = await BuildWorkloadByLaboratoryAsync(fromDate, toDate);

        // TAT Compliance by period
        var tatCompliance = await BuildTATComplianceAsync();

        // Monthly trends for last 12 months
        var monthlyTrends = await BuildMonthlyTrendsAsync();

        // Lab statistics (indicators per lab + technician count)
        var labStatistics = await BuildLabStatisticsAsync(fromDate, toDate);

        // TAT status pie chart (on-time vs late indicators)
        var tatStatus = await BuildTATStatusAsync(fromDate, toDate);

        // Department work assignments (per technician indicator breakdown)
        var departmentWorkAssignments = await BuildDepartmentWorkAssignmentAsync(fromDate, toDate);

        // Regional bubble chart (geographic distribution)
        var regionalBubbles = await BuildRegionalBubblesAsync(fromDate, toDate);

        return new StatisticsDataDto
        {
            Overview = overview,
            SampleMatrix = sampleMatrix,
            TATCompliance = tatCompliance,
            WorkloadByDepartment = workloadByDepartment,
            MonthlyTrends = monthlyTrends,
            LabStatistics = labStatistics,
            TATStatus = tatStatus,
            DepartmentWorkAssignments = departmentWorkAssignments,
            RegionalBubbles = regionalBubbles
        };
    }

    private async Task<List<SampleMatrixDto>> BuildSampleMatrixAsync(DateTime? fromDate, DateTime? toDate)
    {
        var from = fromDate?.Date ?? DateTime.Now.AddDays(-30).Date;
        var to = (toDate?.Date ?? DateTime.Now.Date).AddDays(1);

        var testRequestQuery = await _testRequestRepository.GetQueryableAsync();
        var sampleQuery = await _sampleRepository.GetQueryableAsync();
        var specimenTypes = await _specimenTypeRepository.GetListAsync();
        var specimenTypeDict = specimenTypes.ToDictionary(st => st.Id, st => st.Name);

        // Get samples for filtered test requests
        var filteredRequestIds = testRequestQuery
            .Where(x => x.RequestDate >= from && x.RequestDate < to)
            .Select(r => r.Id).ToList();

        var filteredSamples = await AsyncExecuter.ToListAsync(
            sampleQuery.Where(s => filteredRequestIds.Contains(s.TestRequestId)));

        // Group by SpecimenType
        var grouped = filteredSamples
            .GroupBy(s => s.SpecimenTypeId)
            .Select(g => new SampleMatrixDto
            {
                SpecimenType = g.Key.HasValue && specimenTypeDict.ContainsKey(g.Key.Value)
                    ? specimenTypeDict[g.Key.Value]
                    : "Không phân loại",
                TotalCount = g.Count(),
                ReceivedCount = g.Count(s => s.ProcessingStatus == "Đã nhận" || !string.IsNullOrEmpty(s.ProcessingStatus)),
                TestingCount = g.Count(s => s.ProcessingStatus == "Đang thử nghiệm"),
                CompletedCount = g.Count(s => s.ProcessingStatus == "Hoàn thành" || s.ProcessingStatus == "Trả kết quả")
            })
            .OrderByDescending(x => x.TotalCount)
            .ToList();

        return grouped;
    }

    private async Task<List<WorkloadByDepartmentDto>> BuildWorkloadByLaboratoryAsync(DateTime? fromDate, DateTime? toDate)
    {
        var from = fromDate?.Date ?? DateTime.Now.AddDays(-30).Date;
        var to = (toDate?.Date ?? DateTime.Now.Date).AddDays(1);

        var laboratoryTaskQuery = await _laboratoryTaskRepository.GetQueryableAsync();
        var laboratories = await _laboratoryRepository.GetListAsync();
        var departments = await _departmentRepository.GetListAsync();
        var deptDict = departments.ToDictionary(d => d.Id, d => d.Name);
        var labDict = laboratories.ToDictionary(l => l.Id, l => deptDict.TryGetValue(l.DepartmentId, out var name) ? name : "");

        // Get tasks within date range (based on CreationTime or TransferDate)
        var filteredTasks = await AsyncExecuter.ToListAsync(
            laboratoryTaskQuery.Where(t => t.CreationTime >= from && t.CreationTime < to));

        // Group by Laboratory
        var grouped = filteredTasks
            .GroupBy(t => t.LaboratoryId)
            .Select(g => new WorkloadByDepartmentDto
            {
                Department = labDict.ContainsKey(g.Key) ? labDict[g.Key] : "Không xác định",
                TotalTasks = g.Count(),
                PendingTasks = g.Count(t => t.Status == LaboratoryTaskStatus.UnderEvaluation || t.Status == LaboratoryTaskStatus.Assigned),
                InProgressTasks = g.Count(t => t.Status == LaboratoryTaskStatus.InProgress || t.Status == LaboratoryTaskStatus.WaitingApproval),
                CompletedTasks = g.Count(t => t.Status == LaboratoryTaskStatus.Approved || t.Status == LaboratoryTaskStatus.SentToQuality),
                CompletionRate = g.Count() > 0
                    ? Math.Round((decimal)g.Count(t => t.Status == LaboratoryTaskStatus.Approved || t.Status == LaboratoryTaskStatus.SentToQuality) / g.Count() * 100, 1)
                    : 0
            })
            .OrderByDescending(x => x.TotalTasks)
            .ToList();

        return grouped;
    }

    private async Task<List<TATComplianceDto>> BuildTATComplianceAsync()
    {
        var result = new List<TATComplianceDto>();
        var testRequestQuery = await _testRequestRepository.GetQueryableAsync();

        // Calculate for last 4 weeks
        for (int i = 0; i < 4; i++)
        {
            var weekEnd = DateTime.Now.Date.AddDays(-7 * i);
            var weekStart = weekEnd.AddDays(-7);

            var weekRequests = await AsyncExecuter.ToListAsync(
                testRequestQuery.Where(x => x.RequestDate >= weekStart && x.RequestDate < weekEnd));

            var totalRequests = weekRequests.Count;
            var requestsWithDeadline = weekRequests
                .Where(x => x.ResultDeliveryDate.HasValue && x.Status == TestRequestStatus.Completed)
                .ToList();

            var onTimeRequests = requestsWithDeadline
                .Count(x => x.CompletedDate.HasValue && x.CompletedDate <= x.ResultDeliveryDate);

            var delayedRequests = requestsWithDeadline
                .Count(x => x.CompletedDate.HasValue && x.CompletedDate > x.ResultDeliveryDate);

            result.Add(new TATComplianceDto
            {
                Period = $"Tuần {4 - i} ({weekStart:dd/MM} - {weekEnd.AddDays(-1):dd/MM})",
                TotalRequests = totalRequests,
                OnTimeRequests = onTimeRequests,
                DelayedRequests = delayedRequests,
                ComplianceRate = requestsWithDeadline.Count > 0
                    ? Math.Round((decimal)onTimeRequests / requestsWithDeadline.Count * 100, 1)
                    : 100
            });
        }

        // Add current month summary at the beginning
        var monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var monthRequests = await AsyncExecuter.ToListAsync(
            testRequestQuery.Where(x => x.RequestDate >= monthStart));

        var monthWithDeadline = monthRequests
            .Where(x => x.ResultDeliveryDate.HasValue && x.Status == TestRequestStatus.Completed)
            .ToList();

        var monthOnTime = monthWithDeadline
            .Count(x => x.CompletedDate.HasValue && x.CompletedDate <= x.ResultDeliveryDate);

        result.Insert(0, new TATComplianceDto
        {
            Period = $"Tháng {DateTime.Now.Month}/{DateTime.Now.Year}",
            TotalRequests = monthRequests.Count,
            OnTimeRequests = monthOnTime,
            DelayedRequests = monthWithDeadline.Count(x =>
                x.CompletedDate.HasValue && x.CompletedDate > x.ResultDeliveryDate),
            ComplianceRate = monthWithDeadline.Count > 0
                ? Math.Round((decimal)monthOnTime / monthWithDeadline.Count * 100, 1)
                : 100
        });

        return result;
    }

    private async Task<List<MonthlyTrendDto>> BuildMonthlyTrendsAsync()
    {
        var result = new List<MonthlyTrendDto>();
        var testRequestQuery = await _testRequestRepository.GetQueryableAsync();
        var sampleQuery = await _sampleRepository.GetQueryableAsync();
        var indicatorQuery = await _laboratoryTaskIndicatorRepository.GetQueryableAsync();
        var sampleIndicatorQuery = await _sampleIndicatorRepository.GetQueryableAsync();

        // Last 12 months
        for (int i = 11; i >= 0; i--)
        {
            var date = DateTime.Now.AddMonths(-i);
            var monthStart = new DateTime(date.Year, date.Month, 1);
            var monthEnd = monthStart.AddMonths(1);

            // Count test requests
            var requestCount = await AsyncExecuter.CountAsync(
                testRequestQuery.Where(x => x.RequestDate >= monthStart && x.RequestDate < monthEnd));

            // Count samples
            var requestIds = testRequestQuery
                .Where(x => x.RequestDate >= monthStart && x.RequestDate < monthEnd)
                .Select(r => r.Id).ToList();
            var sampleCount = await AsyncExecuter.CountAsync(
                sampleQuery.Where(s => requestIds.Contains(s.TestRequestId)));

            // Count tests (indicators with results)
            var sampleIds = sampleQuery
                .Where(s => requestIds.Contains(s.TestRequestId))
                .Select(s => s.Id).ToList();
            var sampleIndicatorIds = sampleIndicatorQuery
                .Where(si => sampleIds.Contains(si.TestRequestSampleId))
                .Select(si => si.Id).ToList();
            var testCount = await AsyncExecuter.CountAsync(
                indicatorQuery.Where(ind => sampleIndicatorIds.Contains(ind.TestRequestSampleIndicatorId) && ind.Result != null));

            // Count reports (completed test requests)
            var reportCount = await AsyncExecuter.CountAsync(
                testRequestQuery.Where(x => x.RequestDate >= monthStart && x.RequestDate < monthEnd && x.Status == TestRequestStatus.Completed));

            result.Add(new MonthlyTrendDto
            {
                Month = $"T{date.Month}/{date.Year}",
                TestRequests = requestCount,
                Specimens = sampleCount,
                Tests = testCount,
                Reports = reportCount
            });
        }

        return result;
    }

    private async Task<TATStatusDto> BuildTATStatusAsync(DateTime? fromDate, DateTime? toDate)
    {
        var dateFrom = fromDate?.Date ?? DateTime.Now.AddDays(-30).Date;
        var dateTo = (toDate?.Date ?? DateTime.Now.Date).AddDays(1);

        var taskQuery = await _laboratoryTaskRepository.GetQueryableAsync();
        var indicatorQuery = await _laboratoryTaskIndicatorRepository.GetQueryableAsync();

        var query = from ti in indicatorQuery
                    join t in taskQuery on ti.LaboratoryTaskId equals t.Id
                    where t.CreationTime >= dateFrom && t.CreationTime < dateTo
                          && ti.IsSentToQuality
                          && ti.ApprovalDate != null
                          && t.RequestCompletionDate != null
                    select new { ti.ApprovalDate, t.RequestCompletionDate };

        var data = await AsyncExecuter.ToListAsync(query);

        return new TATStatusDto
        {
            OnTimeCount = data.Count(x => x.ApprovalDate <= x.RequestCompletionDate),
            LateCount = data.Count(x => x.ApprovalDate > x.RequestCompletionDate),
        };
    }

    private async Task<List<LabStatisticsDto>> BuildLabStatisticsAsync(DateTime? fromDate, DateTime? toDate)
    {
        var dateFrom = fromDate?.Date ?? DateTime.Now.AddDays(-30).Date;
        var dateTo = (toDate?.Date ?? DateTime.Now.Date).AddDays(1);

        var labs = await _laboratoryRepository.GetListAsync();
        var departments = await _departmentRepository.GetListAsync();
        var deptDict = departments.ToDictionary(d => d.Id, d => d.Name);

        var taskQuery = await _laboratoryTaskRepository.GetQueryableAsync();
        var indicatorQuery = await _laboratoryTaskIndicatorRepository.GetQueryableAsync();

        var allLabIds = labs.Select(l => l.Id).ToList();
        var labIndicators = from ti in indicatorQuery
                            join t in taskQuery on ti.LaboratoryTaskId equals t.Id
                            where t.CreationTime >= dateFrom && t.CreationTime < dateTo
                                  && allLabIds.Contains(t.LaboratoryId)
                            select new { ti.ApprovalStatus, ti.TestDate, t.LaboratoryId };

        var filtered = await AsyncExecuter.ToListAsync(labIndicators);

        // Technician count per department
        var techRole = (await _identityRoleRepository.GetListAsync())
            .FirstOrDefault(r => r.Name == "LabTechnician");
        var techCountByDept = new Dictionary<Guid, int>();
        if (techRole != null)
        {
            var technicians = await _identityUserRepository.GetListAsync(roleId: techRole.Id);
            techCountByDept = technicians
                .GroupBy(u => u.GetProperty<Guid?>("DepartmentId"))
                .Where(g => g.Key.HasValue)
                .ToDictionary(g => g.Key!.Value, g => g.Count());
        }

        return labs.Select(lab =>
        {
            var labInds = filtered.Where(x => x.LaboratoryId == lab.Id).ToList();
            return new LabStatisticsDto
            {
                LabId = lab.Id,
                LabName = deptDict.TryGetValue(lab.DepartmentId, out var name) ? name : lab.Code,
                // Chưa hoàn thành = tất cả trừ Approved
                InProgressCount = labInds.Count(x =>
                    x.ApprovalStatus != IndicatorApprovalStatus.Approved),
                // Tổng số chỉ tiêu
                PendingCount = labInds.Count,
                TechnicianCount = techCountByDept.TryGetValue(lab.DepartmentId, out var count) ? count : 0,
            };
        })
        .Where(dto => dto.InProgressCount > 0 || dto.PendingCount > 0 || dto.TechnicianCount > 0)
        .ToList();
    }

    private async Task<List<DepartmentWorkAssignmentDto>> BuildDepartmentWorkAssignmentAsync(DateTime? fromDate, DateTime? toDate)
    {
        var dateFrom = fromDate?.Date ?? DateTime.Now.AddDays(-30).Date;
        var dateTo = (toDate?.Date ?? DateTime.Now.Date).AddDays(1);

        var labs = await _laboratoryRepository.GetListAsync();
        var departments = await _departmentRepository.GetListAsync();
        var deptDict = departments.ToDictionary(d => d.Id, d => d.Name);

        var taskQuery = await _laboratoryTaskRepository.GetQueryableAsync();
        var indicatorQuery = await _laboratoryTaskIndicatorRepository.GetQueryableAsync();

        var allLabIds = labs.Select(l => l.Id).ToList();
        var indicators = await AsyncExecuter.ToListAsync(
            from ti in indicatorQuery
            join t in taskQuery on ti.LaboratoryTaskId equals t.Id
            where t.CreationTime >= dateFrom && t.CreationTime < dateTo
                  && allLabIds.Contains(t.LaboratoryId)
            select new
            {
                t.LaboratoryId,
                ti.TechnicianId,
                ti.ApprovalStatus,
                ti.TestDate
            });

        var techIds = indicators
            .Where(x => x.TechnicianId.HasValue)
            .Select(x => x.TechnicianId!.Value)
            .Distinct()
            .ToList();

        var techUsers = new Dictionary<Guid, string>();
        if (techIds.Any())
        {
            var users = await _identityUserRepository.GetListAsync();
            techUsers = users
                .Where(u => techIds.Contains(u.Id))
                .ToDictionary(u => u.Id, u => u.Name ?? u.UserName);
        }

        return labs.Select(lab =>
        {
            var labInds = indicators.Where(x => x.LaboratoryId == lab.Id).ToList();

            var grouped = labInds
                .GroupBy(x => x.TechnicianId ?? Guid.Empty)
                .Select(g =>
                {
                    var techName = g.Key == Guid.Empty
                        ? "Chưa phân công"
                        : techUsers.TryGetValue(g.Key, out var name) ? name : "Không xác định";

                    var items = g.ToList();
                    return new TechnicianWorkDto
                    {
                        TechnicianId = g.Key,
                        TechnicianName = techName,
                        TestingCount = items.Count(x =>
                            (x.ApprovalStatus == IndicatorApprovalStatus.AcceptedForTesting && x.TestDate != null)),
                        NotTestedCount = items.Count(x =>
                            x.ApprovalStatus == IndicatorApprovalStatus.Pending
                            || x.ApprovalStatus == IndicatorApprovalStatus.MethodChanged
                            || (x.ApprovalStatus == IndicatorApprovalStatus.AcceptedForTesting && x.TestDate == null)),
                        WaitingApprovalCount = items.Count(x =>
                            x.ApprovalStatus == IndicatorApprovalStatus.Entered),
                        RejectedCount = items.Count(x =>
                            x.ApprovalStatus == IndicatorApprovalStatus.Rejected),
                        ApprovedCount = items.Count(x =>
                            x.ApprovalStatus == IndicatorApprovalStatus.Approved),
                        TotalCount = items.Count
                    };
                })
                .OrderByDescending(x => x.TotalCount)
                .ToList();

            return new DepartmentWorkAssignmentDto
            {
                LabId = lab.Id,
                LabName = deptDict.TryGetValue(lab.DepartmentId, out var dName) ? dName : lab.Code,
                TotalIndicators = labInds.Count,
                Technicians = grouped
            };
        })
        .ToList();
    }

    private async Task<List<RegionalBubbleDto>> BuildRegionalBubblesAsync(DateTime? fromDate, DateTime? toDate)
    {
        var dateFrom = fromDate?.Date ?? DateTime.Now.AddDays(-30).Date;
        var dateTo = (toDate?.Date ?? DateTime.Now.Date).AddDays(1);

        var testRequestQuery = await _testRequestRepository.GetQueryableAsync();
        var filteredRequests = await AsyncExecuter.ToListAsync(
            testRequestQuery.Where(r => r.RequestDate >= dateFrom && r.RequestDate < dateTo
                                        && r.Ward != null && r.Ward != ""));

        if (!filteredRequests.Any()) return new List<RegionalBubbleDto>();

        var requestIds = filteredRequests.Select(r => r.Id).ToList();

        // Samples per request
        var sampleQuery = await _sampleRepository.GetQueryableAsync();
        var samples = await AsyncExecuter.ToListAsync(
            sampleQuery.Where(s => requestIds.Contains(s.TestRequestId)));

        var sampleCountByRequest = samples
            .GroupBy(s => s.TestRequestId)
            .ToDictionary(g => g.Key, g => g.Count());

        // Indicators per request (through samples)
        var sampleIds = samples.Select(s => s.Id).ToList();
        var indicatorQuery = await _sampleIndicatorRepository.GetQueryableAsync();
        var indicatorCounts = await AsyncExecuter.ToListAsync(
            indicatorQuery
                .Where(i => sampleIds.Contains(i.TestRequestSampleId))
                .GroupBy(i => i.TestRequestSampleId)
                .Select(g => new { SampleId = g.Key, Count = g.Count() }));

        var indicatorCountBySample = indicatorCounts.ToDictionary(x => x.SampleId, x => x.Count);
        var indicatorCountByRequest = samples
            .GroupBy(s => s.TestRequestId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(s => indicatorCountBySample.TryGetValue(s.Id, out var c) ? c : 0));

        // Revenue per request (Receipt → PaymentHistory)
        var receiptQuery = await _receiptRepository.GetQueryableAsync();
        var receipts = await AsyncExecuter.ToListAsync(
            receiptQuery.Where(r => requestIds.Contains(r.TestRequestId)));
        var receiptIds = receipts.Select(r => r.Id).ToList();
        var receiptToRequest = receipts.ToDictionary(r => r.Id, r => r.TestRequestId);

        var paymentQuery = await _paymentHistoryRepository.GetQueryableAsync();
        var payments = await AsyncExecuter.ToListAsync(
            paymentQuery.Where(p => receiptIds.Contains(p.ReceiptId)));

        var revenueByRequest = payments
            .GroupBy(p => receiptToRequest.TryGetValue(p.ReceiptId, out var rid) ? rid : Guid.Empty)
            .Where(g => g.Key != Guid.Empty)
            .ToDictionary(g => g.Key, g => g.Sum(p => p.Amount));

        // Group by Ward
        return filteredRequests
            .GroupBy(r => r.Ward!)
            .Select(g =>
            {
                var ids = g.Select(r => r.Id).ToList();
                return new RegionalBubbleDto
                {
                    Ward = g.Key,
                    TotalRevenue = Math.Round(ids.Sum(id => revenueByRequest.TryGetValue(id, out var rev) ? rev : 0) / 1_000_000m, 1),
                    TotalSamples = ids.Sum(id => sampleCountByRequest.TryGetValue(id, out var sc) ? sc : 0),
                    TotalIndicators = ids.Sum(id => indicatorCountByRequest.TryGetValue(id, out var ic) ? ic : 0),
                };
            })
            .Where(x => x.TotalSamples > 0 || x.TotalRevenue > 0)
            .OrderByDescending(x => x.TotalRevenue)
            .ToList();
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
                new { Chỉ_số = "Tổng yêu cầu thử nghiệm", Giá_trị = data.Overview.TotalTestRequests.ToString() },
                new { Chỉ_số = "Tổng mẫu thử", Giá_trị = data.Overview.TotalSpecimens.ToString() },
                new { Chỉ_số = "Tổng phép thử", Giá_trị = data.Overview.TotalTests.ToString() },
                new { Chỉ_số = "Tổng báo cáo", Giá_trị = data.Overview.TotalReports.ToString() },
                new { Chỉ_số = "Tỷ lệ hoàn thành (%)", Giá_trị = data.Overview.CompletionRate.ToString("F1") },
                new { Chỉ_số = "Tỷ lệ đúng hạn TAT (%)", Giá_trị = data.Overview.TATComplianceRate.ToString("F1") }
            },
            ["Theo tháng"] = data.MonthlyTrends.Select(m => new
            {
                Tháng = m.Month,
                Yêu_cầu = m.TestRequests,
                Mẫu = m.Specimens,
                Phép_thử = m.Tests,
                Báo_cáo = m.Reports
            }).ToList(),
            ["Theo loại mẫu"] = data.SampleMatrix.Select(s => new
            {
                Loại_mẫu = s.SpecimenType,
                Tổng = s.TotalCount,
                Đã_nhận = s.ReceivedCount,
                Đang_thử = s.TestingCount,
                Hoàn_thành = s.CompletedCount
            }).ToList(),
            ["Theo phòng ban"] = data.WorkloadByDepartment.Select(w => new
            {
                Phòng_ban = w.Department,
                Tổng_công_việc = w.TotalTasks,
                Chờ_xử_lý = w.PendingTasks,
                Đang_thực_hiện = w.InProgressTasks,
                Hoàn_thành = w.CompletedTasks,
                Hiệu_suất = $"{w.CompletionRate:F1}%"
            }).ToList(),
            ["TAT Compliance"] = data.TATCompliance.Select(t => new
            {
                Kỳ = t.Period,
                Tổng_yêu_cầu = t.TotalRequests,
                Đúng_hạn = t.OnTimeRequests,
                Trễ_hạn = t.DelayedRequests,
                Tỷ_lệ = $"{t.ComplianceRate:F1}%"
            }).ToList()
        };

        await memoryStream.SaveAsAsync(sheets);
        memoryStream.Position = 0;

        var fileName = fromDate.HasValue && toDate.HasValue
            ? $"ThongKe_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx"
            : $"ThongKe_{DateTime.Now:yyyyMMdd}.xlsx";

        return new RemoteStreamContent(memoryStream, fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }
}
