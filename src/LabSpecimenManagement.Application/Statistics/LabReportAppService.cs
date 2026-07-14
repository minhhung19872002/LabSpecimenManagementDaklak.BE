using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.TestRequests;
using MiniExcelLibs;
using Volo.Abp.Application.Services;
using Volo.Abp.Content;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.Statistics;

public class LabReportAppService : ApplicationService, ILabReportAppService
{
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;
    private readonly IRepository<TestRequestSample, Guid> _sampleRepository;
    private readonly IRepository<TestRequestSampleIndicator, Guid> _sampleIndicatorRepository;
    private readonly IRepository<LaboratoryTask, Guid> _laboratoryTaskRepository;
    private readonly IRepository<LaboratoryTaskIndicator, Guid> _laboratoryTaskIndicatorRepository;
    private readonly IRepository<Indicator, Guid> _indicatorRepository;

    public LabReportAppService(
        IRepository<TestRequest, Guid> testRequestRepository,
        IRepository<TestRequestSample, Guid> sampleRepository,
        IRepository<TestRequestSampleIndicator, Guid> sampleIndicatorRepository,
        IRepository<LaboratoryTask, Guid> laboratoryTaskRepository,
        IRepository<LaboratoryTaskIndicator, Guid> laboratoryTaskIndicatorRepository,
        IRepository<Indicator, Guid> indicatorRepository)
    {
        _testRequestRepository = testRequestRepository;
        _sampleRepository = sampleRepository;
        _sampleIndicatorRepository = sampleIndicatorRepository;
        _laboratoryTaskRepository = laboratoryTaskRepository;
        _laboratoryTaskIndicatorRepository = laboratoryTaskIndicatorRepository;
        _indicatorRepository = indicatorRepository;
    }

    public async Task<LabReportDataDto> GetLabReportDataAsync(Guid labId, DateTime fromDate, DateTime toDate)
    {
        var toDateExclusive = toDate.Date.AddDays(1);

        var trQuery = await _testRequestRepository.GetQueryableAsync();
        var trsQuery = await _sampleRepository.GetQueryableAsync();
        var ltiQuery = await _laboratoryTaskIndicatorRepository.GetQueryableAsync();
        var ltQuery = await _laboratoryTaskRepository.GetQueryableAsync();
        var trsiQuery = await _sampleIndicatorRepository.GetQueryableAsync();

        // Tất cả mẫu trong khoảng ngày (không lọc theo lab) để tính Số mẫu hàng ngày
        var allSampleQuery = from trs in trsQuery
                             join tr in trQuery on trs.TestRequestId equals tr.Id
                             where tr.CreationTime >= fromDate && tr.CreationTime < toDateExclusive
                             select new { tr.CreationTime, SampleId = trs.Id };

        var allSampleData = await AsyncExecuter.ToListAsync(allSampleQuery);
        var sampleCountByDay = allSampleData
            .GroupBy(x => x.CreationTime.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        // Dữ liệu lab-specific: chỉ tiêu không bị từ chối, theo lab
        var labQuery = from lti in ltiQuery
                       join lt in ltQuery on lti.LaboratoryTaskId equals lt.Id
                       join trsi in trsiQuery on lti.TestRequestSampleIndicatorId equals trsi.Id
                       join trs in trsQuery on trsi.TestRequestSampleId equals trs.Id
                       join tr in trQuery on trs.TestRequestId equals tr.Id
                       where lt.LaboratoryId == labId
                           && tr.CreationTime >= fromDate && tr.CreationTime < toDateExclusive
                           && lti.ApprovalStatus != IndicatorApprovalStatus.Rejected
                       select new
                       {
                           LtiId = lti.Id,
                           TrsiUnitPrice = trsi.UnitPrice,
                           TrsSampleId = trs.Id,
                           TrsSampleCode = trs.SampleCode,
                           TrId = tr.Id,
                           TrRequestNumber = tr.RequestNumber,
                           TrContractNumber = tr.ContractNumber,
                           TrContractDate = tr.ContractDate,
                           TrCreationTime = tr.CreationTime
                       };

        var labData = await AsyncExecuter.ToListAsync(labQuery);

        // Build DailyStats
        var revenueByDay = labData
            .GroupBy(x => x.TrCreationTime.Date)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.TrsiUnitPrice));

