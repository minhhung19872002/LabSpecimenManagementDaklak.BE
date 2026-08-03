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
// Alias vì dự án có LabSpecimenManagement.Common.Exceptions.EntityNotFoundException riêng;
// repository.GetAsync throw loại của ABP nên phải trỏ đúng loại đó
using AbpEntityNotFoundException = Volo.Abp.Domain.Entities.EntityNotFoundException;

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

        // Filter by date range (dynamic based on DateFilterField)
        if (input.FromDate.HasValue || input.ToDate.HasValue)
        {
            var dateFilterField = input.DateFilterField?.ToLower();
            var fromDate = input.FromDate;
            var toDate = input.ToDate;
            var toDateEnd = toDate.HasValue ? toDate.Value.Date.AddDays(1) : (DateTime?)null;

            if (dateFilterField == "requestcompletiondate")
            {
                if (fromDate.HasValue)
                    query = query.Where(t => t.RequestCompletionDate >= fromDate.Value);
                if (toDateEnd.HasValue)
                    query = query.Where(t => t.RequestCompletionDate < toDateEnd.Value);
            }
            else if (dateFilterField == "contractdate")
            {
                var trDateQuery = await _testRequestRepository.GetQueryableAsync();
                if (fromDate.HasValue)
                    trDateQuery = trDateQuery.Where(tr => tr.ContractDate >= fromDate.Value);
                if (toDateEnd.HasValue)
                    trDateQuery = trDateQuery.Where(tr => tr.ContractDate < toDateEnd.Value);
                var matchingIds = trDateQuery.Select(tr => tr.Id);
                query = query.Where(t => matchingIds.Contains(t.TestRequestId));
            }
            else if (dateFilterField == "latestapprovaldate")
            {
                var indQuery = await _indicatorRepository.GetQueryableAsync();
                if (fromDate.HasValue)
                    query = query.Where(t => indQuery
                        .Where(i => i.QualityManagementTaskId == t.Id && i.IsPrinted && i.PrintedDate.HasValue)
                        .Max(i => (DateTime?)i.PrintedDate) >= fromDate.Value);
                if (toDateEnd.HasValue)
                    query = query.Where(t => indQuery
                        .Where(i => i.QualityManagementTaskId == t.Id && i.IsPrinted && i.PrintedDate.HasValue)
                        .Max(i => (DateTime?)i.PrintedDate) < toDateEnd.Value);
            }
            else if (dateFilterField == "latestindicatorapprovaldate")
            {
                var indQuery = await _indicatorRepository.GetQueryableAsync();
                if (fromDate.HasValue)
                    query = query.Where(t => indQuery
                        .Where(i => i.QualityManagementTaskId == t.Id
                            && i.ApprovalStatus == QualityIndicatorApprovalStatus.Approved
                            && i.ApprovalDate.HasValue)
                        .Max(i => (DateTime?)i.ApprovalDate) >= fromDate.Value);
                if (toDateEnd.HasValue)
                    query = query.Where(t => indQuery
                        .Where(i => i.QualityManagementTaskId == t.Id
                            && i.ApprovalStatus == QualityIndicatorApprovalStatus.Approved
                            && i.ApprovalDate.HasValue)
                        .Max(i => (DateTime?)i.ApprovalDate) < toDateEnd.Value);
            }
            else
            {
                var trDateQuery = await _testRequestRepository.GetQueryableAsync();
                if (fromDate.HasValue)
                    trDateQuery = trDateQuery.Where(tr => tr.UnderReviewDate >= fromDate.Value);
                if (toDateEnd.HasValue)
                    trDateQuery = trDateQuery.Where(tr => tr.UnderReviewDate < toDateEnd.Value);
                var matchingIds = trDateQuery.Select(tr => tr.Id);
                query = query.Where(t => matchingIds.Contains(t.TestRequestId));
            }
        }

        // Filter overdue: RequestCompletionDate - Now <= 2 days and not HandedOver
        if (input.IsOverdue == true)
        {
            var now = DateTime.Now;
            query = query.Where(t =>
                t.RequestCompletionDate.HasValue &&
                (t.RequestCompletionDate.Value - now).TotalDays <= 2 &&
                t.Status != QualityManagementStatus.HandedOver);
        }

        // Filter by RequestCompletionDate range (Ngày YC xong)
        if (input.RequestCompletionDateFrom.HasValue)
        {
            query = query.Where(t => t.RequestCompletionDate >= input.RequestCompletionDateFrom.Value);
        }
        if (input.RequestCompletionDateTo.HasValue)
        {
            query = query.Where(t => t.RequestCompletionDate <= input.RequestCompletionDateTo.Value);
        }

        // Filter by LaboratoryIds (task chứa ít nhất 1 indicator thuộc lab được chọn)
        if (input.LaboratoryIds != null && input.LaboratoryIds.Count > 0)
        {
            var labTaskQuery = await _laboratoryTaskRepository.GetQueryableAsync();
            var taskIdsWithLab = labTaskQuery
                .Where(lt => input.LaboratoryIds.Contains(lt.LaboratoryId))
                .Select(lt => lt.TestRequestId)
                .Distinct();
            query = query.Where(t => taskIdsWithLab.Contains(t.TestRequestId));
        }

        var totalCount = query.Count();

        // Join TestRequest to get ContractDate for sorting
        var trQueryable = await _testRequestRepository.GetQueryableAsync();
        var joinedQuery = query.Join(
            trQueryable,
            t => t.TestRequestId,
            tr => tr.Id,
            (t, tr) => new { Task = t, ContractDate = tr.ContractDate, UnderReviewDate = tr.UnderReviewDate });

        // Sorting
        var sortingKey = string.IsNullOrWhiteSpace(input.Sorting) ? SortingFields.ContractDateDesc : input.Sorting.ToLower();
        var indicatorQueryable = await _indicatorRepository.GetQueryableAsync();
        var sortedQuery = sortingKey switch
        {
            SortingFields.ContractNumber => joinedQuery.OrderBy(x => x.Task.ContractNumber),
            SortingFields.ContractNumberDesc => joinedQuery.OrderByDescending(x => x.Task.ContractNumber),
            SortingFields.TransferDate => joinedQuery.OrderBy(x => x.UnderReviewDate),
            SortingFields.TransferDateDesc => joinedQuery.OrderByDescending(x => x.UnderReviewDate),
            SortingFields.RequestCompletionDate => joinedQuery.OrderBy(x => x.Task.RequestCompletionDate),
            SortingFields.RequestCompletionDateDesc => joinedQuery.OrderByDescending(x => x.Task.RequestCompletionDate),
            SortingFields.Status => joinedQuery.OrderBy(x => x.Task.Status),
            SortingFields.StatusDesc => joinedQuery.OrderByDescending(x => x.Task.Status),
            SortingFields.ContractDate => joinedQuery.OrderBy(x => x.ContractDate),
            SortingFields.ContractDateDesc => joinedQuery.OrderByDescending(x => x.ContractDate),
            SortingFields.LatestApprovalDate => joinedQuery
                .OrderByDescending(x => indicatorQueryable.Any(i => i.QualityManagementTaskId == x.Task.Id && i.IsPrinted))
                .ThenBy(x => indicatorQueryable
                    .Where(i => i.QualityManagementTaskId == x.Task.Id && i.IsPrinted && i.PrintedDate.HasValue)
                    .Max(i => (DateTime?)i.PrintedDate)),
            SortingFields.LatestApprovalDateDesc => joinedQuery
                .OrderByDescending(x => indicatorQueryable.Any(i => i.QualityManagementTaskId == x.Task.Id && i.IsPrinted))
                .ThenByDescending(x => indicatorQueryable
                    .Where(i => i.QualityManagementTaskId == x.Task.Id && i.IsPrinted && i.PrintedDate.HasValue)
                    .Max(i => (DateTime?)i.PrintedDate)),
            SortingFields.LatestIndicatorApprovalDate => joinedQuery
                .OrderByDescending(x => indicatorQueryable.Any(i => i.QualityManagementTaskId == x.Task.Id
                    && i.ApprovalStatus == QualityIndicatorApprovalStatus.Approved))
                .ThenBy(x => indicatorQueryable
                    .Where(i => i.QualityManagementTaskId == x.Task.Id
                        && i.ApprovalStatus == QualityIndicatorApprovalStatus.Approved
                        && i.ApprovalDate.HasValue)
                    .Max(i => (DateTime?)i.ApprovalDate)),
            SortingFields.LatestIndicatorApprovalDateDesc => joinedQuery
                .OrderByDescending(x => indicatorQueryable.Any(i => i.QualityManagementTaskId == x.Task.Id
                    && i.ApprovalStatus == QualityIndicatorApprovalStatus.Approved))
                .ThenByDescending(x => indicatorQueryable
                    .Where(i => i.QualityManagementTaskId == x.Task.Id
                        && i.ApprovalStatus == QualityIndicatorApprovalStatus.Approved
                        && i.ApprovalDate.HasValue)
                    .Max(i => (DateTime?)i.ApprovalDate)),
            _ => joinedQuery.OrderByDescending(x => x.ContractDate)
        };

        // Pagination
        var tasks = sortedQuery
            .Select(x => x.Task)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        var dtos = await MapToListDtosAsync(tasks);

        return new PagedResultDto<QualityManagementTaskDto>(totalCount, dtos);
    }

    public async Task<QualityManagementTaskDto> GetAsync(Guid id)
    {
        var task = await _taskRepository.GetAsync(id);
        return await MapToDetailDtoAsync(task);
    }

    public async Task<QualityManagementTaskDto> GetFullDetailAsync(Guid id)
    {
        var task = await _taskRepository.GetAsync(id);
        var dto = await MapToDetailDtoAsync(task);
        dto.AllIndicators = await MapAllIndicatorsToDtoAsync(task);
        return dto;
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

        if (indicator.ApprovalStatus != QualityIndicatorApprovalStatus.PendingApproval &&
            indicator.ApprovalStatus != QualityIndicatorApprovalStatus.Approved)
        {
            throw new Common.Exceptions.InvalidOperationException("Chỉ tiêu này không ở trạng thái chờ phê duyệt hoặc đã phê duyệt.", "indicator");
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
        labIndicator.IsRejectedByQuality = true;
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

                if (indicator.ApprovalStatus != QualityIndicatorApprovalStatus.PendingApproval &&
                    indicator.ApprovalStatus != QualityIndicatorApprovalStatus.Approved)
                {
                    result.FailedCount++;
                    result.FailedIndicators.Add($"Chỉ tiêu không ở trạng thái hợp lệ để từ chối");
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
                labIndicator.IsRejectedByQuality = true;
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
            var sample = await _sampleRepository.GetAsync(sampleIndicator.TestRequestSampleId);

            // Try to find matching StandardIndicator
            StandardIndicator? standardIndicator = null;

            if (labIndicator.MethodId.HasValue)
            {
                // First try with exact method + specimenType match
                standardIndicator = await _standardIndicatorRepository.FirstOrDefaultAsync(si =>
                    si.StandardId == standardId.Value &&
                    si.IndicatorId == sampleIndicator.IndicatorId &&
                    si.TestMethodId == labIndicator.MethodId.Value &&
                    (!sample.SpecimenTypeId.HasValue || !si.SpecimenTypeId.HasValue || si.SpecimenTypeId == sample.SpecimenTypeId.Value));
            }

            // If not found with method, try matching by indicator + specimenType only
            if (standardIndicator == null)
            {
                standardIndicator = await _standardIndicatorRepository.FirstOrDefaultAsync(si =>
                    si.StandardId == standardId.Value &&
                    si.IndicatorId == sampleIndicator.IndicatorId &&
                    (!sample.SpecimenTypeId.HasValue || !si.SpecimenTypeId.HasValue || si.SpecimenTypeId == sample.SpecimenTypeId.Value));
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

        // Xác định ngày in: lấy ngày in sớm nhất của BẤT KỲ indicator nào trong task
        var allTaskIndicators = await _indicatorRepository.GetListAsync(
            x => x.QualityManagementTaskId == taskId);
        var printDate = allTaskIndicators
            .Select(i => i.PrintedDate)
            .Where(d => d.HasValue)
            .OrderBy(d => d)
            .FirstOrDefault() ?? DateTime.Now;

        // Generate PDF result form
        var result = await _resultFormAppService.GenerateSingleIndicatorResultFormAsync(new GenerateSingleIndicatorInput
        {
            TaskId = taskId,
            IndicatorId = indicatorId,
            Note = note,
            TemplateName = templateName,
            VicePresidentId = vicePresidentId,
            PrintDate = printDate
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

        // Đánh dấu indicator đã in (phát hành) - chỉ set PrintedDate lần đầu
        if (!indicator.PrintedDate.HasValue)
        {
            indicator.PrintedDate = printDate;
        }
        indicator.IsPrinted = true;
        await _indicatorRepository.UpdateAsync(indicator);

        // Cập nhật task status dựa vào số chỉ tiêu đã in
        await UpdateTaskStatusAfterPrintAsync(task);

        return new PrintResultOutputDto
        {
            FileName = result.FileName
        };
    }

    public async Task<PrintResultOutputDto> PrintMixedResultAsync(Guid taskId, PrintMixedDto input)
    {
        var task = await _taskRepository.GetAsync(taskId);
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

        // Xác định ngày in: lấy ngày in sớm nhất của BẤT KỲ indicator nào trong task
        var allTaskIndicators = await _indicatorRepository.GetListAsync(
            x => x.QualityManagementTaskId == taskId);
        var printDate = allTaskIndicators
            .Select(i => i.PrintedDate)
            .Where(d => d.HasValue)
            .OrderBy(d => d)
            .FirstOrDefault() ?? DateTime.Now;

        // Generate PDF result form
        var result = await _resultFormAppService.GenerateMixedIndicatorResultFormAsync(new GenerateMixedIndicatorInput
        {
            TaskId = taskId,
            IndicatorIds = input.IndicatorIds,
            Note = input.Note,
            TemplateName = input.TemplateName,
            VicePresidentId = input.VicePresidentId,
            PrintDate = printDate
        });

        // Tạo record PublishedReport (log lịch sử in) + lưu structured note data
        var publishedReport = new PublishedReport(
            GuidGenerator.Create(),
            task.TestRequestId,
            task.Id,
            result.FileName,
            "Mixed",
            input.Note,
            JsonSerializer.Serialize(input.IndicatorIds)
        );
        publishedReport.SelectedMasterNoteIds = input.SelectedMasterNoteIds?.Any() == true
            ? JsonSerializer.Serialize(input.SelectedMasterNoteIds)
            : null;
        publishedReport.FreeTextNote = input.FreeTextNote;
        publishedReport.VicePresidentId = input.VicePresidentId;
        await _publishedReportRepository.InsertAsync(publishedReport);

        // Đánh dấu tất cả indicators đã in (phát hành) - chỉ set PrintedDate lần đầu
        foreach (var indicator in indicatorsToPrint)
        {
            if (!indicator.PrintedDate.HasValue)
            {
                indicator.PrintedDate = printDate;
            }
            indicator.IsPrinted = true;
            await _indicatorRepository.UpdateAsync(indicator);
        }

        // Cập nhật task status dựa vào số chỉ tiêu đã in
        await UpdateTaskStatusAfterPrintAsync(task);

        return new PrintResultOutputDto
        {
            FileName = result.FileName
        };
    }

    public async Task<PrintMultiResultOutputDto> PrintMixedPerSampleResultAsync(Guid taskId, PrintMixedDto input)
    {
        var task = await _taskRepository.GetAsync(taskId);
        var indicatorsToPrint = new List<QualityManagementIndicator>();

        foreach (var indicatorId in input.IndicatorIds)
        {
            var indicator = await _indicatorRepository.GetAsync(indicatorId);
            if (indicator.ApprovalStatus != QualityIndicatorApprovalStatus.Approved)
                throw new Common.Exceptions.InvalidOperationException("Chỉ có thể in kết quả đã được duyệt.", "indicator");
            indicatorsToPrint.Add(indicator);
        }

        // Xác định ngày in: lấy ngày in sớm nhất của BẤT KỲ indicator nào trong task
        var allTaskIndicators = await _indicatorRepository.GetListAsync(
            x => x.QualityManagementTaskId == taskId);
        var printDate = allTaskIndicators
            .Select(i => i.PrintedDate)
            .Where(d => d.HasValue)
            .OrderBy(d => d)
            .FirstOrDefault() ?? DateTime.Now;

        var results = await _resultFormAppService.GenerateMixedPerSampleResultFormAsync(new GenerateMixedIndicatorInput
        {
            TaskId = taskId,
            IndicatorIds = input.IndicatorIds,
            Note = input.Note,
            TemplateName = input.TemplateName,
            VicePresidentId = input.VicePresidentId,
            PrintDate = printDate
        });

        var serializedMasterNoteIds = input.SelectedMasterNoteIds?.Any() == true
            ? JsonSerializer.Serialize(input.SelectedMasterNoteIds)
            : null;

        foreach (var result in results)
        {
            var publishedReport = new PublishedReport(
                GuidGenerator.Create(),
                task.TestRequestId,
                task.Id,
                result.FileName,
                "MixedPerSample",
                input.Note,
                JsonSerializer.Serialize(input.IndicatorIds)
            );
            publishedReport.SelectedMasterNoteIds = serializedMasterNoteIds;
            publishedReport.FreeTextNote = input.FreeTextNote;
            publishedReport.VicePresidentId = input.VicePresidentId;
            await _publishedReportRepository.InsertAsync(publishedReport);
        }

        // Chỉ set PrintedDate lần đầu
        foreach (var indicator in indicatorsToPrint)
        {
            if (!indicator.PrintedDate.HasValue)
            {
                indicator.PrintedDate = printDate;
            }
            indicator.IsPrinted = true;
            await _indicatorRepository.UpdateAsync(indicator);
        }

        await UpdateTaskStatusAfterPrintAsync(task);

        return new PrintMultiResultOutputDto
        {
            Results = results.Select(r => new PrintResultItemDto
            {
                FileName = r.FileName,
                SampleCode = r.SampleCode ?? "",
                TemplateName = input.TemplateName ?? "QLNN"
            }).ToList()
        };
    }

    public async Task<PrintNoteConfigDto?> GetLastPrintNoteConfigAsync(Guid taskId)
    {
        var lastReport = (await _publishedReportRepository.GetQueryableAsync())
            .Where(r => r.QualityManagementTaskId == taskId
                && (r.ReportType == "Mixed" || r.ReportType == "MixedPerSample"))
            .OrderByDescending(r => r.CreationTime)
            .FirstOrDefault();

        if (lastReport == null)
            return null;

        var masterNoteIds = new List<Guid>();
        if (!string.IsNullOrEmpty(lastReport.SelectedMasterNoteIds))
        {
            masterNoteIds = JsonSerializer.Deserialize<List<Guid>>(lastReport.SelectedMasterNoteIds) ?? new();
        }

        return new PrintNoteConfigDto
        {
            SelectedMasterNoteIds = masterNoteIds,
            FreeTextNote = lastReport.FreeTextNote,
            VicePresidentId = lastReport.VicePresidentId,
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

        // Xác định ngày in: lấy ngày in sớm nhất của BẤT KỲ indicator nào trong task
        var allTaskIndicators = await _indicatorRepository.GetListAsync(
            x => x.QualityManagementTaskId == taskId);
        var printDate = allTaskIndicators
            .Select(i => i.PrintedDate)
            .Where(d => d.HasValue)
            .OrderBy(d => d)
            .FirstOrDefault() ?? DateTime.Now;

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
                    VicePresidentId = input.VicePresidentId,
                    PrintDate = printDate
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

        // Chỉ set PrintedDate lần đầu
        foreach (var indicator in indicatorsToPrint)
        {
            if (!indicator.PrintedDate.HasValue)
            {
                indicator.PrintedDate = printDate;
            }
            indicator.IsPrinted = true;
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

            var testRequest = await _testRequestRepository.FindAsync(task.TestRequestId);

            result.Add(new QualityManagerPendingTaskDto
            {
                Id = task.Id,
                TestRequestId = task.TestRequestId,
                ContractNumber = task.ContractNumber,
                TaskName = taskName,
                TransferDate = testRequest?.UnderReviewDate,
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
        // Dùng chung logic với bản batch để không thể lệch nhau
        var dtos = await MapToListDtosAsync(new List<QualityManagementTask> { task });
        return dtos[0];
    }

    // Batch toàn bộ dữ liệu liên quan cho cả trang
    // (thay vì query riêng cho MỖI task)
    private async Task<List<QualityManagementTaskDto>> MapToListDtosAsync(List<QualityManagementTask> tasks)
    {
        var dtos = new List<QualityManagementTaskDto>();
        if (tasks.Count == 0)
        {
            return dtos;
        }

        var taskIds = tasks.Select(t => t.Id).ToList();
        var testRequestIds = tasks.Select(t => t.TestRequestId).Distinct().ToList();

        var testRequestById = (await _testRequestRepository.GetListAsync(tr => testRequestIds.Contains(tr.Id)))
            .ToDictionary(tr => tr.Id);

        var allLabTasks = await _laboratoryTaskRepository.GetListAsync(lt => testRequestIds.Contains(lt.TestRequestId));
        var labTasksByRequest = allLabTasks.ToLookup(lt => lt.TestRequestId);

        var allLabIds = allLabTasks.Select(lt => lt.LaboratoryId).Distinct().ToList();
        var allLabs = allLabIds.Count == 0
            ? new List<Laboratory>()
            : await _laboratoryRepository.GetListAsync(l => allLabIds.Contains(l.Id));

        var allDeptIds = allLabs.Select(l => l.DepartmentId).Distinct().ToList();
        var deptDict = allDeptIds.Count == 0
            ? new Dictionary<Guid, string>()
            : (await _departmentRepository.GetListAsync(d => allDeptIds.Contains(d.Id)))
                .ToDictionary(d => d.Id, d => d.Name);

        var allManagerIds = allLabTasks.Where(lt => lt.TechnicalManagerId.HasValue)
            .Select(lt => lt.TechnicalManagerId!.Value).Distinct().ToList();
        var allManagers = allManagerIds.Count == 0
            ? new List<Volo.Abp.Identity.IdentityUser>()
            : await _userRepository.GetListAsync(u => allManagerIds.Contains(u.Id));

        var allIndicators = await _indicatorRepository.GetListAsync(i => taskIds.Contains(i.QualityManagementTaskId));
        var indicatorsByTask = allIndicators.ToLookup(i => i.QualityManagementTaskId);

        // TotalIndicatorCount hiển thị phải là tổng chỉ tiêu của phiếu yêu cầu,
        // không phải số chỉ tiêu đã được gửi vào scope QLCL.
        var allSamples = await _sampleRepository.GetListAsync(s => testRequestIds.Contains(s.TestRequestId));
        var sampleById = allSamples.ToDictionary(s => s.Id);
        var allSampleIds = allSamples.Select(s => s.Id).ToList();
        var allSampleIndicators = allSampleIds.Count == 0
            ? new List<TestRequestSampleIndicator>()
            : await _sampleIndicatorRepository.GetListAsync(si => allSampleIds.Contains(si.TestRequestSampleId));

        var allMasterIndicatorIds = allSampleIndicators.Select(si => si.IndicatorId).Distinct().ToList();
        var masterIndicatorById = allMasterIndicatorIds.Count == 0
            ? new Dictionary<Guid, Indicator>()
            : (await _masterIndicatorRepository.GetListAsync(i => allMasterIndicatorIds.Contains(i.Id)))
                .ToDictionary(i => i.Id);

        // Parent của combo chỉ dùng để nhóm hiển thị; tổng thực tế chỉ tính children và standalone.
        var requestParentKeys = new HashSet<(Guid SampleId, Guid ParentIndicatorId, Guid? MethodId)>();
        foreach (var sampleIndicator in allSampleIndicators)
        {
            if (masterIndicatorById.TryGetValue(sampleIndicator.IndicatorId, out var masterIndicator) &&
                masterIndicator.ParentIndicatorId.HasValue)
            {
                requestParentKeys.Add((
                    sampleIndicator.TestRequestSampleId,
                    masterIndicator.ParentIndicatorId.Value,
                    sampleIndicator.TestMethodId));
            }
        }

        var activeSampleIndicatorsByRequest = allSampleIndicators
            .Where(si => sampleById.ContainsKey(si.TestRequestSampleId))
            .Where(si => !requestParentKeys.Contains((
                si.TestRequestSampleId,
                si.IndicatorId,
                si.TestMethodId)))
            .ToLookup(si => sampleById[si.TestRequestSampleId].TestRequestId);

        // Pending/Approved cũng áp dụng cùng quy tắc loại parent combo.
        var qualityLabIndicatorIds = allIndicators
            .Select(i => i.LaboratoryTaskIndicatorId)
            .Distinct()
            .ToList();
        var qualityLabIndicatorById = qualityLabIndicatorIds.Count == 0
            ? new Dictionary<Guid, LaboratoryTaskIndicator>()
            : (await _laboratoryTaskIndicatorRepository.GetListAsync(li => qualityLabIndicatorIds.Contains(li.Id)))
                .ToDictionary(li => li.Id);
        var sampleIndicatorById = allSampleIndicators.ToDictionary(si => si.Id);

        var qualityParentKeys = new HashSet<(Guid TaskId, Guid SampleId, Guid ParentIndicatorId, Guid? MethodId)>();
        foreach (var qualityIndicator in allIndicators)
        {
            if (!qualityLabIndicatorById.TryGetValue(qualityIndicator.LaboratoryTaskIndicatorId, out var labIndicator) ||
                !sampleIndicatorById.TryGetValue(labIndicator.TestRequestSampleIndicatorId, out var sampleIndicator) ||
                !masterIndicatorById.TryGetValue(sampleIndicator.IndicatorId, out var masterIndicator) ||
                !masterIndicator.ParentIndicatorId.HasValue)
            {
                continue;
            }

            qualityParentKeys.Add((
                qualityIndicator.QualityManagementTaskId,
                sampleIndicator.TestRequestSampleId,
                masterIndicator.ParentIndicatorId.Value,
                labIndicator.MethodId));
        }

        var activeQualityIndicatorsByTask = allIndicators
            .Where(qualityIndicator =>
            {
                if (!qualityLabIndicatorById.TryGetValue(qualityIndicator.LaboratoryTaskIndicatorId, out var labIndicator) ||
                    !sampleIndicatorById.TryGetValue(labIndicator.TestRequestSampleIndicatorId, out var sampleIndicator))
                {
                    return true;
                }

                return !qualityParentKeys.Contains((
                    qualityIndicator.QualityManagementTaskId,
                    sampleIndicator.TestRequestSampleId,
                    sampleIndicator.IndicatorId,
                    labIndicator.MethodId));
            })
            .ToLookup(i => i.QualityManagementTaskId);

        foreach (var task in tasks)
        {
            testRequestById.TryGetValue(task.TestRequestId, out var testRequest);

            // Get laboratory names — giữ thứ tự theo danh sách labs như bản gốc
            var labTasks = labTasksByRequest[task.TestRequestId].ToList();
            var labIds = labTasks.Select(lt => lt.LaboratoryId).Distinct().ToHashSet();
            var labs = allLabs.Where(l => labIds.Contains(l.Id)).ToList();
            var labNames = string.Join(", ", labs.Select(l => deptDict.TryGetValue(l.DepartmentId, out var name) ? name : ""));

            // Get technical manager names (QLKT của phòng thử nghiệm)
            var managerIds = labTasks.Where(lt => lt.TechnicalManagerId.HasValue)
                .Select(lt => lt.TechnicalManagerId!.Value).Distinct().ToHashSet();
            var managers = allManagers.Where(m => managerIds.Contains(m.Id)).ToList();
            var managerNames = string.Join(", ", managers.Select(m =>
            {
                var fullName = $"{m.Surname} {m.Name}".Trim();
                return string.IsNullOrWhiteSpace(fullName) ? m.UserName : fullName;
            }));

            // Get latest printed date from published indicators
            var indicators = indicatorsByTask[task.Id].ToList();
            var activeQualityIndicators = activeQualityIndicatorsByTask[task.Id].ToList();
            var latestPrintedDate = indicators
                .Where(i => i.IsPrinted && i.PrintedDate.HasValue)
                .Select(i => i.PrintedDate!.Value)
                .DefaultIfEmpty()
                .Max();

            var latestIndicatorApprovalDate = indicators
                .Where(i => i.ApprovalStatus == QualityIndicatorApprovalStatus.Approved && i.ApprovalDate.HasValue)
                .Select(i => i.ApprovalDate!.Value)
                .DefaultIfEmpty()
                .Max();

            dtos.Add(new QualityManagementTaskDto
            {
                Id = task.Id,
                TestRequestId = task.TestRequestId,
                ContractNumber = task.ContractNumber,
                TaskType = task.TaskType,
                TotalSampleCount = task.TotalSampleCount,
                TotalIndicatorCount = activeSampleIndicatorsByRequest[task.TestRequestId].Count(),
                PendingApprovalIndicatorCount = activeQualityIndicators.Count(i =>
                    i.ApprovalStatus == QualityIndicatorApprovalStatus.PendingApproval),
                ApprovedIndicatorCount = activeQualityIndicators.Count(i =>
                    i.ApprovalStatus == QualityIndicatorApprovalStatus.Approved),
                LaboratoryNames = labNames,
                TransferDate = testRequest?.UnderReviewDate,
                RequestCompletionDate = task.RequestCompletionDate,
                Status = task.Status,
                HandoverDate = task.HandoverDate,
                HandoverByUserName = task.HandoverByUserName,
                HandoverNote = task.HandoverNote,
                CustomerName = testRequest?.CustomerName,
                TestRequestStatus = testRequest?.Status,
                TechnicalManagerNames = managerNames,
                ChangeHistoryId = task.ChangeHistoryId,
                LatestApprovalDate = latestPrintedDate == default ? null : latestPrintedDate,
                LatestIndicatorApprovalDate = latestIndicatorApprovalDate == default ? null : latestIndicatorApprovalDate,
                CreationTime = task.CreationTime,
                CreatorId = task.CreatorId,
                LastModificationTime = task.LastModificationTime,
                LastModifierId = task.LastModifierId
            });
        }

        return dtos;
    }

    private async Task<QualityManagementTaskDto> MapToDetailDtoAsync(QualityManagementTask task)
    {
        var dto = await MapToListDtoAsync(task);

        // Load indicators (batch)
        var indicators = await _indicatorRepository.GetListAsync(i => i.QualityManagementTaskId == task.Id);
        dto.Indicators = await MapIndicatorsToDtosAsync(indicators);

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
        // Dùng chung logic với bản batch để không thể lệch nhau
        var dtos = await MapIndicatorsToDtosAsync(new List<QualityManagementIndicator> { indicator });
        return dtos[0];
    }

    // Batch toàn bộ dữ liệu liên quan cho danh sách indicators trong ~14 query
    // (thay vì ~14 query cho MỖI indicator như trước)
    private async Task<List<QualityManagementIndicatorDto>> MapIndicatorsToDtosAsync(List<QualityManagementIndicator> indicators)
    {
        var result = new List<QualityManagementIndicatorDto>();
        if (indicators.Count == 0)
        {
            return result;
        }

        var labIndicatorIds = indicators.Select(i => i.LaboratoryTaskIndicatorId).Distinct().ToList();
        var labIndicatorById = (await _laboratoryTaskIndicatorRepository.GetListAsync(li => labIndicatorIds.Contains(li.Id)))
            .ToDictionary(li => li.Id);

        var sampleIndicatorIds = labIndicatorById.Values.Select(li => li.TestRequestSampleIndicatorId).Distinct().ToList();
        var sampleIndicatorById = sampleIndicatorIds.Count == 0
            ? new Dictionary<Guid, TestRequestSampleIndicator>()
            : (await _sampleIndicatorRepository.GetListAsync(si => sampleIndicatorIds.Contains(si.Id))).ToDictionary(si => si.Id);

        var sampleIds = sampleIndicatorById.Values.Select(si => si.TestRequestSampleId).Distinct().ToList();
        var sampleById = sampleIds.Count == 0
            ? new Dictionary<Guid, TestRequestSample>()
            : (await _sampleRepository.GetListAsync(s => sampleIds.Contains(s.Id))).ToDictionary(s => s.Id);

        var masterIndicatorIds = sampleIndicatorById.Values.Select(si => si.IndicatorId).Distinct().ToList();
        var masterIndicatorById = masterIndicatorIds.Count == 0
            ? new Dictionary<Guid, Indicator>()
            : (await _masterIndicatorRepository.GetListAsync(mi => masterIndicatorIds.Contains(mi.Id))).ToDictionary(mi => mi.Id);

        var labTaskIds = labIndicatorById.Values.Select(li => li.LaboratoryTaskId).Distinct().ToList();
        var labTaskById = labTaskIds.Count == 0
            ? new Dictionary<Guid, LaboratoryTask>()
            : (await _laboratoryTaskRepository.GetListAsync(lt => labTaskIds.Contains(lt.Id))).ToDictionary(lt => lt.Id);

        var labIds = labTaskById.Values.Select(lt => lt.LaboratoryId).Distinct().ToList();
        var labById = labIds.Count == 0
            ? new Dictionary<Guid, Laboratory>()
            : (await _laboratoryRepository.GetListAsync(l => labIds.Contains(l.Id))).ToDictionary(l => l.Id);

        var deptIds = labById.Values.Select(l => l.DepartmentId).Distinct().ToList();
        var deptById = deptIds.Count == 0
            ? new Dictionary<Guid, Department>()
            : (await _departmentRepository.GetListAsync(d => deptIds.Contains(d.Id))).ToDictionary(d => d.Id);

        var testRequestIds = labTaskById.Values.Select(lt => lt.TestRequestId).Distinct().ToList();
        var testRequestById = testRequestIds.Count == 0
            ? new Dictionary<Guid, TestRequest>()
            : (await _testRequestRepository.GetListAsync(tr => testRequestIds.Contains(tr.Id))).ToDictionary(tr => tr.Id);

        var methodIds = labIndicatorById.Values.Where(li => li.MethodId.HasValue).Select(li => li.MethodId!.Value).Distinct().ToList();
        var methodById = methodIds.Count == 0
            ? new Dictionary<Guid, TestMethod>()
            : (await _testMethodRepository.GetListAsync(m => methodIds.Contains(m.Id))).ToDictionary(m => m.Id);

        var allStandardIds = indicators
            .Select(i => labIndicatorById.TryGetValue(i.LaboratoryTaskIndicatorId, out var li)
                ? (li.StandardId ?? i.StandardId)
                : i.StandardId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        var standardById = allStandardIds.Count == 0
            ? new Dictionary<Guid, Standard>()
            : (await _standardRepository.GetListAsync(s => allStandardIds.Contains(s.Id))).ToDictionary(s => s.Id);
        var allStandardIndicators = allStandardIds.Count == 0
            ? new List<StandardIndicator>()
            : await _standardIndicatorRepository.GetListAsync(si => allStandardIds.Contains(si.StandardId));

        var approverNames = await _userNameProvider.GetUserNamesAsync(indicators.Select(i => i.ApprovedById));

        var specimenTypeIds = sampleById.Values.Where(s => s.SpecimenTypeId.HasValue).Select(s => s.SpecimenTypeId!.Value).Distinct().ToList();
        var specimenTypeById = specimenTypeIds.Count == 0
            ? new Dictionary<Guid, SpecimenType>()
            : (await _specimenTypeRepository.GetListAsync(st => specimenTypeIds.Contains(st.Id))).ToDictionary(st => st.Id);

        var imagesByLabIndicator = (await _indicatorImageRepository.GetListAsync(x => labIndicatorIds.Contains(x.LaboratoryTaskIndicatorId)))
            .ToLookup(x => x.LaboratoryTaskIndicatorId);

        var allNoteIds = labIndicatorById.Values
            .SelectMany(li => ParseMasterDataNoteIds(li.MasterDataNoteIds))
            .Distinct()
            .ToList();
        var noteById = allNoteIds.Count == 0
            ? new Dictionary<Guid, Note>()
            : (await _noteRepository.GetListAsync(n => allNoteIds.Contains(n.Id))).ToDictionary(n => n.Id);

        var mapContext = new QmIndicatorMapContext(
            labIndicatorById, sampleIndicatorById, sampleById, masterIndicatorById,
            labTaskById, labById, deptById, testRequestById, methodById,
            standardById, allStandardIndicators, approverNames, specimenTypeById,
            imagesByLabIndicator, noteById);

        foreach (var indicator in indicators)
        {
            result.Add(MapIndicatorToDtoCore(indicator, mapContext));
        }

        return result;
    }

    // Gom dữ liệu đã batch-load để map từng indicator mà không cần query thêm
    private sealed record QmIndicatorMapContext(
        Dictionary<Guid, LaboratoryTaskIndicator> LabIndicatorById,
        Dictionary<Guid, TestRequestSampleIndicator> SampleIndicatorById,
        Dictionary<Guid, TestRequestSample> SampleById,
        Dictionary<Guid, Indicator> MasterIndicatorById,
        Dictionary<Guid, LaboratoryTask> LabTaskById,
        Dictionary<Guid, Laboratory> LabById,
        Dictionary<Guid, Department> DeptById,
        Dictionary<Guid, TestRequest> TestRequestById,
        Dictionary<Guid, TestMethod> MethodById,
        Dictionary<Guid, Standard> StandardById,
        List<StandardIndicator> AllStandardIndicators,
        Dictionary<Guid, string> ApproverNames,
        Dictionary<Guid, SpecimenType> SpecimenTypeById,
        ILookup<Guid, IndicatorImage> ImagesByLabIndicator,
        Dictionary<Guid, Note> NoteById);

    private QualityManagementIndicatorDto MapIndicatorToDtoCore(
        QualityManagementIndicator indicator,
        QmIndicatorMapContext context)
    {
        var (labIndicatorById, sampleIndicatorById, sampleById, masterIndicatorById,
            labTaskById, labById, deptById, testRequestById, methodById,
            standardById, allStandardIndicators, approverNames, specimenTypeById,
            imagesByLabIndicator, noteById) = context;

        // Giữ nguyên semantics GetAsync: thiếu entity bắt buộc -> EntityNotFoundException
        var labIndicator = labIndicatorById.GetValueOrDefault(indicator.LaboratoryTaskIndicatorId)
            ?? throw new AbpEntityNotFoundException(typeof(LaboratoryTaskIndicator), indicator.LaboratoryTaskIndicatorId);
        var sampleIndicator = sampleIndicatorById.GetValueOrDefault(labIndicator.TestRequestSampleIndicatorId)
            ?? throw new AbpEntityNotFoundException(typeof(TestRequestSampleIndicator), labIndicator.TestRequestSampleIndicatorId);
        var sample = sampleById.GetValueOrDefault(sampleIndicator.TestRequestSampleId)
            ?? throw new AbpEntityNotFoundException(typeof(TestRequestSample), sampleIndicator.TestRequestSampleId);
        var masterIndicator = masterIndicatorById.GetValueOrDefault(sampleIndicator.IndicatorId)
            ?? throw new AbpEntityNotFoundException(typeof(Indicator), sampleIndicator.IndicatorId);

        var labTask = labTaskById.GetValueOrDefault(labIndicator.LaboratoryTaskId)
            ?? throw new AbpEntityNotFoundException(typeof(LaboratoryTask), labIndicator.LaboratoryTaskId);
        var lab = labById.GetValueOrDefault(labTask.LaboratoryId)
            ?? throw new AbpEntityNotFoundException(typeof(Laboratory), labTask.LaboratoryId);
        var labDept = deptById.GetValueOrDefault(lab.DepartmentId)
            ?? throw new AbpEntityNotFoundException(typeof(Department), lab.DepartmentId);

        string? methodName = null;
        string? methodFormattedName = null;
        if (labIndicator.MethodId.HasValue)
        {
            var method = methodById.GetValueOrDefault(labIndicator.MethodId.Value);
            methodName = method?.Name;
            methodFormattedName = method?.FormattedName;
        }

        if (!string.IsNullOrWhiteSpace(labIndicator.SubContractMethodName))
        {
            methodName = labIndicator.SubContractMethodName;
            methodFormattedName = null;
        }

        string? standardName = null;
        string? comparisonOperator = null;
        string? allowedLimit = null;
        string? lowerOperator = null;
        string? lowerLimit = null;
        string? upperOperator = null;
        string? upperLimit = null;

        // Try to get standard info from LaboratoryTaskIndicator first (KTV's selection)
        var standardId = labIndicator.StandardId ?? indicator.StandardId;
        if (standardId.HasValue)
        {
            standardName = standardById.GetValueOrDefault(standardId.Value)?.Name;

            // Get comparison operator and allowed limit from StandardIndicator
            StandardIndicator? standardIndicator = null;

            if (labIndicator.MethodId.HasValue)
            {
                // First try with exact method match
                standardIndicator = allStandardIndicators.FirstOrDefault(si =>
                    si.StandardId == standardId.Value &&
                    si.IndicatorId == sampleIndicator.IndicatorId &&
                    si.TestMethodId == labIndicator.MethodId.Value);
            }

            // If not found with method, try matching by indicator only
            if (standardIndicator == null)
            {
                standardIndicator = allStandardIndicators.FirstOrDefault(si =>
                    si.StandardId == standardId.Value &&
                    si.IndicatorId == sampleIndicator.IndicatorId);
            }

            if (standardIndicator != null)
            {
                comparisonOperator = standardIndicator.ComparisonOperator;
                allowedLimit = standardIndicator.AllowedLimit;
                lowerOperator = standardIndicator.LowerOperator;
                lowerLimit = standardIndicator.LowerLimit;
                upperOperator = standardIndicator.UpperOperator;
                upperLimit = standardIndicator.UpperLimit;
            }
        }

        // Giữ semantics GetUserNameAsync: null nếu không có id hoặc user không tồn tại
        var approvedByName = indicator.ApprovedById.HasValue && approverNames.TryGetValue(indicator.ApprovedById.Value, out var an)
            ? an
            : null;

        string? specimenTypeName = null;
        if (sample.SpecimenTypeId.HasValue)
        {
            specimenTypeName = specimenTypeById.GetValueOrDefault(sample.SpecimenTypeId.Value)?.Name;
        }

        // Get indicator images
        var indicatorImages = imagesByLabIndicator[indicator.LaboratoryTaskIndicatorId].ToList();

        return new QualityManagementIndicatorDto
        {
            Id = indicator.Id,
            QualityManagementIndicatorId = indicator.Id,
            TestRequestSampleIndicatorId = sampleIndicator.Id,
            LaboratoryTaskIndicatorId = indicator.LaboratoryTaskIndicatorId,
            LaboratoryTaskId = labTask.Id,
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
            LowerOperator = lowerOperator,
            LowerLimit = lowerLimit,
            UpperOperator = upperOperator,
            UpperLimit = upperLimit,
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
            MasterDataNotes = GetMasterDataNoteContentsFromDict(labIndicator.MasterDataNoteIds, noteById),
            ParentIndicatorId = masterIndicator.ParentIndicatorId,
            TechnicalManagerName = null,
            TechnicianId = labIndicator.TechnicianId,
            QlktApprovalDate = labIndicator.ApprovalDate,
            IndicatorApprovalStatus = labIndicator.ApprovalStatus,
            TestingStatus = DetermineTestingStatus(
                labIndicator.ApprovalStatus,
                labIndicator.TestDate,
                labIndicator.Result),
            QlclDisplayStatus = MapQLCLDisplayStatus(
                (testRequestById.GetValueOrDefault(labTask.TestRequestId)
                    ?? throw new AbpEntityNotFoundException(typeof(TestRequest), labTask.TestRequestId)).Status,
                labIndicator.ApprovalStatus,
                indicator.ApprovalStatus,
                indicator.IsPrinted,
                labIndicator.TechnicianId,
                labIndicator.MethodId,
                labIndicator.TestDate,
                labIndicator.Result),
            IsInQualityScope = true
        };
    }

    // Parse MasterDataNoteIds (JSON array hoặc comma-separated) rồi lấy content từ dict đã batch-load;
    // giữ thứ tự note theo chuỗi gốc, bỏ note không tồn tại hoặc content rỗng
    private static List<string> GetMasterDataNoteContentsFromDict(string? masterDataNoteIds, Dictionary<Guid, Note> noteById)
    {
        if (string.IsNullOrWhiteSpace(masterDataNoteIds))
        {
            return new List<string>();
        }

        var noteContents = new List<string>();

        var noteIdStrings = masterDataNoteIds.Contains("[")
            ? JsonSerializer.Deserialize<List<string>>(masterDataNoteIds) ?? new List<string>()
            : masterDataNoteIds.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();

        foreach (var idStr in noteIdStrings)
        {
            if (Guid.TryParse(idStr, out var noteId))
            {
                var note = noteById.GetValueOrDefault(noteId);
                if (note != null && !string.IsNullOrWhiteSpace(note.Content))
                {
                    noteContents.Add(note.Content);
                }
            }
        }

        return noteContents;
    }

    private async Task<List<QualityManagementIndicatorDto>> MapAllIndicatorsToDtoAsync(QualityManagementTask task)
    {
        var qualityIndicatorQuery = await _indicatorRepository.GetQueryableAsync();
        var labIndicatorQuery = await _laboratoryTaskIndicatorRepository.GetQueryableAsync();
        var labTaskQuery = await _laboratoryTaskRepository.GetQueryableAsync();
        var sampleIndicatorQuery = await _sampleIndicatorRepository.GetQueryableAsync();
        var sampleQuery = await _sampleRepository.GetQueryableAsync();
        var testRequestQuery = await _testRequestRepository.GetQueryableAsync();
        var masterIndicatorQuery = await _masterIndicatorRepository.GetQueryableAsync();
        var testMethodQuery = await _testMethodRepository.GetQueryableAsync();
        var userQuery = await _userRepository.GetQueryableAsync();

        var rawItems = (
            from si in sampleIndicatorQuery
            join s in sampleQuery on si.TestRequestSampleId equals s.Id
            join tr in testRequestQuery on s.TestRequestId equals tr.Id
            where tr.Id == task.TestRequestId
            join mi in masterIndicatorQuery on si.IndicatorId equals mi.Id
            join li in labIndicatorQuery on si.Id equals li.TestRequestSampleIndicatorId into liJoin
            from li in liJoin.DefaultIfEmpty()
            join lt in labTaskQuery on li.LaboratoryTaskId equals lt.Id into ltJoin
            from lt in ltJoin.DefaultIfEmpty()
            join qi in qualityIndicatorQuery.Where(q => q.QualityManagementTaskId == task.Id)
                on li.Id equals qi.LaboratoryTaskIndicatorId into qiJoin
            from qi in qiJoin.DefaultIfEmpty()
            join tm in testMethodQuery on (li != null ? li.MethodId : si.TestMethodId) equals tm.Id into tmJoin
            from tm in tmJoin.DefaultIfEmpty()
            join techManager in userQuery on lt.TechnicalManagerId equals techManager.Id into techManagerJoin
            from techManager in techManagerJoin.DefaultIfEmpty()
            join technician in userQuery on li.TechnicianId equals technician.Id into technicianJoin
            from technician in technicianJoin.DefaultIfEmpty()
            select new
            {
                SampleIndicator = si,
                Sample = s,
                TestRequestStatus = tr.Status,
                MasterIndicator = mi,
                LaboratoryTaskIndicator = li,
                LaboratoryTask = lt,
                QualityIndicator = qi,
                Method = tm,
                TechnicalManager = techManager,
                Technician = technician
            }).ToList();

        var laboratoryIds = rawItems
            .Select(x => x.LaboratoryTask?.LaboratoryId ?? x.SampleIndicator.LaboratoryId)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();
        var laboratories = laboratoryIds.Any()
            ? await _laboratoryRepository.GetListAsync(l => laboratoryIds.Contains(l.Id))
            : new List<Laboratory>();
        var departmentIds = laboratories.Select(x => x.DepartmentId).Distinct().ToList();
        var departments = departmentIds.Any()
            ? await _departmentRepository.GetListAsync(d => departmentIds.Contains(d.Id))
            : new List<Department>();
        var labNameMap = laboratories.Join(
                departments,
                lab => lab.DepartmentId,
                dept => dept.Id,
                (lab, dept) => new { lab.Id, DepartmentName = dept.Name })
            .ToDictionary(x => x.Id, x => x.DepartmentName);

        var specimenTypeIds = rawItems
            .Where(x => x.Sample.SpecimenTypeId.HasValue)
            .Select(x => x.Sample.SpecimenTypeId!.Value)
            .Distinct()
            .ToList();
        var specimenTypes = specimenTypeIds.Any()
            ? await _specimenTypeRepository.GetListAsync(s => specimenTypeIds.Contains(s.Id))
            : new List<SpecimenType>();
        var specimenTypeMap = specimenTypes.ToDictionary(x => x.Id, x => x.Name);

        var approvedByIds = rawItems
            .Where(x => x.QualityIndicator?.ApprovedById.HasValue == true)
            .Select(x => (Guid?)x.QualityIndicator!.ApprovedById!.Value);
        var approvedByNames = await _userNameProvider.GetUserNamesAsync(approvedByIds);

        var labIndicatorIds = rawItems
            .Where(x => x.LaboratoryTaskIndicator != null)
            .Select(x => x.LaboratoryTaskIndicator!.Id)
            .Distinct()
            .ToList();
        var indicatorImages = labIndicatorIds.Any()
            ? await _indicatorImageRepository.GetListAsync(x => labIndicatorIds.Contains(x.LaboratoryTaskIndicatorId))
            : new List<IndicatorImage>();
        var indicatorImageMap = indicatorImages
            .GroupBy(x => x.LaboratoryTaskIndicatorId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(x => x.CreationTime).Select(x => new IndicatorImageDto
                {
                    Id = x.Id,
                    LaboratoryTaskIndicatorId = x.LaboratoryTaskIndicatorId,
                    FileId = x.FileId,
                    FileName = x.FileName,
                    FileSize = x.FileSize,
                    Url = $"/api/v1/files/{x.FileId}",
                    CreationTime = x.CreationTime,
                    CreatorId = x.CreatorId
                }).ToList());

        var standardIds = rawItems
            .Select(x => x.LaboratoryTaskIndicator?.StandardId ?? x.QualityIndicator?.StandardId)
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();
        var standards = standardIds.Any()
            ? await _standardRepository.GetListAsync(s => standardIds.Contains(s.Id))
            : new List<Standard>();
        var standardMap = standards.ToDictionary(x => x.Id, x => x.Name);

        var masterIndicatorIds = rawItems.Select(x => x.MasterIndicator.Id).Distinct().ToList();
        var standardIndicators = standardIds.Any()
            ? await _standardIndicatorRepository.GetListAsync(si =>
                standardIds.Contains(si.StandardId) &&
                masterIndicatorIds.Contains(si.IndicatorId))
            : new List<StandardIndicator>();

        var allNoteIds = rawItems
            .Where(x => x.LaboratoryTaskIndicator != null)
            .SelectMany(x => ParseMasterDataNoteIds(x.LaboratoryTaskIndicator!.MasterDataNoteIds))
            .Distinct()
            .ToList();
        var notes = allNoteIds.Any()
            ? await _noteRepository.GetListAsync(n => allNoteIds.Contains(n.Id))
            : new List<Note>();
        var noteMap = notes
            .Where(x => !string.IsNullOrWhiteSpace(x.Content))
            .ToDictionary(x => x.Id, x => x.Content!);

        return rawItems
            .OrderBy(x => x.Sample.SampleCode)
            .ThenBy(x => x.Sample.SampleName)
            .ThenBy(x => x.MasterIndicator.Name)
            .ThenBy(x => x.Method?.Name ?? x.LaboratoryTaskIndicator!.SubContractMethodName)
            .Select(x =>
            {
                var labIndicator = x.LaboratoryTaskIndicator;
                var qualityIndicator = x.QualityIndicator;
                var standardId = labIndicator?.StandardId ?? qualityIndicator?.StandardId;
                var standardName = standardId.HasValue && standardMap.TryGetValue(standardId.Value, out var foundStandardName)
                    ? foundStandardName
                    : null;

                var effectiveMethodId = labIndicator?.MethodId ?? x.SampleIndicator.TestMethodId;
                var matchingStandardIndicator = standardId.HasValue
                    ? standardIndicators.FirstOrDefault(si =>
                        si.StandardId == standardId.Value &&
                        si.IndicatorId == x.MasterIndicator.Id &&
                        effectiveMethodId.HasValue &&
                        si.TestMethodId == effectiveMethodId.Value)
                    : null;

                matchingStandardIndicator ??= standardId.HasValue
                    ? standardIndicators.FirstOrDefault(si =>
                        si.StandardId == standardId.Value &&
                        si.IndicatorId == x.MasterIndicator.Id)
                    : null;

                var effectiveLaboratoryId = x.LaboratoryTask?.LaboratoryId ?? x.SampleIndicator.LaboratoryId;
                var laboratoryName = effectiveLaboratoryId.HasValue && labNameMap.TryGetValue(effectiveLaboratoryId.Value, out var departmentName)
                    ? departmentName
                    : null;

                var approvedByName = qualityIndicator?.ApprovedById.HasValue == true &&
                                     approvedByNames.TryGetValue(qualityIndicator.ApprovedById.Value, out var name)
                    ? name
                    : null;

                var noteContents = labIndicator != null
                    ? ParseMasterDataNoteIds(labIndicator.MasterDataNoteIds)
                        .Where(noteId => noteMap.ContainsKey(noteId))
                        .Select(noteId => noteMap[noteId])
                        .ToList()
                    : new List<string>();

                var technicalManagerName = x.TechnicalManager != null
                    ? string.IsNullOrWhiteSpace($"{x.TechnicalManager.Surname} {x.TechnicalManager.Name}".Trim())
                        ? x.TechnicalManager.UserName
                        : $"{x.TechnicalManager.Surname} {x.TechnicalManager.Name}".Trim()
                    : null;

                var technicianName = x.Technician != null
                    ? string.IsNullOrWhiteSpace($"{x.Technician.Surname} {x.Technician.Name}".Trim())
                        ? x.Technician.UserName
                        : $"{x.Technician.Surname} {x.Technician.Name}".Trim()
                    : null;

                var testingStatus = labIndicator != null
                    ? DetermineTestingStatus(labIndicator.ApprovalStatus, labIndicator.TestDate, labIndicator.Result)
                    : null;

                var qlclDisplayStatus = MapQLCLDisplayStatus(
                    x.TestRequestStatus,
                    labIndicator?.ApprovalStatus,
                    qualityIndicator?.ApprovalStatus,
                    qualityIndicator?.IsPrinted ?? false,
                    labIndicator?.TechnicianId,
                    labIndicator?.MethodId,
                    labIndicator?.TestDate,
                    labIndicator?.Result);

                return new QualityManagementIndicatorDto
                {
                    Id = qualityIndicator?.Id ?? x.SampleIndicator.Id,
                    QualityManagementIndicatorId = qualityIndicator?.Id,
                    TestRequestSampleIndicatorId = x.SampleIndicator.Id,
                    LaboratoryTaskIndicatorId = labIndicator?.Id,
                    LaboratoryTaskId = x.LaboratoryTask?.Id,
                    LaboratoryId = effectiveLaboratoryId ?? Guid.Empty,
                    LaboratoryName = laboratoryName,
                    SampleId = x.Sample.Id,
                    SampleCode = x.Sample.SampleCode,
                    SampleName = x.Sample.SampleName,
                    SpecimenTypeName = x.Sample.SpecimenTypeId.HasValue &&
                                       specimenTypeMap.TryGetValue(x.Sample.SpecimenTypeId.Value, out var specimenTypeName)
                        ? specimenTypeName
                        : null,
                    IndicatorId = x.MasterIndicator.Id,
                    IndicatorCode = x.MasterIndicator.Code,
                    IndicatorName = x.MasterIndicator.Name,
                    IndicatorFormattedName = x.MasterIndicator.FormattedName,
                    MethodId = effectiveMethodId,
                    MethodName = !string.IsNullOrWhiteSpace(labIndicator?.SubContractMethodName)
                        ? labIndicator!.SubContractMethodName
                        : x.Method?.Name,
                    MethodFormattedName = !string.IsNullOrWhiteSpace(labIndicator?.SubContractMethodName)
                        ? null
                        : x.Method?.FormattedName,
                    Unit = labIndicator?.Unit,
                    Result = labIndicator?.Result,
                    ResultSentDate = labIndicator?.EntryDate,
                    ApprovalDate = qualityIndicator?.ApprovalDate,
                    StandardId = standardId,
                    StandardName = standardName,
                    ReferenceLimit = qualityIndicator?.ReferenceLimit,
                    ComparisonOperator = matchingStandardIndicator?.ComparisonOperator,
                    AllowedLimit = matchingStandardIndicator?.AllowedLimit,
                    LowerOperator = matchingStandardIndicator?.LowerOperator,
                    LowerLimit = matchingStandardIndicator?.LowerLimit,
                    UpperOperator = matchingStandardIndicator?.UpperOperator,
                    UpperLimit = matchingStandardIndicator?.UpperLimit,
                    ApprovalStatus = qualityIndicator?.ApprovalStatus,
                    RejectionReason = qualityIndicator?.RejectionReason,
                    IsResultSent = qualityIndicator?.IsResultSent ?? false,
                    QualityResultSentDate = qualityIndicator?.ResultSentDate,
                    IsPrinted = qualityIndicator?.IsPrinted ?? false,
                    PrintedDate = qualityIndicator?.PrintedDate,
                    ApprovedById = qualityIndicator?.ApprovedById,
                    ApprovedByName = approvedByName,
                    Images = labIndicator != null && indicatorImageMap.TryGetValue(labIndicator.Id, out var images)
                        ? images
                        : new List<IndicatorImageDto>(),
                    FreeTextNote = labIndicator?.FreeTextNote,
                    MasterDataNotes = noteContents,
                    ParentIndicatorId = x.MasterIndicator.ParentIndicatorId,
                    TechnicalManagerName = technicalManagerName,
                    TechnicianId = labIndicator?.TechnicianId,
                    TechnicianName = technicianName,
                    QlktApprovalDate = labIndicator?.ApprovalDate,
                    IndicatorApprovalStatus = labIndicator?.ApprovalStatus,
                    TestingStatus = testingStatus,
                    QlclDisplayStatus = qlclDisplayStatus,
                    IsInQualityScope = qualityIndicator != null
                };
            })
            .ToList();
    }

    private static List<Guid> ParseMasterDataNoteIds(string? masterDataNoteIds)
    {
        if (string.IsNullOrWhiteSpace(masterDataNoteIds))
        {
            return new List<Guid>();
        }

        var rawIds = masterDataNoteIds.Contains("[")
            ? JsonSerializer.Deserialize<List<string>>(masterDataNoteIds) ?? new List<string>()
            : masterDataNoteIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToList();

        return rawIds
            .Select(id => Guid.TryParse(id, out var noteId) ? noteId : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();
    }

    private async Task UpdateTaskStatusAsync(Guid taskId)
    {
        var task = await _taskRepository.GetAsync(taskId);
        var allIndicators = await _indicatorRepository.GetListAsync(i => i.QualityManagementTaskId == taskId);

        if (!allIndicators.Any())
        {
            return;
        }

        // Filter out parent indicators with children - only count sub-indicators and standalone indicators
        var indicators = await FilterQualityIndicatorsForStatusCalculationAsync(allIndicators);

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
        var allIndicators = await _indicatorRepository.GetListAsync(i => i.QualityManagementTaskId == task.Id);

        // Filter out parent indicators with children - only count sub-indicators and standalone indicators
        var filteredIndicators = await FilterQualityIndicatorsForStatusCalculationAsync(allIndicators);

        // Kiểm tra còn chỉ tiêu chờ phê duyệt không
        var pendingApprovalIndicators = filteredIndicators.Where(i =>
            i.ApprovalStatus == QualityIndicatorApprovalStatus.PendingApproval).ToList();

        var approvedIndicators = filteredIndicators.Where(i =>
            i.ApprovalStatus == QualityIndicatorApprovalStatus.Approved).ToList();

        // ƯU TIÊN 1: Còn chỉ tiêu chờ phê duyệt -> giữ PendingApproval
        if (pendingApprovalIndicators.Any())
        {
            // Nếu có chỉ tiêu đã được in nhưng vẫn còn chỉ tiêu chờ duyệt
            // -> ưu tiên giữ status PendingApproval
            task.Status = QualityManagementStatus.PendingApproval;
            await _taskRepository.UpdateAsync(task);

            // Vẫn cập nhật TestRequest nếu có chỉ tiêu đã được in
            var printedCount = approvedIndicators.Count(i => i.IsPrinted);
            if (printedCount > 0)
            {
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
            return;
        }

        // Không còn chỉ tiêu chờ duyệt -> tính status dựa vào số lượng đã in
        if (!approvedIndicators.Any())
        {
            return;
        }

        var totalPrintedCount = approvedIndicators.Count(i => i.IsPrinted);

        if (totalPrintedCount == 0)
        {
            // Chưa in gì - giữ PendingPublish
            return;
        }

        // So sánh với tổng chỉ tiêu thực tế của phiếu yêu cầu (không chỉ QM indicators)
        var totalExpected = await GetTotalActiveSampleIndicatorCountAsync(task.TestRequestId);

        if (totalPrintedCount < totalExpected)
        {
            task.Status = QualityManagementStatus.PartialPublished;
        }
        else
        {
            task.Status = QualityManagementStatus.FullyPublished;
        }

        await _taskRepository.UpdateAsync(task);

        // Cập nhật TestRequest.Status = ReportPublished ngay khi có chỉ tiêu được in
        var testRequest2 = await _testRequestRepository.GetAsync(task.TestRequestId);
        if (testRequest2.Status != TestRequestStatus.ReportPublished)
        {
            testRequest2.Status = TestRequestStatus.ReportPublished;
            testRequest2.ReportPublishedDate = DateTime.Now;
            testRequest2.ReportPublishedByUserId = CurrentUser.Id;
            testRequest2.ReportPublishedByUserName = await _userNameProvider.GetUserNameAsync(CurrentUser.Id);
            await _testRequestRepository.UpdateAsync(testRequest2);
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

    /// <summary>
    /// Lọc các QualityManagementIndicator để tính status.
    /// Loại bỏ các parent indicators có children trong cùng task.
    /// Chỉ tính status dựa trên sub-indicators và standalone indicators.
    /// </summary>
    private async Task<List<QualityManagementIndicator>> FilterQualityIndicatorsForStatusCalculationAsync(
        List<QualityManagementIndicator> qualityIndicators)
    {
        if (!qualityIndicators.Any())
        {
            return qualityIndicators;
        }

        // Bước 1: Lấy tất cả LaboratoryTaskIndicator IDs
        var labIndicatorIds = qualityIndicators.Select(qi => qi.LaboratoryTaskIndicatorId).Distinct().ToList();

        // Bước 2: Query tất cả LaboratoryTaskIndicators một lần
        var labIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(
            li => labIndicatorIds.Contains(li.Id));
        var labIndicatorMap = labIndicators.ToDictionary(li => li.Id, li => li);

        // Bước 3: Lấy tất cả TestRequestSampleIndicator IDs
        var sampleIndicatorIds = labIndicators.Select(li => li.TestRequestSampleIndicatorId).Distinct().ToList();

        // Bước 4: Query tất cả SampleIndicators một lần
        var sampleIndicators = await _sampleIndicatorRepository.GetListAsync(
            si => sampleIndicatorIds.Contains(si.Id));
        var sampleIndicatorMap = sampleIndicators.ToDictionary(si => si.Id, si => si.IndicatorId);

        // Bước 5: Lấy tất cả IndicatorIds
        var indicatorIds = sampleIndicatorMap.Values.Distinct().ToList();

        // Bước 6: Query tất cả Master Indicators một lần
        var masterIndicators = await _masterIndicatorRepository.GetListAsync(ind => indicatorIds.Contains(ind.Id));
        var indicatorMap = masterIndicators.ToDictionary(ind => ind.Id, ind => ind);

        // Bước 7: Xây dựng mapping qualityIndicatorId -> (indicatorId, methodId)
        var qualityIndicatorToInfo = new Dictionary<Guid, (Guid IndicatorId, Guid? MethodId)>();
        foreach (var qi in qualityIndicators)
        {
            if (!labIndicatorMap.TryGetValue(qi.LaboratoryTaskIndicatorId, out var labInd)) continue;
            if (!sampleIndicatorMap.TryGetValue(labInd.TestRequestSampleIndicatorId, out var indicatorId)) continue;

            qualityIndicatorToInfo[qi.Id] = (indicatorId, labInd.MethodId);
        }

        // Bước 8: Xác định các parent indicators có children trong cùng task
        // Composite key: indicatorId + methodId
        var parentIndicatorKeys = new HashSet<string>();

        // Tìm tất cả các parent indicator IDs từ các sub-indicators
        foreach (var qi in qualityIndicators)
        {
            if (!qualityIndicatorToInfo.TryGetValue(qi.Id, out var info)) continue;
            if (!indicatorMap.TryGetValue(info.IndicatorId, out var indicator)) continue;

            if (indicator.ParentIndicatorId.HasValue)
            {
                // Đây là sub-indicator, đánh dấu parent của nó
                var parentKey = $"{indicator.ParentIndicatorId.Value}_{info.MethodId}";
                parentIndicatorKeys.Add(parentKey);
            }
        }

        // Bước 9: Lọc ra các indicators cần tính status
        // Loại bỏ các parent indicators có children (tức là có trong parentIndicatorKeys)
        var result = new List<QualityManagementIndicator>();
        foreach (var qi in qualityIndicators)
        {
            if (!qualityIndicatorToInfo.TryGetValue(qi.Id, out var info))
            {
                // Không tìm thấy mapping, vẫn giữ lại (edge case)
                result.Add(qi);
                continue;
            }

            var key = $"{info.IndicatorId}_{info.MethodId}";
            if (parentIndicatorKeys.Contains(key))
            {
                // Đây là parent indicator có children -> skip
                continue;
            }

            // Đây là sub-indicator hoặc standalone indicator -> giữ lại
            result.Add(qi);
        }

        return result;
    }

    private async Task<int> GetTotalActiveSampleIndicatorCountAsync(Guid testRequestId)
    {
        var samples = await _sampleRepository.GetListAsync(s => s.TestRequestId == testRequestId);
        var sampleIds = samples.Select(s => s.Id).ToList();

        if (!sampleIds.Any()) return 0;

        var sampleIndicators = await _sampleIndicatorRepository.GetListAsync(
            si => sampleIds.Contains(si.TestRequestSampleId));

        var indicatorIds = sampleIndicators.Select(si => si.IndicatorId).Distinct().ToList();
        var masterIndicators = indicatorIds.Count == 0
            ? new List<Indicator>()
            : await _masterIndicatorRepository.GetListAsync(i => indicatorIds.Contains(i.Id));
        var indicatorMap = masterIndicators.ToDictionary(i => i.Id);

        var parentKeys = new HashSet<(Guid SampleId, Guid ParentIndicatorId, Guid? MethodId)>();
        foreach (var si in sampleIndicators)
        {
            if (indicatorMap.TryGetValue(si.IndicatorId, out var master) &&
                master.ParentIndicatorId.HasValue)
            {
                parentKeys.Add((si.TestRequestSampleId, master.ParentIndicatorId.Value, si.TestMethodId));
            }
        }

        return sampleIndicators
            .Where(si => !parentKeys.Contains((si.TestRequestSampleId, si.IndicatorId, si.TestMethodId)))
            .Count();
    }

    #endregion

    #region Sample Assignment Log

    public async Task<PagedResultDto<SampleAssignmentLogDto>> GetSampleAssignmentLogAsync(GetSampleAssignmentLogInput input)
    {
        // Build base query starting from TestRequestSampleIndicator (để lấy TẤT CẢ phiếu yêu cầu)
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

        // Join all tables starting from TestRequestSampleIndicator with LEFT JOINs
        // Điều này cho phép hiển thị TẤT CẢ phiếu yêu cầu, kể cả chưa gửi KTN/QLKT
        var query = from si in sampleIndicatorQuery
                    join s in sampleQuery on si.TestRequestSampleId equals s.Id
                    join tr in testRequestQuery on s.TestRequestId equals tr.Id
                    join mi in masterIndicatorQuery on si.IndicatorId equals mi.Id
                    // LEFT JOIN với LaboratoryTaskIndicator (có thể chưa gửi KTN)
                    join li in labIndicatorQuery on si.Id equals li.TestRequestSampleIndicatorId into liJoin
                    from li in liJoin.DefaultIfEmpty()
                    // LEFT JOIN với LaboratoryTask
                    join lt in labTaskQuery on li.LaboratoryTaskId equals lt.Id into ltJoin
                    from lt in ltJoin.DefaultIfEmpty()
                    // LEFT JOIN với QualityManagementIndicator (có thể chưa gửi QLKT)
                    join qi in qualityIndicatorQuery on li.Id equals qi.LaboratoryTaskIndicatorId into qiJoin
                    from qi in qiJoin.DefaultIfEmpty()
                    // LEFT JOIN với QualityManagementTask
                    join qt in qualityTaskQuery on qi.QualityManagementTaskId equals qt.Id into qtJoin
                    from qt in qtJoin.DefaultIfEmpty()
                    // LEFT JOIN với TestMethod
                    join tm in testMethodQuery on (li != null ? li.MethodId : si.TestMethodId) equals tm.Id into tmJoin
                    from tm in tmJoin.DefaultIfEmpty()
                    // LEFT JOIN với User (TechnicalManager)
                    join techManager in userQuery on lt.TechnicalManagerId equals techManager.Id into techManagerJoin
                    from techManager in techManagerJoin.DefaultIfEmpty()
                    // LEFT JOIN với User (Technician)
                    join technician in userQuery on li.TechnicianId equals technician.Id into technicianJoin
                    from technician in technicianJoin.DefaultIfEmpty()
                    select new
                    {
                        // TestRequest info
                        TestRequestId = tr.Id,
                        QualityTaskId = qt != null ? (Guid?)qt.Id : null,
                        LaboratoryTaskId = lt != null ? (Guid?)lt.Id : null,
                        ContractNumber = tr.ContractNumber,
                        ContractDate = tr.ContractDate,
                        TestRequestStatus = tr.Status,
                        RequestCompletionDate = si.RequestDate ?? tr.ExpectedCompletionDate ?? (lt != null ? lt.RequestCompletionDate : null),

                        // Sample info
                        SampleId = s.Id,
                        SampleCode = s.SampleCode,
                        SampleName = s.SampleName,
                        SampleQuantity = s.SampleQuantity,
                        SampleStatusText = s.StatusText,

                        // Indicator info
                        TestRequestSampleIndicatorId = si.Id,
                        LaboratoryTaskIndicatorId = li != null ? (Guid?)li.Id : null,
                        IndicatorName = mi.Name,
                        IndicatorFormattedName = mi.FormattedName,
                        MethodName = li != null && !string.IsNullOrWhiteSpace(li.SubContractMethodName)
                            ? li.SubContractMethodName
                            : (tm != null ? tm.Name : null),
                        MethodFormattedName = li != null && !string.IsNullOrWhiteSpace(li.SubContractMethodName)
                            ? null
                            : (tm != null ? tm.FormattedName : null),
                        Unit = li != null ? li.Unit : null,
                        // Ngày nhận mẫu = ngày in phiếu thử nghiệm (BM05)
                        TransferDate = tr.FormPrintedDate,
                        TestDate = li != null ? li.TestDate : null,
                        Result = li != null ? li.Result : null,

                        // Assignment info
                        TechnicalManagerName = techManager != null ? (techManager.Name ?? techManager.UserName) : null,
                        TechnicianName = technician != null ? (technician.Name ?? technician.UserName) : null,
                        EntryDate = li != null ? li.EntryDate : null,
                        QlktApprovalDate = li != null ? li.ApprovalDate : null,

                        // Status for highlighting
                        IndicatorApprovalStatus = li != null ? (IndicatorApprovalStatus?)li.ApprovalStatus : null,
                        QualityApprovalStatus = qi != null ? (QualityIndicatorApprovalStatus?)qi.ApprovalStatus : null,
                        IsReportPublished = qi != null && qi.IsPrinted,

                        // Additional fields for QLCLDisplayStatus mapping
                        TechnicianId = li != null ? (Guid?)li.TechnicianId : null,
                        MethodId = li != null ? (Guid?)li.MethodId : null,

                        // Laboratory info (for filtering)
                        LaboratoryId = lt != null ? (Guid?)lt.LaboratoryId : null
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
            query = query.Where(x => x.TransferDate >= input.FromDate.Value);
        }

        if (input.ToDate.HasValue)
        {
            var toDateEnd = input.ToDate.Value.Date.AddDays(1);
            query = query.Where(x => x.TransferDate < toDateEnd);
        }

        if (input.LaboratoryIds != null && input.LaboratoryIds.Count > 0)
        {
            query = query.Where(x => x.LaboratoryId.HasValue && input.LaboratoryIds.Contains(x.LaboratoryId.Value));
        }

        // Apply sorting - CRITICAL: Must sort by ContractNumber then SampleId first to ensure row spanning works correctly
        var sortingKey = string.IsNullOrWhiteSpace(input.Sorting) ? SortingFields.ContractDateDesc : input.Sorting.ToLower();

        var sortedQuery = sortingKey switch
        {
            SortingFields.RequestCompletionDate => query.OrderBy(x => x.RequestCompletionDate).ThenBy(x => x.ContractNumber).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName),
            SortingFields.RequestCompletionDateDesc => query.OrderByDescending(x => x.RequestCompletionDate).ThenBy(x => x.ContractNumber).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName),
            SortingFields.ContractNumber => query.OrderBy(x => x.ContractNumber).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName),
            SortingFields.ContractNumberDesc => query.OrderByDescending(x => x.ContractNumber).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName),
            SortingFields.TransferDate => query.OrderBy(x => x.TransferDate).ThenBy(x => x.ContractNumber).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName),
            SortingFields.TransferDateDesc => query.OrderByDescending(x => x.TransferDate).ThenBy(x => x.ContractNumber).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName),
            SortingFields.ContractDate => query.OrderBy(x => x.ContractDate).ThenBy(x => x.ContractNumber).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName),
            SortingFields.ContractDateDesc => query.OrderByDescending(x => x.ContractDate).ThenBy(x => x.ContractNumber).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName),
            _ => query.OrderByDescending(x => x.ContractDate).ThenBy(x => x.ContractNumber).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName)
        };

        // Pagination by contract count (not row count)
        // skipCount and maxResultCount refer to number of contracts, not rows
        var allItems = sortedQuery.ToList();

        // Filter by QLCLDisplayStatus if specified (computed in-memory)
        if (input.QlclDisplayStatuses != null && input.QlclDisplayStatuses.Count > 0)
        {
            var statusSet = new HashSet<int>(input.QlclDisplayStatuses);
            allItems = allItems.Where(x =>
            {
                var status = MapQLCLDisplayStatus(
                    x.TestRequestStatus,
                    x.IndicatorApprovalStatus,
                    x.QualityApprovalStatus,
                    x.IsReportPublished,
                    x.TechnicianId,
                    x.MethodId,
                    x.TestDate,
                    x.Result);
                return status.HasValue && statusSet.Contains((int)status.Value);
            }).ToList();
        }

        // Count unique contracts (not rows) for pagination - after QLCLDisplayStatus filtering
        var totalContractCount = allItems.Select(x => x.ContractNumber).Distinct().Count();

        var items = GetItemsByContractPagination(allItems, input.SkipCount, input.MaxResultCount);

        // Map to DTOs
        var dtos = items.Select(x => new SampleAssignmentLogDto
        {
            TestRequestId = x.TestRequestId,
            QualityTaskId = x.QualityTaskId,
            LaboratoryTaskId = x.LaboratoryTaskId,
            ContractNumber = x.ContractNumber,
            TestRequestStatus = x.TestRequestStatus,
            RequestCompletionDate = x.RequestCompletionDate,
            ContractDate = x.ContractDate,
            SampleId = x.SampleId,
            SampleCode = x.SampleCode,
            SampleName = x.SampleName,
            SampleQuantity = x.SampleQuantity,
            SampleStatusText = x.SampleStatusText,
            TestRequestSampleIndicatorId = x.TestRequestSampleIndicatorId,
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
            TestingStatus = x.IndicatorApprovalStatus.HasValue
                ? DetermineTestingStatus(x.IndicatorApprovalStatus.Value, x.TestDate, x.Result)
                : null,
            // QLCLDisplayStatus - trạng thái hiển thị cho trang QLCL
            QLCLDisplayStatus = MapQLCLDisplayStatus(
                x.TestRequestStatus,
                x.IndicatorApprovalStatus,
                x.QualityApprovalStatus,
                x.IsReportPublished,
                x.TechnicianId,
                x.MethodId,
                x.TestDate,
                x.Result),
            QualityApprovalStatus = x.QualityApprovalStatus,
            IsReportPublished = x.IsReportPublished
        }).ToList();

        return new PagedResultDto<SampleAssignmentLogDto>(totalContractCount, dtos);
    }

    public async Task<QLCLSampleAssignmentLogStatisticsDto> GetSampleAssignmentLogStatisticsAsync()
    {
        // Reuse the same base query as GetSampleAssignmentLogAsync but without filters/pagination
        var qualityIndicatorQuery = await _indicatorRepository.GetQueryableAsync();
        var labIndicatorQuery = await _laboratoryTaskIndicatorRepository.GetQueryableAsync();
        var labTaskQuery = await _laboratoryTaskRepository.GetQueryableAsync();
        var sampleIndicatorQuery = await _sampleIndicatorRepository.GetQueryableAsync();
        var sampleQuery = await _sampleRepository.GetQueryableAsync();
        var testRequestQuery = await _testRequestRepository.GetQueryableAsync();

        // Build base query - same JOIN structure as GetSampleAssignmentLogAsync
        var query = from si in sampleIndicatorQuery
                    join s in sampleQuery on si.TestRequestSampleId equals s.Id
                    join tr in testRequestQuery on s.TestRequestId equals tr.Id
                    // LEFT JOIN với LaboratoryTaskIndicator (có thể chưa gửi KTN)
                    join li in labIndicatorQuery on si.Id equals li.TestRequestSampleIndicatorId into liJoin
                    from li in liJoin.DefaultIfEmpty()
                    // LEFT JOIN với LaboratoryTask
                    join lt in labTaskQuery on li.LaboratoryTaskId equals lt.Id into ltJoin
                    from lt in ltJoin.DefaultIfEmpty()
                    // LEFT JOIN với QualityManagementIndicator (có thể chưa gửi QLKT)
                    join qi in qualityIndicatorQuery on li.Id equals qi.LaboratoryTaskIndicatorId into qiJoin
                    from qi in qiJoin.DefaultIfEmpty()
                    select new
                    {
                        TestRequestStatus = tr.Status,
                        IndicatorApprovalStatus = li != null ? (IndicatorApprovalStatus?)li.ApprovalStatus : null,
                        QualityApprovalStatus = qi != null ? (QualityIndicatorApprovalStatus?)qi.ApprovalStatus : null,
                        IsReportPublished = qi != null && qi.IsPrinted,
                        TechnicianId = li != null ? (Guid?)li.TechnicianId : null,
                        MethodId = li != null ? (Guid?)li.MethodId : null,
                        TestDate = li != null ? li.TestDate : (DateTime?)null,
                        Result = li != null ? li.Result : null,
                    };

        var allItems = query.ToList();

        var statusCounts = allItems
            .Select(x => MapQLCLDisplayStatus(
                x.TestRequestStatus,
                x.IndicatorApprovalStatus,
                x.QualityApprovalStatus,
                x.IsReportPublished,
                x.TechnicianId,
                x.MethodId,
                x.TestDate,
                x.Result))
            .Where(s => s.HasValue)
            .Select(s => s!.Value)
            .ToList();

        return new QLCLSampleAssignmentLogStatisticsDto
        {
            Total = statusCounts.Count,
            ToDo = statusCounts.Count(s => s == QLCLDisplayStatus.ToDo || s == QLCLDisplayStatus.Testing),
            WaitingQLKTApproval = statusCounts.Count(s => s == QLCLDisplayStatus.WaitingQLKTApproval),
            WaitingQLCLApproval = statusCounts.Count(s => s == QLCLDisplayStatus.WaitingQLCLApproval),
            WaitingForPublishing = statusCounts.Count(s => s == QLCLDisplayStatus.WaitingForPublishing),
            Published = statusCounts.Count(s => s == QLCLDisplayStatus.Published),
        };
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
    /// Map to QLCLDisplayStatus for QLCL page - Sổ nhận việc
    /// Flow: Mới → Chờ thanh toán → Đang xem xét YC → Từ chối/Chờ giao việc → Cần làm → Đang TN → Chờ QLKT duyệt → Chờ QLCL duyệt → Chờ phát hành → Đã phát hành
    /// </summary>
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
        {
            return QLCLDisplayStatus.RequestCancelled;
        }

        // 1. Đã phát hành báo cáo
        if (isReportPublished)
        {
            return QLCLDisplayStatus.Published;
        }

        // 2. Chờ phát hành - QLCL đã duyệt nhưng chưa phát hành
        if (qualityApprovalStatus == QualityIndicatorApprovalStatus.Approved)
        {
            return QLCLDisplayStatus.WaitingForPublishing;
        }

        // 3. QLCL từ chối kết quả
        if (qualityApprovalStatus == QualityIndicatorApprovalStatus.Rejected)
        {
            return QLCLDisplayStatus.QLCLRejected;
        }

        // 4. Chờ QLCL duyệt - QLKT đã duyệt, chờ QLCL
        // Bao gồm cả trường hợp qualityApprovalStatus == null (edge case khi QLKT duyệt nhưng chưa tạo QualityManagementIndicator)
        if (indicatorApprovalStatus == IndicatorApprovalStatus.Approved &&
            (qualityApprovalStatus == QualityIndicatorApprovalStatus.PendingApproval || qualityApprovalStatus == null))
        {
            return QLCLDisplayStatus.WaitingQLCLApproval;
        }

        // 5. QLKT từ chối kết quả
        if (indicatorApprovalStatus == IndicatorApprovalStatus.Rejected)
        {
            return QLCLDisplayStatus.ResultRejected;
        }

        // 6. Chờ QLKT duyệt - KTV đã gửi kết quả
        if (indicatorApprovalStatus == IndicatorApprovalStatus.Entered)
        {
            return QLCLDisplayStatus.WaitingQLKTApproval;
        }

        // 7. Đang thử nghiệm - đã có testDate hoặc đã bắt đầu TN
        if (indicatorApprovalStatus == IndicatorApprovalStatus.AcceptedForTesting &&
            (testDate.HasValue || !string.IsNullOrWhiteSpace(result)))
        {
            return QLCLDisplayStatus.Testing;
        }

        // 8. Cần làm - đã giao việc (có TechnicianId và MethodId)
        if (indicatorApprovalStatus == IndicatorApprovalStatus.AcceptedForTesting &&
            technicianId.HasValue && methodId.HasValue)
        {
            return QLCLDisplayStatus.ToDo;
        }

        // 9. Chờ giao việc - đã chấp nhận nhưng chưa giao KTV
        if (indicatorApprovalStatus == IndicatorApprovalStatus.AcceptedForTesting)
        {
            return QLCLDisplayStatus.WaitingForAssignment;
        }

        // 10. QLCL/QLKT từ chối yêu cầu TN
        if (indicatorApprovalStatus == IndicatorApprovalStatus.TestingRejected)
        {
            return QLCLDisplayStatus.Rejected;
        }

        // 11. Kiểm tra theo TestRequestStatus
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
                // Các trạng thái khác map về UnderReview nếu chưa có IndicatorApprovalStatus
                if (!indicatorApprovalStatus.HasValue)
                {
                    return QLCLDisplayStatus.UnderReview;
                }
                return QLCLDisplayStatus.ToDo;
        }
    }

    /// <summary>
    /// Get items by contract pagination.
    /// skipCount and maxResultCount refer to number of contracts, not rows.
    /// Returns all rows belonging to the selected contracts.
    /// </summary>
    private static List<T> GetItemsByContractPagination<T>(List<T> allItems, int skipCount, int maxResultCount) where T : class
    {
        if (allItems.Count == 0 || maxResultCount <= 0)
            return new List<T>();

        // Get ContractNumber property via reflection (works for anonymous types)
        var contractNumberProperty = typeof(T).GetProperty("ContractNumber");
        if (contractNumberProperty == null)
        {
            // Fallback to standard pagination if no ContractNumber property
            return allItems.Skip(skipCount).Take(maxResultCount).ToList();
        }

        Func<T, string?> getContractNumber = item => contractNumberProperty.GetValue(item) as string;

        // Get unique contract numbers in order of appearance
        var uniqueContracts = new List<string?>();
        string? lastContract = null;
        foreach (var item in allItems)
        {
            var contract = getContractNumber(item);
            if (contract != lastContract)
            {
                uniqueContracts.Add(contract);
                lastContract = contract;
            }
        }

        // Apply pagination to contracts (skip and take by contract count)
        var selectedContracts = uniqueContracts
            .Skip(skipCount)
            .Take(maxResultCount)
            .ToHashSet();

        // Return all rows belonging to the selected contracts
        return allItems
            .Where(item => selectedContracts.Contains(getContractNumber(item)))
            .ToList();
    }

    #endregion
}
