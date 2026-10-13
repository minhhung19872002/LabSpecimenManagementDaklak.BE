using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using ClosedXML.Excel;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.QualityManagement;
using LabSpecimenManagement.TestRequests;
using Microsoft.Extensions.Hosting;
using MiniExcelLibs;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Content;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace LabSpecimenManagement.Statistics;

public class LabReportAppService : ApplicationService, ILabReportAppService
{
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;
    private readonly IRepository<TestRequestSample, Guid> _sampleRepository;
    private readonly IRepository<TestRequestSampleIndicator, Guid> _sampleIndicatorRepository;
    private readonly IRepository<LaboratoryTask, Guid> _laboratoryTaskRepository;
    private readonly IRepository<LaboratoryTaskIndicator, Guid> _laboratoryTaskIndicatorRepository;
    private readonly IRepository<Indicator, Guid> _indicatorRepository;
    private readonly IRepository<SpecimenType, Guid> _specimenTypeRepository;
    private readonly IRepository<TestMethod, Guid> _testMethodRepository;
    private readonly IRepository<Standard, Guid> _standardRepository;
    private readonly IRepository<StandardIndicator, Guid> _standardIndicatorRepository;
    private readonly IRepository<QualityManagementIndicator, Guid> _qualityManagementIndicatorRepository;
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly IDataFilter _dataFilter;