        var allDates = sampleCountByDay.Keys.Union(revenueByDay.Keys).OrderBy(d => d).ToList();
        var dailyStats = allDates.Select(d => new LabDailyStatDto
        {
            Date = d.ToString("yyyy-MM-dd"),
            SampleCount = sampleCountByDay.TryGetValue(d, out var sc) ? sc : 0,
            Revenue = revenueByDay.TryGetValue(d, out var rev) ? rev : 0
        }).ToList();

        // Build ContractIndicators (Chart 2) — sắp xếp theo ContractDate tăng dần
        var contractIndicators = labData
            .GroupBy(x => new
            {
                x.TrId,
                x.TrRequestNumber,
                x.TrContractNumber,
                x.TrContractDate,
                TrCreationTime = x.TrCreationTime.Date,
                Month = x.TrCreationTime.Month,
                Year = x.TrCreationTime.Year
            })
            .Select(g => new ContractMonthlyIndicatorDto
            {
                TestRequestId = g.Key.TrId,
                RequestNumber = g.Key.TrRequestNumber,
                ContractNumber = g.Key.TrContractNumber,
                ContractDate = g.Key.TrContractDate,
                CreatedAt = g.Key.TrCreationTime,
                Month = g.Key.Month,
                Year = g.Key.Year,
                IndicatorCount = g.Count()
            })
            .OrderBy(x => x.CreatedAt)
            .ThenBy(x => x.RequestNumber)
            .ToList();

        // Build Contracts (Section 3 left panel)
        var contracts = labData
            .GroupBy(x => new { x.TrId, x.TrRequestNumber, x.TrContractNumber, x.TrContractDate })
            .Select(g => new LabContractSummaryDto
            {
                TestRequestId = g.Key.TrId,
                RequestNumber = g.Key.TrRequestNumber,
                ContractNumber = g.Key.TrContractNumber,
                ContractDate = g.Key.TrContractDate,
                SampleCount = g.Select(x => x.TrsSampleId).Distinct().Count(),
                IndicatorCount = g.Count(),
                TotalPrice = g.Sum(x => x.TrsiUnitPrice)
            })
            .OrderBy(x => x.ContractDate)
            .ToList();

        // Build IndicatorContractCounts (Chart 3: số HĐ theo chỉ tiêu / tháng)
        var indQuery = await _indicatorRepository.GetQueryableAsync();

        var indicatorContractQuery = from lti in ltiQuery
                                     join lt in ltQuery on lti.LaboratoryTaskId equals lt.Id
                                     join trsi in trsiQuery on lti.TestRequestSampleIndicatorId equals trsi.Id
                                     join trs in trsQuery on trsi.TestRequestSampleId equals trs.Id
                                     join tr in trQuery on trs.TestRequestId equals tr.Id
                                     join ind in indQuery on trsi.IndicatorId equals ind.Id
                                     where lt.LaboratoryId == labId
                                         && tr.CreationTime >= fromDate && tr.CreationTime < toDateExclusive
                                         && lti.ApprovalStatus != IndicatorApprovalStatus.Rejected
                                     select new
                                     {
                                         IndicatorName = ind.Name,
                                         Month = tr.CreationTime.Month,
                                         Year = tr.CreationTime.Year,
                                         TrId = tr.Id
                                     };

        var indicatorContractRaw = await AsyncExecuter.ToListAsync(indicatorContractQuery);

        var indicatorContractCounts = indicatorContractRaw
            .GroupBy(x => new { x.IndicatorName, x.Month, x.Year })
            .Select(g => new IndicatorContractCountDto
            {
                IndicatorName = g.Key.IndicatorName,
                Month = g.Key.Month,
                Year = g.Key.Year,
                ContractCount = g.Select(x => x.TrId).Distinct().Count()
            })
            .OrderBy(x => x.IndicatorName)
            .ToList();

