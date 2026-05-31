using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.QualityManagement;
using LabSpecimenManagement.Receipts;
using LabSpecimenManagement.TestRequests;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace LabSpecimenManagement.DataMigration;

public partial class MigrationAppService
{
    [RemoteService(IsEnabled = false)]
    [UnitOfWork]
    public async Task<MigrationResultDto> ImportVnptMigrationAsync(byte[] fileBytes)
    {
        var rows = ReadVnptExcelRows(fileBytes);
        var masterData = await LoadMasterDataAsync();

        var result = new MigrationResultDto();

        var validationErrors = ValidateAllRows(rows, masterData);
        if (validationErrors.Count > 0)
        {
            result.Success = false;
            result.ValidationErrors = validationErrors;
            return result;
        }

        var phieuGroups = rows.GroupBy(r => r.SoHopDong?.Trim()).ToList();

        var allTestRequests = new List<TestRequest>();
        var allSamples = new List<TestRequestSample>();
        var allIndicators = new List<TestRequestSampleIndicator>();
        var allLabTasks = new List<LaboratoryTask>();
        var allLabTaskIndicators = new List<LaboratoryTaskIndicator>();
        var allQmTasks = new List<QualityManagementTask>();
        var allQmIndicators = new List<QualityManagementIndicator>();
        var allReceipts = new List<Receipt>();
        var allPaymentHistories = new List<PaymentHistory>();

        var (requestNumberPrefix, requestNumberCounter) = await GetMigrationRequestNumberBaseAsync();

        foreach (var phieuGroup in phieuGroups)
        {
            var phieuRows = phieuGroup.ToList();
            var contractNumber = phieuRows.First().SoHopDong?.Trim();

            if (!string.IsNullOrWhiteSpace(contractNumber) && masterData.ExistingContracts.Contains(contractNumber))
            {
                result.SkippedCount++;
                continue;
            }

            BuildSingleTestRequest(
                phieuRows, masterData, result,
                requestNumberPrefix, ref requestNumberCounter,
                allTestRequests, allSamples, allIndicators,
                allLabTasks, allLabTaskIndicators,
                allQmTasks, allQmIndicators,
                allReceipts, allPaymentHistories);
        }

        await _testRequestRepository.InsertManyAsync(allTestRequests, autoSave: true);
        await _sampleRepository.InsertManyAsync(allSamples, autoSave: true);
        await _sampleIndicatorRepository.InsertManyAsync(allIndicators, autoSave: true);
        await _labTaskRepository.InsertManyAsync(allLabTasks, autoSave: true);
        await _labTaskIndicatorRepository.InsertManyAsync(allLabTaskIndicators, autoSave: true);
        await _qmTaskRepository.InsertManyAsync(allQmTasks, autoSave: true);
        await _qmIndicatorRepository.InsertManyAsync(allQmIndicators, autoSave: true);
        await _receiptRepository.InsertManyAsync(allReceipts, autoSave: true);
        await _paymentHistoryRepository.InsertManyAsync(allPaymentHistories);

        result.Success = true;
        return result;
    }

    private static readonly DateTime MigrationCutoffDate = new(2026, 5, 1);

    private static int DetermineIndicatorCase(VnptExcelRow row)
    {
        if (!string.IsNullOrWhiteSpace(row.NgayQlDuyet)) return 4;
        if (!string.IsNullOrWhiteSpace(row.NgayGuiQl)) return 3;
        if (!string.IsNullOrWhiteSpace(row.NgayPhanChiTieu)) return 2;
        return 1;
    }

