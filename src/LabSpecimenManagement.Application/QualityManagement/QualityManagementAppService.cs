using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.Common.Exceptions;
using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Notifications;
using LabSpecimenManagement.ResultForms;
using LabSpecimenManagement.TestRequests;
using LabSpecimenManagement.Testing;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace LabSpecimenManagement.QualityManagement;

[Authorize]
public partial class QualityManagementAppService : ApplicationService, IQualityManagementAppService
{
    private readonly IRepository<QualityManagementTask, Guid> _taskRepository;
    private readonly IRepository<QualityManagementIndicator, Guid> _indicatorRepository;
    private readonly IRepository<QualityIndicatorStatusHistory, Guid> _historyRepository;
    private readonly IRepository<LaboratoryTask, Guid> _laboratoryTaskRepository;
    private readonly IRepository<LaboratoryTaskIndicator, Guid> _laboratoryTaskIndicatorRepository;
    private readonly IRepository<IndicatorRejectionHistory, Guid> _rejectionHistoryRepository;
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;
    private readonly IRepository<TestRequestSample, Guid> _sampleRepository;
    private readonly IRepository<TestRequestSampleIndicator, Guid> _sampleIndicatorRepository;
    private readonly IRepository<Indicator, Guid> _masterIndicatorRepository;
    private readonly IRepository<Laboratory, Guid> _laboratoryRepository;
    private readonly IRepository<TestMethod, Guid> _testMethodRepository;
    private readonly IRepository<Standard, Guid> _standardRepository;
    private readonly IRepository<StandardIndicator, Guid> _standardIndicatorRepository;
    private readonly IRepository<TestRequestChangeHistory, Guid> _changeHistoryRepository;
    private readonly IRepository<IdentityUser, Guid> _userRepository;
    private readonly IUserNameProvider _userNameProvider;
    private readonly ITestRequestAppService _testRequestAppService;
    private readonly IRepository<SpecimenType, Guid> _specimenTypeRepository;
    private readonly INotificationCreator _notificationCreator;
    private readonly IResultFormAppService _resultFormAppService;
    private readonly IRepository<IndicatorImage, Guid> _indicatorImageRepository;

    public QualityManagementAppService(
        IRepository<QualityManagementTask, Guid> taskRepository,
        IRepository<QualityManagementIndicator, Guid> indicatorRepository,
        IRepository<QualityIndicatorStatusHistory, Guid> historyRepository,
        IRepository<LaboratoryTask, Guid> laboratoryTaskRepository,
        IRepository<LaboratoryTaskIndicator, Guid> laboratoryTaskIndicatorRepository,
        IRepository<IndicatorRejectionHistory, Guid> rejectionHistoryRepository,
        IRepository<TestRequest, Guid> testRequestRepository,
        IRepository<TestRequestSample, Guid> sampleRepository,
        IRepository<TestRequestSampleIndicator, Guid> sampleIndicatorRepository,
        IRepository<Indicator, Guid> masterIndicatorRepository,
        IRepository<Laboratory, Guid> laboratoryRepository,
        IRepository<TestMethod, Guid> testMethodRepository,
        IRepository<Standard, Guid> standardRepository,
        IRepository<StandardIndicator, Guid> standardIndicatorRepository,
        IRepository<TestRequestChangeHistory, Guid> changeHistoryRepository,
        IRepository<IdentityUser, Guid> userRepository,
        IUserNameProvider userNameProvider,
        ITestRequestAppService testRequestAppService,
        IRepository<SpecimenType, Guid> specimenTypeRepository,
        INotificationCreator notificationCreator,
        IResultFormAppService resultFormAppService,
        IRepository<IndicatorImage, Guid> indicatorImageRepository)
    {
        _taskRepository = taskRepository;
        _indicatorRepository = indicatorRepository;
        _historyRepository = historyRepository;
        _laboratoryTaskRepository = laboratoryTaskRepository;
        _laboratoryTaskIndicatorRepository = laboratoryTaskIndicatorRepository;
        _rejectionHistoryRepository = rejectionHistoryRepository;
        _testRequestRepository = testRequestRepository;
        _sampleRepository = sampleRepository;
        _sampleIndicatorRepository = sampleIndicatorRepository;
        _masterIndicatorRepository = masterIndicatorRepository;
        _laboratoryRepository = laboratoryRepository;
        _testMethodRepository = testMethodRepository;
        _standardRepository = standardRepository;
        _standardIndicatorRepository = standardIndicatorRepository;
        _changeHistoryRepository = changeHistoryRepository;
        _userRepository = userRepository;
        _userNameProvider = userNameProvider;
        _testRequestAppService = testRequestAppService;
        _specimenTypeRepository = specimenTypeRepository;
        _notificationCreator = notificationCreator;
        _resultFormAppService = resultFormAppService;
        _indicatorImageRepository = indicatorImageRepository;
    }

    public async Task<PagedResultDto<QualityManagementTaskDto>> GetListAsync(GetQualityManagementListInput input)
    {
        var query = await _taskRepository.GetQueryableAsync();

        // Filter by search
        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            query = query.Where(t => t.ContractNumber.Contains(input.Search));
        }

        // Filter by status
        if (input.Status.HasValue)
        {
            query = query.Where(t => t.Status == input.Status.Value);
        }

        // Filter by task type
        if (input.TaskType.HasValue)
        {
            query = query.Where(t => t.TaskType == input.TaskType.Value);
        }

        // Filter by date range
        if (input.FromDate.HasValue)
        {
            query = query.Where(t => t.TransferDate >= input.FromDate.Value);
        }
        if (input.ToDate.HasValue)
        {
            query = query.Where(t => t.TransferDate <= input.ToDate.Value);
        }

        var totalCount = query.Count();

        // Sorting
        if (!string.IsNullOrWhiteSpace(input.Sorting))
        {
            query = input.Sorting.ToLower() switch
            {
                SortingFields.ContractNumber => query.OrderBy(t => t.ContractNumber),
                SortingFields.ContractNumberDesc => query.OrderByDescending(t => t.ContractNumber),
                SortingFields.TransferDate => query.OrderBy(t => t.TransferDate),
                SortingFields.TransferDateDesc => query.OrderByDescending(t => t.TransferDate),
                SortingFields.RequestCompletionDate => query.OrderBy(t => t.RequestCompletionDate),
                SortingFields.RequestCompletionDateDesc => query.OrderByDescending(t => t.RequestCompletionDate),
                SortingFields.Status => query.OrderBy(t => t.Status),
                SortingFields.StatusDesc => query.OrderByDescending(t => t.Status),
                _ => query.OrderByDescending(t => t.CreationTime)
            };
        }
        else
        {
            query = query.OrderByDescending(t => t.CreationTime);
        }