        // Kết quả thử nghiệm: Đạt (Approved) / Không đạt (Rejected)
        var testResultQuery = from lti in ltiQuery
                              join lt in ltQuery on lti.LaboratoryTaskId equals lt.Id
                              join trsi in trsiQuery on lti.TestRequestSampleIndicatorId equals trsi.Id
                              join trs in trsQuery on trsi.TestRequestSampleId equals trs.Id
                              join tr in trQuery on trs.TestRequestId equals tr.Id
                              where lt.LaboratoryId == labId
                                  && tr.CreationTime >= fromDate && tr.CreationTime < toDateExclusive
                                  && (lti.ApprovalStatus == IndicatorApprovalStatus.Approved
                                      || lti.ApprovalStatus == IndicatorApprovalStatus.Rejected)
                              select lti.ApprovalStatus;

        var testResultStatuses = await AsyncExecuter.ToListAsync(testResultQuery);
        var passCount = testResultStatuses.Count(s => s == IndicatorApprovalStatus.Approved);
        var failCount = testResultStatuses.Count(s => s == IndicatorApprovalStatus.Rejected);

        return new LabReportDataDto
        {
            DailyStats = dailyStats,
            ContractIndicators = contractIndicators,
            Contracts = contracts,
            IndicatorContractCounts = indicatorContractCounts,
            PassCount = passCount,
            FailCount = failCount
        };
    }

    public async Task<LabContractDetailDto> GetLabContractDetailAsync(Guid testRequestId, Guid labId)
    {
        var trQuery = await _testRequestRepository.GetQueryableAsync();
        var trsQuery = await _sampleRepository.GetQueryableAsync();
        var ltiQuery = await _laboratoryTaskIndicatorRepository.GetQueryableAsync();
        var ltQuery = await _laboratoryTaskRepository.GetQueryableAsync();
        var trsiQuery = await _sampleIndicatorRepository.GetQueryableAsync();

        var query = from lti in ltiQuery
                    join lt in ltQuery on lti.LaboratoryTaskId equals lt.Id
                    join trsi in trsiQuery on lti.TestRequestSampleIndicatorId equals trsi.Id
                    join trs in trsQuery on trsi.TestRequestSampleId equals trs.Id
                    join tr in trQuery on trs.TestRequestId equals tr.Id
                    where lt.LaboratoryId == labId
                        && tr.Id == testRequestId
                        && lti.ApprovalStatus != IndicatorApprovalStatus.Rejected
                    select new
                    {
                        tr.RequestNumber,
                        tr.ContractNumber,
                        tr.ContractDate,
                        TrsSampleId = trs.Id,
                        TrsSampleCode = trs.SampleCode,
                        TrsiUnitPrice = trsi.UnitPrice
                    };

        var data = await AsyncExecuter.ToListAsync(query);

        if (!data.Any())
            return new LabContractDetailDto();

        var first = data.First();
        var samples = data
            .GroupBy(x => x.TrsSampleCode)
            .Select(g => new SamplePriceItemDto
            {
                SampleCode = g.Key,
                Price = g.Sum(x => x.TrsiUnitPrice)
            })
            .OrderByDescending(x => x.Price)
            .ToList();

        return new LabContractDetailDto
        {
            RequestNumber = first.RequestNumber,
            ContractNumber = first.ContractNumber,
            ContractDate = first.ContractDate,
            TotalSamples = data.Select(x => x.TrsSampleId).Distinct().Count(),
            TotalPrice = data.Sum(x => x.TrsiUnitPrice),
            Samples = samples
        };
    }

    public async Task<IRemoteStreamContent> ExportLabReportAsync(Guid labId, DateTime fromDate, DateTime toDate)
    {
        var toDateExclusive = toDate.Date.AddDays(1);

        var trQuery = await _testRequestRepository.GetQueryableAsync();
        var trsQuery = await _sampleRepository.GetQueryableAsync();
        var ltiQuery = await _laboratoryTaskIndicatorRepository.GetQueryableAsync();
        var ltQuery = await _laboratoryTaskRepository.GetQueryableAsync();
        var trsiQuery = await _sampleIndicatorRepository.GetQueryableAsync();

        var labQuery = from lti in ltiQuery
                       join lt in ltQuery on lti.LaboratoryTaskId equals lt.Id
                       join trsi in trsiQuery on lti.TestRequestSampleIndicatorId equals trsi.Id
                       join trs in trsQuery on trsi.TestRequestSampleId equals trs.Id
                       join tr in trQuery on trs.TestRequestId equals tr.Id
                       where lt.LaboratoryId == labId
                           && tr.CreationTime >= fromDate && tr.CreationTime < toDateExclusive
                           && lti.ApprovalStatus != IndicatorApprovalStatus.Rejected
                       select new
                       {
                           TrsiUnitPrice = trsi.UnitPrice,
                           TrsSampleId = trs.Id,
                           TrsSampleCode = trs.SampleCode,
                           TrRequestNumber = tr.RequestNumber,
                           TrContractNumber = tr.ContractNumber,
                           TrContractDate = tr.ContractDate,
                           TrCreationTime = tr.CreationTime
                       };

        var labData = await AsyncExecuter.ToListAsync(labQuery);

        // Sheet 1: Doanh thu theo ngày (lab-filtered)
        var dailyStats = labData
            .GroupBy(x => x.TrCreationTime.Date)
            .Select(g => new
            {
                Ngày = g.Key.ToString("yyyy-MM-dd"),
                Số_mẫu = g.Select(x => x.TrsSampleId).Distinct().Count(),
                Doanh_thu_VND = g.Sum(x => x.TrsiUnitPrice)
            })
            .OrderBy(x => x.Ngày)
            .ToList();

        // Sheet 2: Chỉ tiêu theo hợp đồng / tháng
        var contractIndicators = labData
            .GroupBy(x => new
            {
                x.TrRequestNumber,
                x.TrContractNumber,
                x.TrContractDate,
                Month = x.TrCreationTime.Month,
                Year = x.TrCreationTime.Year
            })
            .Select(g => new
            {
                Số_HĐ = g.Key.TrContractNumber ?? g.Key.TrRequestNumber,
                Ngày_HĐ = g.Key.TrContractDate.HasValue ? g.Key.TrContractDate.Value.ToString("dd/MM/yyyy") : "",
                Tháng = g.Key.Month,
                Năm = g.Key.Year,
                Số_chỉ_tiêu = g.Count()
            })
            .OrderBy(x => x.Năm).ThenBy(x => x.Tháng).ThenBy(x => x.Số_HĐ)
            .ToList();

        // Sheet 3: Danh sách hợp đồng — sort by raw DateTime before projecting to string
        var contracts = labData
            .GroupBy(x => new { x.TrRequestNumber, x.TrContractNumber, x.TrContractDate })
            .OrderBy(g => g.Key.TrContractDate).ThenBy(g => g.Key.TrRequestNumber)
            .Select(g => new
            {
                Số_HĐ = g.Key.TrContractNumber ?? g.Key.TrRequestNumber,
                Ngày_HĐ = g.Key.TrContractDate.HasValue ? g.Key.TrContractDate.Value.ToString("dd/MM/yyyy") : "",
                Số_mẫu = g.Select(x => x.TrsSampleId).Distinct().Count(),
                Số_chỉ_tiêu = g.Count(),
                Tổng_tiền_VND = g.Sum(x => x.TrsiUnitPrice)
            })
            .ToList();

        // Sheet 4: Chi tiết đơn giá từng mẫu
        var sampleDetails = labData
            .GroupBy(x => new { x.TrRequestNumber, x.TrContractNumber, x.TrsSampleCode })
            .Select(g => new
            {
                Số_HĐ = g.Key.TrContractNumber ?? g.Key.TrRequestNumber,
                Mã_mẫu = g.Key.TrsSampleCode,
                Đơn_giá_VND = g.Sum(x => x.TrsiUnitPrice)
            })
            .OrderBy(x => x.Số_HĐ).ThenBy(x => x.Mã_mẫu)
            .ToList();

        var memoryStream = new MemoryStream();
        var sheets = new Dictionary<string, object>
        {
            ["Doanh thu theo ngày"] = dailyStats,
            ["Chỉ tiêu theo HĐ"] = contractIndicators,
            ["Danh sách HĐ"] = contracts,
            ["Chi tiết mẫu"] = sampleDetails
        };

        await memoryStream.SaveAsAsync(sheets);
        memoryStream.Position = 0;

        var fileName = $"BaoCaoPhongLab_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx";
        return new RemoteStreamContent(memoryStream, fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }
}
