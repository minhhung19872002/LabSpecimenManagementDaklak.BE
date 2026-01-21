using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.Common.Exceptions;
using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Notifications;
using LabSpecimenManagement.PublishedReports;
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
    private readonly IPublishedReportRepository _publishedReportRepository;
    private readonly IRepository<Note, Guid> _noteRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;

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
        IRepository<Department, Guid> departmentRepository,
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
        IRepository<IndicatorImage, Guid> indicatorImageRepository,
        IPublishedReportRepository publishedReportRepository,
        IRepository<Note, Guid> noteRepository)
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
        _departmentRepository = departmentRepository;
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
        _publishedReportRepository = publishedReportRepository;
        _noteRepository = noteRepository;
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
                t.Status != QualityManagementStatus.HandedOver),
            PendingPublishCount = tasks.Count(t => t.Status == QualityManagementStatus.PendingPublish),
            PublishedCount = tasks.Count(t =>
                t.Status == QualityManagementStatus.PartialPublished ||
                t.Status == QualityManagementStatus.FullyPublished)
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
        labTask.Status = LaboratoryTaskStatus.WaitingApproval;
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
                labTask.Status = LaboratoryTaskStatus.WaitingApproval;
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
            IndicatorFormattedName = masterIndicator.FormattedName,
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
                    IndicatorFormattedName = masterIndicator.FormattedName,
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
                        IndicatorFormattedName = masterIndicator.FormattedName,
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

    public async Task<PrintResultOutputDto> PrintSingleResultAsync(Guid taskId, Guid indicatorId, string? note = null, string? templateName = null, Guid? vicePresidentId = null)
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
            TemplateName = templateName,
            VicePresidentId = vicePresidentId
        });

        // Tạo record PublishedReport (log lịch sử in)
        var publishedReport = new PublishedReport(
            GuidGenerator.Create(),
            task.TestRequestId,
            task.Id,
            result.FileName,
            "Single",
            note,
            JsonSerializer.Serialize(new[] { indicatorId })
        );
        await _publishedReportRepository.InsertAsync(publishedReport);

        // Đánh dấu indicator đã in (phát hành)
        indicator.IsPrinted = true;
        indicator.PrintedDate = DateTime.Now;
        await _indicatorRepository.UpdateAsync(indicator);

        // Cập nhật task status dựa vào số chỉ tiêu đã in
        await UpdateTaskStatusAfterPrintAsync(task);

        return new PrintResultOutputDto
        {
            FileName = result.FileName
        };
    }

    public async Task<PrintResultOutputDto> PrintMixedResultAsync(Guid taskId, List<Guid> indicatorIds, string? note = null, string? templateName = null, Guid? vicePresidentId = null)
    {
        var task = await _taskRepository.GetAsync(taskId);
        var indicatorsToPrint = new List<QualityManagementIndicator>();

        foreach (var indicatorId in indicatorIds)
        {
            var indicator = await _indicatorRepository.GetAsync(indicatorId);
            if (indicator.ApprovalStatus != QualityIndicatorApprovalStatus.Approved)
            {
                throw new Common.Exceptions.InvalidOperationException("Chỉ có thể in kết quả đã được duyệt.", "indicator");
            }
            indicatorsToPrint.Add(indicator);
        }

        // Generate PDF result form
        var result = await _resultFormAppService.GenerateMixedIndicatorResultFormAsync(new GenerateMixedIndicatorInput
        {
            TaskId = taskId,
            IndicatorIds = indicatorIds,
            Note = note,
            TemplateName = templateName,
            VicePresidentId = vicePresidentId
        });

        // Tạo record PublishedReport (log lịch sử in)
        var publishedReport = new PublishedReport(
            GuidGenerator.Create(),
            task.TestRequestId,
            task.Id,
            result.FileName,
            "Mixed",
            note,
            JsonSerializer.Serialize(indicatorIds)
        );
        await _publishedReportRepository.InsertAsync(publishedReport);

        // Đánh dấu tất cả indicators đã in (phát hành)
        foreach (var indicator in indicatorsToPrint)
        {
            indicator.IsPrinted = true;
            indicator.PrintedDate = DateTime.Now;
            await _indicatorRepository.UpdateAsync(indicator);
        }

        // Cập nhật task status dựa vào số chỉ tiêu đã in
        await UpdateTaskStatusAfterPrintAsync(task);

        return new PrintResultOutputDto
        {
            FileName = result.FileName
        };
    }

    public async Task<PrintMultiResultOutputDto> PrintMultiSingleResultAsync(Guid taskId, PrintMultiSingleDto input)
    {
        var task = await _taskRepository.GetAsync(taskId);

        // Get all selected indicators
        var indicatorsToPrint = new List<QualityManagementIndicator>();
        foreach (var indicatorId in input.IndicatorIds)
        {
            var indicator = await _indicatorRepository.GetAsync(indicatorId);
            if (indicator.ApprovalStatus != QualityIndicatorApprovalStatus.Approved)
            {
                throw new Common.Exceptions.InvalidOperationException("Chỉ có thể in kết quả đã được duyệt.", "indicator");
            }
            indicatorsToPrint.Add(indicator);
        }

        // Build mapping: indicator -> (labId, labName, sampleId, sampleCode, sampleName)
        var indicatorMapping = new Dictionary<Guid, (Guid LabId, string LabName, Guid SampleId, string SampleCode, string SampleName)>();

        foreach (var indicator in indicatorsToPrint)
        {
            // Get laboratory from LaboratoryTaskIndicator
            var labTaskIndicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicator.LaboratoryTaskIndicatorId);
            var labTask = await _laboratoryTaskRepository.GetAsync(labTaskIndicator.LaboratoryTaskId);
            var laboratory = await _laboratoryRepository.GetAsync(labTask.LaboratoryId);
            var labDepartment = await _departmentRepository.GetAsync(laboratory.DepartmentId);

            // Get sample from TestRequestSampleIndicator
            var sampleIndicator = await _sampleIndicatorRepository.GetAsync(labTaskIndicator.TestRequestSampleIndicatorId);
            var sample = await _sampleRepository.GetAsync(sampleIndicator.TestRequestSampleId);

            indicatorMapping[indicator.Id] = (laboratory.Id, labDepartment.Name, sample.Id, sample.SampleCode, sample.SampleName);
        }

        // Group indicators by (laboratoryId, sampleId)
        var labSampleGroups = indicatorsToPrint
            .GroupBy(ind => (indicatorMapping[ind.Id].LabId, indicatorMapping[ind.Id].SampleId))
            .ToDictionary(g => g.Key, g => g.Select(ind => ind.Id).ToList());

        // Generate PDFs for each (lab × sample × template) combination
        var results = new List<PrintResultItemDto>();

        foreach (var labSampleGroup in labSampleGroups)
        {
            var (labId, sampleId) = labSampleGroup.Key;
            var labSampleIndicatorIds = labSampleGroup.Value;

            // Get lab and sample info from first indicator
            var firstIndicatorId = labSampleIndicatorIds.First();
            var (_, labName, _, sampleCode, sampleName) = indicatorMapping[firstIndicatorId];

            foreach (var templateName in input.TemplateNames)
            {
                // Generate PDF for this lab + sample + template
                var result = await _resultFormAppService.GenerateMixedIndicatorResultFormAsync(new GenerateMixedIndicatorInput
                {
                    TaskId = taskId,
                    IndicatorIds = labSampleIndicatorIds,
                    Note = input.Note,
                    TemplateName = templateName,
                    VicePresidentId = input.VicePresidentId
                });

                results.Add(new PrintResultItemDto
                {
                    FileName = result.FileName,
                    LaboratoryId = labId,
                    LaboratoryName = labName,
                    SampleId = sampleId,
                    SampleCode = sampleCode,
                    SampleName = sampleName,
                    TemplateName = templateName
                });

                // Tạo record PublishedReport (log lịch sử in)
                var publishedReport = new PublishedReport(
                    GuidGenerator.Create(),
                    task.TestRequestId,
                    task.Id,
                    result.FileName,
                    "Single",
                    input.Note,
                    JsonSerializer.Serialize(labSampleIndicatorIds)
                );
                await _publishedReportRepository.InsertAsync(publishedReport);
            }
        }

        // Đánh dấu tất cả indicators đã in (phát hành)
        foreach (var indicator in indicatorsToPrint)
        {
            indicator.IsPrinted = true;
            indicator.PrintedDate = DateTime.Now;
            await _indicatorRepository.UpdateAsync(indicator);
        }

        // Cập nhật task status dựa vào số chỉ tiêu đã in
        await UpdateTaskStatusAfterPrintAsync(task);

        return new PrintMultiResultOutputDto
        {
            Results = results
        };
    }

    /// <summary>
    /// [DEPRECATED] Sử dụng ConfirmHandoverAsync thay thế
    /// Method này được giữ lại để backward compatibility
    /// </summary>
    public async Task SendResultAsync(Guid taskId)
    {
        // Chuyển hướng sang ConfirmHandoverAsync
        await ConfirmHandoverAsync(taskId, null);
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

        // Update task status - ChangeApproval tasks go directly to HandedOver (xong việc)
        task.Status = QualityManagementStatus.HandedOver;
        task.HandoverDate = DateTime.Now;
        task.HandoverByUserId = CurrentUser.Id;
        task.HandoverByUserName = await _userNameProvider.GetUserNameAsync(CurrentUser.Id);
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

        // Lấy tất cả tasks chưa hoàn thành (chưa HandedOver)
        var tasks = await _taskRepository.GetListAsync(t =>
            t.Status != QualityManagementStatus.HandedOver);

        var result = new List<QualityManagerPendingTaskDto>();
        var now = DateTime.Now;

        foreach (var task in tasks)
        {
            // Determine task name based on priority:
            // 1. "Có kết quả cần phê duyệt" (PendingApproval)
            // 2. "Kết quả cần bàn giao cho bộ phận nhận/ trả mẫu" (PendingPublish, PartialPublished, FullyPublished)
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
            else if (task.Status == QualityManagementStatus.PendingPublish ||
                     task.Status == QualityManagementStatus.PartialPublished ||
                     task.Status == QualityManagementStatus.FullyPublished)
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

        // Sort by priority: PendingApproval > PendingPublish/Published > Deadline approaching
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
        var deptIds = labs.Select(l => l.DepartmentId).Distinct().ToList();
        var depts = await _departmentRepository.GetListAsync(d => deptIds.Contains(d.Id));
        var deptDict = depts.ToDictionary(d => d.Id, d => d.Name);
        var labNames = string.Join(", ", labs.Select(l => deptDict.TryGetValue(l.DepartmentId, out var name) ? name : ""));

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
            Status = task.Status,
            HandoverDate = task.HandoverDate,
            HandoverByUserName = task.HandoverByUserName,
            HandoverNote = task.HandoverNote,
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
        var labDept = await _departmentRepository.GetAsync(lab.DepartmentId);

        string? methodName = null;
        string? methodFormattedName = null;
        if (labIndicator.MethodId.HasValue)
        {
            var method = await _testMethodRepository.FindAsync(labIndicator.MethodId.Value);
            methodName = method?.Name;
            methodFormattedName = method?.FormattedName;
        }

        string? standardName = null;
        string? comparisonOperator = null;
        string? allowedLimit = null;

        // Try to get standard info from LaboratoryTaskIndicator first (KTV's selection)
        var standardId = labIndicator.StandardId ?? indicator.StandardId;
        if (standardId.HasValue)
        {
            var standard = await _standardRepository.FindAsync(standardId.Value);
            standardName = standard?.Name;

            // Get comparison operator and allowed limit from StandardIndicator
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
                comparisonOperator = standardIndicator.ComparisonOperator;
                allowedLimit = standardIndicator.AllowedLimit;
            }
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
            LaboratoryName = labDept.Name,
            SampleId = sample.Id,
            SampleCode = sample.SampleCode,
            SampleName = sample.SampleName,
            SpecimenTypeName = specimenTypeName,
            IndicatorId = masterIndicator.Id,
            IndicatorCode = masterIndicator.Code,
            IndicatorName = masterIndicator.Name,
            IndicatorFormattedName = masterIndicator.FormattedName,
            MethodId = labIndicator.MethodId,
            MethodName = methodName,
            MethodFormattedName = methodFormattedName,
            Unit = labIndicator.Unit,
            Result = labIndicator.Result,
            ResultSentDate = labIndicator.EntryDate,
            ApprovalDate = indicator.ApprovalDate,
            StandardId = standardId,
            StandardName = standardName,
            ReferenceLimit = indicator.ReferenceLimit,
            ComparisonOperator = comparisonOperator,
            AllowedLimit = allowedLimit,
            ApprovalStatus = indicator.ApprovalStatus,
            RejectionReason = indicator.RejectionReason,
            IsResultSent = indicator.IsResultSent,
            QualityResultSentDate = indicator.ResultSentDate,
            IsPrinted = indicator.IsPrinted,
            PrintedDate = indicator.PrintedDate,
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
            }).ToList(),
            FreeTextNote = labIndicator.FreeTextNote,
            MasterDataNotes = await GetMasterDataNotesAsync(labIndicator.MasterDataNoteIds)
        };
    }

    /// <summary>
    /// Parse MasterDataNoteIds and get note contents
    /// </summary>
    private async Task<List<string>> GetMasterDataNotesAsync(string? masterDataNoteIds)
    {
        if (string.IsNullOrWhiteSpace(masterDataNoteIds))
        {
            return new List<string>();
        }

        var noteContents = new List<string>();

        // Parse comma-separated or JSON array format
        var noteIdStrings = masterDataNoteIds.Contains("[")
            ? JsonSerializer.Deserialize<List<string>>(masterDataNoteIds) ?? new List<string>()
            : masterDataNoteIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();

        foreach (var idStr in noteIdStrings)
        {
            if (Guid.TryParse(idStr, out var noteId))
            {
                var note = await _noteRepository.FindAsync(noteId);
                if (note != null && !string.IsNullOrWhiteSpace(note.Content))
                {
                    noteContents.Add(note.Content);
                }
            }
        }

        return noteContents;
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
            // Tất cả APPROVED → Chờ phát hành (PendingPublish)
            task.Status = QualityManagementStatus.PendingPublish;
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

        // Update TestRequest approval info when task becomes PendingPublish (tất cả chỉ tiêu đã duyệt)
        if (task.Status == QualityManagementStatus.PendingPublish && previousStatus != QualityManagementStatus.PendingPublish)
        {
            var testRequest = await _testRequestRepository.GetAsync(task.TestRequestId);

            // Only update if not already set (to preserve first approval info)
            if (!testRequest.ApprovedDate.HasValue)
            {
                var currentUserName = await _userNameProvider.GetUserNameAsync(CurrentUser.Id);

                testRequest.ApprovedDate = DateTime.Now;
                testRequest.ApprovedByUserId = CurrentUser.Id;
                testRequest.ApprovedByUserName = currentUserName;
                // Không cập nhật TestRequest.Status ở đây nữa - sẽ cập nhật khi QLCL giao BPGN
                await _testRequestRepository.UpdateAsync(testRequest);
            }
        }
    }

    /// <summary>
    /// Cập nhật trạng thái task sau khi in kết quả
    /// Đồng thời cập nhật TestRequest.Status = ReportPublished ngay khi có chỉ tiêu được in
    /// </summary>
    private async Task UpdateTaskStatusAfterPrintAsync(QualityManagementTask task)
    {
        var indicators = await _indicatorRepository.GetListAsync(i => i.QualityManagementTaskId == task.Id);
        var approvedIndicators = indicators.Where(i => i.ApprovalStatus == QualityIndicatorApprovalStatus.Approved).ToList();

        if (!approvedIndicators.Any())
        {
            return;
        }

        var printedCount = approvedIndicators.Count(i => i.IsPrinted);
        var totalApproved = approvedIndicators.Count;

        if (printedCount == 0)
        {
            // Chưa in gì - giữ PendingPublish
            return;
        }
        else if (printedCount < totalApproved)
        {
            // In 1 phần
            task.Status = QualityManagementStatus.PartialPublished;
        }
        else
        {
            // In tất cả
            task.Status = QualityManagementStatus.FullyPublished;
        }

        await _taskRepository.UpdateAsync(task);

        // Cập nhật TestRequest.Status = ReportPublished ngay khi có chỉ tiêu được in
        var testRequest = await _testRequestRepository.GetAsync(task.TestRequestId);
        if (testRequest.Status != TestRequestStatus.ReportPublished)
        {
            testRequest.Status = TestRequestStatus.ReportPublished;
            testRequest.ReportPublishedDate = DateTime.Now;
            testRequest.ReportPublishedByUserId = CurrentUser.Id;
            testRequest.ReportPublishedByUserName = await _userNameProvider.GetUserNameAsync(CurrentUser.Id);
            await _testRequestRepository.UpdateAsync(testRequest);
        }
    }

    /// <summary>
    /// Xác nhận đã giao kết quả cho BPGN
    /// </summary>
    public async Task ConfirmHandoverAsync(Guid taskId, ConfirmHandoverInput? input)
    {
        var task = await _taskRepository.GetAsync(taskId);

        // Chỉ cho phép khi đã phát hành (1 phần hoặc full)
        if (task.Status != QualityManagementStatus.PartialPublished &&
            task.Status != QualityManagementStatus.FullyPublished)
        {
            throw new System.InvalidOperationException("Chỉ có thể giao BPGN khi đã phát hành kết quả.");
        }

        // Cập nhật task
        task.Status = QualityManagementStatus.HandedOver;
        task.HandoverDate = DateTime.Now;
        task.HandoverByUserId = CurrentUser.Id;
        task.HandoverByUserName = await _userNameProvider.GetUserNameAsync(CurrentUser.Id);
        task.HandoverNote = input?.Note;
        await _taskRepository.UpdateAsync(task);

        // Cập nhật TestRequest → ReportPublished
        var testRequest = await _testRequestRepository.GetAsync(task.TestRequestId);

        // Kiểm tra tất cả quality tasks đã HandedOver chưa
        var allQualityTasks = await _taskRepository.GetListAsync(qt => qt.TestRequestId == task.TestRequestId);
        var allHandedOver = allQualityTasks.All(qt => qt.Status == QualityManagementStatus.HandedOver);

        if (allHandedOver && testRequest.Status != TestRequestStatus.ReportPublished)
        {
            testRequest.Status = TestRequestStatus.ReportPublished;
            testRequest.ReportPublishedDate = DateTime.Now;
            testRequest.ReportPublishedByUserId = CurrentUser.Id;
            testRequest.ReportPublishedByUserName = await _userNameProvider.GetUserNameAsync(CurrentUser.Id);
            await _testRequestRepository.UpdateAsync(testRequest);

            // Notify BPGN (SampleReceiver)
            if (testRequest.CreatorId.HasValue)
            {
                await _notificationCreator.NotifySampleReturnReminderAsync(
                    testRequest.CreatorId.Value,
                    testRequest.ContractNumber ?? "",
                    testRequest.Id);
            }
        }
    }

    #endregion

    #region Sample Assignment Log

    public async Task<PagedResultDto<SampleAssignmentLogDto>> GetSampleAssignmentLogAsync(GetSampleAssignmentLogInput input)
    {
        // Build base query from QualityManagementIndicator with all necessary joins
        var qualityIndicatorQuery = await _indicatorRepository.GetQueryableAsync();
        var labIndicatorQuery = await _laboratoryTaskIndicatorRepository.GetQueryableAsync();
        var labTaskQuery = await _laboratoryTaskRepository.GetQueryableAsync();
        var sampleIndicatorQuery = await _sampleIndicatorRepository.GetQueryableAsync();
        var sampleQuery = await _sampleRepository.GetQueryableAsync();
        var testRequestQuery = await _testRequestRepository.GetQueryableAsync();
        var qualityTaskQuery = await _taskRepository.GetQueryableAsync();
        var masterIndicatorQuery = await _masterIndicatorRepository.GetQueryableAsync();
        var testMethodQuery = await _testMethodRepository.GetQueryableAsync();
        var userQuery = await _userRepository.GetQueryableAsync();

        // Join all tables to create flat list
        var query = from qi in qualityIndicatorQuery
                    join li in labIndicatorQuery on qi.LaboratoryTaskIndicatorId equals li.Id
                    join lt in labTaskQuery on li.LaboratoryTaskId equals lt.Id
                    join si in sampleIndicatorQuery on li.TestRequestSampleIndicatorId equals si.Id
                    join s in sampleQuery on si.TestRequestSampleId equals s.Id
                    join tr in testRequestQuery on lt.TestRequestId equals tr.Id
                    join qt in qualityTaskQuery on qi.QualityManagementTaskId equals qt.Id
                    join mi in masterIndicatorQuery on si.IndicatorId equals mi.Id
                    join tm in testMethodQuery on li.MethodId equals tm.Id into tmJoin
                    from tm in tmJoin.DefaultIfEmpty()
                    join techManager in userQuery on lt.TechnicalManagerId equals techManager.Id into techManagerJoin
                    from techManager in techManagerJoin.DefaultIfEmpty()
                    join technician in userQuery on li.TechnicianId equals technician.Id into technicianJoin
                    from technician in technicianJoin.DefaultIfEmpty()
                    select new
                    {
                        // TestRequest info
                        TestRequestId = tr.Id,
                        QualityTaskId = qt.Id,
                        ContractNumber = tr.ContractNumber,
                        TestRequestStatus = tr.Status,
                        RequestCompletionDate = tr.ExpectedCompletionDate ?? lt.RequestCompletionDate,

                        // Sample info
                        SampleId = s.Id,
                        SampleCode = s.SampleCode,
                        SampleName = s.SampleName,
                        SampleQuantity = s.SampleQuantity,
                        SampleStatusText = s.StatusText,

                        // Indicator info
                        IndicatorId = qi.Id,
                        LaboratoryTaskIndicatorId = li.Id,
                        IndicatorName = mi.Name,
                        IndicatorFormattedName = mi.FormattedName,
                        MethodName = tm != null ? tm.Name : null,
                        MethodFormattedName = tm != null ? tm.FormattedName : null,
                        Unit = li.Unit,
                        TransferDate = si.TransferDate,
                        TestDate = li.TestDate,
                        Result = li.Result,

                        // Assignment info
                        TechnicalManagerName = techManager != null ? (techManager.Name ?? techManager.UserName) : null,
                        TechnicianName = technician != null ? (technician.Name ?? technician.UserName) : null,
                        EntryDate = li.EntryDate,
                        QlktApprovalDate = li.ApprovalDate,

                        // Status for highlighting
                        IndicatorApprovalStatus = li.ApprovalStatus,
                        QualityApprovalStatus = qi.ApprovalStatus,
                        IsReportPublished = qi.IsPrinted
                    };

        // Apply filters
        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var searchLower = input.Search.ToLower();
            query = query.Where(x =>
                (x.ContractNumber != null && x.ContractNumber.ToLower().Contains(searchLower)) ||
                (x.SampleCode != null && x.SampleCode.ToLower().Contains(searchLower)) ||
                (x.SampleName != null && x.SampleName.ToLower().Contains(searchLower)) ||
                (x.IndicatorName != null && x.IndicatorName.ToLower().Contains(searchLower)));
        }

        if (input.TestRequestStatus.HasValue)
        {
            query = query.Where(x => x.TestRequestStatus == input.TestRequestStatus.Value);
        }

        if (input.FromDate.HasValue)
        {
            query = query.Where(x => x.RequestCompletionDate >= input.FromDate.Value);
        }

        if (input.ToDate.HasValue)
        {
            query = query.Where(x => x.RequestCompletionDate <= input.ToDate.Value);
        }

        var totalCount = query.Count();

        // Apply sorting - default: RequestCompletionDate ASC, then by SampleId and IndicatorName for consistent grouping
        var sortingKey = string.IsNullOrWhiteSpace(input.Sorting) ? "requestcompletiondate asc" : input.Sorting.ToLower();

        var sortedQuery = sortingKey switch
        {
            "requestcompletiondate desc" => query.OrderByDescending(x => x.RequestCompletionDate).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName),
            "contractnumber asc" => query.OrderBy(x => x.ContractNumber).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName),
            "contractnumber desc" => query.OrderByDescending(x => x.ContractNumber).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName),
            "transferdate asc" => query.OrderBy(x => x.TransferDate).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName),
            "transferdate desc" => query.OrderByDescending(x => x.TransferDate).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName),
            _ => query.OrderBy(x => x.RequestCompletionDate).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName)
        };

        // Pagination with sample-aware boundary snapping
        // This ensures all indicators of the same sample are on the same page
        var allItems = sortedQuery.ToList();
        var items = GetSampleAwarePage(allItems, input.SkipCount, input.MaxResultCount);

        // Map to DTOs
        var dtos = items.Select(x => new SampleAssignmentLogDto
        {
            TestRequestId = x.TestRequestId,
            QualityTaskId = x.QualityTaskId,
            ContractNumber = x.ContractNumber,
            TestRequestStatus = x.TestRequestStatus,
            RequestCompletionDate = x.RequestCompletionDate,
            SampleId = x.SampleId,
            SampleCode = x.SampleCode,
            SampleName = x.SampleName,
            SampleQuantity = x.SampleQuantity,
            SampleStatusText = x.SampleStatusText,
            IndicatorId = x.IndicatorId,
            LaboratoryTaskIndicatorId = x.LaboratoryTaskIndicatorId,
            IndicatorName = x.IndicatorName,
            IndicatorFormattedName = x.IndicatorFormattedName,
            MethodName = x.MethodName,
            MethodFormattedName = x.MethodFormattedName,
            Unit = x.Unit,
            TransferDate = x.TransferDate,
            TestDate = x.TestDate,
            Result = x.Result,
            TechnicalManagerName = x.TechnicalManagerName,
            TechnicianName = x.TechnicianName,
            EntryDate = x.EntryDate,
            QlktApprovalDate = x.QlktApprovalDate,
            IndicatorApprovalStatus = x.IndicatorApprovalStatus,
            // TestingStatus will be determined based on IndicatorApprovalStatus
            TestingStatus = DetermineTestingStatus(x.IndicatorApprovalStatus, x.TestDate, x.Result),
            QualityApprovalStatus = x.QualityApprovalStatus,
            IsReportPublished = x.IsReportPublished
        }).ToList();

        return new PagedResultDto<SampleAssignmentLogDto>(totalCount, dtos);
    }

    /// <summary>
    /// Determine TestingStatus based on IndicatorApprovalStatus and other fields
    /// </summary>
    private static TestingStatus? DetermineTestingStatus(
        IndicatorApprovalStatus approvalStatus,
        DateTime? testDate,
        string? result)
    {
        // If already has result, it's completed or pending approval
        if (!string.IsNullOrWhiteSpace(result))
        {
            return approvalStatus == IndicatorApprovalStatus.Approved
                ? TestingStatus.Completed
                : TestingStatus.PendingApproval;
        }

        // If test date is set but no result, it's in progress
        if (testDate.HasValue)
        {
            return TestingStatus.InProgress;
        }

        // Otherwise, not started
        return approvalStatus == IndicatorApprovalStatus.AcceptedForTesting
            ? TestingStatus.NotStarted
            : null;
    }

    /// <summary>
    /// Get a page of items while ensuring all indicators of the same sample stay together.
    /// If a sample spans across page boundary, include all its indicators on the current page.
    /// </summary>
    private static List<T> GetSampleAwarePage<T>(List<T> allItems, int skipCount, int maxResultCount) where T : class
    {
        if (allItems.Count == 0 || maxResultCount <= 0)
            return new List<T>();

        // Get SampleId property via reflection (works for anonymous types)
        var sampleIdProperty = typeof(T).GetProperty("SampleId");
        if (sampleIdProperty == null)
        {
            // Fallback to standard pagination if no SampleId property
            return allItems.Skip(skipCount).Take(maxResultCount).ToList();
        }

        Func<T, Guid> getSampleId = item => (Guid)sampleIdProperty.GetValue(item)!;

        // Find the actual start index (snap to sample boundary)
        var actualSkip = skipCount;
        if (skipCount > 0 && skipCount < allItems.Count)
        {
            var currentSampleId = getSampleId(allItems[skipCount]);
            // Check if we're in the middle of a sample group
            if (skipCount > 0 && getSampleId(allItems[skipCount - 1]) == currentSampleId)
            {
                // Move forward to find the start of next sample
                while (actualSkip < allItems.Count && getSampleId(allItems[actualSkip]) == currentSampleId)
                {
                    actualSkip++;
                }
            }
        }

        // Take items, but include complete sample at the end
        var result = new List<T>();
        var taken = 0;
        Guid? lastSampleId = null;

        for (var i = actualSkip; i < allItems.Count && (taken < maxResultCount || (lastSampleId.HasValue && getSampleId(allItems[i]) == lastSampleId.Value)); i++)
        {
            var item = allItems[i];
            var sampleId = getSampleId(item);

            result.Add(item);
            taken++;
            lastSampleId = sampleId;

            // If we've reached maxResultCount and the next item is a different sample, stop
            if (taken >= maxResultCount && i + 1 < allItems.Count && getSampleId(allItems[i + 1]) != sampleId)
            {
                break;
            }
        }

        return result;
    }

    #endregion
}
