using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Notifications;
using LabSpecimenManagement.TestRequests;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace LabSpecimenManagement.QualityManagement;

[Authorize]
public class QualityManagementAppService : ApplicationService, IQualityManagementAppService
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
    private readonly ITestRequestAppService _testRequestAppService;
    private readonly IRepository<SpecimenType, Guid> _specimenTypeRepository;
    private readonly INotificationCreator _notificationCreator;

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
        ITestRequestAppService testRequestAppService,
        IRepository<SpecimenType, Guid> specimenTypeRepository,
        INotificationCreator notificationCreator)
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
        _testRequestAppService = testRequestAppService;
        _specimenTypeRepository = specimenTypeRepository;
        _notificationCreator = notificationCreator;
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
                "contractnumber" => query.OrderBy(t => t.ContractNumber),
                "contractnumber desc" => query.OrderByDescending(t => t.ContractNumber),
                "transferdate" => query.OrderBy(t => t.TransferDate),
                "transferdate desc" => query.OrderByDescending(t => t.TransferDate),
                "requestcompletiondate" => query.OrderBy(t => t.RequestCompletionDate),
                "requestcompletiondate desc" => query.OrderByDescending(t => t.RequestCompletionDate),
                "status" => query.OrderBy(t => t.Status),
                "status desc" => query.OrderByDescending(t => t.Status),
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
            throw new UserFriendlyException("Chỉ tiêu này không ở trạng thái chờ phê duyệt.");
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
        if (labTask.TechnicalManagerId.HasValue)
        {
            var testRequest = await _testRequestRepository.GetAsync(labTask.TestRequestId);
            var sampleIndicator = await _sampleIndicatorRepository.FindAsync(labIndicator.TestRequestSampleIndicatorId);
            var sample = sampleIndicator != null ? await _sampleRepository.FindAsync(sampleIndicator.TestRequestSampleId) : null;
            var masterIndicator = sampleIndicator != null ? await _masterIndicatorRepository.FindAsync(sampleIndicator.IndicatorId) : null;
            var method = labIndicator.MethodId.HasValue ? await _testMethodRepository.FindAsync(labIndicator.MethodId.Value) : null;

            var qualityManagerName = CurrentUser.Name ?? CurrentUser.UserName ?? "Quản lý chất lượng";

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

        return await MapIndicatorToDto(indicator);
    }

    public async Task<QualityManagementIndicatorDto> RejectIndicatorAsync(Guid indicatorId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new UserFriendlyException("Vui lòng nhập lý do từ chối.");
        }

        var indicator = await _indicatorRepository.GetAsync(indicatorId);

        if (indicator.ApprovalStatus != QualityIndicatorApprovalStatus.PendingApproval)
        {
            throw new UserFriendlyException("Chỉ tiêu này không ở trạng thái chờ phê duyệt.");
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
        await _laboratoryTaskIndicatorRepository.UpdateAsync(labIndicator);

        // Add to rejection history
        await _rejectionHistoryRepository.InsertAsync(new IndicatorRejectionHistory(
            GuidGenerator.Create(),
            labIndicator.Id,
            reason,
            CurrentUser.Id ?? Guid.Empty
        ));

        // Update LaboratoryTask status back to Approved (for QLKT to review and resend)
        var labTask = await _laboratoryTaskRepository.GetAsync(labIndicator.LaboratoryTaskId);
        labTask.Status = LaboratoryTaskStatus.Approved;
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

    public async Task<QualityManagementIndicatorDto> UpdateIndicatorStandardAsync(Guid indicatorId, Guid? standardId)
    {
        var indicator = await _indicatorRepository.GetAsync(indicatorId);

        indicator.StandardId = standardId;

        // Auto-fill reference limit if standard is selected
        if (standardId.HasValue)
        {
            var labIndicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicator.LaboratoryTaskIndicatorId);
            var sampleIndicator = await _sampleIndicatorRepository.GetAsync(labIndicator.TestRequestSampleIndicatorId);

            if (labIndicator.MethodId.HasValue)
            {
                var standardIndicator = await _standardIndicatorRepository.FirstOrDefaultAsync(si =>
                    si.StandardId == standardId.Value &&
                    si.IndicatorId == sampleIndicator.IndicatorId &&
                    si.TestMethodId == labIndicator.MethodId.Value);

                if (standardIndicator != null)
                {
                    indicator.ReferenceLimit = $"{standardIndicator.ComparisonOperator} {standardIndicator.AllowedLimit}";
                }
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

        var indicator = await _indicatorRepository.GetAsync(indicatorId);
        var labIndicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicator.LaboratoryTaskIndicatorId);
        var sampleIndicator = await _sampleIndicatorRepository.GetAsync(labIndicator.TestRequestSampleIndicatorId);
        var masterIndicator = await _masterIndicatorRepository.GetAsync(sampleIndicator.IndicatorId);

        var dtos = new List<QualityIndicatorStatusHistoryDto>();
        foreach (var h in histories.OrderByDescending(x => x.CreationTime))
        {
            var creatorName = "";
            if (h.CreatorId.HasValue)
            {
                var user = await _userRepository.FindAsync(h.CreatorId.Value);
                creatorName = user?.Name ?? user?.UserName ?? "";
            }

            dtos.Add(new QualityIndicatorStatusHistoryDto
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

        return dtos;
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

            // Cache user names to avoid repeated queries
            var userCache = new Dictionary<Guid, string>();

            // Build ChangeHistoryBatches (new grouped format)
            foreach (var batch in batches.OrderByDescending(b => b.BatchTime))
            {
                var firstChange = batch.Changes.First();

                // Get creator name
                var creatorName = "";
                if (firstChange.CreatorId.HasValue)
                {
                    if (!userCache.TryGetValue(firstChange.CreatorId.Value, out creatorName!))
                    {
                        var user = await _userRepository.FindAsync(firstChange.CreatorId.Value);
                        creatorName = user?.Name ?? user?.UserName ?? "";
                        userCache[firstChange.CreatorId.Value] = creatorName;
                    }
                }

                // Get approver name
                var approvedByName = "";
                if (firstChange.ApprovedById.HasValue)
                {
                    if (!userCache.TryGetValue(firstChange.ApprovedById.Value, out approvedByName!))
                    {
                        var approver = await _userRepository.FindAsync(firstChange.ApprovedById.Value);
                        approvedByName = approver?.Name ?? approver?.UserName ?? "";
                        userCache[firstChange.ApprovedById.Value] = approvedByName;
                    }
                }

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
                    var creatorName = "";
                    if (h.CreatorId.HasValue)
                    {
                        var user = await _userRepository.FindAsync(h.CreatorId.Value);
                        creatorName = user?.Name ?? user?.UserName ?? "";
                    }

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

    public async Task PrintSingleResultAsync(Guid taskId, Guid indicatorId)
    {
        var task = await _taskRepository.GetAsync(taskId);
        var indicator = await _indicatorRepository.GetAsync(indicatorId);

        if (indicator.ApprovalStatus != QualityIndicatorApprovalStatus.Approved)
        {
            throw new UserFriendlyException("Chỉ có thể in kết quả đã được duyệt.");
        }

        // Update status to PendingSignature
        task.Status = QualityManagementStatus.PendingSignature;
        await _taskRepository.UpdateAsync(task);
    }

    public async Task PrintMixedResultAsync(Guid taskId, List<Guid> indicatorIds)
    {
        var task = await _taskRepository.GetAsync(taskId);

        foreach (var indicatorId in indicatorIds)
        {
            var indicator = await _indicatorRepository.GetAsync(indicatorId);
            if (indicator.ApprovalStatus != QualityIndicatorApprovalStatus.Approved)
            {
                throw new UserFriendlyException("Chỉ có thể in kết quả đã được duyệt.");
            }
        }

        // Update status to PendingSignature
        task.Status = QualityManagementStatus.PendingSignature;
        await _taskRepository.UpdateAsync(task);
    }

    public async Task SendResultAsync(Guid taskId)
    {
        var task = await _taskRepository.GetAsync(taskId);

        if (task.Status != QualityManagementStatus.Approved && task.Status != QualityManagementStatus.PendingSignature)
        {
            throw new UserFriendlyException("Chỉ có thể gửi kết quả khi trạng thái là 'Đã duyệt' hoặc 'Chờ ký'.");
        }

        task.Status = QualityManagementStatus.ResultSent;
        task.ResultSentDate = DateTime.Now;
        await _taskRepository.UpdateAsync(task);

        // Get current user name
        string? currentUserName = null;
        if (CurrentUser.Id.HasValue)
        {
            var user = await _userRepository.FindAsync(CurrentUser.Id.Value);
            currentUserName = user?.Name ?? user?.UserName;
        }

        // Also update TestRequest status and ResultSent info
        var testRequest = await _testRequestRepository.GetAsync(task.TestRequestId);
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
            throw new UserFriendlyException("Task này không phải là phê duyệt thay đổi.");
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
    }

    public async Task RejectChangesAsync(Guid taskId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new UserFriendlyException("Vui lòng nhập lý do từ chối.");
        }

        var task = await _taskRepository.GetAsync(taskId);

        if (task.TaskType != QualityTaskType.ChangeApproval)
        {
            throw new UserFriendlyException("Task này không phải là phê duyệt thay đổi.");
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
                string? createdByName = null;
                if (changeHistory.CreatorId.HasValue)
                {
                    var creator = await _userRepository.FindAsync(changeHistory.CreatorId.Value);
                    createdByName = creator?.Name ?? creator?.UserName;
                }

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

        string? approvedByName = null;
        if (indicator.ApprovedById.HasValue)
        {
            var approver = await _userRepository.FindAsync(indicator.ApprovedById.Value);
            approvedByName = approver?.Name ?? approver?.UserName;
        }

        string? specimenTypeName = null;
        if (sample.SpecimenTypeId.HasValue)
        {
            var specimenType = await _specimenTypeRepository.FindAsync(sample.SpecimenTypeId.Value);
            specimenTypeName = specimenType?.Name;
        }

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
            ApprovedById = indicator.ApprovedById,
            ApprovedByName = approvedByName
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
                // Get current user name
                string? currentUserName = null;
                if (CurrentUser.Id.HasValue)
                {
                    var user = await _userRepository.FindAsync(CurrentUser.Id.Value);
                    currentUserName = user?.Name ?? user?.UserName;
                }

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
