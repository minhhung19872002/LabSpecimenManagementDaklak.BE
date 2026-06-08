using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common.Exceptions;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.TestRequests;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.LaboratoryTasks;

/// <summary>
/// Partial class for QLKT Evaluation workflow (per-indicator)
/// </summary>
public partial class LaboratoryTaskAppService
{
    #region Workflow Redesign - QLKT Evaluation (Per-Indicator)

    /// <summary>
    /// QLKT đánh giá từng chỉ tiêu (Accept/RequestMethodChange/Reject)
    /// Khi evaluate chỉ tiêu cha, tự động áp dụng cho tất cả chỉ tiêu phụ trong cùng group (cùng sample)
    /// </summary>
    [Authorize]
    public async Task<LaboratoryTaskIndicatorDto> EvaluateIndicatorAsync(Guid indicatorId, EvaluateIndicatorDto input)
    {
        var taskIndicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicatorId);
        var task = await _laboratoryTaskRepository.GetAsync(taskIndicator.LaboratoryTaskId);

        // Get sample indicator to find indicator info
        var sampleIndicator = await _sampleIndicatorRepository.GetAsync(taskIndicator.TestRequestSampleIndicatorId);
        var indicator = await _indicatorRepository.GetAsync(sampleIndicator.IndicatorId);

        // Find sub-indicators if this is a parent indicator
        var subTaskIndicators = new List<LaboratoryTaskIndicator>();
        if (indicator.ParentIndicatorId == null)
        {
            // This is a parent indicator - find all sub-indicators in master data
            var subIndicatorIds = await AsyncExecuter.ToListAsync(
                (await _indicatorRepository.GetQueryableAsync())
                    .Where(i => i.ParentIndicatorId == indicator.Id)
                    .Select(i => i.Id)
            );

            if (subIndicatorIds.Any())
            {
                // Find all task indicators that are sub-indicators in the same task and sample
                var allTaskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(
                    ti => ti.LaboratoryTaskId == task.Id);

                foreach (var ti in allTaskIndicators)
                {
                    if (ti.Id == taskIndicator.Id) continue; // Skip the parent

                    var si = await _sampleIndicatorRepository.FindAsync(ti.TestRequestSampleIndicatorId);
                    if (si != null &&
                        si.TestRequestSampleId == sampleIndicator.TestRequestSampleId && // Same sample
                        subIndicatorIds.Contains(si.IndicatorId)) // Is a sub-indicator
                    {
                        subTaskIndicators.Add(ti);
                    }
                }
            }
        }

        // Validate: Only QLKT or supporter can evaluate
        if (!await CanManageTaskAsync(task))
        {
            throw new ForbiddenAccessException("Bạn không có quyền đánh giá chỉ tiêu");
        }

        // Validate: Task status and indicator status
        // Cho phép đánh giá khi:
        // 1. Task đang ở trạng thái UnderEvaluation hoặc WaitingCustomerResponse (flow bình thường)
        // 2. HOẶC Task đã ở trạng thái khác (FullyAccepted, PartialAccepted, Assigned, InProgress, WaitingApproval, SentToQuality)
        //    NHƯNG indicator vẫn còn Pending (trường hợp gửi QLKT sau hoặc một số chỉ tiêu chưa được duyệt)
        var isNormalEvaluationStatus = task.Status == LaboratoryTaskStatus.UnderEvaluation ||
                                       task.Status == LaboratoryTaskStatus.WaitingCustomerResponse ||
                                       task.Status == LaboratoryTaskStatus.WaitingSharedSample;

        var isLateIndicatorCase = (task.Status == LaboratoryTaskStatus.FullyAccepted ||
                                   task.Status == LaboratoryTaskStatus.PartialAccepted ||
                                   task.Status == LaboratoryTaskStatus.Assigned ||
                                   task.Status == LaboratoryTaskStatus.InProgress ||
                                   task.Status == LaboratoryTaskStatus.WaitingApproval ||
                                   task.Status == LaboratoryTaskStatus.SentToQuality) &&
                                  (taskIndicator.ApprovalStatus == IndicatorApprovalStatus.Pending ||
                                   taskIndicator.ApprovalStatus == IndicatorApprovalStatus.CannotTest);

        if (!isNormalEvaluationStatus && !isLateIndicatorCase)
        {
            throw new Common.Exceptions.InvalidOperationException(
                "Chỉ có thể đánh giá chỉ tiêu khi task đang ở trạng thái cho phép hoặc chỉ tiêu đang chờ đánh giá", "task");
        }