    public LabReportAppService(
        IRepository<TestRequest, Guid> testRequestRepository,
        IRepository<TestRequestSample, Guid> sampleRepository,
        IRepository<TestRequestSampleIndicator, Guid> sampleIndicatorRepository,
        IRepository<LaboratoryTask, Guid> laboratoryTaskRepository,
        IRepository<LaboratoryTaskIndicator, Guid> laboratoryTaskIndicatorRepository,
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<SpecimenType, Guid> specimenTypeRepository,
        IRepository<TestMethod, Guid> testMethodRepository,
        IRepository<Standard, Guid> standardRepository,
        IRepository<StandardIndicator, Guid> standardIndicatorRepository,
        IRepository<QualityManagementIndicator, Guid> qualityManagementIndicatorRepository,
        IRepository<IdentityUser, Guid> userRepository,
        IHostEnvironment hostEnvironment,
        IDataFilter dataFilter)
    {
        _testRequestRepository = testRequestRepository;
        _sampleRepository = sampleRepository;
        _sampleIndicatorRepository = sampleIndicatorRepository;
        _laboratoryTaskRepository = laboratoryTaskRepository;
        _laboratoryTaskIndicatorRepository = laboratoryTaskIndicatorRepository;
        _indicatorRepository = indicatorRepository;
        _specimenTypeRepository = specimenTypeRepository;
        _testMethodRepository = testMethodRepository;
        _standardRepository = standardRepository;
        _standardIndicatorRepository = standardIndicatorRepository;
        _qualityManagementIndicatorRepository = qualityManagementIndicatorRepository;
        _userRepository = userRepository;
        _hostEnvironment = hostEnvironment;
        _dataFilter = dataFilter;
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

    public async Task<IRemoteStreamContent> ExportLabReportDetailAsync(Guid labId, DateTime fromDate, DateTime toDate)
    {
        var toDateExclusive = toDate.Date.AddDays(1);

        var trQuery = await _testRequestRepository.GetQueryableAsync();
        var trsQuery = await _sampleRepository.GetQueryableAsync();
        var ltiQuery = await _laboratoryTaskIndicatorRepository.GetQueryableAsync();
        var ltQuery = await _laboratoryTaskRepository.GetQueryableAsync();
        var trsiQuery = await _sampleIndicatorRepository.GetQueryableAsync();
        var indQuery = await _indicatorRepository.GetQueryableAsync();
        var stQuery = await _specimenTypeRepository.GetQueryableAsync();
        var tmQuery = await _testMethodRepository.GetQueryableAsync();
        var stdQuery = await _standardRepository.GetQueryableAsync();
        var siQuery = await _standardIndicatorRepository.GetQueryableAsync();
        var qmiQuery = await _qualityManagementIndicatorRepository.GetQueryableAsync();

        // Main flat query — tất cả LTI của lab trong kỳ (không lọc ApprovalStatus)
        var rawQuery = from lti in ltiQuery
                       join lt in ltQuery on lti.LaboratoryTaskId equals lt.Id
                       join trsi in trsiQuery on lti.TestRequestSampleIndicatorId equals trsi.Id
                       join trs in trsQuery on trsi.TestRequestSampleId equals trs.Id
                       join tr in trQuery on trs.TestRequestId equals tr.Id
                       join ind in indQuery on trsi.IndicatorId equals ind.Id
                       join tm in tmQuery on trsi.TestMethodId equals (Guid?)tm.Id into tmGroup
                       from tm in tmGroup.DefaultIfEmpty()
                       join st in stQuery on trsi.SpecimenTypeId equals (Guid?)st.Id into stGroup
                       from st in stGroup.DefaultIfEmpty()
                       where lt.LaboratoryId == labId
                           && tr.CreationTime >= fromDate && tr.CreationTime < toDateExclusive
                       select new
                       {
                           LtiId = lti.Id,
                           LtiResult = lti.Result,
                           LtiStandardId = lti.StandardId,
                           LtiMethodId = lti.MethodId,
                           TrsId = trs.Id,
                           TrsSampleName = trs.SampleName,
                           TrsSampleCode = trs.SampleCode,
                           TrContractDate = tr.ContractDate,
                           SpecimenTypeId = trsi.SpecimenTypeId,
                           SpecimenTypeName = st != null ? st.Name : "",
                           IndicatorId = trsi.IndicatorId,
                           IndicatorName = ind.Name,
                           TestMethodId = trsi.TestMethodId,
                           TestMethodName = tm != null ? tm.Name : "",
                       };

        var rawData = await AsyncExecuter.ToListAsync(rawQuery);

        var ltiIds = rawData.Select(x => x.LtiId).Distinct().ToList();
        var indicatorIds = rawData.Select(x => x.IndicatorId).Distinct().ToList();

        // Tiêu chuẩn của từng LTI giống trang QLCL: LTI.StandardId (KTV chọn) ?? QMI.StandardId
        var qmiData = await AsyncExecuter.ToListAsync(
            qmiQuery
                .Where(qmi => ltiIds.Contains(qmi.LaboratoryTaskIndicatorId))
                .Select(qmi => new { qmi.LaboratoryTaskIndicatorId, qmi.StandardId })
        );

        var qmiStandardIdByLtiId = qmiData
            .GroupBy(x => x.LaboratoryTaskIndicatorId)
            .ToDictionary(g => g.Key, g => g.First().StandardId);

        Guid? ResolveStandardId(Guid ltiId, Guid? ltiStandardId) =>
            ltiStandardId ?? qmiStandardIdByLtiId.GetValueOrDefault(ltiId);

        var standardIds = rawData
            .Select(x => ResolveStandardId(x.LtiId, x.LtiStandardId))
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var standardNameById = standardIds.Any()
            ? (await AsyncExecuter.ToListAsync(
                    stdQuery.Where(s => standardIds.Contains(s.Id)).Select(s => new { s.Id, s.Name })))
                .ToDictionary(s => s.Id, s => s.Name)
            : new Dictionary<Guid, string>();

        // StandardIndicator — dùng cho Giới hạn phát hiện (không dùng cho Nền mẫu)
        // Không filter IsActive vì báo cáo cần hiển thị giới hạn của phiếu cũ kể cả khi đã ngưng
        var siData = indicatorIds.Any()
            ? await AsyncExecuter.ToListAsync(
                siQuery.Where(si => indicatorIds.Contains(si.IndicatorId)))
            : new List<StandardIndicator>();

        var siByStdAndInd = siData
            .GroupBy(si => (si.StandardId, si.IndicatorId))
            .ToDictionary(g => g.Key, g => g.First());

        var siByStdIndAndMethod = siData
            .GroupBy(si => (si.StandardId, si.IndicatorId, si.TestMethodId))
            .ToDictionary(g => g.Key, g => g.First());

        // === Sheet 1: group by (SpecimenType + Indicator + Method) ===
        var sheet1Rows = rawData
            .GroupBy(x => (x.SpecimenTypeId, x.SpecimenTypeName, x.IndicatorId, x.IndicatorName, x.TestMethodId, x.TestMethodName))
            .Select(g =>
            {
                // Tìm StandardIndicator để lấy Giới hạn phát hiện
                // Fallback: nếu row không có SpecimenType thì bỏ qua filter SpecimenType của SI
                var matchingSi = siData.Where(si =>
                    si.IndicatorId == g.Key.IndicatorId
                    && (!g.Key.TestMethodId.HasValue || si.TestMethodId == g.Key.TestMethodId.Value)
                    && (!si.SpecimenTypeId.HasValue || si.SpecimenTypeId.Value == g.Key.SpecimenTypeId)
                ).ToList();

                var limits = matchingSi
                    .Select(si => BuildCompareWithStandard(
                        si.ComparisonOperator, si.AllowedLimit,
                        si.LowerOperator, si.LowerLimit,
                        si.UpperOperator, si.UpperLimit))
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Distinct()
                    .ToList();

                return new
                {
                    NenMau = g.Key.SpecimenTypeName,
                    TenChiTieu = g.Key.IndicatorName,
                    PhuongPhapThu = g.Key.TestMethodName,
                    CongSuat = g.Select(x => x.TrsId).Distinct().Count(),
                    GioiHanPhatHien = string.Join(", ", limits),
                };
            })
            .OrderBy(x => x.NenMau).ThenBy(x => x.TenChiTieu)
            .ToList();

        // === Sheet 2: flat list — 1 dòng / LaboratoryTaskIndicator ===
        var sheet2Rows = rawData.Select(x =>
        {
            var standardId = ResolveStandardId(x.LtiId, x.LtiStandardId);

            // Khớp StandardIndicator giống QLCL: ưu tiên đúng phương pháp (LTI ?? phiếu), fallback theo chỉ tiêu
            StandardIndicator? si = null;
            if (standardId.HasValue)
            {
                var methodId = x.LtiMethodId ?? x.TestMethodId;
                if (methodId.HasValue)
                {
                    siByStdIndAndMethod.TryGetValue((standardId.Value, x.IndicatorId, methodId.Value), out si);
                }
                if (si == null)
                {
                    siByStdAndInd.TryGetValue((standardId.Value, x.IndicatorId), out si);
                }
            }

            var limitStr = si == null
                ? ""
                : BuildCompareWithStandard(
                    si.ComparisonOperator, si.AllowedLimit,
                    si.LowerOperator, si.LowerLimit,
                    si.UpperOperator, si.UpperLimit);

            return new
            {
                TenMau = x.TrsSampleName,
                MaMau = x.TrsSampleCode,
                NgayNhanMau = x.TrContractDate,
                NenMau = x.SpecimenTypeName,
                TenChiTieu = x.IndicatorName,
                PhuongPhapThu = x.TestMethodName,
                SoVoiTieuChuan = standardId.HasValue ? standardNameById.GetValueOrDefault(standardId.Value) ?? "" : "",
                GioiHanPhatHien = limitStr,
                KetQua = x.LtiResult ?? "",
                TrangThai = GetResultStatus(x.LtiResult, si),
            };
        }).ToList();

        // === Tạo file Excel với ClosedXML ===
        using var workbook = new XLWorkbook();

        var ws1 = workbook.Worksheets.Add("Dữ liệu tổng quan");
        var headers1 = new[]
        {
            "Nền mẫu (loại mẫu)", "Tên chỉ tiêu", "Phương pháp thử",
            "Công suất kiểm nghiệm (tổng số mẫu/6 tháng)", "Giới hạn phát hiện (nếu có)", "Ghi chú"
        };
        WriteSheetHeaders(ws1, headers1);
        for (int r = 0; r < sheet1Rows.Count; r++)
        {
            var row = sheet1Rows[r];
            ws1.Cell(r + 2, 1).Value = row.NenMau;
            ws1.Cell(r + 2, 2).Value = row.TenChiTieu;
            ws1.Cell(r + 2, 3).Value = row.PhuongPhapThu;
            ws1.Cell(r + 2, 4).Value = row.CongSuat;
            ws1.Cell(r + 2, 5).Value = row.GioiHanPhatHien;
            ws1.Cell(r + 2, 6).Value = "";
        }
        ws1.Columns().AdjustToContents();

        var ws2 = workbook.Worksheets.Add("Dữ liệu chi tiết");
        var headers2 = new[]
        {
            "Tên mẫu", "Mã mẫu", "Ngày nhận mẫu", "Nền mẫu (loại mẫu)", "Tên chỉ tiêu",
            "Phương pháp thử", "So với tiêu chuẩn", "Giới hạn phát hiện", "Kết quả", "Ghi chú", "Trạng thái"
        };
        WriteSheetHeaders(ws2, headers2);
        for (int r = 0; r < sheet2Rows.Count; r++)
        {
            var row = sheet2Rows[r];
            ws2.Cell(r + 2, 1).Value = row.TenMau;
            ws2.Cell(r + 2, 2).Value = row.MaMau;
            ws2.Cell(r + 2, 3).Value = row.NgayNhanMau?.ToString("dd/MM/yyyy") ?? "";
            ws2.Cell(r + 2, 4).Value = row.NenMau;
            ws2.Cell(r + 2, 5).Value = row.TenChiTieu;
            ws2.Cell(r + 2, 6).Value = row.PhuongPhapThu;
            ws2.Cell(r + 2, 7).Value = row.SoVoiTieuChuan;
            ws2.Cell(r + 2, 8).Value = row.GioiHanPhatHien;
            ws2.Cell(r + 2, 9).Value = row.KetQua;
            ws2.Cell(r + 2, 10).Value = "";
            ws2.Cell(r + 2, 11).Value = row.TrangThai;
        }

        // Excel Table làm nguồn cho 3 pivot — Table cần ít nhất 1 dòng dữ liệu nên giữ 1 dòng trống khi kỳ không có số liệu
        var detailTable = ws2
            .Range(1, 1, Math.Max(sheet2Rows.Count, 1) + 1, headers2.Length)
            .CreateTable("DuLieuChiTiet");
        detailTable.Theme = XLTableTheme.TableStyleMedium2;
        ws2.SheetView.FreezeRows(1);
        ws2.Columns().AdjustToContents();

        // === 3 sheet pivot từ "Dữ liệu chi tiết" (Excel tự refresh khi mở file) ===
        AddCountPivot(workbook, detailTable, "Tổng chỉ tiêu mỗi nền mẫu",
            rowFields: new[] { "Tên mẫu", "Tên chỉ tiêu" },
            columnField: null,
            valueField: "Tên chỉ tiêu", valueName: "Tổng chỉ tiêu mỗi nền mẫu");

        AddCountPivot(workbook, detailTable, "Tổng mẫu mỗi nền",
            rowFields: new[] { "Tên mẫu", "Mã mẫu" },
            columnField: "Trạng thái",
            valueField: "Mã mẫu", valueName: "Tổng mẫu mỗi nền");

        AddCountPivot(workbook, detailTable, "Theo chỉ tiêu",
            rowFields: new[] { "Tên chỉ tiêu", "Tên mẫu", "Mã mẫu" },
            columnField: "Trạng thái",
            valueField: "Tên mẫu", valueName: "Count of Tên mẫu");

        var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);

        // Pivot chỉ hiện cột Đạt / Không đạt như file mẫu của BA (vẫn bật lại được bằng filter trong Excel)
        HidePivotFieldItems(memoryStream, "Trạng thái", new HashSet<string> { StatusNotTested, StatusUndetermined });
        memoryStream.Position = 0;

        var fileName = $"BaoCaoPhongLab_ChiTiet_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}.xlsx";
        return new RemoteStreamContent(memoryStream, fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    private static void WriteSheetHeaders(IXLWorksheet ws, string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(1, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.Red;
        }
    }

    private const string StatusPass = "Đạt";
    private const string StatusFail = "Không đạt";
    private const string StatusNotTested = "Chưa thử nghiệm";
    private const string StatusUndetermined = "Không xác định";

    /// <summary>
    /// Trạng thái 1 chỉ tiêu: chưa có kết quả → "Chưa thử nghiệm"; còn lại so với giới hạn tiêu chuẩn
    /// như cột Kết luận trên trang chi tiết task / QLCL; không so được (không có giới hạn...) → "Không xác định".
    /// </summary>
    private static string GetResultStatus(string? result, StandardIndicator? si)
    {
        if (string.IsNullOrWhiteSpace(result)) return StatusNotTested;
        if (si == null) return StatusUndetermined;

        return PassFailEvaluator.Evaluate(
            result,
            si.ComparisonOperator, si.AllowedLimit,
            si.LowerOperator, si.LowerLimit,
            si.UpperOperator, si.UpperLimit) switch
        {
            true => StatusPass,
            false => StatusFail,
            null => StatusUndetermined
        };
    }

    /// <summary>
    /// Thêm sheet pivot đếm số dòng từ bảng nguồn. Các row field (trừ field cuối) được thu gọn như file mẫu của BA.
    /// </summary>
    private static void AddCountPivot(
        XLWorkbook workbook,
        IXLTable source,
        string sheetName,
        string[] rowFields,
        string? columnField,
        string valueField,
        string valueName)
    {
        var ws = workbook.Worksheets.Add(sheetName);
        var pivot = ws.PivotTables.Add($"Pivot{workbook.Worksheets.Count}", ws.Cell(3, 1), source);

        for (var i = 0; i < rowFields.Length; i++)
        {
            var field = pivot.RowLabels.Add(rowFields[i]);
            if (i < rowFields.Length - 1)
            {
                field.SetCollapsed();
            }
        }

        if (columnField != null)
        {
            pivot.ColumnLabels.Add(columnField);
        }

        pivot.Values.Add(valueField, valueName).SetSummaryFormula(XLPivotSummary.Count);
    }

    /// <summary>
    /// Ẩn các item của 1 field trong mọi pivot của file (ClosedXML 0.104 chưa có API public cho item ẩn)
    /// bằng cách gắn h="1" vào item tương ứng trong pivotTable XML.
    /// </summary>
    private static void HidePivotFieldItems(MemoryStream xlsx, string fieldName, ISet<string> hiddenValues)
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace relNs = "http://schemas.openxmlformats.org/package/2006/relationships";

        using var zip = new ZipArchive(xlsx, ZipArchiveMode.Update, leaveOpen: true);
        var pivotEntries = zip.Entries
            .Where(e => Regex.IsMatch(e.FullName, @"(^|/)pivotTables/pivotTable\d*\.xml$"))
            .ToList();

        foreach (var pivotEntry in pivotEntries)
        {
            var dir = pivotEntry.FullName[..(pivotEntry.FullName.LastIndexOf('/') + 1)];
            var relsEntry = zip.GetEntry($"{dir}_rels/{pivotEntry.Name}.rels");
            if (relsEntry == null) continue;

            var target = LoadXml(relsEntry).Descendants(relNs + "Relationship")
                .FirstOrDefault(rel => ((string?)rel.Attribute("Type"))?.EndsWith("/pivotCacheDefinition") == true)
                ?.Attribute("Target")?.Value;
            if (target == null) continue;

            var cachePath = target.StartsWith('/')
                ? target.TrimStart('/')
                : new Uri(new Uri("http://package/" + dir), target).AbsolutePath.TrimStart('/');
            var cacheEntry = zip.GetEntry(cachePath);
            if (cacheEntry == null) continue;

            // Index của pivotField trong pivotTable trùng với index cacheField; item x = index trong sharedItems
            var cacheFields = LoadXml(cacheEntry).Descendants(ns + "cacheField").ToList();
            var fieldIndex = cacheFields.FindIndex(f => (string?)f.Attribute("name") == fieldName);
            if (fieldIndex < 0) continue;

            var sharedItems = cacheFields[fieldIndex].Element(ns + "sharedItems")?.Elements()
                .Select(e => (string?)e.Attribute("v"))
                .ToList() ?? new List<string?>();

            var pivotDoc = LoadXml(pivotEntry);
            var pivotField = pivotDoc.Descendants(ns + "pivotField").ElementAtOrDefault(fieldIndex);
            if (pivotField?.Attribute("axis") == null) continue;

            var changed = false;
            foreach (var item in pivotField.Descendants(ns + "item"))
            {
                if (int.TryParse((string?)item.Attribute("x"), out var x)
                    && x < sharedItems.Count
                    && sharedItems[x] is { } value
                    && hiddenValues.Contains(value))
                {
                    item.SetAttributeValue("h", "1");
                    changed = true;
                }
            }
            if (!changed) continue;

            using var stream = pivotEntry.Open();
            stream.SetLength(0);
            pivotDoc.Save(stream, System.Xml.Linq.SaveOptions.DisableFormatting);
        }
    }

    private static XDocument LoadXml(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        return XDocument.Load(stream);
    }

    private static string BuildCompareWithStandard(
        string? comparisonOperator,
        string? allowedLimit,
        string? lowerOperator = null,
        string? lowerLimit = null,
        string? upperOperator = null,
        string? upperLimit = null)
    {
        if (!string.IsNullOrWhiteSpace(lowerLimit) && !string.IsNullOrWhiteSpace(upperLimit))
        {
            var lower = NormalizeOperator(lowerOperator) ?? ">";
            var upper = NormalizeOperator(upperOperator) ?? "<";
            return $"{lower} {lowerLimit.Trim()} và {upper} {upperLimit.Trim()}";
        }
        if (string.IsNullOrWhiteSpace(allowedLimit)) return "";
        if (string.IsNullOrWhiteSpace(comparisonOperator)) return allowedLimit.Trim();
        return $"{NormalizeOperator(comparisonOperator) ?? comparisonOperator} {allowedLimit.Trim()}";
    }

    private static string? NormalizeOperator(string? op) => op?.Trim() switch
    {
        "<=" => "≤",
        ">=" => "≥",
        var x => x
    };

    // ── Sổ nhận mẫu (cross-lab, template-based) ──────────────────────────────

    public async Task<IRemoteStreamContent> ExportSoNhanMauAsync(DateTime fromDate, DateTime toDate)
    {
        var toDateExclusive = toDate.Date.AddDays(1);

        var siQuery   = await _sampleIndicatorRepository.GetQueryableAsync();
        var sampleQ   = await _sampleRepository.GetQueryableAsync();
        var trQuery   = await _testRequestRepository.GetQueryableAsync();
        IQueryable<Indicator> miQuery;
        using (_dataFilter.Disable<ISoftDelete>())
        {
            miQuery = await _indicatorRepository.GetQueryableAsync();
        }
        var liQuery   = await _laboratoryTaskIndicatorRepository.GetQueryableAsync();
        var ltQuery   = await _laboratoryTaskRepository.GetQueryableAsync();
        var qiQuery   = await _qualityManagementIndicatorRepository.GetQueryableAsync();
        var tmQuery   = await _testMethodRepository.GetQueryableAsync();
        var userQuery = await _userRepository.GetQueryableAsync();

        var query = from si in siQuery
                    join s  in sampleQ  on si.TestRequestSampleId          equals s.Id
                    join tr in trQuery  on s.TestRequestId                  equals tr.Id
                    join mi in miQuery  on si.IndicatorId                   equals mi.Id
                    join li in liQuery  on si.Id equals li.TestRequestSampleIndicatorId into liJ
                    from li in liJ.DefaultIfEmpty()
                    join lt in ltQuery  on li.LaboratoryTaskId              equals lt.Id into ltJ
                    from lt in ltJ.DefaultIfEmpty()
                    join qi in qiQuery  on li.Id equals qi.LaboratoryTaskIndicatorId into qiJ
                    from qi in qiJ.DefaultIfEmpty()
                    join tm in tmQuery  on (li != null ? li.MethodId : si.TestMethodId) equals tm.Id into tmJ
                    from tm in tmJ.DefaultIfEmpty()
                    join qm  in userQuery on lt.TechnicalManagerId          equals qm.Id  into qmJ
                    from qm  in qmJ.DefaultIfEmpty()
                    join ktv in userQuery on li.TechnicianId                equals ktv.Id into ktvJ
                    from ktv in ktvJ.DefaultIfEmpty()
                    where s.IsDeleted  == false
                       && tr.IsDeleted == false
                       && tr.Status    != TestRequestStatus.Cancelled
                       && tr.RequestDate >= fromDate
                       && tr.RequestDate <  toDateExclusive
                    orderby tr.ContractDate descending, tr.ContractNumber, s.Id, mi.Name
                    select new
                    {
                        TestRequestId          = tr.Id,
                        SampleId               = s.Id,
                        ContractNumber         = tr.ContractNumber,
                        SampleCode             = s.SampleCode,
                        SampleName             = s.SampleName,
                        RequestCompletionDate  = tr.ExpectedCompletionDate ?? (lt != null ? lt.RequestCompletionDate : null),
                        LaboratoryTaskId       = lt != null ? (Guid?)lt.Id : null,
                        IndicatorName          = mi.Name,
                        MethodName             = li != null && !string.IsNullOrWhiteSpace(li.SubContractMethodName)
                                                    ? li.SubContractMethodName
                                                    : (tm != null ? tm.Name : null),
                        Unit                   = li != null ? li.Unit    : null,
                        TestDate               = li != null ? li.TestDate : null,
                        Result                 = li != null ? li.Result   : null,
                        TechnicalManagerName   = qm  != null ? (qm.Name  ?? qm.UserName)  : null,
                        TechnicianName         = ktv != null ? (ktv.Name ?? ktv.UserName)  : null,
                        EntryDate              = li != null ? li.EntryDate    : null,
                        ApprovalDate           = li != null ? li.ApprovalDate : null,
                        TestRequestStatus      = tr.Status,
                        IndicatorApprovalStatus = li != null ? (IndicatorApprovalStatus?)li.ApprovalStatus : null,
                        QualityApprovalStatus  = qi != null ? (QualityIndicatorApprovalStatus?)qi.ApprovalStatus : null,
                        IsReportPublished      = qi != null && qi.IsPrinted,
                        TechnicianId           = li != null ? (Guid?)li.TechnicianId : null,
                        MethodId               = li != null ? (Guid?)li.MethodId      : null,
                    };

        var rawData = await AsyncExecuter.ToListAsync(query);

        var taskDateRanges = rawData
            .Where(x => x.LaboratoryTaskId.HasValue && x.TestDate.HasValue)
            .GroupBy(x => x.LaboratoryTaskId!.Value)
            .ToDictionary(g => g.Key,
                g => (Min: g.Min(x => x.TestDate!.Value), Max: g.Max(x => x.TestDate!.Value)));

        var rows = new List<SoNhanMauRow>();
        for (var idx = 0; idx < rawData.Count; idx++)
        {
            var x = rawData[idx];

            var tgTn = "";
            if (x.LaboratoryTaskId.HasValue && taskDateRanges.TryGetValue(x.LaboratoryTaskId.Value, out var dr))
            {
                var min = dr.Min; var max = dr.Max;
                if (min.Date == max.Date)
                    tgTn = min.ToString("dd/MM/yyyy");
                else if (min.Month == max.Month && min.Year == max.Year)
                    tgTn = $"{min.Day}/{min.Month} - {max:dd/MM/yyyy}";
                else
                    tgTn = $"{min:dd/MM/yyyy} - {max:dd/MM/yyyy}";
            }

            var status = MapQLCLDisplayStatus(
                x.TestRequestStatus, x.IndicatorApprovalStatus, x.QualityApprovalStatus,
                x.IsReportPublished, x.TechnicianId, x.MethodId, x.TestDate, x.Result);

            rows.Add(new SoNhanMauRow
            {
                TestRequestId  = x.TestRequestId,
                SampleId       = x.SampleId,
                Stt            = idx + 1,
                SoHD           = x.ContractNumber ?? "",
                MaSoMau        = x.SampleCode ?? "",
                TenMau         = x.SampleName ?? "",
                NgayYCXong     = x.RequestCompletionDate.HasValue ? x.RequestCompletionDate.Value.ToString("dd/MM/yyyy") : "",
                ThoiGianTN     = tgTn,
                TenChiTieu     = StripHtml(x.IndicatorName ?? ""),
                PhuongPhap     = StripHtml(x.MethodName ?? ""),
                DVT            = StripHtml(x.Unit ?? ""),
                KetQua         = x.Result ?? "",
                QLKyThuat      = x.TechnicalManagerName ?? "",
                KyThuat        = x.TechnicianName ?? "",
                NgayNhap       = x.EntryDate.HasValue    ? x.EntryDate.Value.ToString("dd/MM/yyyy")    : "",
                NgayQLDuyet    = x.ApprovalDate.HasValue ? x.ApprovalDate.Value.ToString("dd/MM/yyyy") : "",
                TinhTrang      = GetStatusLabel(status),
            });
        }

        ComputeRowSpans(rows);

        var templatePath = Path.Combine(_hostEnvironment.ContentRootPath, "wwwroot",
            "templates", "Meta Data Template", "Báo cáo chi tiết.xlsx");
        using var workbook = new XLWorkbook(templatePath);
        var ws = workbook.Worksheet(1);

        var lastExistingRow = ws.LastRowUsed()?.RowNumber() ?? 1;
        for (var r = lastExistingRow; r >= 2; r--)
            ws.Row(r).Delete();

        for (var i = 0; i < rows.Count; i++)
        {
            var row = rows[i];
            var r   = i + 2;
            ws.Row(r).Height = 18;

            WriteDetailCell(ws, r, 1,  row.Stt.ToString(), center: true);
            WriteMergedCell(ws, r, 2,  row.SoHD,      row.SoHDSpan,        center: false);
            WriteMergedCell(ws, r, 3,  row.MaSoMau,   row.MaSoMauSpan,     center: false);
            WriteMergedCell(ws, r, 4,  row.TenMau,    row.TenMauSpan,      center: false);
            WriteMergedCell(ws, r, 5,  row.NgayYCXong,row.NgayYCXongSpan,  center: true);
            WriteMergedCell(ws, r, 6,  row.ThoiGianTN,row.ThoiGianTNSpan,  center: true);
            WriteDetailCell(ws, r, 7,  row.TenChiTieu, center: false);
            WriteDetailCell(ws, r, 8,  row.PhuongPhap, center: false);
            WriteDetailCell(ws, r, 9,  row.DVT,         center: true);
            WriteDetailCell(ws, r, 10, row.KetQua,      center: false);
            WriteMergedCell(ws, r, 11, row.QLKyThuat,  row.QLKyThuatSpan,  center: false);
            WriteMergedCell(ws, r, 12, row.KyThuat,    row.KyThuatSpan,    center: false);
            WriteMergedCell(ws, r, 13, row.NgayNhap,   row.NgayNhapSpan,   center: true);
            WriteMergedCell(ws, r, 14, row.NgayQLDuyet,row.NgayQLDuyetSpan,center: true);
            WriteDetailCell(ws, r, 15, row.TinhTrang,  center: false);
        }

        var ms = new MemoryStream();
        workbook.SaveAs(ms);
        ms.Position = 0;

        var fileName = $"SoNhanMau_{fromDate:ddMMyyyy}_{toDate:ddMMyyyy}.xlsx";
        return new RemoteStreamContent(ms, fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    private static void WriteDetailCell(IXLWorksheet ws, int row, int col, string value, bool center)
    {
        var cell = ws.Cell(row, col);
        cell.Value = value;
        ApplyDetailStyle(cell.Style, center);
    }

    private static void WriteMergedCell(IXLWorksheet ws, int row, int col, string value, int span, bool center)
    {
        if (span == 0)
        {
            ApplyDetailStyle(ws.Cell(row, col).Style, center);
            return;
        }
        var cell = ws.Cell(row, col);
        cell.Value = value;
        if (span > 1)
        {
            var range = ws.Range(row, col, row + span - 1, col);
            range.Merge();
            for (var r = row; r <= row + span - 1; r++)
                ApplyDetailStyle(ws.Cell(r, col).Style, center);
        }
        else
        {
            ApplyDetailStyle(cell.Style, center);
        }
    }

    private static void ApplyDetailStyle(IXLStyle style, bool center)
    {
        style.Font.FontName  = "Times New Roman";
        style.Font.FontSize  = 11;
        style.Alignment.Vertical   = XLAlignmentVerticalValues.Center;
        style.Alignment.WrapText   = true;
        style.Alignment.Horizontal = center
            ? XLAlignmentHorizontalValues.Center
            : XLAlignmentHorizontalValues.Left;
        style.Border.OutsideBorder = XLBorderStyleValues.Thin;
    }

    private static void ComputeRowSpans(List<SoNhanMauRow> rows)
    {
        var n = rows.Count;
        var i = 0;
        while (i < n)
        {
            var contractId = rows[i].TestRequestId;
            var j = i;
            while (j < n && rows[j].TestRequestId == contractId) j++;

            rows[i].SoHDSpan = j - i;
            for (var k = i + 1; k < j; k++) rows[k].SoHDSpan = 0;

            var si2 = i;
            while (si2 < j)
            {
                var sampleId = rows[si2].SampleId;
                var sj = si2;
                while (sj < j && rows[sj].SampleId == sampleId) sj++;
                rows[si2].MaSoMauSpan = sj - si2;
                rows[si2].TenMauSpan  = sj - si2;
                for (var k = si2 + 1; k < sj; k++) { rows[k].MaSoMauSpan = 0; rows[k].TenMauSpan = 0; }
                si2 = sj;
            }

            CalcSpan(rows, i, j - 1, r => r.NgayYCXong,  (r, v) => r.NgayYCXongSpan  = v, bySample: true);
            CalcSpan(rows, i, j - 1, r => r.ThoiGianTN,  (r, v) => r.ThoiGianTNSpan  = v, bySample: true);
            CalcSpan(rows, i, j - 1, r => r.NgayNhap,    (r, v) => r.NgayNhapSpan    = v, bySample: true);
            CalcSpan(rows, i, j - 1, r => r.NgayQLDuyet, (r, v) => r.NgayQLDuyetSpan = v, bySample: true);
            CalcSpan(rows, i, j - 1, r => r.QLKyThuat, (r, v) => r.QLKyThuatSpan = v, bySample: false);
            CalcSpan(rows, i, j - 1, r => r.KyThuat,   (r, v) => r.KyThuatSpan   = v, bySample: false);

            i = j;
        }
    }

    private static void CalcSpan(List<SoNhanMauRow> rows, int gs, int ge,
        Func<SoNhanMauRow, string> getVal, Action<SoNhanMauRow, int> setSpan, bool bySample)
    {
        for (var idx = gs; idx <= ge; idx++)
        {
            var val = getVal(rows[idx]);
            var prevSameGroup = idx > gs && (!bySample || rows[idx - 1].SampleId == rows[idx].SampleId);

            if (prevSameGroup && getVal(rows[idx - 1]) == val) { setSpan(rows[idx], 0); continue; }
            if (string.IsNullOrEmpty(val))                     { setSpan(rows[idx], 1); continue; }

            var span = 1;
            for (var k = idx + 1; k <= ge; k++)
            {
                if (bySample && rows[k].SampleId != rows[idx].SampleId) break;
                if (getVal(rows[k]) != val) break;
                span++;
            }
            setSpan(rows[idx], span);
        }
    }

    private static readonly Regex HtmlTagRegex = new Regex("<[^>]+>", RegexOptions.Compiled);
    private static string StripHtml(string s) =>
        string.IsNullOrEmpty(s) ? s : HtmlTagRegex.Replace(s, "").Trim();

    private static string GetStatusLabel(QLCLDisplayStatus? status) => status switch
    {
        QLCLDisplayStatus.New                  => "Mới",
        QLCLDisplayStatus.WaitingForPayment    => "Chờ thanh toán",
        QLCLDisplayStatus.UnderReview          => "Đang xem xét YC",
        QLCLDisplayStatus.Rejected             => "Từ chối",
        QLCLDisplayStatus.WaitingForAssignment => "Chờ giao việc",
        QLCLDisplayStatus.ToDo                 => "Cần làm",
        QLCLDisplayStatus.Testing              => "Đang thử nghiệm",
        QLCLDisplayStatus.WaitingQLKTApproval  => "Chờ QLKT phê duyệt",
        QLCLDisplayStatus.ResultRejected       => "Từ chối KQ",
        QLCLDisplayStatus.WaitingQLCLApproval  => "Chờ QLCL Duyệt",
        QLCLDisplayStatus.QLCLRejected         => "QLCL từ chối",
        QLCLDisplayStatus.WaitingForPublishing => "Chờ phát hành",
        QLCLDisplayStatus.Published            => "Đã phát hành",
        QLCLDisplayStatus.RequestCancelled     => "Hủy bỏ",
        _                                      => ""
    };

    private static QLCLDisplayStatus? MapQLCLDisplayStatus(
        TestRequestStatus testRequestStatus,
        IndicatorApprovalStatus? indicatorApprovalStatus,
        QualityIndicatorApprovalStatus? qualityApprovalStatus,
        bool isReportPublished,
        Guid? technicianId,
        Guid? methodId,
        DateTime? testDate,
        string? result)
    {
        if (testRequestStatus == TestRequestStatus.Cancelled)
            return QLCLDisplayStatus.RequestCancelled;

        if (isReportPublished)
            return QLCLDisplayStatus.Published;

        if (qualityApprovalStatus == QualityIndicatorApprovalStatus.Approved)
            return QLCLDisplayStatus.WaitingForPublishing;

        if (qualityApprovalStatus == QualityIndicatorApprovalStatus.Rejected)
            return QLCLDisplayStatus.QLCLRejected;

        if (indicatorApprovalStatus == IndicatorApprovalStatus.Approved &&
            (qualityApprovalStatus == QualityIndicatorApprovalStatus.PendingApproval || qualityApprovalStatus == null))
            return QLCLDisplayStatus.WaitingQLCLApproval;

        if (indicatorApprovalStatus == IndicatorApprovalStatus.Rejected)
            return QLCLDisplayStatus.ResultRejected;

        if (indicatorApprovalStatus == IndicatorApprovalStatus.Entered)
            return QLCLDisplayStatus.WaitingQLKTApproval;

        if (indicatorApprovalStatus == IndicatorApprovalStatus.AcceptedForTesting &&
            (testDate.HasValue || !string.IsNullOrWhiteSpace(result)))
            return QLCLDisplayStatus.Testing;

        if (indicatorApprovalStatus == IndicatorApprovalStatus.AcceptedForTesting &&
            technicianId.HasValue && methodId.HasValue)
            return QLCLDisplayStatus.ToDo;

        if (indicatorApprovalStatus == IndicatorApprovalStatus.AcceptedForTesting)
            return QLCLDisplayStatus.WaitingForAssignment;

        if (indicatorApprovalStatus == IndicatorApprovalStatus.TestingRejected)
            return QLCLDisplayStatus.Rejected;

        switch (testRequestStatus)
        {
            case TestRequestStatus.New:
                return QLCLDisplayStatus.New;
            case TestRequestStatus.PendingPayment:
                return QLCLDisplayStatus.WaitingForPayment;
            case TestRequestStatus.UnderReview:
            case TestRequestStatus.WaitingCustomerConfirm:
                return QLCLDisplayStatus.UnderReview;
            case TestRequestStatus.Completed:
                return QLCLDisplayStatus.Published;
            default:
                return !indicatorApprovalStatus.HasValue
                    ? QLCLDisplayStatus.UnderReview
                    : QLCLDisplayStatus.ToDo;
        }
    }

    private class SoNhanMauRow
    {
        public Guid   TestRequestId { get; set; }
        public Guid   SampleId      { get; set; }
        public int    Stt           { get; set; }
        public string SoHD          { get; set; } = "";
        public string MaSoMau       { get; set; } = "";
        public string TenMau        { get; set; } = "";
        public string NgayYCXong    { get; set; } = "";
        public string ThoiGianTN    { get; set; } = "";
        public string TenChiTieu    { get; set; } = "";
        public string PhuongPhap    { get; set; } = "";
        public string DVT           { get; set; } = "";
        public string KetQua        { get; set; } = "";
        public string QLKyThuat     { get; set; } = "";
        public string KyThuat       { get; set; } = "";
        public string NgayNhap      { get; set; } = "";
        public string NgayQLDuyet   { get; set; } = "";
        public string TinhTrang     { get; set; } = "";
        public int SoHDSpan        { get; set; } = 1;
        public int MaSoMauSpan     { get; set; } = 1;
        public int TenMauSpan      { get; set; } = 1;
        public int NgayYCXongSpan  { get; set; } = 1;
        public int ThoiGianTNSpan  { get; set; } = 1;
        public int QLKyThuatSpan   { get; set; } = 1;
        public int KyThuatSpan     { get; set; } = 1;
        public int NgayNhapSpan    { get; set; } = 1;
        public int NgayQLDuyetSpan { get; set; } = 1;
    }
}
