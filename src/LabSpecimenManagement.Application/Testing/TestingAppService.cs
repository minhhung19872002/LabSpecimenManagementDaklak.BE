using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.MasterData.Indicators;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.TestRequests;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace LabSpecimenManagement.Testing;

[RemoteService(IsEnabled = false)]
[Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Default)]
public class TestingAppService : ApplicationService, ITestingAppService
{
    private readonly IRepository<LaboratoryTask, Guid> _laboratoryTaskRepository;
    private readonly IRepository<LaboratoryTaskIndicator, Guid> _laboratoryTaskIndicatorRepository;
    private readonly IRepository<IndicatorRejectionHistory, Guid> _rejectionHistoryRepository;
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;
    private readonly IRepository<TestRequestSample, Guid> _sampleRepository;
    private readonly IRepository<TestRequestSampleIndicator, Guid> _sampleIndicatorRepository;
    private readonly IRepository<Laboratory, Guid> _laboratoryRepository;
    private readonly IRepository<Indicator, Guid> _indicatorRepository;
    private readonly IRepository<TestMethod, Guid> _testMethodRepository;
    private readonly IIdentityUserRepository _userRepository;

    public TestingAppService(
        IRepository<LaboratoryTask, Guid> laboratoryTaskRepository,
        IRepository<LaboratoryTaskIndicator, Guid> laboratoryTaskIndicatorRepository,
        IRepository<IndicatorRejectionHistory, Guid> rejectionHistoryRepository,
        IRepository<TestRequest, Guid> testRequestRepository,
        IRepository<TestRequestSample, Guid> sampleRepository,
        IRepository<TestRequestSampleIndicator, Guid> sampleIndicatorRepository,
        IRepository<Laboratory, Guid> laboratoryRepository,
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<TestMethod, Guid> testMethodRepository,
        IIdentityUserRepository userRepository)
    {
        _laboratoryTaskRepository = laboratoryTaskRepository;
        _laboratoryTaskIndicatorRepository = laboratoryTaskIndicatorRepository;
        _rejectionHistoryRepository = rejectionHistoryRepository;
        _testRequestRepository = testRequestRepository;
        _sampleRepository = sampleRepository;
        _sampleIndicatorRepository = sampleIndicatorRepository;
        _laboratoryRepository = laboratoryRepository;
        _indicatorRepository = indicatorRepository;
        _testMethodRepository = testMethodRepository;
        _userRepository = userRepository;
    }

    public async Task<PagedResultDto<TestingListDto>> GetListAsync(GetTestingListInput input)
    {
        var currentUserId = CurrentUser.Id;
        var queryable = await _laboratoryTaskRepository.GetQueryableAsync();
        var indicatorQueryable = await _laboratoryTaskIndicatorRepository.GetQueryableAsync();

        // Build the base query with indicator filtering
        var taskQuery = queryable.Select(task => new
        {
            Task = task,
            Indicators = indicatorQueryable.Where(i => i.LaboratoryTaskId == task.Id).ToList()
        });

        // Apply user filter - both tabs filter tasks where current user is assigned
        // Tab "MyAssignments": Shows only user's data (samples, indicators, technician name)
        // Tab "All": Shows full data but still filters tasks where user has assignments
        if (currentUserId.HasValue)
        {
            taskQuery = taskQuery.Where(x => x.Indicators.Any(i => i.TechnicianId == currentUserId.Value));
        }

        // Apply technician filter (AND logic with current user if in MyAssignments tab)
        if (input.TechnicianIds != null && input.TechnicianIds.Any())
        {
            taskQuery = taskQuery.Where(x => x.Indicators.Any(i =>
                i.TechnicianId.HasValue && input.TechnicianIds.Contains(i.TechnicianId.Value)));
        }

        // Apply laboratory filter
        if (input.LaboratoryId.HasValue)
        {
            taskQuery = taskQuery.Where(x => x.Task.LaboratoryId == input.LaboratoryId.Value);
        }

        // Apply search filter (Contract Number)
        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var searchLower = input.Search.ToLower();
            taskQuery = taskQuery.Where(x =>
                x.Task.ContractNumber != null && x.Task.ContractNumber.ToLower().Contains(searchLower));
        }

