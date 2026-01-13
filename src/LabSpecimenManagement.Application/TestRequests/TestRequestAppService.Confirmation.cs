using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common.Exceptions;
using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.Permissions;
using Microsoft.AspNetCore.Authorization;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// Partial class for BPGN Customer Confirmation workflow (per-indicator)
/// </summary>
public partial class TestRequestAppService
{
    #region Workflow Redesign - BPGN Confirmation (Per-Indicator)

    /// <summary>
    /// Lấy danh sách chỉ tiêu chờ KH xác nhận
    /// </summary>
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Default)]
    public async Task<List<IndicatorPendingConfirmationDto>> GetIndicatorsPendingConfirmationAsync(Guid id)
    {
        var testRequest = await Repository.GetAsync(id);

        // Validate: TR must be in WaitingCustomerConfirm status
        if (testRequest.Status != TestRequestStatus.WaitingCustomerConfirm)
        {
            return new List<IndicatorPendingConfirmationDto>();
        }

        var result = new List<IndicatorPendingConfirmationDto>();

        // Get all LaboratoryTasks of this TestRequest
        var tasks = await _laboratoryTaskRepository.GetListAsync(t => t.TestRequestId == id);

        foreach (var task in tasks)
        {
            // Get all indicators that need customer confirmation
            // Status: MethodChangeRequested, TestingRejected, or SupplementaryRequested AND not yet confirmed
            var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(
                ti => ti.LaboratoryTaskId == task.Id &&
                      !ti.CustomerConfirmationDate.HasValue && // Chưa được xác nhận
                      (ti.ApprovalStatus == IndicatorApprovalStatus.MethodChangeRequested ||
                       ti.ApprovalStatus == IndicatorApprovalStatus.TestingRejected ||
                       ti.ApprovalStatus == IndicatorApprovalStatus.SupplementaryRequested));

            foreach (var indicator in taskIndicators)
            {
                var dto = await MapToIndicatorPendingConfirmationDtoAsync(indicator, task);
                result.Add(dto);
            }
        }

        return result;
    }

    /// <summary>
    /// BPGN xác nhận từng chỉ tiêu (KH xác nhận đồng ý/không đồng ý)
    /// </summary>
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public async Task<IndicatorPendingConfirmationDto> ConfirmIndicatorAsync(
        Guid id,
        Guid indicatorId,
        ConfirmIndicatorDto input)
    {
        var testRequest = await Repository.GetAsync(id);

        // Validate: TR must be in WaitingCustomerConfirm status
        if (testRequest.Status != TestRequestStatus.WaitingCustomerConfirm)
        {
            throw new Common.Exceptions.InvalidOperationException(
                "Chỉ có thể xác nhận chỉ tiêu khi trạng thái là 'Chờ KH xác nhận'", "testRequest");
        }

        var taskIndicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicatorId);
        var task = await _laboratoryTaskRepository.GetAsync(taskIndicator.LaboratoryTaskId);

        // Validate: Task must belong to this TestRequest
        if (task.TestRequestId != id)
        {
            throw new ValidationException("indicatorId", "Chỉ tiêu không thuộc phiếu yêu cầu này");
        }

        // Process confirmation based on current status
        await ProcessIndicatorConfirmationAsync(taskIndicator, input.Confirmed);
        await _laboratoryTaskIndicatorRepository.UpdateAsync(taskIndicator);

        // Update task and TR status after confirmation
        await UpdateTaskAndTestRequestStatusAfterConfirmationAsync(task, testRequest);

        return await MapToIndicatorPendingConfirmationDtoAsync(taskIndicator, task);
    }

    /// <summary>
    /// BPGN xác nhận nhiều chỉ tiêu cùng lúc (KH xác nhận đồng ý/không đồng ý)
    /// </summary>
    [Authorize(LabSpecimenManagementPermissions.TestRequests.Edit)]
    public async Task<List<IndicatorPendingConfirmationDto>> BulkConfirmIndicatorsAsync(
        Guid id,
        BulkConfirmIndicatorsDto input)
    {
        var testRequest = await Repository.GetAsync(id);

        // Validate: TR must be in WaitingCustomerConfirm status
        if (testRequest.Status != TestRequestStatus.WaitingCustomerConfirm)
        {
            throw new Common.Exceptions.InvalidOperationException(
                "Chỉ có thể xác nhận chỉ tiêu khi trạng thái là 'Chờ KH xác nhận'", "testRequest");
        }

        if (!input.IndicatorIds.Any())
        {
            throw new ValidationException("indicatorIds", "Vui lòng chọn ít nhất một chỉ tiêu");
        }

        var results = new List<IndicatorPendingConfirmationDto>();
        var affectedTasks = new HashSet<Guid>();

        foreach (var indicatorId in input.IndicatorIds)
        {
            var taskIndicator = await _laboratoryTaskIndicatorRepository.FindAsync(indicatorId);
            if (taskIndicator == null) continue;

            var task = await _laboratoryTaskRepository.GetAsync(taskIndicator.LaboratoryTaskId);
            if (task.TestRequestId != id) continue;

            // Process confirmation
            await ProcessIndicatorConfirmationAsync(taskIndicator, input.Confirmed);
            await _laboratoryTaskIndicatorRepository.UpdateAsync(taskIndicator);

            affectedTasks.Add(task.Id);
            results.Add(await MapToIndicatorPendingConfirmationDtoAsync(taskIndicator, task));
        }

        // Update all affected tasks and TR status
        foreach (var taskId in affectedTasks)
        {
            var task = await _laboratoryTaskRepository.GetAsync(taskId);
            await UpdateTaskStatusAfterConfirmationAsync(task);
        }

        // Update TestRequest status
        await UpdateTestRequestStatusAfterConfirmationAsync(testRequest);

        return results;
    }

    #endregion

    #region Private Confirmation Helper Methods

    /// <summary>
    /// Xử lý xác nhận cho từng chỉ tiêu dựa trên trạng thái hiện tại
    /// </summary>
    private async Task ProcessIndicatorConfirmationAsync(LaboratoryTaskIndicator indicator, bool confirmed)
    {
        indicator.CustomerConfirmationDate = Clock.Now;
        indicator.CustomerConfirmedById = CurrentUser.Id;
        indicator.CustomerConfirmedByName = CurrentUser.Name ?? CurrentUser.UserName;

        switch (indicator.ApprovalStatus)
        {
            case IndicatorApprovalStatus.MethodChangeRequested:
                if (confirmed)
                {
                    // KH đồng ý đổi PP → cập nhật PP mới và chuyển sang MethodChanged
                    var latestHistory = await GetLatestMethodChangeHistoryAsync(indicator.Id);
                    if (latestHistory != null)
                    {
                        // Cập nhật PP mới cho indicator
                        indicator.MethodId = latestHistory.NewMethodId;

                        // Cập nhật history
                        latestHistory.Approve(CurrentUser.Id!.Value, CurrentUser.Name ?? CurrentUser.UserName ?? "");
                        await _methodChangeHistoryRepository.UpdateAsync(latestHistory);
                    }
                    indicator.ApprovalStatus = IndicatorApprovalStatus.MethodChanged;
                }
                else
                {
                    // KH không đồng ý đổi PP → CustomerCancelled (hủy chỉ tiêu)
                    var latestHistory = await GetLatestMethodChangeHistoryAsync(indicator.Id);
                    if (latestHistory != null)
                    {
                        latestHistory.Reject(CurrentUser.Id!.Value, CurrentUser.Name ?? CurrentUser.UserName ?? "");
                        await _methodChangeHistoryRepository.UpdateAsync(latestHistory);
                    }
                    indicator.ApprovalStatus = IndicatorApprovalStatus.CustomerCancelled;
                }
                break;

            case IndicatorApprovalStatus.TestingRejected:
                if (confirmed)
                {
                    // KH đồng ý từ chối TN → giữ nguyên TestingRejected
                    // (không thay đổi status, chỉ ghi nhận ngày xác nhận)
                }
                else
                {
                    // KH không đồng ý từ chối (vẫn muốn TN) → quay về Pending để QLKT đánh giá lại
                    // Clear rejection info
                    indicator.TestingRejectionReason = null;
                    indicator.TestingRejectionDate = null;
                    indicator.TestingRejectedById = null;
                    indicator.TestingRejectedByName = null;
                    indicator.ApprovalStatus = IndicatorApprovalStatus.Pending;
                }
                break;

            case IndicatorApprovalStatus.SupplementaryRequested:
                if (confirmed)
                {
                    // KH đồng ý cung cấp bổ sung → quay về Pending để tiếp tục TN
                    // (Giống như đổi PP được đồng ý - chỉ tiêu quay về trạng thái chờ TN)
                    // Clear supplementary request info
                    indicator.SupplementaryRequestReason = null;
                    indicator.SupplementaryRequestDate = null;
                    indicator.SupplementaryRequestedById = null;
                    indicator.SupplementaryRequestedByName = null;
                    indicator.ApprovalStatus = IndicatorApprovalStatus.Pending;
                }
                else
                {
                    // KH từ chối cung cấp bổ sung → hủy chỉ tiêu
                    indicator.ApprovalStatus = IndicatorApprovalStatus.CustomerCancelled;
                }
                break;
        }
    }

    /// <summary>
    /// Lấy lịch sử đổi PP mới nhất của chỉ tiêu (chưa được xác nhận)
    /// </summary>
    private async Task<MethodChangeHistory?> GetLatestMethodChangeHistoryAsync(Guid indicatorId)
    {
        var histories = await _methodChangeHistoryRepository.GetListAsync(
            h => h.IndicatorId == indicatorId && h.IsApproved == null);
        return histories.OrderByDescending(h => h.RequestedDate).FirstOrDefault();
    }

    /// <summary>
    /// Cập nhật trạng thái Task và TestRequest sau khi xác nhận
    /// </summary>
    private async Task UpdateTaskAndTestRequestStatusAfterConfirmationAsync(
        LaboratoryTask task,
        TestRequest testRequest)
    {
        await UpdateTaskStatusAfterConfirmationAsync(task);
        await UpdateTestRequestStatusAfterConfirmationAsync(testRequest);
    }

    /// <summary>
    /// Cập nhật trạng thái Task sau khi xác nhận
    /// </summary>
    private async Task UpdateTaskStatusAfterConfirmationAsync(LaboratoryTask task)
    {
        var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(
            ti => ti.LaboratoryTaskId == task.Id);

        // Loại bỏ các chỉ tiêu đã bị KH hủy
        var activeIndicators = taskIndicators
            .Where(ti => ti.ApprovalStatus != IndicatorApprovalStatus.CustomerCancelled)
            .ToList();

        // Kiểm tra còn chỉ tiêu chờ xác nhận không
        var hasPendingConfirmation = activeIndicators.Any(ti =>
            ti.ApprovalStatus == IndicatorApprovalStatus.MethodChangeRequested ||
            ti.ApprovalStatus == IndicatorApprovalStatus.SupplementaryRequested ||
            (ti.ApprovalStatus == IndicatorApprovalStatus.TestingRejected && !ti.CustomerConfirmationDate.HasValue));

        if (hasPendingConfirmation)
        {
            // Còn chỉ tiêu chờ xác nhận - giữ nguyên WaitingCustomerResponse
            return;
        }

        // Kiểm tra có chỉ tiêu quay về Pending không (KH không đồng ý từ chối)
        var hasPending = activeIndicators.Any(ti => ti.ApprovalStatus == IndicatorApprovalStatus.Pending);
        if (hasPending)
        {
            // Có chỉ tiêu quay về Pending → Task quay về UnderEvaluation để QLKT đánh giá lại
            task.Status = LaboratoryTaskStatus.UnderEvaluation;
            await _laboratoryTaskRepository.UpdateAsync(task);
            return;
        }

        // Tính toán trạng thái cuối cùng
        // Bao gồm cả Approved vì có thể một số chỉ tiêu đã hoàn thành trước đó
        var hasAccepted = activeIndicators.Any(ti =>
            ti.ApprovalStatus == IndicatorApprovalStatus.AcceptedForTesting ||
            ti.ApprovalStatus == IndicatorApprovalStatus.MethodChanged ||
            ti.ApprovalStatus == IndicatorApprovalStatus.Approved);
        var hasRejected = activeIndicators.Any(ti =>
            ti.ApprovalStatus == IndicatorApprovalStatus.TestingRejected);
        var allRejected = activeIndicators.All(ti =>
            ti.ApprovalStatus == IndicatorApprovalStatus.TestingRejected);

        if (allRejected || !activeIndicators.Any())
        {
            task.Status = LaboratoryTaskStatus.TestingRejected;
        }
        else if (hasAccepted && hasRejected)
        {
            task.Status = LaboratoryTaskStatus.PartialAccepted;
        }
        else if (hasAccepted)
        {
            task.Status = LaboratoryTaskStatus.FullyAccepted;
        }

        await _laboratoryTaskRepository.UpdateAsync(task);
    }

    /// <summary>
    /// Cập nhật trạng thái TestRequest sau khi xác nhận
    /// </summary>
    private async Task UpdateTestRequestStatusAfterConfirmationAsync(TestRequest testRequest)
    {
        // Lấy tất cả tasks của TestRequest này
        var tasks = await _laboratoryTaskRepository.GetListAsync(t => t.TestRequestId == testRequest.Id);

        // Kiểm tra có task nào còn chờ xác nhận không
        var hasWaitingCustomerResponse = tasks.Any(t => t.Status == LaboratoryTaskStatus.WaitingCustomerResponse);
        if (hasWaitingCustomerResponse)
        {
            // Còn task chờ xác nhận - giữ nguyên WaitingCustomerConfirm
            return;
        }

        // Kiểm tra có task nào quay về UnderEvaluation không
        var hasUnderEvaluation = tasks.Any(t => t.Status == LaboratoryTaskStatus.UnderEvaluation);
        if (hasUnderEvaluation)
        {
            // Có task quay về UnderEvaluation → TR quay về UnderReview
            testRequest.Status = TestRequestStatus.UnderReview;
            await Repository.UpdateAsync(testRequest);
            return;
        }

        // Tính toán trạng thái cuối cùng
        var allTasksRejected = tasks.All(t => t.Status == LaboratoryTaskStatus.TestingRejected);
        var allTasksProcessed = tasks.All(t =>
            t.Status == LaboratoryTaskStatus.FullyAccepted ||
            t.Status == LaboratoryTaskStatus.PartialAccepted ||
            t.Status == LaboratoryTaskStatus.TestingRejected);

        if (allTasksRejected)
        {
            testRequest.Status = TestRequestStatus.Cancelled;
        }
        else if (allTasksProcessed)
        {
            testRequest.Status = TestRequestStatus.RequestApproved;
        }

        // ===== THÊM MỚI: Recalculate TotalCost sau khi xác nhận =====
        // Chỉ tính lại nếu KHÔNG phải mẫu nhiệm vụ
        if (!testRequest.SampleTypeTask)
        {
            var oldTotalCost = testRequest.TotalCost;
            var newTotalCost = await RecalculateTotalCostExcludingCancelledAsync(testRequest.Id);

            if (oldTotalCost != newTotalCost)
            {
                // Lưu lịch sử thay đổi tiền
                await SaveCostChangeHistoryAsync(
                    testRequest.Id,
                    CostChangeType.IndicatorCancelled,
                    oldTotalCost,
                    newTotalCost,
                    reason: "KH hủy chỉ tiêu do không đồng ý thay đổi phương pháp");

                testRequest.TotalCost = newTotalCost;
            }
        }

        await Repository.UpdateAsync(testRequest);
    }

    /// <summary>
    /// Tính lại TotalCost, loại trừ các chỉ tiêu CustomerCancelled
    /// </summary>
    private async Task<decimal> RecalculateTotalCostExcludingCancelledAsync(Guid testRequestId)
    {
        decimal totalCost = 0;

        // Lấy tất cả samples không bị hủy
        var samples = await _sampleRepository.GetListAsync(s =>
            s.TestRequestId == testRequestId && !s.IsCustomerCancelled);

        // Lấy tất cả sample IDs
        var sampleIds = samples.Select(s => s.Id).ToList();

        // Lấy tất cả sample indicators một lần
        var allSampleIndicators = await _sampleIndicatorRepository.GetListAsync(si =>
            sampleIds.Contains(si.TestRequestSampleId));

        // Lấy tất cả sample indicator IDs
        var sampleIndicatorIds = allSampleIndicators.Select(si => si.Id).ToList();

        // Lấy tất cả lab task indicators một lần (tối ưu query)
        var allLabTaskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(lti =>
            sampleIndicatorIds.Contains(lti.TestRequestSampleIndicatorId));

        foreach (var sample in samples)
        {
            decimal sampleCost = 0;

            // Lấy sample indicators của sample này
            var sampleIndicators = allSampleIndicators.Where(si => si.TestRequestSampleId == sample.Id);

            foreach (var sampleIndicator in sampleIndicators)
            {
                // Kiểm tra LaboratoryTaskIndicator có bị CustomerCancelled không
                var labTaskIndicator = allLabTaskIndicators.FirstOrDefault(lti =>
                    lti.TestRequestSampleIndicatorId == sampleIndicator.Id);

                // Chỉ tính tiền nếu KHÔNG bị CustomerCancelled
                if (labTaskIndicator == null ||
                    labTaskIndicator.ApprovalStatus != IndicatorApprovalStatus.CustomerCancelled)
                {
                    sampleCost += sampleIndicator.UnitPrice;
                }
            }

            // Cập nhật sample cost nếu thay đổi
            if (sample.TotalIndicatorCost != sampleCost)
            {
                sample.TotalIndicatorCost = sampleCost;
                await _sampleRepository.UpdateAsync(sample);
            }

            totalCost += sampleCost;
        }

        return totalCost;
    }

    /// <summary>
    /// Lưu lịch sử thay đổi tiền
    /// </summary>
    private async Task SaveCostChangeHistoryAsync(
        Guid testRequestId,
        CostChangeType changeType,
        decimal oldTotalCost,
        decimal newTotalCost,
        Guid? affectedIndicatorId = null,
        string? affectedIndicatorName = null,
        decimal? indicatorUnitPrice = null,
        string? reason = null)
    {
        var history = new CostChangeHistory(
            GuidGenerator.Create(),
            testRequestId,
            changeType,
            oldTotalCost,
            newTotalCost,
            affectedIndicatorId,
            affectedIndicatorName,
            indicatorUnitPrice,
            reason);

        await _costChangeHistoryRepository.InsertAsync(history);
    }

    /// <summary>
    /// Map indicator to pending confirmation DTO
    /// </summary>
    private async Task<IndicatorPendingConfirmationDto> MapToIndicatorPendingConfirmationDtoAsync(
        LaboratoryTaskIndicator indicator,
        LaboratoryTask task)
    {
        // Get sample indicator info
        var sampleIndicator = await _sampleIndicatorRepository.FindAsync(indicator.TestRequestSampleIndicatorId);
        var sample = sampleIndicator != null
            ? await _sampleRepository.FindAsync(sampleIndicator.TestRequestSampleId)
            : null;
        var indicatorInfo = sampleIndicator != null
            ? await _indicatorRepository.FindAsync(sampleIndicator.IndicatorId)
            : null;

        // Get laboratory info
        var laboratory = await _laboratoryRepository.FindAsync(task.LaboratoryId);

        // Get method change history if applicable
        string? oldMethodName = null;
        string? newMethodName = null;
        string? methodChangeReason = null;
        string? requestedByName = null;
        DateTime? requestedDate = null;

        if (indicator.ApprovalStatus == IndicatorApprovalStatus.MethodChangeRequested)
        {
            var latestHistory = await GetLatestMethodChangeHistoryAsync(indicator.Id);
            if (latestHistory != null)
            {
                oldMethodName = latestHistory.OldMethodName;
                newMethodName = latestHistory.NewMethodName;
                methodChangeReason = latestHistory.Reason;
                requestedByName = latestHistory.RequestedByUserName;
                requestedDate = latestHistory.RequestedDate;
            }
        }
        else if (indicator.ApprovalStatus == IndicatorApprovalStatus.TestingRejected)
        {
            requestedByName = indicator.TestingRejectedByName;
            requestedDate = indicator.TestingRejectionDate;
        }
        else if (indicator.ApprovalStatus == IndicatorApprovalStatus.SupplementaryRequested)
        {
            requestedByName = indicator.SupplementaryRequestedByName;
            requestedDate = indicator.SupplementaryRequestDate;
        }

        return new IndicatorPendingConfirmationDto
        {
            Id = indicator.Id,
            SampleCode = sample?.SampleCode,
            SampleName = sample?.SampleName,
            IndicatorCode = indicatorInfo?.Code,
            IndicatorName = indicatorInfo?.Name,
            LaboratoryName = laboratory?.Name,
            ApprovalStatus = indicator.ApprovalStatus,
            TestingRejectionReason = indicator.TestingRejectionReason,
            OldMethodName = oldMethodName,
            NewMethodName = newMethodName,
            MethodChangeReason = methodChangeReason,
            RequestedByName = requestedByName,
            RequestedDate = requestedDate,
            // Supplementary request fields
            SupplementaryRequestReason = indicator.SupplementaryRequestReason,
            SupplementaryRequestDate = indicator.SupplementaryRequestDate,
            SupplementaryRequestedByName = indicator.SupplementaryRequestedByName
        };
    }

    #endregion
}
