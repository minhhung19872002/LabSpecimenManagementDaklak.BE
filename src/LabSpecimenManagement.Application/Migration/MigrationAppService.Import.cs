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

        var requestNumber = $"{requestNumberPrefix}-{(requestNumberCounter++):D4}";
        var testRequest = new TestRequest(
            _guidGenerator.Create(),
            requestNumber,
            requestDate,
            customerName);

        testRequest.ContractNumber = contractNumber;
        testRequest.ContractDate = ParseExcelDate(firstRow.NgayHopDong);
        testRequest.Status = TestRequestStatus.ReportPublished;
        testRequest.PaymentReceived = true;
        testRequest.ReportPublishedDate = ParseExcelDate(firstRow.NgayQlDuyet);
        testRequest.TotalSampleCount = phieuRows.Select(r => r.MaSoMau?.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().Count();
        testRequest.TotalCost = phieuRows.Sum(r => ParseDecimal(r.DonGia));

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

        var trsiList = new List<(TestRequestSampleIndicator Trsi, VnptExcelRow Row, Guid? LabId)>();

        foreach (var row in phieuRows)
        {
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
            trsi.TransferDate = ParseExcelDate(row.NgayPhanChiTieu);
            trsi.RequestDate = ParseExcelDate(row.NgayYcHoanThanh);
            trsi.SentToDepartment = true;

            allIndicators.Add(trsi);
            trsiList.Add((trsi, row, trsi.LaboratoryId));
            result.IndicatorCount++;
        }

        var labGroups = trsiList
            .Where(x => x.LabId.HasValue)
            .GroupBy(x => x.LabId!.Value)
            .ToList();

        var ltiMap = new Dictionary<Guid, LaboratoryTaskIndicator>();

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

            labTask.Status = LaboratoryTaskStatus.SentToQuality;

            allLabTasks.Add(labTask);
            result.LabTaskCount++;

            foreach (var item in groupItems)
            {
                var lti = new LaboratoryTaskIndicator(
                    _guidGenerator.Create(),
                    labTask.Id,
                    item.Trsi.Id);

                lti.ApprovalStatus = IndicatorApprovalStatus.Approved;
                lti.IsSentToQuality = true;
                lti.Result = item.Row.KetQua?.Trim();
                lti.Unit = item.Row.DonViTinh?.Trim();
                lti.EntryDate = ParseExcelDate(item.Row.NgayLuuKq);
                lti.ApprovalDate = ParseExcelDate(item.Row.NgayQlDuyet);

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

                if (technicalManagerId.HasValue)
                    lti.ApprovedById = technicalManagerId;

                allLabTaskIndicators.Add(lti);
                ltiMap[item.Trsi.Id] = lti;
            }
        }

        if (ltiMap.Count > 0)
        {
            var qmTask = new QualityManagementTask(
                _guidGenerator.Create(),
                testRequest.Id,
                contractNumber ?? "",
                QualityTaskType.ResultApproval,
                trsiList.Where(x => ltiMap.ContainsKey(x.Trsi.Id)).Select(x => x.Row.MaSoMau).Distinct().Count(),
                ltiMap.Count,
                phieuRows.Min(r => ParseExcelDate(r.NgayGuiQl)),
                phieuRows.Max(r => ParseExcelDate(r.NgayYcHoanThanh)));

            qmTask.Status = QualityManagementStatus.FullyPublished;

            allQmTasks.Add(qmTask);
            result.QmTaskCount++;

            foreach (var (trsiId, lti) in ltiMap)
            {
                var qmi = new QualityManagementIndicator(
                    _guidGenerator.Create(),
                    qmTask.Id,
                    lti.Id);

                qmi.ApprovalStatus = QualityIndicatorApprovalStatus.Approved;
                qmi.IsPrinted = true;

                var matchingRow = trsiList.FirstOrDefault(x => x.Trsi.Id == trsiId);
                qmi.PrintedDate = ParseExcelDate(matchingRow.Row?.NgayQlDuyet);
                qmi.ApprovalDate = ParseExcelDate(matchingRow.Row?.NgayQlDuyet);

                allQmIndicators.Add(qmi);
            }
        }

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