        // Pagination
        var tasks = query
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        var dtos = new List<QualityManagementTaskDto>();
        foreach (var task in tasks)
        {
            dtos.Add(await MapToListDtoAsync(task));
        }

        return new PagedResultDto<QualityManagementTaskDto>(totalCount, dtos);
    }

    public async Task<QualityManagementTaskDto> GetAsync(Guid id)
    {
        var task = await _taskRepository.GetAsync(id);
        return await MapToDetailDtoAsync(task);
    }

    public async Task<QualityManagementStatisticsDto> GetStatisticsAsync()
    {
        var tasks = await _taskRepository.GetListAsync();
        var now = DateTime.Now;

        return new QualityManagementStatisticsDto
        {
            PendingApprovalCount = tasks.Count(t => t.Status == QualityManagementStatus.PendingApproval),
            OverdueCount = tasks.Count(t =>
                t.RequestCompletionDate.HasValue &&
                (t.RequestCompletionDate.Value - now).TotalDays <= 2 &&
                t.Status != QualityManagementStatus.ResultSent),
            PendingSignatureCount = tasks.Count(t => t.Status == QualityManagementStatus.PendingSignature)
        };
    }

    public async Task<QualityManagementIndicatorDto> ApproveIndicatorAsync(Guid indicatorId)
    {
        var indicator = await _indicatorRepository.GetAsync(indicatorId);

        if (indicator.ApprovalStatus != QualityIndicatorApprovalStatus.PendingApproval)
        {
            throw new Common.Exceptions.InvalidOperationException("Chỉ tiêu này không ở trạng thái chờ phê duyệt.", "indicator");
        }

        var oldStatus = indicator.ApprovalStatus;

        // Update QualityManagementIndicator
        indicator.ApprovalStatus = QualityIndicatorApprovalStatus.Approved;
        indicator.ApprovalDate = DateTime.Now;
        indicator.ApprovedById = CurrentUser.Id;
        await _indicatorRepository.UpdateAsync(indicator);

        // Create history
        await _historyRepository.InsertAsync(new QualityIndicatorStatusHistory(
            GuidGenerator.Create(),
            indicator.Id,
            oldStatus,
            QualityIndicatorApprovalStatus.Approved,
            null
        ));

        // Update task status
        await UpdateTaskStatusAsync(indicator.QualityManagementTaskId);

        // Notify QLKT about result approved (Case 7)
        var labIndicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicator.LaboratoryTaskIndicatorId);
        var labTask = await _laboratoryTaskRepository.GetAsync(labIndicator.LaboratoryTaskId);
        var testRequest = await _testRequestRepository.GetAsync(labTask.TestRequestId);
        var sampleIndicator = await _sampleIndicatorRepository.FindAsync(labIndicator.TestRequestSampleIndicatorId);
        var sample = sampleIndicator != null ? await _sampleRepository.FindAsync(sampleIndicator.TestRequestSampleId) : null;
        var masterIndicator = sampleIndicator != null ? await _masterIndicatorRepository.FindAsync(sampleIndicator.IndicatorId) : null;
        var method = labIndicator.MethodId.HasValue ? await _testMethodRepository.FindAsync(labIndicator.MethodId.Value) : null;
        var qualityManagerName = CurrentUser.Name ?? CurrentUser.UserName ?? "Quản lý chất lượng";

        if (labTask.TechnicalManagerId.HasValue)
        {
            await _notificationCreator.NotifyResultApprovedByQualityManagerAsync(
                labTask.TechnicalManagerId.Value,
                testRequest.ContractNumber ?? labTask.ContractNumber ?? "",
                labTask.TestRequestId,
                qualityManagerName,
                sample?.SampleName,
                masterIndicator?.Name,
                method?.Name
            );
        }

        // CC: Notify SampleReceiver (test request creator)
        if (testRequest.CreatorId.HasValue)
        {
            await _notificationCreator.NotifyResultApprovedToSampleReceiverAsync(
                testRequest.CreatorId.Value,
                testRequest.ContractNumber ?? labTask.ContractNumber ?? "",
                labTask.TestRequestId,
                qualityManagerName,
                sample?.SampleName,
                masterIndicator?.Name,
                method?.Name
            );
        }

        return await MapIndicatorToDto(indicator);
    }

    public async Task<QualityManagementIndicatorDto> RejectIndicatorAsync(Guid indicatorId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ValidationException("reason", "Vui lòng nhập lý do từ chối.");
        }

        var indicator = await _indicatorRepository.GetAsync(indicatorId);

        if (indicator.ApprovalStatus != QualityIndicatorApprovalStatus.PendingApproval)
        {
            throw new Common.Exceptions.InvalidOperationException("Chỉ tiêu này không ở trạng thái chờ phê duyệt.", "indicator");
        }

        var oldStatus = indicator.ApprovalStatus;

        // Update QualityManagementIndicator
        indicator.ApprovalStatus = QualityIndicatorApprovalStatus.Rejected;
        indicator.ApprovalDate = DateTime.Now;
        indicator.ApprovedById = CurrentUser.Id;
        indicator.RejectionReason = reason;
        await _indicatorRepository.UpdateAsync(indicator);

        // Create history
        await _historyRepository.InsertAsync(new QualityIndicatorStatusHistory(
            GuidGenerator.Create(),
            indicator.Id,
            oldStatus,
            QualityIndicatorApprovalStatus.Rejected,
            reason
        ));

        // Sync to LaboratoryTaskIndicator
        var labIndicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicator.LaboratoryTaskIndicatorId);
        labIndicator.ApprovalStatus = IndicatorApprovalStatus.Rejected;
        labIndicator.RejectionReason = reason;
        labIndicator.IsSentToQuality = false; // Reset để QLKT có thể gửi lại sau khi KTV sửa
        await _laboratoryTaskIndicatorRepository.UpdateAsync(labIndicator);

        // Add to rejection history
        await _rejectionHistoryRepository.InsertAsync(new IndicatorRejectionHistory(
            GuidGenerator.Create(),
            labIndicator.Id,
            reason,
            CurrentUser.Id ?? Guid.Empty,
            Clock.Now
        ));

        // Update LaboratoryTask status to PendingApproval (QLCL từ chối → chờ QLKT xử lý)
        var labTask = await _laboratoryTaskRepository.GetAsync(labIndicator.LaboratoryTaskId);
        labTask.Status = LaboratoryTaskStatus.PendingApproval;
        labTask.ResultRejectionReason = reason;
        await _laboratoryTaskRepository.UpdateAsync(labTask);

        // Update QualityManagementTask status
        await UpdateTaskStatusAsync(indicator.QualityManagementTaskId);

        // Notify QLKT about result rejected (Case 4 & 5)
        if (labTask.TechnicalManagerId.HasValue)
        {
            var testRequest = await _testRequestRepository.GetAsync(labTask.TestRequestId);
            var sampleIndicator = await _sampleIndicatorRepository.FindAsync(labIndicator.TestRequestSampleIndicatorId);
            var sample = sampleIndicator != null ? await _sampleRepository.FindAsync(sampleIndicator.TestRequestSampleId) : null;
            var masterIndicator = sampleIndicator != null ? await _masterIndicatorRepository.FindAsync(sampleIndicator.IndicatorId) : null;
            var method = labIndicator.MethodId.HasValue ? await _testMethodRepository.FindAsync(labIndicator.MethodId.Value) : null;

            var qualityManagerName = CurrentUser.Name ?? CurrentUser.UserName ?? "Quản lý chất lượng";

            await _notificationCreator.NotifyResultRejectedByQualityManagerAsync(
                labTask.TechnicalManagerId.Value,
                testRequest.ContractNumber ?? labTask.ContractNumber ?? "",
                labTask.TestRequestId,
                qualityManagerName,
                1, // Single rejection
                sample?.SampleName,
                masterIndicator?.Name,
                method?.Name
            );
        }

        return await MapIndicatorToDto(indicator);
    }

    public async Task<BulkApproveIndicatorsResultDto> BulkApproveIndicatorsAsync(List<Guid> indicatorIds)
    {
        var result = new BulkApproveIndicatorsResultDto();

        foreach (var indicatorId in indicatorIds)
        {
            try
            {
                var indicator = await _indicatorRepository.FindAsync(indicatorId);
                if (indicator == null)
                {
                    result.FailedCount++;
                    result.FailedIndicators.Add($"Không tìm thấy chỉ tiêu {indicatorId}");
                    continue;
                }

                if (indicator.ApprovalStatus != QualityIndicatorApprovalStatus.PendingApproval)
                {
                    result.FailedCount++;
                    result.FailedIndicators.Add($"Chỉ tiêu không ở trạng thái chờ phê duyệt");
                    continue;
                }

                var oldStatus = indicator.ApprovalStatus;

                // Update QualityManagementIndicator
                indicator.ApprovalStatus = QualityIndicatorApprovalStatus.Approved;
                indicator.ApprovalDate = DateTime.Now;
                indicator.ApprovedById = CurrentUser.Id;
                await _indicatorRepository.UpdateAsync(indicator);

                // Create history
                await _historyRepository.InsertAsync(new QualityIndicatorStatusHistory(
                    GuidGenerator.Create(),
                    indicator.Id,
                    oldStatus,
                    QualityIndicatorApprovalStatus.Approved,
                    null
                ));

                // Update task status
                await UpdateTaskStatusAsync(indicator.QualityManagementTaskId);

                // Notify QLKT about result approved (Case 7)
                var labIndicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicator.LaboratoryTaskIndicatorId);
                var labTask = await _laboratoryTaskRepository.GetAsync(labIndicator.LaboratoryTaskId);
                var testRequest = await _testRequestRepository.GetAsync(labTask.TestRequestId);
                var sampleIndicator = await _sampleIndicatorRepository.FindAsync(labIndicator.TestRequestSampleIndicatorId);
                var sample = sampleIndicator != null ? await _sampleRepository.FindAsync(sampleIndicator.TestRequestSampleId) : null;
                var masterIndicator = sampleIndicator != null ? await _masterIndicatorRepository.FindAsync(sampleIndicator.IndicatorId) : null;
                var method = labIndicator.MethodId.HasValue ? await _testMethodRepository.FindAsync(labIndicator.MethodId.Value) : null;
                var qualityManagerName = CurrentUser.Name ?? CurrentUser.UserName ?? "Quản lý chất lượng";

                if (labTask.TechnicalManagerId.HasValue)
                {
                    await _notificationCreator.NotifyResultApprovedByQualityManagerAsync(
                        labTask.TechnicalManagerId.Value,
                        testRequest.ContractNumber ?? labTask.ContractNumber ?? "",
                        labTask.TestRequestId,
                        qualityManagerName,
                        sample?.SampleName,
                        masterIndicator?.Name,
                        method?.Name
                    );
                }

                // CC: Notify SampleReceiver (test request creator)
                if (testRequest.CreatorId.HasValue)
                {
                    await _notificationCreator.NotifyResultApprovedToSampleReceiverAsync(
                        testRequest.CreatorId.Value,
                        testRequest.ContractNumber ?? labTask.ContractNumber ?? "",
                        labTask.TestRequestId,
                        qualityManagerName,
                        sample?.SampleName,
                        masterIndicator?.Name,
                        method?.Name
                    );
                }

                result.SuccessCount++;
                result.ApprovedIndicators.Add(await MapIndicatorToDto(indicator));
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.FailedIndicators.Add($"Lỗi khi duyệt chỉ tiêu: {ex.Message}");
            }
        }

        return result;
    }

    public async Task<BulkRejectIndicatorsResultDto> BulkRejectIndicatorsAsync(List<Guid> indicatorIds, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ValidationException("reason", "Vui lòng nhập lý do từ chối.");
        }

        var result = new BulkRejectIndicatorsResultDto();

        foreach (var indicatorId in indicatorIds)
        {
            try
            {
                var indicator = await _indicatorRepository.FindAsync(indicatorId);
                if (indicator == null)
                {
                    result.FailedCount++;
                    result.FailedIndicators.Add($"Không tìm thấy chỉ tiêu {indicatorId}");
                    continue;
                }

                if (indicator.ApprovalStatus != QualityIndicatorApprovalStatus.PendingApproval)
                {
                    result.FailedCount++;
                    result.FailedIndicators.Add($"Chỉ tiêu không ở trạng thái chờ phê duyệt");
                    continue;
                }

                var oldStatus = indicator.ApprovalStatus;

                // Update QualityManagementIndicator
                indicator.ApprovalStatus = QualityIndicatorApprovalStatus.Rejected;
                indicator.ApprovalDate = DateTime.Now;
                indicator.ApprovedById = CurrentUser.Id;
                indicator.RejectionReason = reason;
                await _indicatorRepository.UpdateAsync(indicator);

                // Create history
                await _historyRepository.InsertAsync(new QualityIndicatorStatusHistory(
                    GuidGenerator.Create(),
                    indicator.Id,
                    oldStatus,
                    QualityIndicatorApprovalStatus.Rejected,
                    reason
                ));

                // Sync to LaboratoryTaskIndicator
                var labIndicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicator.LaboratoryTaskIndicatorId);
                labIndicator.ApprovalStatus = IndicatorApprovalStatus.Rejected;
                labIndicator.RejectionReason = reason;
                labIndicator.IsSentToQuality = false;
                await _laboratoryTaskIndicatorRepository.UpdateAsync(labIndicator);

                // Add to rejection history
                await _rejectionHistoryRepository.InsertAsync(new IndicatorRejectionHistory(
                    GuidGenerator.Create(),
                    labIndicator.Id,
                    reason,
                    CurrentUser.Id ?? Guid.Empty,
                    Clock.Now
                ));

                // Update LaboratoryTask status to PendingApproval (QLCL từ chối → chờ QLKT xử lý)
                var labTask = await _laboratoryTaskRepository.GetAsync(labIndicator.LaboratoryTaskId);
                labTask.Status = LaboratoryTaskStatus.PendingApproval;
                labTask.ResultRejectionReason = reason;
                await _laboratoryTaskRepository.UpdateAsync(labTask);

                // Update QualityManagementTask status
                await UpdateTaskStatusAsync(indicator.QualityManagementTaskId);

                // Notify QLKT about result rejected
                if (labTask.TechnicalManagerId.HasValue)
                {
                    var testRequest = await _testRequestRepository.GetAsync(labTask.TestRequestId);
                    var sampleIndicator = await _sampleIndicatorRepository.FindAsync(labIndicator.TestRequestSampleIndicatorId);
                    var sample = sampleIndicator != null ? await _sampleRepository.FindAsync(sampleIndicator.TestRequestSampleId) : null;
                    var masterIndicator = sampleIndicator != null ? await _masterIndicatorRepository.FindAsync(sampleIndicator.IndicatorId) : null;
                    var method = labIndicator.MethodId.HasValue ? await _testMethodRepository.FindAsync(labIndicator.MethodId.Value) : null;

                    var qualityManagerName = CurrentUser.Name ?? CurrentUser.UserName ?? "Quản lý chất lượng";

                    await _notificationCreator.NotifyResultRejectedByQualityManagerAsync(
                        labTask.TechnicalManagerId.Value,
                        testRequest.ContractNumber ?? labTask.ContractNumber ?? "",
                        labTask.TestRequestId,
                        qualityManagerName,
                        1,
                        sample?.SampleName,
                        masterIndicator?.Name,
                        method?.Name
                    );
                }

                result.SuccessCount++;
                result.RejectedIndicators.Add(await MapIndicatorToDto(indicator));
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.FailedIndicators.Add($"Lỗi khi từ chối chỉ tiêu: {ex.Message}");
            }
        }

        return result;
    }

    public async Task<QualityManagementIndicatorDto> UpdateIndicatorStandardAsync(Guid indicatorId, Guid? standardId)
    {
        var indicator = await _indicatorRepository.GetAsync(indicatorId);

        indicator.StandardId = standardId;

        // Auto-fill reference limit if standard is selected
        if (standardId.HasValue)
        {
            var labIndicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicator.LaboratoryTaskIndicatorId);
            var sampleIndicator = await _sampleIndicatorRepository.GetAsync(labIndicator.TestRequestSampleIndicatorId);

            // Try to find matching StandardIndicator
            StandardIndicator? standardIndicator = null;

            if (labIndicator.MethodId.HasValue)
            {
                // First try with exact method match
                standardIndicator = await _standardIndicatorRepository.FirstOrDefaultAsync(si =>
                    si.StandardId == standardId.Value &&
                    si.IndicatorId == sampleIndicator.IndicatorId &&
                    si.TestMethodId == labIndicator.MethodId.Value);
            }

            // If not found with method, try matching by indicator only
            if (standardIndicator == null)
            {
                standardIndicator = await _standardIndicatorRepository.FirstOrDefaultAsync(si =>
                    si.StandardId == standardId.Value &&
                    si.IndicatorId == sampleIndicator.IndicatorId);
            }

            if (standardIndicator != null)
            {
                indicator.ReferenceLimit = $"{standardIndicator.ComparisonOperator} {standardIndicator.AllowedLimit}";
            }
            else
            {
                // No matching indicator found in this standard
                indicator.ReferenceLimit = null;
            }
        }
        else
        {
            indicator.ReferenceLimit = null;
        }

        await _indicatorRepository.UpdateAsync(indicator);

        return await MapIndicatorToDto(indicator);
    }

    public async Task<List<QualityIndicatorStatusHistoryDto>> GetIndicatorHistoryAsync(Guid indicatorId)
    {
        var histories = await _historyRepository.GetListAsync(h => h.QualityManagementIndicatorId == indicatorId);
        var orderedHistories = histories.OrderByDescending(x => x.CreationTime).ToList();

        var indicator = await _indicatorRepository.GetAsync(indicatorId);
        var labIndicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicator.LaboratoryTaskIndicatorId);
        var sampleIndicator = await _sampleIndicatorRepository.GetAsync(labIndicator.TestRequestSampleIndicatorId);
        var masterIndicator = await _masterIndicatorRepository.GetAsync(sampleIndicator.IndicatorId);

        // Batch fetch user names
        var userNames = await _userNameProvider.GetUserNamesAsync(orderedHistories.Select(h => h.CreatorId));

        return orderedHistories.Select(h => new QualityIndicatorStatusHistoryDto
        {
            Id = h.Id,
            QualityManagementIndicatorId = h.QualityManagementIndicatorId,
            IndicatorCode = masterIndicator.Code,
            IndicatorName = masterIndicator.Name,
            OldStatus = h.OldStatus,
            NewStatus = h.NewStatus,
            Comment = h.Comment,
            CreationTime = h.CreationTime,
            CreatorId = h.CreatorId,
            CreatorName = h.CreatorId.HasValue && userNames.TryGetValue(h.CreatorId.Value, out var name) ? name : ""
        }).ToList();
    }

    public async Task<TaskHistoryResponseDto> GetTaskHistoryAsync(Guid taskId)
    {
        var task = await _taskRepository.GetAsync(taskId);
        var response = new TaskHistoryResponseDto
        {
            TaskType = task.TaskType
        };

        if (task.TaskType == QualityTaskType.ChangeApproval)
        {
            // Get ALL change histories for this TestRequest (not just the linked batch)
            var allChangeHistories = await _changeHistoryRepository.GetListAsync(
                c => c.TestRequestId == task.TestRequestId);

            // Group change histories by batch (using CreationTime - changes in same transaction have same time)
            // Using a tolerance of 1 second to group changes in the same batch
            var batches = new List<(DateTime BatchTime, List<TestRequestChangeHistory> Changes)>();

            foreach (var ch in allChangeHistories.OrderBy(x => x.CreationTime))
            {
                var existingBatch = batches.FirstOrDefault(b =>
                    Math.Abs((b.BatchTime - ch.CreationTime).TotalSeconds) < 1);

                if (existingBatch.Changes != null)
                {
                    existingBatch.Changes.Add(ch);
                }
                else
                {
                    batches.Add((ch.CreationTime, new List<TestRequestChangeHistory> { ch }));
                }
            }

            // Batch fetch all user names upfront
            var allUserIds = allChangeHistories
                .SelectMany(ch => new[] { ch.CreatorId, ch.ApprovedById })
                .ToList();
            var userCache = await _userNameProvider.GetUserNamesAsync(allUserIds);

            // Build ChangeHistoryBatches (new grouped format)
            foreach (var batch in batches.OrderByDescending(b => b.BatchTime))
            {
                var firstChange = batch.Changes.First();

                // Get creator name from cache
                var creatorName = firstChange.CreatorId.HasValue && userCache.TryGetValue(firstChange.CreatorId.Value, out var cn) ? cn : "";

                // Get approver name from cache
                var approvedByName = firstChange.ApprovedById.HasValue && userCache.TryGetValue(firstChange.ApprovedById.Value, out var an) ? an : "";

                // Determine action based on status
                var action = firstChange.ApprovalStatus switch
                {
                    ChangeApprovalStatus.Pending => "Yêu cầu thay đổi",
                    ChangeApprovalStatus.Approved => "Đã phê duyệt",
                    ChangeApprovalStatus.Rejected => "Đã từ chối",
                    _ => "Không xác định"
                };

                var batchDto = new ChangeApprovalBatchDto
                {
                    CreationTime = batch.BatchTime,
                    CreatorId = firstChange.CreatorId,
                    CreatorName = creatorName,
                    Status = firstChange.ApprovalStatus,
                    Action = action,
                    ApprovalDate = firstChange.ApprovalDate,
                    ApprovedById = firstChange.ApprovedById,
                    ApprovedByName = approvedByName,
                    RejectionReason = firstChange.RejectionReason,
                    Fields = batch.Changes.Select(ch => new ChangeApprovalFieldDto
                    {
                        Id = ch.Id,
                        FieldName = ch.FieldName,
                        OldValue = ch.OldValue,
                        NewValue = ch.NewValue
                    }).ToList()
                };

                response.ChangeHistoryBatches.Add(batchDto);
            }

            // Also populate legacy ChangeHistories for backward compatibility (ALL records from ALL batches)
            foreach (var ch in allChangeHistories.OrderByDescending(x => x.CreationTime))
            {
                var creatorName = "";
                if (ch.CreatorId.HasValue && userCache.TryGetValue(ch.CreatorId.Value, out var name))
                {
                    creatorName = name;
                }

                var approvedByName = "";
                if (ch.ApprovedById.HasValue && userCache.TryGetValue(ch.ApprovedById.Value, out var approverName))
                {
                    approvedByName = approverName;
                }

                var action = ch.ApprovalStatus switch
                {
                    ChangeApprovalStatus.Pending => "Yêu cầu thay đổi",
                    ChangeApprovalStatus.Approved => "Đã phê duyệt",
                    ChangeApprovalStatus.Rejected => "Đã từ chối",
                    _ => "Không xác định"
                };

                response.ChangeHistories.Add(new ChangeApprovalHistoryDto
                {
                    Id = ch.Id,
                    Action = action,
                    Status = ch.ApprovalStatus,
                    FieldName = ch.FieldName,
                    OldValue = ch.OldValue,
                    NewValue = ch.NewValue,
                    RejectionReason = ch.RejectionReason,
                    CreationTime = ch.CreationTime,
                    CreatorId = ch.CreatorId,
                    CreatorName = creatorName,
                    ApprovalDate = ch.ApprovalDate,
                    ApprovedById = ch.ApprovedById,
                    ApprovedByName = approvedByName
                });
            }
        }
        else
        {
            // Get indicator status history for ResultApproval
            var indicators = await _indicatorRepository.GetListAsync(i => i.QualityManagementTaskId == taskId);
            var indicatorIds = indicators.Select(i => i.Id).ToList();

            var histories = await _historyRepository.GetListAsync(h => indicatorIds.Contains(h.QualityManagementIndicatorId));

            // Batch fetch all user names for histories
            var historyUserNames = await _userNameProvider.GetUserNamesAsync(histories.Select(h => h.CreatorId));

            // Group histories by indicator
            var groupedHistories = histories.GroupBy(h => h.QualityManagementIndicatorId);

            foreach (var group in groupedHistories)
            {
                var indicator = indicators.First(i => i.Id == group.Key);
                var labIndicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicator.LaboratoryTaskIndicatorId);
                var sampleIndicator = await _sampleIndicatorRepository.GetAsync(labIndicator.TestRequestSampleIndicatorId);
                var sample = await _sampleRepository.GetAsync(sampleIndicator.TestRequestSampleId);
                var masterIndicator = await _masterIndicatorRepository.GetAsync(sampleIndicator.IndicatorId);

                var indicatorHistoryDto = new QualityTaskHistoryDto
                {
                    IndicatorId = indicator.Id,
                    IndicatorCode = masterIndicator.Code,
                    IndicatorName = masterIndicator.Name,
                    SampleCode = sample.SampleCode,
                    SampleName = sample.SampleName
                };

                foreach (var h in group.OrderByDescending(x => x.CreationTime))
                {
                    var creatorName = h.CreatorId.HasValue && historyUserNames.TryGetValue(h.CreatorId.Value, out var name) ? name : "";

                    indicatorHistoryDto.Histories.Add(new QualityIndicatorStatusHistoryDto
                    {
                        Id = h.Id,
                        QualityManagementIndicatorId = h.QualityManagementIndicatorId,
                        IndicatorCode = masterIndicator.Code,
                        IndicatorName = masterIndicator.Name,
                        OldStatus = h.OldStatus,
                        NewStatus = h.NewStatus,
                        Comment = h.Comment,
                        CreationTime = h.CreationTime,
                        CreatorId = h.CreatorId,
                        CreatorName = creatorName
                    });
                }

                response.IndicatorHistories.Add(indicatorHistoryDto);
            }
        }

        return response;
    }

    public async Task<PrintResultOutputDto> PrintSingleResultAsync(Guid taskId, Guid indicatorId, string? note = null, string? templateName = null)
    {
        var task = await _taskRepository.GetAsync(taskId);
        var indicator = await _indicatorRepository.GetAsync(indicatorId);

        if (indicator.ApprovalStatus != QualityIndicatorApprovalStatus.Approved)
        {
            throw new Common.Exceptions.InvalidOperationException("Chỉ có thể in kết quả đã được duyệt.", "indicator");
        }

        // Generate PDF result form
        var result = await _resultFormAppService.GenerateSingleIndicatorResultFormAsync(new GenerateSingleIndicatorInput
        {
            TaskId = taskId,
            IndicatorId = indicatorId,
            Note = note,
            TemplateName = templateName
        });

        // Update status to PendingSignature (only if not already ResultSent - allow reprinting)
        if (task.Status != QualityManagementStatus.ResultSent)
        {
            task.Status = QualityManagementStatus.PendingSignature;
            await _taskRepository.UpdateAsync(task);
        }

        return new PrintResultOutputDto
        {
            FileName = result.FileName
        };
    }

    public async Task<PrintResultOutputDto> PrintMixedResultAsync(Guid taskId, List<Guid> indicatorIds, string? note = null, string? templateName = null)
    {
        var task = await _taskRepository.GetAsync(taskId);

        foreach (var indicatorId in indicatorIds)
        {
            var indicator = await _indicatorRepository.GetAsync(indicatorId);
            if (indicator.ApprovalStatus != QualityIndicatorApprovalStatus.Approved)
            {
                throw new Common.Exceptions.InvalidOperationException("Chỉ có thể in kết quả đã được duyệt.", "indicator");
            }
        }

        // Generate PDF result form
        var result = await _resultFormAppService.GenerateMixedIndicatorResultFormAsync(new GenerateMixedIndicatorInput
        {
            TaskId = taskId,
            IndicatorIds = indicatorIds,
            Note = note,
            TemplateName = templateName
        });

        // Update status to PendingSignature (only if not already ResultSent - allow reprinting)
        if (task.Status != QualityManagementStatus.ResultSent)
        {
            task.Status = QualityManagementStatus.PendingSignature;
            await _taskRepository.UpdateAsync(task);
        }

        return new PrintResultOutputDto
        {
            FileName = result.FileName
        };
    }

    public async Task SendResultAsync(Guid taskId)
    {
        var task = await _taskRepository.GetAsync(taskId);

        // Allow resending - check if there are approved indicators that haven't been sent yet
        var indicators = await _indicatorRepository.GetListAsync(i => i.QualityManagementTaskId == taskId);
        var approvedNotSentIndicators = indicators
            .Where(i => i.ApprovalStatus == QualityIndicatorApprovalStatus.Approved && !i.IsResultSent)
            .ToList();

        if (!approvedNotSentIndicators.Any())
        {
            throw new Common.Exceptions.InvalidOperationException("Không có chỉ tiêu nào đã được duyệt để gửi.", "indicator");
        }

        // Mark approved indicators as sent
        var now = DateTime.Now;
        foreach (var indicator in approvedNotSentIndicators)
        {
            indicator.IsResultSent = true;
            indicator.ResultSentDate = now;
        }
        await _indicatorRepository.UpdateManyAsync(approvedNotSentIndicators);

        // Check if all indicators are processed (approved or rejected, no pending)
        var hasPendingIndicators = indicators.Any(i => i.ApprovalStatus == QualityIndicatorApprovalStatus.PendingApproval);

        // Only update task status to ResultSent when ALL indicators are processed (no pending)
        if (!hasPendingIndicators)
        {
            task.Status = QualityManagementStatus.ResultSent;
            task.ResultSentDate = now;
            await _taskRepository.UpdateAsync(task);
        }

        // Get current user name
        var currentUserName = await _userNameProvider.GetUserNameAsync(CurrentUser.Id);

        // Check if ALL quality tasks for this TestRequest have sent results
        // Only update TestRequest status when all tasks are ResultSent
        var allQualityTasks = await _taskRepository.GetListAsync(qt => qt.TestRequestId == task.TestRequestId);
        var allTasksSentOrNoIndicatorsPending = allQualityTasks.All(qt =>
            qt.Status == QualityManagementStatus.ResultSent || (qt.Id == taskId && !hasPendingIndicators));

        if (allTasksSentOrNoIndicatorsPending)
        {
            var testRequest = await _testRequestRepository.GetAsync(task.TestRequestId);

            // Only update if not already ResultSent (avoid overwriting date/user)
            if (testRequest.Status != TestRequestStatus.ResultSent)
            {
                testRequest.Status = TestRequestStatus.ResultSent;
                testRequest.ResultSentDate = DateTime.Now;
                testRequest.ResultSentByUserId = CurrentUser.Id;
                testRequest.ResultSentByUserName = currentUserName;
                await _testRequestRepository.UpdateAsync(testRequest);

                // Notify the test request creator - Case 4 SampleReceiver (first notification)
                if (testRequest.CreatorId.HasValue)
                {
                    await _notificationCreator.NotifySampleReturnReminderAsync(
                        testRequest.CreatorId.Value,
                        testRequest.ContractNumber ?? "",
                        testRequest.Id);
                }
            }
        }
    }

    public async Task<List<StandardLookupDto>> GetStandardsLookupAsync()
    {
        var standards = await _standardRepository.GetListAsync(s => s.IsActive);

        return standards
            .OrderBy(s => s.SortOrder)
            .ThenBy(s => s.Name)
            .Select(s => new StandardLookupDto
            {
                Id = s.Id,
                Code = s.Code,
                Name = s.Name
            })
            .ToList();
    }

    public async Task<StandardIndicatorInfoDto?> GetStandardIndicatorInfoAsync(Guid standardId, Guid indicatorId, Guid methodId)
    {
        var standardIndicator = await _standardIndicatorRepository.FirstOrDefaultAsync(si =>
            si.StandardId == standardId &&
            si.IndicatorId == indicatorId &&
            si.TestMethodId == methodId);

        if (standardIndicator == null)
        {
            return null;
        }

        return new StandardIndicatorInfoDto
        {
            StandardId = standardIndicator.StandardId,
            IndicatorId = standardIndicator.IndicatorId,
            MethodId = standardIndicator.TestMethodId,
            Unit = standardIndicator.Unit,
            ComparisonOperator = standardIndicator.ComparisonOperator,
            AllowedLimit = standardIndicator.AllowedLimit,
            ReferenceLimit = $"{standardIndicator.ComparisonOperator} {standardIndicator.AllowedLimit}"
        };
    }

    public async Task ApproveChangesAsync(Guid taskId)
    {
        var task = await _taskRepository.GetAsync(taskId);

        if (task.TaskType != QualityTaskType.ChangeApproval)
        {
            throw new Common.Exceptions.InvalidOperationException("Task này không phải là phê duyệt thay đổi.", "task");
        }

        // Call TestRequestAppService to apply changes and update status
        await _testRequestAppService.ApproveChangesAsync(task.TestRequestId);

        // Update task status
        task.Status = QualityManagementStatus.Approved;
        await _taskRepository.UpdateAsync(task);

        // Notify the test request creator (SampleReceiver)
        var testRequest = await _testRequestRepository.GetAsync(task.TestRequestId);
        if (testRequest.CreatorId.HasValue)
        {
            await _notificationCreator.NotifyChangeApprovedAsync(
                testRequest.CreatorId.Value,
                testRequest.ContractNumber ?? "",
                testRequest.Id);
        }

        // CC: Notify all technical managers (QLKT) cho các khoa phòng được gán ở phiếu yêu cầu
        var labTasks = await _laboratoryTaskRepository.GetListAsync(lt => lt.TestRequestId == testRequest.Id);
        var technicalManagerIds = labTasks
            .Where(lt => lt.TechnicalManagerId.HasValue)
            .Select(lt => lt.TechnicalManagerId!.Value)
            .Distinct()
            .ToList();

        foreach (var managerId in technicalManagerIds)
        {
            await _notificationCreator.NotifyChangeApprovedAsync(
                managerId,
                testRequest.ContractNumber ?? "",
                testRequest.Id);
        }
    }

    public async Task RejectChangesAsync(Guid taskId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ValidationException("reason", "Vui lòng nhập lý do từ chối.");
        }

        var task = await _taskRepository.GetAsync(taskId);

        if (task.TaskType != QualityTaskType.ChangeApproval)
        {
            throw new Common.Exceptions.InvalidOperationException("Task này không phải là phê duyệt thay đổi.", "task");
        }

        // Call TestRequestAppService to reject changes and update status
        await _testRequestAppService.RejectChangesAsync(task.TestRequestId, reason);

        // Update task status
        task.Status = QualityManagementStatus.Rejected;
        await _taskRepository.UpdateAsync(task);

        // Notify the test request creator (SampleReceiver)
        var testRequest = await _testRequestRepository.GetAsync(task.TestRequestId);
        if (testRequest.CreatorId.HasValue)
        {
            await _notificationCreator.NotifyChangeRejectedAsync(
                testRequest.CreatorId.Value,
                testRequest.ContractNumber ?? "",
                testRequest.Id);
        }

        // CC: Notify all technical managers (QLKT) cho các khoa phòng được gán ở phiếu yêu cầu
        var labTasks = await _laboratoryTaskRepository.GetListAsync(lt => lt.TestRequestId == testRequest.Id);
        var technicalManagerIds = labTasks
            .Where(lt => lt.TechnicalManagerId.HasValue)
            .Select(lt => lt.TechnicalManagerId!.Value)
            .Distinct()
            .ToList();

        foreach (var managerId in technicalManagerIds)
        {
            await _notificationCreator.NotifyChangeRejectedAsync(
                managerId,
                testRequest.ContractNumber ?? "",
                testRequest.Id);
        }
    }

    #region Work Reminder

    public async Task<List<QualityManagerPendingTaskDto>> GetQualityManagerPendingTasksAsync()
    {
        var currentUserId = CurrentUser.Id;
        if (!currentUserId.HasValue)
        {
            return new List<QualityManagerPendingTaskDto>();
        }

        var tasks = await _taskRepository.GetListAsync(t =>
            t.Status != QualityManagementStatus.ResultSent);

        var result = new List<QualityManagerPendingTaskDto>();
        var now = DateTime.Now;

        foreach (var task in tasks)
        {
            // Determine task name based on priority:
            // 1. "Có kết quả cần phê duyệt" (PendingApproval)
            // 2. "Kết quả cần bàn giao cho bộ phận nhận/ trả mẫu" (Approved)
            // 3. "Sắp tới ngày trả kết quả thử nghiệm" (deadline <= 5 days)
            string taskName;
            int? daysUntilDeadline = null;

            if (task.RequestCompletionDate.HasValue)
            {
                daysUntilDeadline = (int)(task.RequestCompletionDate.Value - now).TotalDays;
            }

            if (task.Status == QualityManagementStatus.PendingApproval)
            {
                taskName = "Có kết quả cần phê duyệt";
            }
            else if (task.Status == QualityManagementStatus.Approved || task.Status == QualityManagementStatus.PendingSignature)
            {
                taskName = "Kết quả cần bàn giao cho bộ phận nhận/ trả mẫu";
            }
            else if (daysUntilDeadline.HasValue && daysUntilDeadline.Value <= 5)
            {
                taskName = "Sắp tới ngày trả kết quả thử nghiệm";
            }
            else
            {
                // Skip tasks that don't match any condition
                continue;
            }

            result.Add(new QualityManagerPendingTaskDto
            {
                Id = task.Id,
                TestRequestId = task.TestRequestId,
                ContractNumber = task.ContractNumber,
                TaskName = taskName,
                TransferDate = task.TransferDate,
                RequestCompletionDate = task.RequestCompletionDate,
                Status = task.Status,
                DaysUntilDeadline = daysUntilDeadline
            });
        }

        // Sort by priority: PendingApproval > Approved/PendingSignature > Deadline approaching
        return result
            .OrderBy(t => t.TaskName == "Có kết quả cần phê duyệt" ? 0 :
                         t.TaskName == "Kết quả cần bàn giao cho bộ phận nhận/ trả mẫu" ? 1 : 2)
            .ThenBy(t => t.DaysUntilDeadline ?? int.MaxValue)
            .ToList();
    }

    #endregion

    #region Private Methods

    private async Task<QualityManagementTaskDto> MapToListDtoAsync(QualityManagementTask task)
    {
        var testRequest = await _testRequestRepository.FindAsync(task.TestRequestId);

        // Get laboratory names
        var labTasks = await _laboratoryTaskRepository.GetListAsync(lt => lt.TestRequestId == task.TestRequestId);
        var labIds = labTasks.Select(lt => lt.LaboratoryId).Distinct().ToList();
        var labs = await _laboratoryRepository.GetListAsync(l => labIds.Contains(l.Id));
        var labNames = string.Join(", ", labs.Select(l => l.Name));

        // Get technical manager names
        var managerIds = labTasks.Where(lt => lt.TechnicalManagerId.HasValue).Select(lt => lt.TechnicalManagerId!.Value).Distinct().ToList();
        var managers = await _userRepository.GetListAsync(u => managerIds.Contains(u.Id));
        var managerNames = string.Join(", ", managers.Select(m => m.Name ?? m.UserName));

        return new QualityManagementTaskDto
        {
            Id = task.Id,
            TestRequestId = task.TestRequestId,
            ContractNumber = task.ContractNumber,
            TaskType = task.TaskType,
            TotalSampleCount = task.TotalSampleCount,
            TotalIndicatorCount = task.TotalIndicatorCount,
            LaboratoryNames = labNames,
            TransferDate = task.TransferDate,
            RequestCompletionDate = task.RequestCompletionDate,
            ResultSentDate = task.ResultSentDate,
            Status = task.Status,
            CustomerName = testRequest?.CustomerName,
            TechnicalManagerNames = managerNames,
            ChangeHistoryId = task.ChangeHistoryId,
            CreationTime = task.CreationTime,
            CreatorId = task.CreatorId,
            LastModificationTime = task.LastModificationTime,
            LastModifierId = task.LastModifierId
        };
    }

    private async Task<QualityManagementTaskDto> MapToDetailDtoAsync(QualityManagementTask task)
    {
        var dto = await MapToListDtoAsync(task);

        // Load indicators
        var indicators = await _indicatorRepository.GetListAsync(i => i.QualityManagementTaskId == task.Id);
        dto.Indicators = new List<QualityManagementIndicatorDto>();

        foreach (var indicator in indicators)
        {
            dto.Indicators.Add(await MapIndicatorToDto(indicator));
        }

        // Load ChangeHistory for ChangeApproval tasks
        if (task.TaskType == QualityTaskType.ChangeApproval && task.ChangeHistoryId.HasValue)
        {
            var changeHistory = await _changeHistoryRepository.FindAsync(task.ChangeHistoryId.Value);
            if (changeHistory != null)
            {
                var createdByName = await _userNameProvider.GetUserNameAsync(changeHistory.CreatorId);

                dto.ChangeHistory = new ChangeHistoryInfoDto
                {
                    Id = changeHistory.Id,
                    FieldName = changeHistory.FieldName ?? "",
                    OldValue = changeHistory.OldValue,
                    NewValue = changeHistory.NewValue,
                    RejectionReason = changeHistory.RejectionReason,
                    CreationTime = changeHistory.CreationTime,
                    CreatedById = changeHistory.CreatorId,
                    CreatedByName = createdByName
                };
            }
        }

        return dto;
    }

    private async Task<QualityManagementIndicatorDto> MapIndicatorToDto(QualityManagementIndicator indicator)
    {
        var labIndicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicator.LaboratoryTaskIndicatorId);
        var sampleIndicator = await _sampleIndicatorRepository.GetAsync(labIndicator.TestRequestSampleIndicatorId);
        var sample = await _sampleRepository.GetAsync(sampleIndicator.TestRequestSampleId);
        var masterIndicator = await _masterIndicatorRepository.GetAsync(sampleIndicator.IndicatorId);

        var labTask = await _laboratoryTaskRepository.GetAsync(labIndicator.LaboratoryTaskId);
        var lab = await _laboratoryRepository.GetAsync(labTask.LaboratoryId);

        string? methodName = null;
        if (labIndicator.MethodId.HasValue)
        {
            var method = await _testMethodRepository.FindAsync(labIndicator.MethodId.Value);
            methodName = method?.Name;
        }

        string? standardName = null;
        if (indicator.StandardId.HasValue)
        {
            var standard = await _standardRepository.FindAsync(indicator.StandardId.Value);
            standardName = standard?.Name;
        }

        var approvedByName = await _userNameProvider.GetUserNameAsync(indicator.ApprovedById);

        string? specimenTypeName = null;
        if (sample.SpecimenTypeId.HasValue)
        {
            var specimenType = await _specimenTypeRepository.FindAsync(sample.SpecimenTypeId.Value);
            specimenTypeName = specimenType?.Name;
        }

        // Get indicator images
        var indicatorImages = await _indicatorImageRepository.GetListAsync(x =>
            x.LaboratoryTaskIndicatorId == indicator.LaboratoryTaskIndicatorId);

        return new QualityManagementIndicatorDto
        {
            Id = indicator.Id,
            LaboratoryTaskIndicatorId = indicator.LaboratoryTaskIndicatorId,
            LaboratoryId = lab.Id,
            LaboratoryName = lab.Name,
            SampleId = sample.Id,
            SampleCode = sample.SampleCode,
            SampleName = sample.SampleName,
            SpecimenTypeName = specimenTypeName,
            IndicatorId = masterIndicator.Id,
            IndicatorCode = masterIndicator.Code,
            IndicatorName = masterIndicator.Name,
            MethodId = labIndicator.MethodId,
            MethodName = methodName,
            Unit = labIndicator.Unit,
            Result = labIndicator.Result,
            ResultSentDate = labIndicator.EntryDate,
            ApprovalDate = indicator.ApprovalDate,
            StandardId = indicator.StandardId,
            StandardName = standardName,
            ReferenceLimit = indicator.ReferenceLimit,
            ApprovalStatus = indicator.ApprovalStatus,
            RejectionReason = indicator.RejectionReason,
            IsResultSent = indicator.IsResultSent,
            QualityResultSentDate = indicator.ResultSentDate,
            ApprovedById = indicator.ApprovedById,
            ApprovedByName = approvedByName,
            Images = indicatorImages.OrderBy(x => x.CreationTime).Select(x => new IndicatorImageDto
            {
                Id = x.Id,
                LaboratoryTaskIndicatorId = x.LaboratoryTaskIndicatorId,
                FileId = x.FileId,
                FileName = x.FileName,
                FileSize = x.FileSize,
                Url = $"/api/v1/files/{x.FileId}",
                CreationTime = x.CreationTime,
                CreatorId = x.CreatorId
            }).ToList()
        };
    }

    private async Task UpdateTaskStatusAsync(Guid taskId)
    {
        var task = await _taskRepository.GetAsync(taskId);
        var indicators = await _indicatorRepository.GetListAsync(i => i.QualityManagementTaskId == taskId);

        if (!indicators.Any())
        {
            return;
        }

        var allApproved = indicators.All(i => i.ApprovalStatus == QualityIndicatorApprovalStatus.Approved);
        var anyRejected = indicators.Any(i => i.ApprovalStatus == QualityIndicatorApprovalStatus.Rejected);
        var anyPending = indicators.Any(i => i.ApprovalStatus == QualityIndicatorApprovalStatus.PendingApproval);

        var previousStatus = task.Status;

        if (allApproved)
        {
            task.Status = QualityManagementStatus.Approved;
        }
        else if (anyRejected && !anyPending)
        {
            task.Status = QualityManagementStatus.Rejected;
        }
        else if (anyPending)
        {
            task.Status = QualityManagementStatus.PendingApproval;
        }

        await _taskRepository.UpdateAsync(task);

        // Update TestRequest approval info when task becomes Approved
        if (task.Status == QualityManagementStatus.Approved && previousStatus != QualityManagementStatus.Approved)
        {
            var testRequest = await _testRequestRepository.GetAsync(task.TestRequestId);

            // Only update if not already set (to preserve first approval info)
            if (!testRequest.ApprovedDate.HasValue)
            {
                var currentUserName = await _userNameProvider.GetUserNameAsync(CurrentUser.Id);

                testRequest.ApprovedDate = DateTime.Now;
                testRequest.ApprovedByUserId = CurrentUser.Id;
                testRequest.ApprovedByUserName = currentUserName;
                testRequest.Status = TestRequestStatus.Approved;
                await _testRequestRepository.UpdateAsync(testRequest);
            }
        }
    }

    #endregion
}