        // Validate: Indicator must be in Pending or CannotTest status
        // Riêng RequestSupplementary chỉ cho phép khi CannotTest
        if (input.Action == IndicatorEvaluationAction.RequestSupplementary)
        {
            if (taskIndicator.ApprovalStatus != IndicatorApprovalStatus.CannotTest)
            {
                throw new Common.Exceptions.InvalidOperationException(
                    "Chỉ có thể yêu cầu bổ sung khi chỉ tiêu ở trạng thái 'Không TN được'", "indicator");
            }
        }
        else if (taskIndicator.ApprovalStatus != IndicatorApprovalStatus.Pending &&
                 taskIndicator.ApprovalStatus != IndicatorApprovalStatus.CannotTest)
        {
            if (taskIndicator.ApprovalStatus != IndicatorApprovalStatus.AcceptedForTesting &&
                taskIndicator.ApprovalStatus != IndicatorApprovalStatus.CannotTest)
            {
                throw new Common.Exceptions.InvalidOperationException(
                    "Chỉ có thể yêu cầu bổ sung cho chỉ tiêu đã chấp nhận TN hoặc không TN được", "indicator");
            }
        }
        else
        {
            if (taskIndicator.ApprovalStatus != IndicatorApprovalStatus.Pending &&
                taskIndicator.ApprovalStatus != IndicatorApprovalStatus.CannotTest)
            {
                throw new Common.Exceptions.InvalidOperationException("Chỉ tiêu đã được đánh giá hoặc không ở trạng thái chờ đánh giá", "indicator");
            }
        }

        // Apply action to parent indicator
        switch (input.Action)
        {
            case IndicatorEvaluationAction.Accept:
                await AcceptIndicatorForTestingAsync(taskIndicator);
                // Also accept all sub-indicators
                foreach (var subIndicator in subTaskIndicators)
                {
                    if (subIndicator.ApprovalStatus == IndicatorApprovalStatus.Pending ||
                        subIndicator.ApprovalStatus == IndicatorApprovalStatus.CannotTest)
                    {
                        await AcceptIndicatorForTestingAsync(subIndicator);
                    }
                }
                break;

            case IndicatorEvaluationAction.RequestMethodChange:
                if (!input.NewMethodId.HasValue)
                {
                    throw new ValidationException("newMethodId", "Vui lòng chọn phương pháp mới");
                }
                if (string.IsNullOrWhiteSpace(input.Reason))
                {
                    throw new ValidationException("reason", "Vui lòng nhập lý do đổi phương pháp");
                }
                await RequestMethodChangeForIndicatorAsync(taskIndicator, input.NewMethodId.Value, input.Reason);
                // Also request method change for all sub-indicators
                foreach (var subIndicator in subTaskIndicators)
                {
                    if (subIndicator.ApprovalStatus == IndicatorApprovalStatus.Pending ||
                        subIndicator.ApprovalStatus == IndicatorApprovalStatus.CannotTest)
                    {
                        await RequestMethodChangeForIndicatorAsync(subIndicator, input.NewMethodId.Value, input.Reason);
                    }
                }
                break;

            case IndicatorEvaluationAction.Reject:
                if (string.IsNullOrWhiteSpace(input.Reason))
                {
                    throw new ValidationException("reason", "Vui lòng nhập lý do từ chối");
                }
                await RejectIndicatorForTestingAsync(taskIndicator, input.Reason);
                // Also reject all sub-indicators
                foreach (var subIndicator in subTaskIndicators)
                {
                    if (subIndicator.ApprovalStatus == IndicatorApprovalStatus.Pending ||
                        subIndicator.ApprovalStatus == IndicatorApprovalStatus.CannotTest)
                    {
                        await RejectIndicatorForTestingAsync(subIndicator, input.Reason);
                    }
                }
                break;

            case IndicatorEvaluationAction.RequestSupplementary:
                if (string.IsNullOrWhiteSpace(input.Reason))
                {
                    throw new ValidationException("reason", "Vui lòng nhập nội dung yêu cầu bổ sung");
                }
                RequestSupplementaryForIndicator(taskIndicator, input.Reason);
                // Also request supplementary for all sub-indicators
                foreach (var subIndicator in subTaskIndicators)
                {
                    if (subIndicator.ApprovalStatus == IndicatorApprovalStatus.CannotTest)
                    {
                        RequestSupplementaryForIndicator(subIndicator, input.Reason);
                    }
                }
                break;

            default:
                throw new ValidationException("action", "Hành động không hợp lệ");
        }