        // Get all tasks for status filtering and processing
        var allTasks = taskQuery.ToList();

        // Calculate testing status for each task
        var tasksWithStatus = allTasks.Select(x => new
        {
            x.Task,
            x.Indicators,
            TestingStatus = CalculateTestingStatus(x.Indicators),
            IsUrgent = x.Task.RequestCompletionDate.HasValue &&
                       (x.Task.RequestCompletionDate.Value - DateTime.Today).TotalDays <= 5
        }).ToList();

        // Apply status filter
        if (input.Statuses != null && input.Statuses.Any())
        {
            tasksWithStatus = tasksWithStatus.Where(x => input.Statuses.Contains(x.TestingStatus)).ToList();
        }

        // Get total count after all filters
        var totalCount = tasksWithStatus.Count;

        // Apply sorting
        var sortedTasks = tasksWithStatus.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(input.Sorting))
        {
            sortedTasks = input.Sorting.ToLower() switch
            {
                "transferdate" => sortedTasks.OrderBy(x => x.Task.TransferDate),
                "transferdate desc" => sortedTasks.OrderByDescending(x => x.Task.TransferDate),
                "requestcompletiondate" => sortedTasks.OrderBy(x => x.Task.RequestCompletionDate),
                "requestcompletiondate desc" => sortedTasks.OrderByDescending(x => x.Task.RequestCompletionDate),
                "testdate" => sortedTasks.OrderBy(x => x.Task.TestDate),
                "testdate desc" => sortedTasks.OrderByDescending(x => x.Task.TestDate),
                "testingstatus" => sortedTasks.OrderBy(x => x.TestingStatus),
                "testingstatus desc" => sortedTasks.OrderByDescending(x => x.TestingStatus),
                _ => sortedTasks.OrderByDescending(x => x.Task.CreationTime)
            };
        }
        else
        {
            sortedTasks = sortedTasks.OrderByDescending(x => x.Task.CreationTime);
        }

        // Apply paging
        var pagedTasks = sortedTasks
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        // Map to DTOs
        var dtos = new List<TestingListDto>();
        foreach (var item in pagedTasks)
        {
            dtos.Add(await MapToListDtoAsync(item.Task, item.Indicators, item.TestingStatus, item.IsUrgent, input.Tab, currentUserId));
        }

        return new PagedResultDto<TestingListDto>(totalCount, dtos);
    }

    public async Task<TestingDetailDto> GetAsync(Guid laboratoryTaskId, TestingTabType? viewMode = null)
    {
        var task = await _laboratoryTaskRepository.GetAsync(laboratoryTaskId);
        return await MapToDetailDtoAsync(task, viewMode);
    }

    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public async Task SaveResultsAsync(Guid laboratoryTaskId, SaveResultsInput input)
    {
        var task = await _laboratoryTaskRepository.GetAsync(laboratoryTaskId);
        var currentUserId = CurrentUser.Id;

        foreach (var result in input.Results)
        {
            var indicator = await _laboratoryTaskIndicatorRepository.GetAsync(result.IndicatorId);

            // Validate: Only assigned technician or technical manager can edit
            if (!CanEditIndicator(indicator, task, currentUserId))
            {
                throw new UserFriendlyException("Bạn không có quyền chỉnh sửa chỉ tiêu này");
            }

            // Validate: Cannot edit if already approved
            if (indicator.ApprovalStatus == IndicatorApprovalStatus.Approved)
            {
                throw new UserFriendlyException("Không thể chỉnh sửa chỉ tiêu đã được phê duyệt");
            }

            // Update indicator
            indicator.MethodId = result.MethodId;
            indicator.Unit = result.Unit;
            indicator.Result = result.Result;

            // Auto-set entry date when result is entered
            if (!string.IsNullOrWhiteSpace(result.Result) && !indicator.EntryDate.HasValue)
            {
                indicator.EntryDate = DateTime.Now;
                indicator.EnteredById = currentUserId;
            }

            await _laboratoryTaskIndicatorRepository.UpdateAsync(indicator);
        }

        // Update task status
        await UpdateTaskStatusAsync(task);
    }

    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public async Task SubmitForApprovalAsync(Guid laboratoryTaskId, SubmitForApprovalInput input)
    {
        var task = await _laboratoryTaskRepository.GetAsync(laboratoryTaskId);
        var currentUserId = CurrentUser.Id;

        foreach (var indicatorId in input.IndicatorIds)
        {
            var indicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicatorId);

            // Validate: Only assigned technician or technical manager can submit
            if (!CanEditIndicator(indicator, task, currentUserId))
            {
                throw new UserFriendlyException("Bạn không có quyền gửi duyệt chỉ tiêu này");
            }

            // Validate: Must have result
            if (string.IsNullOrWhiteSpace(indicator.Result))
            {
                throw new UserFriendlyException("Vui lòng nhập kết quả trước khi gửi duyệt");
            }

            // Validate: Cannot submit if already approved
            if (indicator.ApprovalStatus == IndicatorApprovalStatus.Approved)
            {
                continue; // Skip already approved
            }

            // Update status to Entered (waiting for approval)
            indicator.ApprovalStatus = IndicatorApprovalStatus.Entered;
            indicator.EntryDate = indicator.EntryDate ?? DateTime.Now;
            indicator.EnteredById = indicator.EnteredById ?? currentUserId;

            // Set TestDate on first result entry (when technician starts testing)
            if (!indicator.TestDate.HasValue)
            {
                indicator.TestDate = DateTime.Now;
            }

            await _laboratoryTaskIndicatorRepository.UpdateAsync(indicator);
        }

        // Update task status
        await UpdateTaskStatusAsync(task);
    }

    public async Task<List<RejectionHistoryDto>> GetRejectionHistoryAsync(Guid indicatorId)
    {
        var dtos = new List<RejectionHistoryDto>();

        try
        {
            var histories = await _rejectionHistoryRepository.GetListAsync(x => x.IndicatorId == indicatorId);

            foreach (var history in histories.OrderByDescending(h => h.RejectedDate))
            {
                var rejectedByUser = await _userRepository.FindAsync(history.RejectedById);
                dtos.Add(new RejectionHistoryDto
                {
                    Id = history.Id,
                    IndicatorId = history.IndicatorId,
                    RejectionReason = history.RejectionReason,
                    RejectedDate = history.RejectedDate,
                    RejectedById = history.RejectedById,
                    RejectedByName = rejectedByUser?.Name ?? rejectedByUser?.UserName
                });
            }
        }
        catch
        {
            // Table may not exist yet - migration not applied
        }

        return dtos;
    }

    #region Private Methods

    private TestingStatus CalculateTestingStatus(List<LaboratoryTaskIndicator> indicators)
    {
        // Loại trừ các chỉ tiêu bị KH hủy khi tính toán trạng thái
        var activeIndicators = indicators.Where(i =>
            i.ApprovalStatus != IndicatorApprovalStatus.CustomerCancelled).ToList();

        if (!activeIndicators.Any())
        {
            return TestingStatus.Todo;
        }

        // "Đang thí nghiệm" = có ≥1 chỉ tiêu đã nhập/lưu nháp (có result) HOẶC đã gửi duyệt (Entered)
        var anyInProgress = activeIndicators.Any(i =>
            i.ApprovalStatus >= IndicatorApprovalStatus.Entered ||
            !string.IsNullOrWhiteSpace(i.Result));

        // "Đã xong" = TẤT CẢ chỉ tiêu (trừ KH hủy) đã được QLKT phê duyệt (Approved)
        var allApproved = activeIndicators.All(i =>
            i.ApprovalStatus == IndicatorApprovalStatus.Approved);

        if (allApproved)
        {
            return TestingStatus.Done;
        }
        else if (anyInProgress)
        {
            return TestingStatus.InProgress;
        }
        else
        {
            return TestingStatus.Todo;
        }
    }

    private bool CanEditIndicator(LaboratoryTaskIndicator indicator, LaboratoryTask task, Guid? currentUserId)
    {
        if (!currentUserId.HasValue)
        {
            return false;
        }

        // Technical manager can always edit
        if (task.TechnicalManagerId == currentUserId.Value)
        {
            return true;
        }

        // Assigned technician can edit
        if (indicator.TechnicianId == currentUserId.Value)
        {
            return true;
        }

        return false;
    }

    private async Task UpdateTaskStatusAsync(LaboratoryTask task)
    {
        var allTaskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => x.LaboratoryTaskId == task.Id);

        // Loại trừ các chỉ tiêu bị KH hủy khi tính toán trạng thái
        var taskIndicators = allTaskIndicators.Where(ti =>
            ti.ApprovalStatus != IndicatorApprovalStatus.CustomerCancelled).ToList();

        if (!taskIndicators.Any())
        {
            return;
        }

        // Don't change status if already ResultSent (final state)
        if (task.Status == LaboratoryTaskStatus.ResultSent)
        {
            return;
        }

        // Check conditions - LOGIC MỚI (giống LaboratoryTaskAppService)
        var allAssigned = taskIndicators.All(ti => ti.MethodId.HasValue && ti.TechnicianId.HasValue);
        var anyEntered = taskIndicators.Any(ti => ti.ApprovalStatus == IndicatorApprovalStatus.Entered);
        var allEnteredOrApproved = taskIndicators.All(ti =>
            ti.ApprovalStatus == IndicatorApprovalStatus.Entered ||
            ti.ApprovalStatus == IndicatorApprovalStatus.Approved);
        var allApproved = taskIndicators.All(ti => ti.ApprovalStatus == IndicatorApprovalStatus.Approved);

        LaboratoryTaskStatus newStatus;

        if (!allAssigned)
        {
            // Có chỉ tiêu chưa gán KTV
            newStatus = LaboratoryTaskStatus.NotAssignedTechnician;
        }
        else if (allApproved)
        {
            // Tất cả đã được duyệt
            newStatus = LaboratoryTaskStatus.Approved;
        }
        else if (allEnteredOrApproved)
        {
            // TẤT CẢ chỉ tiêu đã gửi duyệt (Entered hoặc Approved)
            newStatus = LaboratoryTaskStatus.PendingApproval;
        }
        else if (anyEntered)
        {
            // Có ≥1 chỉ tiêu đã gửi duyệt (Entered)
            newStatus = LaboratoryTaskStatus.Testing;
        }
        else
        {
            // Tất cả đã có KTV nhưng chưa gửi duyệt
            newStatus = LaboratoryTaskStatus.AssignedTechnician;
        }

        if (task.Status != newStatus)
        {
            task.Status = newStatus;
            await _laboratoryTaskRepository.UpdateAsync(task);
        }
    }

    private async Task<TestingListDto> MapToListDtoAsync(
        LaboratoryTask task,
        List<LaboratoryTaskIndicator> indicators,
        TestingStatus testingStatus,
        bool isUrgent,
        TestingTabType tabType,
        Guid? currentUserId)
    {
        var dto = new TestingListDto
        {
            Id = task.Id,
            LaboratoryTaskId = task.Id,
            ContractNumber = task.ContractNumber,
            TransferDate = task.TransferDate,
            RequestCompletionDate = task.RequestCompletionDate,
            TestDate = task.TestDate,
            TestingStatus = testingStatus,
            IsUrgent = isUrgent
        };

        // Get laboratory name
        var laboratory = await _laboratoryRepository.FindAsync(task.LaboratoryId);
        dto.LaboratoryName = laboratory?.Name;

        // Get customer name from test request
        var testRequest = await _testRequestRepository.FindAsync(task.TestRequestId);
        dto.CustomerName = testRequest?.CustomerName;

        // Tab "MyAssignments": Show only current user's data
        if (tabType == TestingTabType.MyAssignments && currentUserId.HasValue)
        {
            // Loại trừ CustomerCancelled khỏi danh sách
            var userIndicators = indicators.Where(i =>
                i.TechnicianId == currentUserId.Value &&
                i.ApprovalStatus != IndicatorApprovalStatus.CustomerCancelled).ToList();

            // TotalIndicatorCount = count of user's active indicators only
            dto.TotalIndicatorCount = userIndicators.Count;

            // TotalSampleCount = count of samples that have at least 1 indicator assigned to user
            var userSampleIds = new HashSet<Guid>();
            foreach (var ind in userIndicators)
            {
                var sampleIndicator = await _sampleIndicatorRepository.FindAsync(ind.TestRequestSampleIndicatorId);
                if (sampleIndicator != null)
                {
                    userSampleIds.Add(sampleIndicator.TestRequestSampleId);
                }
            }
            dto.TotalSampleCount = userSampleIds.Count;

            // TechnicianNames = current user's name only
            var currentUser = await _userRepository.FindAsync(currentUserId.Value);
            dto.TechnicianNames = currentUser?.Name ?? currentUser?.UserName ?? "";
        }
        else
        {
            // Tab "All": Show full data (loại trừ CustomerCancelled)
            var activeIndicators = indicators.Where(i =>
                i.ApprovalStatus != IndicatorApprovalStatus.CustomerCancelled).ToList();
            dto.TotalSampleCount = task.TotalSampleCount;
            dto.TotalIndicatorCount = activeIndicators.Count;

            // Get all technician names (distinct)
            var technicianIds = indicators
                .Where(i => i.TechnicianId.HasValue)
                .Select(i => i.TechnicianId!.Value)
                .Distinct()
                .ToList();

            if (technicianIds.Any())
            {
                var technicianNames = new List<string>();
                foreach (var techId in technicianIds)
                {
                    var user = await _userRepository.FindAsync(techId);
                    if (user != null)
                    {
                        technicianNames.Add(user.Name ?? user.UserName ?? "");
                    }
                }
                dto.TechnicianNames = string.Join(", ", technicianNames.Where(n => !string.IsNullOrWhiteSpace(n)));
            }
        }

        return dto;
    }

    private async Task<TestingDetailDto> MapToDetailDtoAsync(LaboratoryTask task, TestingTabType? viewMode = null)
    {
        var allTaskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => x.LaboratoryTaskId == task.Id);
        var currentUserId = CurrentUser.Id;

        // Filter indicators based on viewMode
        // MyAssignments: Only show indicators assigned to current user
        // All (or null): Show all indicators
        List<LaboratoryTaskIndicator> taskIndicators;
        if (viewMode == TestingTabType.MyAssignments && currentUserId.HasValue)
        {
            taskIndicators = allTaskIndicators.Where(i => i.TechnicianId == currentUserId.Value).ToList();
        }
        else
        {
            taskIndicators = allTaskIndicators;
        }

        // Calculate testing status based on filtered indicators for display
        var testingStatus = CalculateTestingStatus(taskIndicators);

        var dto = new TestingDetailDto
        {
            Id = task.Id,
            LaboratoryTaskId = task.Id,
            ContractNumber = task.ContractNumber,
            TestingStatus = testingStatus,
            LaboratoryId = task.LaboratoryId,
            RequestCompletionDate = task.RequestCompletionDate,
            TechnicalManagerId = task.TechnicalManagerId
        };

        // Get laboratory name
        var laboratory = await _laboratoryRepository.FindAsync(task.LaboratoryId);
        dto.LaboratoryName = laboratory?.Name;

        // Get customer name
        var testRequest = await _testRequestRepository.FindAsync(task.TestRequestId);
        dto.CustomerName = testRequest?.CustomerName;

        // Get technical manager name
        if (task.TechnicalManagerId.HasValue)
        {
            var manager = await _userRepository.FindAsync(task.TechnicalManagerId.Value);
            dto.TechnicalManagerName = manager?.Name ?? manager?.UserName;
        }

        // Get technician names
        var technicianIds = taskIndicators
            .Where(i => i.TechnicianId.HasValue)
            .Select(i => i.TechnicianId!.Value)
            .Distinct()
            .ToList();

        if (technicianIds.Any())
        {
            var technicianNames = new List<string>();
            foreach (var techId in technicianIds)
            {
                var user = await _userRepository.FindAsync(techId);
                if (user != null)
                {
                    technicianNames.Add(user.Name ?? user.UserName ?? "");
                }
            }
            dto.TechnicianNames = string.Join(", ", technicianNames.Where(n => !string.IsNullOrWhiteSpace(n)));
        }

        // Count completed indicators (loại trừ CustomerCancelled khỏi tổng số)
        var activeIndicators = taskIndicators.Where(i => i.ApprovalStatus != IndicatorApprovalStatus.CustomerCancelled).ToList();
        dto.CompletedIndicatorCount = activeIndicators.Count(i => i.ApprovalStatus == IndicatorApprovalStatus.Approved);
        dto.TotalIndicatorCount = activeIndicators.Count;

        // Group indicators by sample
        var sampleGroups = new Dictionary<Guid, TestingSampleDto>();

        foreach (var taskIndicator in taskIndicators)
        {
            var sampleIndicator = await _sampleIndicatorRepository.FindAsync(taskIndicator.TestRequestSampleIndicatorId);
            if (sampleIndicator == null) continue;

            var sampleId = sampleIndicator.TestRequestSampleId;

            if (!sampleGroups.ContainsKey(sampleId))
            {
                var sample = await _sampleRepository.FindAsync(sampleId);
                sampleGroups[sampleId] = new TestingSampleDto
                {
                    SampleId = sampleId,
                    SampleCode = sample?.SampleCode,
                    SampleName = sample?.SampleName,
                    StatusText = sample?.StatusText
                };
            }

            // Map indicator
            var indicator = await _indicatorRepository.FindAsync(sampleIndicator.IndicatorId);
            string? methodName = null;
            string? technicianName = null;
            string? unit = taskIndicator.Unit;

            if (taskIndicator.MethodId.HasValue)
            {
                var method = await _testMethodRepository.FindAsync(taskIndicator.MethodId.Value);
                methodName = method?.Name;
                // Fallback to TestMethod.Unit if LaboratoryTaskIndicator.Unit is not set
                if (string.IsNullOrEmpty(unit) && method != null)
                {
                    unit = method.Unit;
                }
            }

            if (taskIndicator.TechnicianId.HasValue)
            {
                var technician = await _userRepository.FindAsync(taskIndicator.TechnicianId.Value);
                technicianName = technician?.Name ?? technician?.UserName;
            }

            // Get rejection history count and latest reason
            var rejectionHistories = new List<IndicatorRejectionHistory>();
            IndicatorRejectionHistory? latestRejection = null;
            try
            {
                rejectionHistories = await _rejectionHistoryRepository.GetListAsync(x => x.IndicatorId == taskIndicator.Id);
                latestRejection = rejectionHistories.OrderByDescending(h => h.RejectedDate).FirstOrDefault();
            }
            catch
            {
                // Table may not exist yet - migration not applied
            }

            // Determine if can edit
            var canEdit = taskIndicator.ApprovalStatus != IndicatorApprovalStatus.Approved &&
                          CanEditIndicator(taskIndicator, task, CurrentUser.Id);

            sampleGroups[sampleId].Indicators.Add(new TestingIndicatorDto
            {
                Id = taskIndicator.Id,
                TestRequestSampleIndicatorId = taskIndicator.TestRequestSampleIndicatorId,
                IndicatorId = sampleIndicator.IndicatorId,
                IndicatorName = indicator?.Name,
                MethodId = taskIndicator.MethodId,
                MethodName = methodName,
                Unit = unit,
                Result = taskIndicator.Result,
                EntryDate = taskIndicator.EntryDate,
                AssignedDate = taskIndicator.AssignedDate,
                CompletionDate = taskIndicator.ApprovalDate,
                ApprovalStatus = taskIndicator.ApprovalStatus,
                LatestRejectionReason = latestRejection?.RejectionReason ?? taskIndicator.RejectionReason,
                RejectionHistoryCount = rejectionHistories.Count,
                TechnicianId = taskIndicator.TechnicianId,
                TechnicianName = technicianName,
                CanEdit = canEdit
            });
        }

        dto.Samples = sampleGroups.Values.ToList();

        return dto;
    }

    #endregion
}