    private void BuildSingleTestRequest(
        List<VnptExcelRow> phieuRows,
        MasterData masterData,
        MigrationResultDto result,
        string requestNumberPrefix,
        ref int requestNumberCounter,
        List<TestRequest> allTestRequests,
        List<TestRequestSample> allSamples,
        List<TestRequestSampleIndicator> allIndicators,
        List<LaboratoryTask> allLabTasks,
        List<LaboratoryTaskIndicator> allLabTaskIndicators,
        List<QualityManagementTask> allQmTasks,
        List<QualityManagementIndicator> allQmIndicators,
        List<Receipt> allReceipts,
        List<PaymentHistory> allPaymentHistories)
    {
        var firstRow = phieuRows.First();
        var contractNumber = firstRow.SoHopDong?.Trim();
        var customerName = firstRow.KhachHang?.Trim() ?? "";
        var requestDate = ParseExcelDate(firstRow.NgayHopDong) ?? DateTime.Now;
        var isOld = requestDate < MigrationCutoffDate;
        var phieuCase = isOld ? 4 : phieuRows.Max(DetermineIndicatorCase);

        var requestNumber = $"{requestNumberPrefix}-{(requestNumberCounter++):D4}";
        var testRequest = new TestRequest(
            _guidGenerator.Create(),
            requestNumber,
            requestDate,
            customerName);

        testRequest.ContractNumber = contractNumber;
        testRequest.ContractDate = ParseExcelDate(firstRow.NgayHopDong);
        testRequest.TotalSampleCount = phieuRows.Select(r => r.MaSoMau?.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().Count();
        testRequest.TotalCost = phieuRows.Sum(r => ParseDecimal(r.DonGia));

        switch (phieuCase)
        {
            case 4:
                testRequest.Status = TestRequestStatus.ReportPublished;
                testRequest.PaymentReceived = true;
                testRequest.SentToDepartment = true;
                testRequest.SampleSentDate = phieuRows.Min(r => ParseExcelDate(r.NgayPhanChiTieu));
                testRequest.InProgressDate = phieuRows.Min(r => ParseExcelDate(r.NgayBdKiemNghiem))
                    ?? phieuRows.Min(r => ParseExcelDate(r.NgayLuuKq));
                testRequest.ReportPublishedDate = phieuRows.Max(r => ParseExcelDate(r.NgayQlDuyet));
                break;
            case 3:
                testRequest.Status = TestRequestStatus.InProgress;
                testRequest.SentToDepartment = true;
                testRequest.SampleSentDate = phieuRows.Min(r => ParseExcelDate(r.NgayPhanChiTieu));
                testRequest.InProgressDate = phieuRows.Min(r => ParseExcelDate(r.NgayBdKiemNghiem))
                    ?? phieuRows.Min(r => ParseExcelDate(r.NgayGuiQl));
                break;
            case 2:
                testRequest.Status = TestRequestStatus.SampleSent;
                testRequest.SentToDepartment = true;
                testRequest.SampleSentDate = phieuRows.Min(r => ParseExcelDate(r.NgayPhanChiTieu));
                break;
            default:
                testRequest.Status = TestRequestStatus.New;
                break;
        }

        allTestRequests.Add(testRequest);
        result.TestRequestCount++;

        var sampleGroups = phieuRows.GroupBy(r => r.MaSoMau?.Trim()).ToList();
        var sampleMap = new Dictionary<string, TestRequestSample>();

        int sampleOrder = 0;
        foreach (var sampleGroup in sampleGroups)
        {
            var sampleRows = sampleGroup.ToList();
            var sampleFirst = sampleRows.First();

            var sample = new TestRequestSample(
                _guidGenerator.Create(),
                testRequest.Id,
                sampleFirst.MaSoMau?.Trim() ?? "",
                sampleFirst.TenMau?.Trim() ?? "");

            sample.SortOrder = sampleOrder++;
            sample.TotalIndicatorCost = sampleRows.Sum(r => ParseDecimal(r.DonGia));

            allSamples.Add(sample);
            sampleMap[sampleGroup.Key!] = sample;
            result.SampleCount++;
        }

        var trsiList = new List<(TestRequestSampleIndicator Trsi, VnptExcelRow Row, Guid? LabId, int Case)>();

        foreach (var row in phieuRows)
        {
            var indicatorCase = isOld ? 4 : DetermineIndicatorCase(row);
            var sample = sampleMap[row.MaSoMau!.Trim()];
            var indicatorKey = StringHelper.NormalizeAndTrim(row.TenChiTieu!).ToLowerInvariant();
            var indicatorId = masterData.IndicatorByName[indicatorKey];

            var trsi = new TestRequestSampleIndicator(
                _guidGenerator.Create(),
                sample.Id,
                indicatorId);

            if (!string.IsNullOrWhiteSpace(row.PhuongPhap))
            {
                var ppKey = StringHelper.NormalizeAndTrim(row.PhuongPhap).ToLowerInvariant();
                if (masterData.MethodByName.TryGetValue(ppKey, out var methodId))
                    trsi.TestMethodId = methodId;
            }

            trsi.UnitPrice = ParseDecimal(row.DonGia);
            trsi.LaboratoryId = ResolveLabForIndicator(row.KyThuatVien, row.TenChiTieu, masterData);
            trsi.RequestDate = ParseExcelDate(row.NgayYcHoanThanh);

            if (indicatorCase >= 2)
            {
                trsi.SentToDepartment = true;
                trsi.TransferDate = ParseExcelDate(row.NgayPhanChiTieu);
            }

            allIndicators.Add(trsi);

            if (indicatorCase >= 2)
                trsiList.Add((trsi, row, trsi.LaboratoryId, indicatorCase));

            result.IndicatorCount++;
        }

        var labGroups = trsiList
            .Where(x => x.LabId.HasValue)
            .GroupBy(x => x.LabId!.Value)
            .ToList();

        var ltiMap = new Dictionary<Guid, (LaboratoryTaskIndicator Lti, int Case)>();

        foreach (var labGroup in labGroups)
        {
            var labId = labGroup.Key;
            var groupItems = labGroup.ToList();

            Guid? technicalManagerId = null;
            var qlktName = groupItems.First().Row.QuanLyKyThuat;
            if (!string.IsNullOrWhiteSpace(qlktName))
            {
                var qlktKey = StringHelper.NormalizeAndTrim(qlktName).ToLowerInvariant();
                masterData.UserByFullName.TryGetValue(qlktKey, out var qlktId);
                technicalManagerId = qlktId;
            }

            var labTask = new LaboratoryTask(
                _guidGenerator.Create(),
                testRequest.Id,
                labId,
                contractNumber,
                groupItems.Select(x => x.Row.MaSoMau).Distinct().Count(),
                technicalManagerId,
                groupItems.Min(x => ParseExcelDate(x.Row.NgayPhanChiTieu)),
                groupItems.Max(x => ParseExcelDate(x.Row.NgayYcHoanThanh)));

            labTask.SampleSentDate = testRequest.SampleSentDate;

            allLabTasks.Add(labTask);
            result.LabTaskCount++;

            foreach (var item in groupItems)
            {
                var lti = new LaboratoryTaskIndicator(
                    _guidGenerator.Create(),
                    labTask.Id,
                    item.Trsi.Id);

                lti.Result = item.Row.KetQua?.Trim();
                lti.Unit = item.Row.DonViTinh?.Trim();

                if (!string.IsNullOrWhiteSpace(item.Row.PhuongPhap))
                {
                    var ppKey = StringHelper.NormalizeAndTrim(item.Row.PhuongPhap).ToLowerInvariant();
                    if (masterData.MethodByName.TryGetValue(ppKey, out var methodId))
                        lti.MethodId = methodId;
                }

                if (!string.IsNullOrWhiteSpace(item.Row.KyThuatVien))
                {
                    var ktvKey = StringHelper.NormalizeAndTrim(item.Row.KyThuatVien).ToLowerInvariant();
                    if (masterData.UserByFullName.TryGetValue(ktvKey, out var ktvId))
                        lti.TechnicianId = ktvId;
                }

                switch (item.Case)
                {
                    case 4:
                        lti.ApprovalStatus = IndicatorApprovalStatus.Approved;
                        lti.IsSentToQuality = true;
                        lti.AssignedDate = ParseExcelDate(item.Row.NgayPhanChiTieu) ?? requestDate;
                        lti.TestDate = ParseExcelDate(item.Row.NgayBdKiemNghiem) ?? ParseExcelDate(item.Row.NgayLuuKq) ?? requestDate;
                        lti.EntryDate = ParseExcelDate(item.Row.NgayLuuKq) ?? requestDate;
                        lti.SubmitDate = ParseExcelDate(item.Row.NgayGuiQl) ?? requestDate;
                        lti.ApprovalDate = ParseExcelDate(item.Row.NgayQlDuyet) ?? requestDate;
                        if (technicalManagerId.HasValue)
                            lti.ApprovedById = technicalManagerId;
                        break;
                    case 3:
                        lti.ApprovalStatus = IndicatorApprovalStatus.Entered;
                        lti.IsSentToQuality = false;
                        lti.AssignedDate = ParseExcelDate(item.Row.NgayPhanChiTieu) ?? requestDate;
                        lti.TestDate = ParseExcelDate(item.Row.NgayBdKiemNghiem) ?? ParseExcelDate(item.Row.NgayLuuKq) ?? requestDate;
                        lti.EntryDate = ParseExcelDate(item.Row.NgayLuuKq) ?? requestDate;
                        lti.SubmitDate = ParseExcelDate(item.Row.NgayGuiQl);
                        break;
                    default:
                        lti.ApprovalStatus = IndicatorApprovalStatus.Pending;
                        lti.IsSentToQuality = false;
                        lti.AssignedDate = ParseExcelDate(item.Row.NgayPhanChiTieu);
                        lti.TestDate = ParseExcelDate(item.Row.NgayBdKiemNghiem);
                        lti.EntryDate = ParseExcelDate(item.Row.NgayLuuKq);
                        break;
                }

                allLabTaskIndicators.Add(lti);
                ltiMap[item.Trsi.Id] = (lti, item.Case);
            }

            if (isOld || groupItems.All(x => x.Case == 4))
            {
                labTask.Status = LaboratoryTaskStatus.SentToQuality;
            }
            else if (groupItems.Any(x => x.Case == 3))
            {
                labTask.Status = LaboratoryTaskStatus.WaitingApproval;
            }
            else if (groupItems.Any(x => x.Case == 4) || groupItems.Any(x => ParseExcelDate(x.Row.NgayBdKiemNghiem) != null))
            {
                labTask.Status = LaboratoryTaskStatus.InProgress;
            }
            else
            {
                labTask.Status = LaboratoryTaskStatus.Assigned;
            }
        }

        var qmEligibleItems = ltiMap.Where(x => x.Value.Case == 4).ToList();

        if (qmEligibleItems.Count > 0)
        {
            var qmRows = qmEligibleItems
                .Select(x => trsiList.First(t => t.Trsi.Id == x.Key).Row)
                .ToList();

            var qmTask = new QualityManagementTask(
                _guidGenerator.Create(),
                testRequest.Id,
                contractNumber ?? "",
                QualityTaskType.ResultApproval,
                qmRows.Select(r => r.MaSoMau).Distinct().Count(),
                qmEligibleItems.Count,
                qmRows.Min(r => ParseExcelDate(r.NgayQlDuyet)),
                qmRows.Max(r => ParseExcelDate(r.NgayYcHoanThanh)));

            qmTask.Status = QualityManagementStatus.FullyPublished;

            allQmTasks.Add(qmTask);
            result.QmTaskCount++;

            foreach (var (trsiId, (lti, _)) in qmEligibleItems)
            {
                var qmi = new QualityManagementIndicator(
                    _guidGenerator.Create(),
                    qmTask.Id,
                    lti.Id);

                var matchingRow = trsiList.First(x => x.Trsi.Id == trsiId);
                qmi.ApprovalStatus = QualityIndicatorApprovalStatus.Approved;
                qmi.IsPrinted = true;
                qmi.PrintedDate = ParseExcelDate(matchingRow.Row.NgayQlDuyet) ?? requestDate;
                qmi.ApprovalDate = ParseExcelDate(matchingRow.Row.NgayQlDuyet) ?? requestDate;

                allQmIndicators.Add(qmi);
            }
        }

        if (isOld || phieuCase == 4)
        {
            var receipt = new Receipt(
                _guidGenerator.Create(),
                testRequest.Id,
                ReceiptStatus.Paid);

            receipt.PaidDate = requestDate;

            allReceipts.Add(receipt);

            if (testRequest.TotalCost > 0)
            {
                var paymentHistory = new PaymentHistory(
                    _guidGenerator.Create(),
                    receipt.Id,
                    1,
                    testRequest.TotalCost,
                    testRequest.TotalCost,
                    PaymentMethod.BankTransfer,
                    requestDate,
                    "Migration từ hệ thống VNPT");

                allPaymentHistories.Add(paymentHistory);
            }
        }
    }

    [RemoteService(IsEnabled = false)]
    [UnitOfWork]
    public async Task<MigrationRollbackResultDto> RollbackVnptMigrationAsync(byte[] fileBytes)
    {
        var rows = ReadVnptExcelRows(fileBytes);
        var contractNumbers = rows
            .Select(r => r.SoHopDong?.Trim())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct()
            .ToList();

        var result = new MigrationRollbackResultDto();

        using (_dataFilter.Disable<ISoftDelete>())
        {
            var testRequests = await _testRequestRepository.GetListAsync(
                x => x.ContractNumber != null && contractNumbers.Contains(x.ContractNumber));

            if (testRequests.Count == 0)
            {
                result.Message = "Không tìm thấy phiếu nào để rollback.";
                return result;
            }

            var trIds = testRequests.Select(x => x.Id).ToList();

            var receipts = await _receiptRepository.GetListAsync(x => trIds.Contains(x.TestRequestId));
            var receiptIds = receipts.Select(x => x.Id).ToList();
            if (receiptIds.Count > 0)
            {
                var paymentHistories = await _paymentHistoryRepository.GetListAsync(x => receiptIds.Contains(x.ReceiptId));
                if (paymentHistories.Count > 0)
                    await _paymentHistoryRepository.DeleteManyAsync(paymentHistories);
                await _receiptRepository.DeleteManyAsync(receipts);
                result.ReceiptCount = receipts.Count;
            }

            var qmTasks = await _qmTaskRepository.GetListAsync(x => trIds.Contains(x.TestRequestId));
            var qmTaskIds = qmTasks.Select(x => x.Id).ToList();
            if (qmTaskIds.Count > 0)
            {
                var qmIndicators = await _qmIndicatorRepository.GetListAsync(x => qmTaskIds.Contains(x.QualityManagementTaskId));
                if (qmIndicators.Count > 0)
                {
                    await _qmIndicatorRepository.DeleteManyAsync(qmIndicators);
                    result.QmIndicatorCount = qmIndicators.Count;
                }
                await _qmTaskRepository.DeleteManyAsync(qmTasks);
                result.QmTaskCount = qmTasks.Count;
            }

            var labTasks = await _labTaskRepository.GetListAsync(x => trIds.Contains(x.TestRequestId));
            var labTaskIds = labTasks.Select(x => x.Id).ToList();
            if (labTaskIds.Count > 0)
            {
                var labTaskIndicators = await _labTaskIndicatorRepository.GetListAsync(x => labTaskIds.Contains(x.LaboratoryTaskId));
                if (labTaskIndicators.Count > 0)
                {
                    await _labTaskIndicatorRepository.DeleteManyAsync(labTaskIndicators);
                    result.LabTaskIndicatorCount = labTaskIndicators.Count;
                }
                await _labTaskRepository.DeleteManyAsync(labTasks);
                result.LabTaskCount = labTasks.Count;
            }

            var samples = await _sampleRepository.GetListAsync(x => trIds.Contains(x.TestRequestId));
            var sampleIds = samples.Select(x => x.Id).ToList();
            if (sampleIds.Count > 0)
            {
                var sampleIndicators = await _sampleIndicatorRepository.GetListAsync(x => sampleIds.Contains(x.TestRequestSampleId));
                if (sampleIndicators.Count > 0)
                {
                    await _sampleIndicatorRepository.DeleteManyAsync(sampleIndicators);
                    result.IndicatorCount = sampleIndicators.Count;
                }
                await _sampleRepository.DeleteManyAsync(samples);
                result.SampleCount = samples.Count;
            }

            await _testRequestRepository.DeleteManyAsync(testRequests);
            result.TestRequestCount = testRequests.Count;
        }

        result.Success = true;
        result.Message = $"Đã rollback {result.TestRequestCount} phiếu thành công.";
        return result;
    }

    private async Task<(string Prefix, int StartCount)> GetMigrationRequestNumberBaseAsync()
    {
        using (_dataFilter.Disable<ISoftDelete>())
        {
            var prefix = _codeGenerator.GetTodayPrefix("REQ");
            var count = await _testRequestRepository.CountAsync(x => x.RequestNumber.StartsWith(prefix));
            return (prefix, (int)count + 1);
        }
    }

    private async Task<string> GenerateMigrationRequestNumberAsync()
    {
        using (_dataFilter.Disable<ISoftDelete>())
        {
            var prefix = _codeGenerator.GetTodayPrefix("REQ");
            var count = await _testRequestRepository.CountAsync(x => x.RequestNumber.StartsWith(prefix));
            return _codeGenerator.GenerateCodeWithDate("REQ", (int)count);
        }
    }

    private static DateTime? ParseExcelDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var serial) && serial > 1)
        {
            try
            {
                return DateTime.FromOADate(serial);
            }
            catch
            {
                return null;
            }
        }

        string[] formats = { "M/d/yyyy h:mm:ss tt", "M/d/yyyy", "d/M/yyyy", "yyyy-MM-dd" };
        if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtExact))
            return dtExact;

        if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt;

        return null;
    }

    private static decimal ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        var cleaned = value.Replace(".", "").Replace(",", ".");

        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;

        return 0;
    }
}