        // Update parent indicator
        await _laboratoryTaskIndicatorRepository.UpdateAsync(taskIndicator, autoSave: true);

        // Update all sub-indicators
        foreach (var subIndicator in subTaskIndicators)
        {
            await _laboratoryTaskIndicatorRepository.UpdateAsync(subIndicator, autoSave: true);
        }

        // Check and update task/TR status after evaluation
        await UpdateTaskAndTestRequestStatusAfterEvaluationAsync(task);

        return await MapToIndicatorDtoWithHistoryAsync(taskIndicator);
    }

    /// <summary>
    /// QLKT yêu cầu đổi phương pháp cho nhiều chỉ tiêu cùng lúc
    /// </summary>
    [Authorize]
    public async Task<List<LaboratoryTaskIndicatorDto>> RequestMethodChangeAsync(Guid taskId, RequestMethodChangeDto input)
    {
        var task = await _laboratoryTaskRepository.GetAsync(taskId);

        // Validate: Only QLKT or supporter can request method change
        if (!await CanManageTaskAsync(task))
        {
            throw new ForbiddenAccessException("Bạn không có quyền yêu cầu đổi phương pháp");
        }

        if (!input.Items.Any())
        {
            throw new ValidationException("items", "Vui lòng chọn ít nhất một chỉ tiêu");
        }

        // Get indicators to check their status
        var indicatorIds = input.Items.Select(i => i.IndicatorId).ToList();
        var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => indicatorIds.Contains(x.Id));
        var hasPendingIndicators = taskIndicators.Any(ti => ti.ApprovalStatus == IndicatorApprovalStatus.Pending ||
                                                            ti.ApprovalStatus == IndicatorApprovalStatus.CannotTest);

        // Validate: Task status and indicator status
        var isNormalEvaluationStatus = task.Status == LaboratoryTaskStatus.UnderEvaluation ||
                                       task.Status == LaboratoryTaskStatus.WaitingCustomerResponse ||
                                       task.Status == LaboratoryTaskStatus.WaitingSharedSample;

        var isLateIndicatorCase = (task.Status == LaboratoryTaskStatus.FullyAccepted ||
                                   task.Status == LaboratoryTaskStatus.PartialAccepted ||
                                   task.Status == LaboratoryTaskStatus.Assigned ||
                                   task.Status == LaboratoryTaskStatus.SentToQuality) &&
                                  hasPendingIndicators;

        if (!isNormalEvaluationStatus && !isLateIndicatorCase)
        {
            throw new Common.Exceptions.InvalidOperationException(
                "Chỉ có thể yêu cầu đổi phương pháp khi task đang ở trạng thái cho phép hoặc chỉ tiêu đang chờ đánh giá", "task");
        }

        if (string.IsNullOrWhiteSpace(input.Reason))
        {
            throw new ValidationException("reason", "Vui lòng nhập lý do đổi phương pháp");
        }

        var results = new List<LaboratoryTaskIndicatorDto>();

        foreach (var item in input.Items)
        {
            var taskIndicator = await _laboratoryTaskIndicatorRepository.FindAsync(item.IndicatorId);
            if (taskIndicator == null || taskIndicator.LaboratoryTaskId != taskId)
            {
                continue;
            }

            // Validate: Indicator must be in Pending or CannotTest status
            if (taskIndicator.ApprovalStatus != IndicatorApprovalStatus.Pending &&
                taskIndicator.ApprovalStatus != IndicatorApprovalStatus.CannotTest)
            {
                continue;
            }

            await RequestMethodChangeForIndicatorAsync(taskIndicator, item.NewMethodId, input.Reason);
            await _laboratoryTaskIndicatorRepository.UpdateAsync(taskIndicator, autoSave: true);
            results.Add(await MapToIndicatorDtoWithHistoryAsync(taskIndicator));
        }

        // Check and update task/TR status after evaluation
        await UpdateTaskAndTestRequestStatusAfterEvaluationAsync(task);

        return results;
    }

    /// <summary>
    /// QLKT từ chối thử nghiệm cho nhiều chỉ tiêu cùng lúc
    /// </summary>
    [Authorize]
    public async Task<List<LaboratoryTaskIndicatorDto>> RejectIndicatorsForTestingAsync(Guid taskId, RejectIndicatorsForTestingDto input)
    {
        var task = await _laboratoryTaskRepository.GetAsync(taskId);

        // Validate: Only QLKT or supporter can reject
        if (!await CanManageTaskAsync(task))
        {
            throw new ForbiddenAccessException("Bạn không có quyền từ chối thử nghiệm");
        }

        if (!input.IndicatorIds.Any())
        {
            throw new ValidationException("indicatorIds", "Vui lòng chọn ít nhất một chỉ tiêu");
        }

        // Get indicators to check their status
        var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => input.IndicatorIds.Contains(x.Id));
        var hasPendingIndicators = taskIndicators.Any(ti => ti.ApprovalStatus == IndicatorApprovalStatus.Pending ||
                                                            ti.ApprovalStatus == IndicatorApprovalStatus.CannotTest);

        // Validate: Task status and indicator status
        var isNormalEvaluationStatus = task.Status == LaboratoryTaskStatus.UnderEvaluation ||
                                       task.Status == LaboratoryTaskStatus.WaitingCustomerResponse ||
                                       task.Status == LaboratoryTaskStatus.WaitingSharedSample;

        var isLateIndicatorCase = (task.Status == LaboratoryTaskStatus.FullyAccepted ||
                                   task.Status == LaboratoryTaskStatus.PartialAccepted ||
                                   task.Status == LaboratoryTaskStatus.Assigned ||
                                   task.Status == LaboratoryTaskStatus.SentToQuality) &&
                                  hasPendingIndicators;

        if (!isNormalEvaluationStatus && !isLateIndicatorCase)
        {
            throw new Common.Exceptions.InvalidOperationException(
                "Chỉ có thể từ chối thử nghiệm khi task đang ở trạng thái cho phép hoặc chỉ tiêu đang chờ đánh giá", "task");
        }

        if (string.IsNullOrWhiteSpace(input.Reason))
        {
            throw new ValidationException("reason", "Vui lòng nhập lý do từ chối");
        }

        var results = new List<LaboratoryTaskIndicatorDto>();

        foreach (var indicatorId in input.IndicatorIds)
        {
            var taskIndicator = await _laboratoryTaskIndicatorRepository.FindAsync(indicatorId);
            if (taskIndicator == null || taskIndicator.LaboratoryTaskId != taskId)
            {
                continue;
            }

            // Validate: Indicator must be in Pending or CannotTest status
            if (taskIndicator.ApprovalStatus != IndicatorApprovalStatus.Pending &&
                taskIndicator.ApprovalStatus != IndicatorApprovalStatus.CannotTest)
            {
                continue;
            }

            await RejectIndicatorForTestingAsync(taskIndicator, input.Reason);
            await _laboratoryTaskIndicatorRepository.UpdateAsync(taskIndicator, autoSave: true);
            results.Add(await MapToIndicatorDtoWithHistoryAsync(taskIndicator));
        }

        // Check and update task/TR status after evaluation
        await UpdateTaskAndTestRequestStatusAfterEvaluationAsync(task);

        return results;
    }

    /// <summary>
    /// QLKT duyệt tất cả chỉ tiêu còn lại (status = Pending) của task
    /// Khi approve parent indicator, tự động approve tất cả sub-indicators trong cùng group
    /// </summary>
    [Authorize]
    public async Task<LaboratoryTaskDto> ApproveAllRemainingIndicatorsAsync(Guid taskId)
    {
        var task = await _laboratoryTaskRepository.GetAsync(taskId);

        // Validate: Only QLKT or supporter can approve
        if (!await CanManageTaskAsync(task))
        {
            throw new ForbiddenAccessException("Bạn không có quyền duyệt yêu cầu");
        }

        // Get all indicators with Pending status first (needed for validation)
        var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => x.LaboratoryTaskId == taskId);
        var pendingIndicators = taskIndicators.Where(ti => ti.ApprovalStatus == IndicatorApprovalStatus.Pending).ToList();

        if (!pendingIndicators.Any())
        {
            throw new Common.Exceptions.InvalidOperationException("Không có chỉ tiêu nào đang chờ đánh giá", "indicator");
        }

        // Validate: Task status and pending indicators
        // Cho phép duyệt khi:
        // 1. Task đang ở trạng thái UnderEvaluation hoặc WaitingCustomerResponse (flow bình thường)
        // 2. HOẶC Task đã ở trạng thái khác (FullyAccepted, PartialAccepted, Assigned, SentToQuality)
        //    NHƯNG vẫn còn indicators Pending (trường hợp gửi QLKT sau)
        var isNormalEvaluationStatus = task.Status == LaboratoryTaskStatus.UnderEvaluation ||
                                       task.Status == LaboratoryTaskStatus.WaitingCustomerResponse ||
                                       task.Status == LaboratoryTaskStatus.WaitingSharedSample;

        var isLateIndicatorCase = (task.Status == LaboratoryTaskStatus.FullyAccepted ||
                                   task.Status == LaboratoryTaskStatus.PartialAccepted ||
                                   task.Status == LaboratoryTaskStatus.Assigned ||
                                   task.Status == LaboratoryTaskStatus.SentToQuality) &&
                                  pendingIndicators.Any();

        if (!isNormalEvaluationStatus && !isLateIndicatorCase)
        {
            throw new Common.Exceptions.InvalidOperationException(
                "Chỉ có thể duyệt yêu cầu khi task đang ở trạng thái cho phép hoặc có chỉ tiêu đang chờ đánh giá", "task");
        }

        // Get all sample indicators and master indicators for parent/sub detection
        var sampleIndicatorIds = pendingIndicators.Select(ti => ti.TestRequestSampleIndicatorId).Distinct().ToList();
        var sampleIndicators = await _sampleIndicatorRepository.GetListAsync(si => sampleIndicatorIds.Contains(si.Id));
        var indicatorIds = sampleIndicators.Select(si => si.IndicatorId).Distinct().ToList();
        var indicators = await _indicatorRepository.GetListAsync(i => indicatorIds.Contains(i.Id));

        // Filter to get only parent indicators (not sub-indicators)
        var parentPendingIndicators = new List<LaboratoryTaskIndicator>();
        foreach (var taskIndicator in pendingIndicators)
        {
            var sampleIndicator = sampleIndicators.FirstOrDefault(si => si.Id == taskIndicator.TestRequestSampleIndicatorId);
            if (sampleIndicator == null) continue;

            var masterIndicator = indicators.FirstOrDefault(i => i.Id == sampleIndicator.IndicatorId);
            if (masterIndicator == null) continue;

            // Only include if it's a parent indicator (no ParentIndicatorId)
            if (masterIndicator.ParentIndicatorId == null)
            {
                parentPendingIndicators.Add(taskIndicator);
            }
        }

        // Accept all parent pending indicators and their sub-indicators
        foreach (var parentIndicator in parentPendingIndicators)
        {
            // Accept parent
            await AcceptIndicatorForTestingAsync(parentIndicator);
            await _laboratoryTaskIndicatorRepository.UpdateAsync(parentIndicator, autoSave: true);

            // Find and accept sub-indicators
            var parentSampleIndicator = sampleIndicators.FirstOrDefault(si => si.Id == parentIndicator.TestRequestSampleIndicatorId);
            if (parentSampleIndicator == null) continue;

            var parentMasterIndicator = indicators.FirstOrDefault(i => i.Id == parentSampleIndicator.IndicatorId);
            if (parentMasterIndicator == null) continue;

            // Find sub-indicators in master data
            var subMasterIndicatorIds = indicators
                .Where(i => i.ParentIndicatorId == parentMasterIndicator.Id)
                .Select(i => i.Id)
                .ToList();

            if (!subMasterIndicatorIds.Any()) continue;

            // Find task indicators that are sub-indicators in the same sample
            foreach (var taskIndicator in pendingIndicators)
            {
                if (taskIndicator.Id == parentIndicator.Id) continue;
                if (taskIndicator.ApprovalStatus != IndicatorApprovalStatus.Pending) continue;

                var si = sampleIndicators.FirstOrDefault(s => s.Id == taskIndicator.TestRequestSampleIndicatorId);
                if (si == null) continue;

                // Same sample and is a sub-indicator
                if (si.TestRequestSampleId == parentSampleIndicator.TestRequestSampleId &&
                    subMasterIndicatorIds.Contains(si.IndicatorId))
                {
                    await AcceptIndicatorForTestingAsync(taskIndicator);
                    await _laboratoryTaskIndicatorRepository.UpdateAsync(taskIndicator, autoSave: true);
                }
            }
        }

        // Check and update task/TR status after evaluation
        await UpdateTaskAndTestRequestStatusAfterEvaluationAsync(task);

        return await MapToDetailDtoAsync(task);
    }

    #endregion

    #region Private Evaluation Helper Methods

    /// <summary>
    /// Chấp nhận chỉ tiêu cho thử nghiệm
    /// ApprovalDate không được set ở đây - chỉ set khi duyệt kết quả (Approved)
    /// </summary>
    private Task AcceptIndicatorForTestingAsync(LaboratoryTaskIndicator indicator)
    {
        indicator.ApprovalStatus = IndicatorApprovalStatus.AcceptedForTesting;
        // Không set ApprovalDate - chỉ set khi QLKT duyệt kết quả (Approved)
        indicator.ApprovedById = CurrentUser.Id;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Yêu cầu đổi phương pháp cho chỉ tiêu
    /// </summary>
    private async Task RequestMethodChangeForIndicatorAsync(
        LaboratoryTaskIndicator indicator,
        Guid newMethodId,
        string reason)
    {
        // Get current method name
        string? oldMethodName = null;
        if (indicator.MethodId.HasValue)
        {
            var oldMethod = await _testMethodRepository.FindAsync(indicator.MethodId.Value);
            oldMethodName = oldMethod?.Name;
        }

        // Get new method name
        var newMethod = await _testMethodRepository.GetAsync(newMethodId);

        // Create method change history
        var history = new MethodChangeHistory(
            GuidGenerator.Create(),
            indicator.Id,
            indicator.MethodId,
            oldMethodName,
            newMethodId,
            newMethod.Name,
            reason,
            Clock.Now,
            CurrentUser.Id!.Value,
            CurrentUser.Name ?? CurrentUser.UserName ?? ""
        );
        await _methodChangeHistoryRepository.InsertAsync(history);

        // Update indicator status
        indicator.ApprovalStatus = IndicatorApprovalStatus.MethodChangeRequested;
        // Không set ApprovalDate - chỉ set khi QLKT duyệt kết quả (Approved)
        indicator.ApprovedById = CurrentUser.Id;
    }

    /// <summary>
    /// Từ chối chỉ tiêu cho thử nghiệm (chờ KH xác nhận)
    /// </summary>
    private Task RejectIndicatorForTestingAsync(LaboratoryTaskIndicator indicator, string reason)
    {
        indicator.ApprovalStatus = IndicatorApprovalStatus.TestingRejected;
        indicator.TestingRejectionReason = reason;
        indicator.TestingRejectionDate = Clock.Now;
        indicator.TestingRejectedById = CurrentUser.Id;
        indicator.TestingRejectedByName = CurrentUser.Name ?? CurrentUser.UserName;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Yêu cầu KH cung cấp thông tin bổ sung (chờ KH xác nhận)
    /// Chỉ áp dụng khi chỉ tiêu đang ở trạng thái CannotTest
    /// </summary>
    private void RequestSupplementaryForIndicator(LaboratoryTaskIndicator indicator, string reason)
    {
        indicator.ApprovalStatus = IndicatorApprovalStatus.SupplementaryRequested;
        indicator.SupplementaryRequestReason = reason;
        indicator.SupplementaryRequestDate = Clock.Now;
        indicator.SupplementaryRequestedById = CurrentUser.Id;
        indicator.SupplementaryRequestedByName = CurrentUser.Name ?? CurrentUser.UserName;
    }

    /// <summary>
    /// Cập nhật trạng thái Task và TestRequest sau khi QLKT đánh giá
    /// Logic:
    /// - Nếu có chỉ tiêu cần KH xác nhận (MethodChangeRequested/TestingRejected) → WaitingCustomerResponse
    /// - Nếu tất cả đã được xử lý và accepted → FullyAccepted
    /// - Nếu một phần accepted → PartialAccepted
    /// - Nếu còn Pending → giữ UnderEvaluation (chỉ khi không có chỉ tiêu cần KH xác nhận)
    /// </summary>
    private async Task UpdateTaskAndTestRequestStatusAfterEvaluationAsync(LaboratoryTask task)
    {
        var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => x.LaboratoryTaskId == task.Id);

        // Loại bỏ các chỉ tiêu đã bị KH hủy
        var activeIndicators = taskIndicators
            .Where(ti => ti.ApprovalStatus != IndicatorApprovalStatus.CustomerCancelled)
            .ToList();

        if (!activeIndicators.Any())
        {
            return;
        }

        // Kiểm tra có chỉ tiêu cần KH xác nhận không (ưu tiên cao nhất)
        var hasMethodChangeRequested = activeIndicators.Any(ti =>
            ti.ApprovalStatus == IndicatorApprovalStatus.MethodChangeRequested);
        var hasTestingRejected = activeIndicators.Any(ti =>
            ti.ApprovalStatus == IndicatorApprovalStatus.TestingRejected);
        var hasSupplementaryRequested = activeIndicators.Any(ti =>
            ti.ApprovalStatus == IndicatorApprovalStatus.SupplementaryRequested);

        // Có chỉ tiêu cần KH xác nhận (đổi PP, từ chối, hoặc YC bổ sung) → chuyển ngay sang WaitingCustomerResponse
        if (hasMethodChangeRequested || hasTestingRejected || hasSupplementaryRequested)
        {
            task.Status = LaboratoryTaskStatus.WaitingCustomerResponse;
            await _laboratoryTaskRepository.UpdateAsync(task);

            // Cập nhật TestRequest sang WaitingCustomerConfirm
            await UpdateTestRequestToWaitingCustomerConfirmAsync(task.TestRequestId);
            return;
        }

        // Kiểm tra còn chỉ tiêu cần đánh giá không (Pending hoặc CannotTest)
        var hasPendingOrCannotTest = activeIndicators.Any(ti =>
            ti.ApprovalStatus == IndicatorApprovalStatus.Pending ||
            ti.ApprovalStatus == IndicatorApprovalStatus.CannotTest);

        if (hasPendingOrCannotTest)
        {
            // Còn chỉ tiêu chờ đánh giá và không có chỉ tiêu cần KH xác nhận - giữ nguyên UnderEvaluation
            return;
        }

        // Tất cả chỉ tiêu đã được xử lý - kiểm tra để chuyển trạng thái
        // Bao gồm cả Approved (đã duyệt KQ) vì có thể một số chỉ tiêu đã hoàn thành trước đó
        var hasAccepted = activeIndicators.Any(ti =>
            ti.ApprovalStatus == IndicatorApprovalStatus.AcceptedForTesting ||
            ti.ApprovalStatus == IndicatorApprovalStatus.MethodChanged ||
            ti.ApprovalStatus == IndicatorApprovalStatus.Approved);
        var allAccepted = activeIndicators.All(ti =>
            ti.ApprovalStatus == IndicatorApprovalStatus.AcceptedForTesting ||
            ti.ApprovalStatus == IndicatorApprovalStatus.MethodChanged ||
            ti.ApprovalStatus == IndicatorApprovalStatus.Approved);

        // Tất cả chỉ tiêu được chấp nhận
        if (allAccepted)
        {
            task.Status = LaboratoryTaskStatus.FullyAccepted;
            await _laboratoryTaskRepository.UpdateAsync(task);

            // Kiểm tra tất cả phòng đã xử lý xong chưa
            await TryUpdateTestRequestToRequestApprovedAsync(task.TestRequestId);
            return;
        }

        // Một phần chấp nhận, một phần bị từ chối (đã có KH xác nhận từ trước)
        if (hasAccepted)
        {
            task.Status = LaboratoryTaskStatus.PartialAccepted;
            await _laboratoryTaskRepository.UpdateAsync(task);

            // Kiểm tra tất cả phòng đã xử lý xong chưa
            await TryUpdateTestRequestToRequestApprovedAsync(task.TestRequestId);
        }
    }

    /// <summary>
    /// Cập nhật TestRequest sang WaitingCustomerConfirm
    /// </summary>
    private async Task UpdateTestRequestToWaitingCustomerConfirmAsync(Guid testRequestId)
    {
        var testRequest = await _testRequestRepository.GetAsync(testRequestId);

        // Cập nhật khi đang ở UnderReview, RequestApproved,
        // hoặc các trạng thái ban đầu khi gửi QLKT một phần (partial send)
        if (testRequest.Status == TestRequestStatus.UnderReview ||
            testRequest.Status == TestRequestStatus.RequestApproved ||
            testRequest.Status == TestRequestStatus.New ||
            testRequest.Status == TestRequestStatus.Paid ||
            testRequest.Status == TestRequestStatus.PendingPayment ||
            testRequest.Status == TestRequestStatus.NoPaymentRequired)
        {
            testRequest.Status = TestRequestStatus.WaitingCustomerConfirm;
            await _testRequestRepository.UpdateAsync(testRequest);
        }
    }

    /// <summary>
    /// Kiểm tra và cập nhật TestRequest sang RequestApproved nếu tất cả phòng đã xử lý xong
    /// </summary>
    private async Task TryUpdateTestRequestToRequestApprovedAsync(Guid testRequestId)
    {
        var testRequest = await _testRequestRepository.GetAsync(testRequestId);

        // Chỉ cập nhật khi đang ở UnderReview, WaitingCustomerConfirm,
        // hoặc các trạng thái ban đầu khi gửi QLKT một phần (partial send)
        if (testRequest.Status != TestRequestStatus.UnderReview &&
            testRequest.Status != TestRequestStatus.WaitingCustomerConfirm &&
            testRequest.Status != TestRequestStatus.New &&
            testRequest.Status != TestRequestStatus.Paid &&
            testRequest.Status != TestRequestStatus.PendingPayment &&
            testRequest.Status != TestRequestStatus.NoPaymentRequired)
        {
            return;
        }

        // Lấy tất cả tasks của TestRequest này
        var tasks = await _laboratoryTaskRepository.GetListAsync(t => t.TestRequestId == testRequestId);

        // Kiểm tra còn task nào đang chờ KH xác nhận không
        var hasWaitingCustomerResponse = tasks.Any(t => t.Status == LaboratoryTaskStatus.WaitingCustomerResponse);
        if (hasWaitingCustomerResponse)
        {
            // Còn task chờ KH xác nhận - không được chuyển sang RequestApproved
            // Giữ nguyên WaitingCustomerConfirm
            if (testRequest.Status != TestRequestStatus.WaitingCustomerConfirm)
            {
                testRequest.Status = TestRequestStatus.WaitingCustomerConfirm;
                await _testRequestRepository.UpdateAsync(testRequest);
            }
            return;
        }

        // Kiểm tra tất cả phòng đã xử lý xong chưa
        // Phòng "đã xử lý xong" = FullyAccepted, PartialAccepted, hoặc TestingRejected
        var allTasksProcessed = tasks.All(t =>
            t.Status == LaboratoryTaskStatus.FullyAccepted ||
            t.Status == LaboratoryTaskStatus.PartialAccepted ||
            t.Status == LaboratoryTaskStatus.TestingRejected);

        if (!allTasksProcessed)
        {
            return; // Còn phòng chưa xử lý xong
        }

        // Kiểm tra nếu TẤT CẢ phòng đều bị từ chối → TR bị hủy
        var allTasksRejected = tasks.All(t => t.Status == LaboratoryTaskStatus.TestingRejected);
        if (allTasksRejected)
        {
            testRequest.Status = TestRequestStatus.Cancelled;
            await _testRequestRepository.UpdateAsync(testRequest);
            return;
        }

        // Có ít nhất 1 phòng chấp nhận → TR được duyệt yêu cầu
        testRequest.Status = TestRequestStatus.RequestApproved;
        testRequest.RequestApprovedDate = DateTime.Now;
        await _testRequestRepository.UpdateAsync(testRequest);
    }

    /// <summary>
    /// Map indicator to DTO with method change history
    /// </summary>
    private async Task<LaboratoryTaskIndicatorDto> MapToIndicatorDtoWithHistoryAsync(LaboratoryTaskIndicator taskIndicator)
    {
        var dto = await MapToIndicatorDtoAsync(taskIndicator);

        // Add workflow redesign fields
        dto.TestingRejectionReason = taskIndicator.TestingRejectionReason;
        dto.TestingRejectionDate = taskIndicator.TestingRejectionDate;
        dto.TestingRejectedById = taskIndicator.TestingRejectedById;
        dto.TestingRejectedByName = taskIndicator.TestingRejectedByName;
        dto.CustomerConfirmationDate = taskIndicator.CustomerConfirmationDate;
        dto.CustomerConfirmedById = taskIndicator.CustomerConfirmedById;
        dto.CustomerConfirmedByName = taskIndicator.CustomerConfirmedByName;

        // Add supplementary request fields
        dto.SupplementaryRequestReason = taskIndicator.SupplementaryRequestReason;
        dto.SupplementaryRequestDate = taskIndicator.SupplementaryRequestDate;
        dto.SupplementaryRequestedById = taskIndicator.SupplementaryRequestedById;
        dto.SupplementaryRequestedByName = taskIndicator.SupplementaryRequestedByName;

        // Get method change histories
        var histories = await _methodChangeHistoryRepository.GetListAsync(h => h.IndicatorId == taskIndicator.Id);
        dto.MethodChangeHistories = histories
            .OrderByDescending(h => h.RequestedDate)
            .Select(h => new MethodChangeHistoryDto
            {
                Id = h.Id,
                IndicatorId = h.IndicatorId,
                OldMethodId = h.OldMethodId,
                OldMethodName = h.OldMethodName,
                NewMethodId = h.NewMethodId,
                NewMethodName = h.NewMethodName,
                Reason = h.Reason,
                RequestedDate = h.RequestedDate,
                RequestedByUserId = h.RequestedByUserId,
                RequestedByUserName = h.RequestedByUserName,
                ConfirmedDate = h.ConfirmedDate,
                ConfirmedByUserId = h.ConfirmedByUserId,
                ConfirmedByUserName = h.ConfirmedByUserName,
                IsApproved = h.IsApproved
            })
            .ToList();

        return dto;
    }

    #endregion
}
