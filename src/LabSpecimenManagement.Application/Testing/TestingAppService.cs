using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.Common.Exceptions;
using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.MasterData.Indicators;
using LabSpecimenManagement.Notifications;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.TestRequests;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace LabSpecimenManagement.Testing;

[RemoteService(IsEnabled = false)]
[Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Default)]
public partial class TestingAppService : ApplicationService, ITestingAppService
{
    private readonly IRepository<LaboratoryTask, Guid> _laboratoryTaskRepository;
    private readonly IRepository<LaboratoryTaskIndicator, Guid> _laboratoryTaskIndicatorRepository;
    private readonly IRepository<IndicatorRejectionHistory, Guid> _rejectionHistoryRepository;
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;
    private readonly IRepository<TestRequestSample, Guid> _sampleRepository;
    private readonly IRepository<TestRequestSampleIndicator, Guid> _sampleIndicatorRepository;
    private readonly IRepository<TestRequestSampleAttachment, Guid> _sampleAttachmentRepository;
    private readonly IRepository<Laboratory, Guid> _laboratoryRepository;
    private readonly IRepository<Indicator, Guid> _indicatorRepository;
    private readonly IRepository<TestMethod, Guid> _testMethodRepository;
    private readonly IRepository<SampleTestingImage, Guid> _sampleTestingImageRepository;
    private readonly IRepository<IndicatorImage, Guid> _indicatorImageRepository;
    private readonly IUserNameProvider _userNameProvider;
    private readonly IGuidGenerator _guidGenerator;
    private readonly INotificationCreator _notificationCreator;
    private readonly ITestResultValueAppService _testResultValueAppService;

    public TestingAppService(
        IRepository<LaboratoryTask, Guid> laboratoryTaskRepository,
        IRepository<LaboratoryTaskIndicator, Guid> laboratoryTaskIndicatorRepository,
        IRepository<IndicatorRejectionHistory, Guid> rejectionHistoryRepository,
        IRepository<TestRequest, Guid> testRequestRepository,
        IRepository<TestRequestSample, Guid> sampleRepository,
        IRepository<TestRequestSampleIndicator, Guid> sampleIndicatorRepository,
        IRepository<TestRequestSampleAttachment, Guid> sampleAttachmentRepository,
        IRepository<Laboratory, Guid> laboratoryRepository,
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<TestMethod, Guid> testMethodRepository,
        IRepository<SampleTestingImage, Guid> sampleTestingImageRepository,
        IRepository<IndicatorImage, Guid> indicatorImageRepository,
        IUserNameProvider userNameProvider,
        IGuidGenerator guidGenerator,
        INotificationCreator notificationCreator,
        ITestResultValueAppService testResultValueAppService)
    {
        _laboratoryTaskRepository = laboratoryTaskRepository;
        _laboratoryTaskIndicatorRepository = laboratoryTaskIndicatorRepository;
        _rejectionHistoryRepository = rejectionHistoryRepository;
        _testRequestRepository = testRequestRepository;
        _sampleRepository = sampleRepository;
        _sampleIndicatorRepository = sampleIndicatorRepository;
        _sampleAttachmentRepository = sampleAttachmentRepository;
        _laboratoryRepository = laboratoryRepository;
        _indicatorRepository = indicatorRepository;
        _testMethodRepository = testMethodRepository;
        _sampleTestingImageRepository = sampleTestingImageRepository;
        _indicatorImageRepository = indicatorImageRepository;
        _userNameProvider = userNameProvider;
        _guidGenerator = guidGenerator;
        _notificationCreator = notificationCreator;
        _testResultValueAppService = testResultValueAppService;
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

        // Apply user filter:
        // - Admin: xem tất cả
        // - QLKT: xem được task mà họ là TechnicalManager hoặc là KTV
        // - KTV: chỉ xem được task mà họ được assign làm KTV
        var isAdmin = CurrentUser.IsInRole(AppConstants.RoleAdmin);
        if (currentUserId.HasValue && !isAdmin)
        {
            // User có thể xem task nếu:
            // 1. Họ là TechnicalManager của task (QLKT)
            // 2. Họ được assign làm KTV cho ít nhất 1 indicator
            taskQuery = taskQuery.Where(x =>
                x.Task.TechnicalManagerId == currentUserId.Value ||
                x.Indicators.Any(i => i.TechnicianId == currentUserId.Value));
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
                SortingFields.TransferDate => sortedTasks.OrderBy(x => x.Task.TransferDate),
                SortingFields.TransferDateDesc => sortedTasks.OrderByDescending(x => x.Task.TransferDate),
                SortingFields.RequestCompletionDate => sortedTasks.OrderBy(x => x.Task.RequestCompletionDate),
                SortingFields.RequestCompletionDateDesc => sortedTasks.OrderByDescending(x => x.Task.RequestCompletionDate),
                SortingFields.TestDate => sortedTasks.OrderBy(x => x.Task.TestDate),
                SortingFields.TestDateDesc => sortedTasks.OrderByDescending(x => x.Task.TestDate),
                SortingFields.TestingStatus => sortedTasks.OrderBy(x => x.TestingStatus),
                SortingFields.TestingStatusDesc => sortedTasks.OrderByDescending(x => x.TestingStatus),
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
                throw new ForbiddenAccessException("Bạn không có quyền chỉnh sửa chỉ tiêu này");
            }

            // Validate: Cannot edit if already approved
            if (indicator.ApprovalStatus == IndicatorApprovalStatus.Approved)
            {
                throw new Common.Exceptions.InvalidOperationException("Không thể chỉnh sửa chỉ tiêu đã được phê duyệt", "indicator");
            }

            // Note: Result now accepts both numeric values, formulas (2^2, 10^-3),
            // unicode superscript (2², 10⁻³), and text values (Đạt, Không đạt, Âm tính)
            // Text values will be auto-created in master data when submitting for approval

            // Update indicator
            indicator.MethodId = result.MethodId;
            indicator.Unit = result.Unit;
            indicator.Result = result.Result;
            indicator.Note = result.Note;

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
                throw new ForbiddenAccessException("Bạn không có quyền gửi duyệt chỉ tiêu này");
            }

            // Validate: Must have result
            if (string.IsNullOrWhiteSpace(indicator.Result))
            {
                throw new ValidationException("result", "Vui lòng nhập kết quả trước khi gửi duyệt");
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
        try
        {
            var histories = await _rejectionHistoryRepository.GetListAsync(x => x.IndicatorId == indicatorId);
            var orderedHistories = histories.OrderByDescending(h => h.RejectedDate).ToList();

            // Batch fetch user names
            var userNames = await _userNameProvider.GetUserNamesAsync(
                orderedHistories.Select(h => (Guid?)h.RejectedById));

            return orderedHistories.Select(history => new RejectionHistoryDto
            {
                Id = history.Id,
                IndicatorId = history.IndicatorId,
                RejectionReason = history.RejectionReason,
                RejectedDate = history.RejectedDate,
                RejectedById = history.RejectedById,
                RejectedByName = userNames.TryGetValue(history.RejectedById, out var name) ? name : null
            }).ToList();
        }
        catch
        {
            // Table may not exist yet - migration not applied
            return new List<RejectionHistoryDto>();
        }
    }

    #region Private Methods

    /// <summary>
    /// Tính trạng thái công việc xét nghiệm (Task level) dựa trên các chỉ tiêu
    /// Ưu tiên: Rejected > PendingApproval > InProgress > NotStarted
    /// Completed khi tất cả chỉ tiêu đã được duyệt
    /// </summary>
    private TestingStatus CalculateTestingStatus(List<LaboratoryTaskIndicator> indicators)
    {
        // Loại trừ các chỉ tiêu bị KH hủy khi tính toán trạng thái
        var activeIndicators = indicators.Where(i =>
            i.ApprovalStatus != IndicatorApprovalStatus.CustomerCancelled).ToList();

        if (!activeIndicators.Any())
        {
            return TestingStatus.NotStarted;
        }

        // "Hoàn thành" = TẤT CẢ chỉ tiêu (trừ KH hủy) đã được QLKT phê duyệt (Approved)
        var allApproved = activeIndicators.All(i =>
            i.ApprovalStatus == IndicatorApprovalStatus.Approved);

        if (allApproved)
        {
            return TestingStatus.Completed;
        }

        // "Bị từ chối" = Có ≥1 chỉ tiêu bị từ chối
        var anyRejected = activeIndicators.Any(i =>
            i.ApprovalStatus == IndicatorApprovalStatus.Rejected);

        if (anyRejected)
        {
            return TestingStatus.Rejected;
        }

        // "Chờ phê duyệt" = Có ≥1 chỉ tiêu đã gửi (SubmitDate != null và status = Entered)
        var anyPendingApproval = activeIndicators.Any(i =>
            i.ApprovalStatus == IndicatorApprovalStatus.Entered);

        if (anyPendingApproval)
        {
            return TestingStatus.PendingApproval;
        }

        // "Đang thử nghiệm" = Có ≥1 chỉ tiêu đã check-in (TestDate != null) và đang Pending
        // Chỉ tính những chỉ tiêu đang "testing" thực sự, không tính Approved (đã hoàn thành)
        var anyInProgress = activeIndicators.Any(i =>
            i.TestDate.HasValue && i.ApprovalStatus == IndicatorApprovalStatus.Pending);

        if (anyInProgress)
        {
            return TestingStatus.InProgress;
        }

        // Mặc định: "Chưa bắt đầu"
        return TestingStatus.NotStarted;
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

    /// <summary>
    /// Tự động cập nhật trạng thái task dựa trên trạng thái các indicators
    /// ĐỒNG BỘ VỚI LaboratoryTaskAppService.UpdateTaskStatusAsync
    /// Logic: Ưu tiên trạng thái THẤP NHẤT (phải hoàn thành TẤT CẢ mới lên status cao hơn)
    /// Ngoại trừ PendingApproval - ưu tiên hiển thị khi có chỉ tiêu chờ duyệt (cần action từ QLKT)
    /// </summary>
    private async Task UpdateTaskStatusAsync(LaboratoryTask task)
    {
        var allTaskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => x.LaboratoryTaskId == task.Id);

        // Loại trừ các chỉ tiêu bị KH hủy khi tính toán trạng thái
        var activeIndicators = allTaskIndicators.Where(ti =>
            ti.ApprovalStatus != IndicatorApprovalStatus.CustomerCancelled).ToList();

        if (!activeIndicators.Any())
        {
            return;
        }

        // Check conditions for status calculation - ĐỒNG BỘ VỚI LaboratoryTaskAppService
        // Logic mới: Ưu tiên trạng thái THẤP NHẤT (phải hoàn thành tất cả mới lên status cao hơn)
        // Ngoại trừ PendingApproval - cần hiển thị ngay khi có chỉ tiêu chờ duyệt (cần action từ QLKT)

        // TẤT CẢ chỉ tiêu đã gửi QLCL
        var allSentToQuality = activeIndicators.All(ti =>
            ti.ApprovalStatus == IndicatorApprovalStatus.Approved && ti.IsSentToQuality);

        // TẤT CẢ chỉ tiêu đã được duyệt (Approved)
        var allApproved = activeIndicators.All(ti => ti.ApprovalStatus == IndicatorApprovalStatus.Approved);

        // Có ≥1 chỉ tiêu đã nhập KQ (Entered - chờ duyệt) - ưu tiên hiển thị vì cần action
        var anyEntered = activeIndicators.Any(ti => ti.ApprovalStatus == IndicatorApprovalStatus.Entered);

        // TẤT CẢ chỉ tiêu đã được giao việc (có method + KTV)
        var allAssigned = activeIndicators.All(ti => ti.MethodId.HasValue && ti.TechnicianId.HasValue);

        // Có ≥1 chỉ tiêu đang thử nghiệm (có testDate, KTV đã check-in, status = Pending)
        // Chỉ tính những chỉ tiêu đang "testing" thực sự, không tính Approved/Entered/Rejected
        var anyTesting = activeIndicators.Any(ti =>
            ti.TestDate.HasValue && ti.ApprovalStatus == IndicatorApprovalStatus.Pending);

        LaboratoryTaskStatus newStatus;

        // Logic ưu tiên:
        // 1. PendingApproval được ưu tiên hiển thị khi có chỉ tiêu chờ duyệt (cần QLKT action)
        // 2. Các status khác phải TẤT CẢ chỉ tiêu đạt được mới chuyển lên

        if (anyEntered)
        {
            // Có ≥1 chỉ tiêu chờ duyệt - ưu tiên hiển thị vì cần QLKT duyệt
            newStatus = LaboratoryTaskStatus.PendingApproval;
        }
        else if (allSentToQuality)
        {
            // TẤT CẢ chỉ tiêu đã gửi QLCL
            newStatus = LaboratoryTaskStatus.SentToQuality;
        }
        else if (allApproved)
        {
            // TẤT CẢ chỉ tiêu đã được duyệt (nhưng chưa gửi hết QLCL)
            newStatus = LaboratoryTaskStatus.Approved;
        }
        else if (allAssigned && anyTesting)
        {
            // TẤT CẢ chỉ tiêu đã được giao việc VÀ có ≥1 đang thử nghiệm
            newStatus = LaboratoryTaskStatus.Testing;
        }
        else if (allAssigned)
        {
            // TẤT CẢ chỉ tiêu đã được giao việc (chưa có ai test)
            newStatus = LaboratoryTaskStatus.Assigned;
        }
        else
        {
            // Còn chỉ tiêu chưa được giao việc
            newStatus = LaboratoryTaskStatus.NotAssigned;
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
            dto.TechnicianNames = await _userNameProvider.GetUserNameAsync(currentUserId.Value) ?? "";
        }
        else
        {
            // Tab "All": Show full data (loại trừ CustomerCancelled)
            var activeIndicators = indicators.Where(i =>
                i.ApprovalStatus != IndicatorApprovalStatus.CustomerCancelled).ToList();
            dto.TotalSampleCount = task.TotalSampleCount;
            dto.TotalIndicatorCount = activeIndicators.Count;

            // Get all technician names (distinct) - batch fetch
            var technicianIds = indicators
                .Where(i => i.TechnicianId.HasValue)
                .Select(i => i.TechnicianId)
                .Distinct()
                .ToList();

            if (technicianIds.Any())
            {
                var userNames = await _userNameProvider.GetUserNamesAsync(technicianIds);
                dto.TechnicianNames = string.Join(", ", userNames.Values.Where(n => !string.IsNullOrWhiteSpace(n)));
            }
        }

        return dto;
    }

    private async Task<TestingDetailDto> MapToDetailDtoAsync(LaboratoryTask task, TestingTabType? viewMode = null)
    {
        var allTaskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => x.LaboratoryTaskId == task.Id);
        var currentUserId = CurrentUser.Id;
        var isAdmin = CurrentUser.IsInRole(AppConstants.RoleAdmin);

        // Filter indicators based on viewMode
        // Admin và QLKT: luôn xem tất cả indicators (bỏ qua viewMode = MyAssignments)
        // KTV với MyAssignments: chỉ xem indicators được assign cho họ
        // All (or null): Show all indicators
        List<LaboratoryTaskIndicator> taskIndicators;
        var isQLKT = task.TechnicalManagerId == currentUserId;

        if (viewMode == TestingTabType.MyAssignments && currentUserId.HasValue && !isAdmin && !isQLKT)
        {
            // Chỉ KTV mới filter theo TechnicianId khi viewMode = MyAssignments
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

        // Collect all user IDs needed for this DTO (batch fetch)
        var allUserIds = new List<Guid?> { task.TechnicalManagerId };
        allUserIds.AddRange(taskIndicators.Select(i => i.TechnicianId));
        var userNames = await _userNameProvider.GetUserNamesAsync(allUserIds);

        // Get technical manager name
        if (task.TechnicalManagerId.HasValue && userNames.TryGetValue(task.TechnicalManagerId.Value, out var managerName))
        {
            dto.TechnicalManagerName = managerName;
        }

        // Get technician names
        var technicianIds = taskIndicators
            .Where(i => i.TechnicianId.HasValue)
            .Select(i => i.TechnicianId!.Value)
            .Distinct()
            .ToList();

        if (technicianIds.Any())
        {
            var technicianNameList = technicianIds
                .Where(id => userNames.ContainsKey(id))
                .Select(id => userNames[id])
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .ToList();
            dto.TechnicianNames = string.Join(", ", technicianNameList);
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
                var sampleDto = new TestingSampleDto
                {
                    SampleId = sampleId,
                    SampleCode = sample?.SampleCode,
                    SampleName = sample?.SampleName,
                    StatusText = sample?.StatusText
                };

                // Load sample images from TestRequest (readonly)
                var sampleAttachments = await _sampleAttachmentRepository.GetListAsync(x => x.TestRequestSampleId == sampleId);
                sampleDto.SampleImages = sampleAttachments.Select(a => new SampleAttachmentDto
                {
                    Id = a.Id,
                    FileId = a.FileId,
                    FileName = a.FileName,
                    FilePath = a.FilePath,
                    FileSize = a.FileSize
                }).ToList();

                // Load testing images (before/after testing)
                var testingImages = await _sampleTestingImageRepository.GetListAsync(x =>
                    x.LaboratoryTaskId == task.Id && x.SampleId == sampleId);

                sampleDto.BeforeTestingImages = testingImages
                    .Where(i => i.ImageType == SampleTestingImageType.BeforeTesting)
                    .OrderBy(i => i.CreationTime)
                    .Select(i => new SampleTestingImageDto
                    {
                        Id = i.Id,
                        SampleId = i.SampleId,
                        FileId = i.FileId,
                        FileName = i.FileName,
                        FilePath = i.FilePath,
                        FileSize = i.FileSize,
                        ImageType = i.ImageType,
                        CreationTime = i.CreationTime,
                        CreatorId = i.CreatorId
                    }).ToList();

                sampleDto.AfterTestingImages = testingImages
                    .Where(i => i.ImageType == SampleTestingImageType.AfterTesting)
                    .OrderBy(i => i.CreationTime)
                    .Select(i => new SampleTestingImageDto
                    {
                        Id = i.Id,
                        SampleId = i.SampleId,
                        FileId = i.FileId,
                        FileName = i.FileName,
                        FilePath = i.FilePath,
                        FileSize = i.FileSize,
                        ImageType = i.ImageType,
                        CreationTime = i.CreationTime,
                        CreatorId = i.CreatorId
                    }).ToList();

                sampleGroups[sampleId] = sampleDto;
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

            if (taskIndicator.TechnicianId.HasValue && userNames.TryGetValue(taskIndicator.TechnicianId.Value, out var techName))
            {
                technicianName = techName;
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
            var hasPermission = CanEditIndicator(taskIndicator, task, CurrentUser.Id);
            var canEdit = taskIndicator.ApprovalStatus != IndicatorApprovalStatus.Approved && hasPermission;

            // Determine if can check-in (Thực hiện)
            // Có thể check-in nếu: có quyền, status = Pending hoặc Rejected, chưa có TestDate
            var canCheckIn = hasPermission &&
                             (taskIndicator.ApprovalStatus == IndicatorApprovalStatus.Pending ||
                              taskIndicator.ApprovalStatus == IndicatorApprovalStatus.Rejected) &&
                             !taskIndicator.TestDate.HasValue;

            // Determine if can submit (Gửi kết quả)
            // Có thể submit nếu: có quyền, đã check-in (có TestDate), status = Pending hoặc Rejected, có kết quả
            var canSubmit = hasPermission &&
                            taskIndicator.TestDate.HasValue &&
                            (taskIndicator.ApprovalStatus == IndicatorApprovalStatus.Pending ||
                             taskIndicator.ApprovalStatus == IndicatorApprovalStatus.Rejected) &&
                            !string.IsNullOrWhiteSpace(taskIndicator.Result);

            // Calculate indicator testing status
            var indicatorTestingStatus = CalculateIndicatorTestingStatus(taskIndicator);

            // Get indicator images
            var indicatorImages = await _indicatorImageRepository.GetListAsync(x =>
                x.LaboratoryTaskIndicatorId == taskIndicator.Id);

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
                Note = taskIndicator.Note,
                EntryDate = taskIndicator.EntryDate,
                AssignedDate = taskIndicator.AssignedDate,
                StartDate = taskIndicator.TestDate,
                SubmitDate = taskIndicator.SubmitDate,
                CompletionDate = taskIndicator.ApprovalDate,
                ApprovalStatus = taskIndicator.ApprovalStatus,
                TestingStatus = indicatorTestingStatus,
                LatestRejectionReason = latestRejection?.RejectionReason ?? taskIndicator.RejectionReason,
                RejectionHistoryCount = rejectionHistories.Count,
                TechnicianId = taskIndicator.TechnicianId,
                TechnicianName = technicianName,
                CanEdit = canEdit,
                CanCheckIn = canCheckIn,
                CanSubmit = canSubmit,
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
            });
        }

        dto.Samples = sampleGroups.Values.ToList();

        return dto;
    }

    /// <summary>
    /// Check-in các chỉ tiêu (bắt đầu thử nghiệm - ấn "Thực hiện")
    /// Set TestDate = DateTime.Now cho các chỉ tiêu được chọn
    /// </summary>
    public async Task CheckInIndicatorsAsync(Guid laboratoryTaskId, CheckInIndicatorsInput input)
    {
        var currentUserId = CurrentUser.Id;

        var task = await _laboratoryTaskRepository.GetAsync(laboratoryTaskId);
        if (task == null)
        {
            throw new EntityNotFoundException("LaboratoryTask", laboratoryTaskId);
        }

        if (input.IndicatorIds == null || !input.IndicatorIds.Any())
        {
            throw new ValidationException("indicatorIds", "Vui lòng chọn ít nhất 1 chỉ tiêu");
        }

        foreach (var indicatorId in input.IndicatorIds)
        {
            var indicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicatorId);
            if (indicator == null) continue;

            // Kiểm tra quyền
            if (!CanEditIndicator(indicator, task, currentUserId))
            {
                continue;
            }

            // Chỉ check-in indicators có status = Pending (Todo) hoặc Rejected
            if (indicator.ApprovalStatus != IndicatorApprovalStatus.Pending &&
                indicator.ApprovalStatus != IndicatorApprovalStatus.Rejected)
            {
                continue;
            }

            // Nếu đang bị từ chối, reset ApprovalStatus về Pending để thử nghiệm lại
            if (indicator.ApprovalStatus == IndicatorApprovalStatus.Rejected)
            {
                indicator.ApprovalStatus = IndicatorApprovalStatus.Pending;
                // Clear SubmitDate khi thử nghiệm lại
                indicator.SubmitDate = null;
            }

            // Set TestDate = ngày hiện tại (bắt đầu thử nghiệm)
            indicator.TestDate = DateTime.Now;

            await _laboratoryTaskIndicatorRepository.UpdateAsync(indicator);
        }

        // Update task status
        await UpdateTaskStatusAsync(task);

        // Update TestRequest status to InProgress when KTV checks in
        await TryUpdateTestRequestToInProgressAsync(task.TestRequestId);
    }

    /// <summary>
    /// Kiểm tra và cập nhật TestRequest sang InProgress khi có KTV check-in (có testDate)
    /// </summary>
    private async Task TryUpdateTestRequestToInProgressAsync(Guid testRequestId)
    {
        var testRequest = await _testRequestRepository.FindAsync(testRequestId);
        if (testRequest == null) return;

        // Chỉ cập nhật khi status đang ở SampleSent
        if (testRequest.Status != TestRequests.TestRequestStatus.SampleSent)
        {
            return;
        }

        // Lấy tất cả tasks của TestRequest này
        var tasks = await _laboratoryTaskRepository.GetListAsync(t => t.TestRequestId == testRequestId);
        if (!tasks.Any()) return;

        var taskIds = tasks.Select(t => t.Id).ToList();

        // Lấy tất cả indicators của các tasks
        var allIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(
            ti => taskIds.Contains(ti.LaboratoryTaskId));

        // Loại trừ các chỉ tiêu bị KH hủy
        var activeIndicators = allIndicators
            .Where(ti => ti.ApprovalStatus != LaboratoryTasks.IndicatorApprovalStatus.CustomerCancelled)
            .ToList();

        if (!activeIndicators.Any()) return;

        // Chỉ chuyển sang InProgress khi có ≥1 KTV đã check-in (có testDate)
        var anyTesting = activeIndicators.Any(ti => ti.TestDate.HasValue);

        if (anyTesting)
        {
            testRequest.Status = TestRequests.TestRequestStatus.InProgress;
            await _testRequestRepository.UpdateAsync(testRequest);
        }
    }

    /// <summary>
    /// Gửi kết quả cho QLKT duyệt
    /// Set SubmitDate = DateTime.Now và ApprovalStatus = Entered
    /// </summary>
    public async Task SubmitResultsAsync(Guid laboratoryTaskId, SubmitResultsInput input)
    {
        var currentUserId = CurrentUser.Id;

        var task = await _laboratoryTaskRepository.GetAsync(laboratoryTaskId);
        if (task == null)
        {
            throw new EntityNotFoundException("LaboratoryTask", laboratoryTaskId);
        }

        if (input.IndicatorIds == null || !input.IndicatorIds.Any())
        {
            throw new ValidationException("indicatorIds", "Vui lòng chọn ít nhất 1 chỉ tiêu");
        }

        var errors = new List<string>();
        var submittedIndicators = new List<LaboratoryTaskIndicator>();

        foreach (var indicatorId in input.IndicatorIds)
        {
            var indicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicatorId);
            if (indicator == null) continue;

            // Kiểm tra quyền
            if (!CanEditIndicator(indicator, task, currentUserId))
            {
                continue;
            }

            // Chỉ submit indicators đã check-in (có TestDate) và chưa submit
            if (!indicator.TestDate.HasValue)
            {
                errors.Add($"Chỉ tiêu chưa được thực hiện (check-in)");
                continue;
            }

            // Chỉ submit nếu status = Pending hoặc Rejected
            if (indicator.ApprovalStatus != IndicatorApprovalStatus.Pending &&
                indicator.ApprovalStatus != IndicatorApprovalStatus.Rejected)
            {
                continue;
            }

            // Kiểm tra đã nhập kết quả
            if (string.IsNullOrWhiteSpace(indicator.Result))
            {
                errors.Add($"Chỉ tiêu chưa có kết quả");
                continue;
            }

            // Auto-create TestResultValue for text values (not numeric/formula)
            // This allows user to enter new text values like "Đạt", "Không đạt"
            // which will be saved to master data for future autocomplete
            if (!IsNumericOrFormulaValue(indicator.Result))
            {
                await _testResultValueAppService.GetOrCreateByNameAsync(indicator.Result);
            }

            // Set SubmitDate và ApprovalStatus = Entered
            indicator.SubmitDate = DateTime.Now;
            indicator.EntryDate ??= DateTime.Now;
            indicator.EnteredById ??= currentUserId;
            indicator.ApprovalStatus = IndicatorApprovalStatus.Entered;

            await _laboratoryTaskIndicatorRepository.UpdateAsync(indicator);
            submittedIndicators.Add(indicator);
        }

        if (errors.Any())
        {
            throw new ValidationException("indicators", string.Join("; ", errors.Distinct()));
        }

        // Update task status
        await UpdateTaskStatusAsync(task);

        // Notify QLKT about submitted results (Case 1 & 6)
        if (task.TechnicalManagerId.HasValue && submittedIndicators.Any())
        {
            await NotifyTechnicalManagerOnSubmitAsync(task, submittedIndicators);
        }
    }

    /// <summary>
    /// Notify technical manager when KTV submits indicator results
    /// </summary>
    private async Task NotifyTechnicalManagerOnSubmitAsync(LaboratoryTask task, List<LaboratoryTaskIndicator> submittedIndicators)
    {
        var testRequest = await _testRequestRepository.GetAsync(task.TestRequestId);
        var senderName = CurrentUser.Name ?? CurrentUser.UserName ?? "Kỹ thuật viên";
        var indicatorCount = submittedIndicators.Count;

        // Get sample/indicator/method info for single indicator case
        string? sampleName = null;
        string? indicatorName = null;
        string? methodName = null;

        if (indicatorCount == 1)
        {
            var taskIndicator = submittedIndicators.First();
            var sampleIndicator = await _sampleIndicatorRepository.FindAsync(taskIndicator.TestRequestSampleIndicatorId);
            if (sampleIndicator != null)
            {
                var sample = await _sampleRepository.FindAsync(sampleIndicator.TestRequestSampleId);
                var indicator = await _indicatorRepository.FindAsync(sampleIndicator.IndicatorId);
                sampleName = sample?.SampleName;
                indicatorName = indicator?.Name;
            }
            if (taskIndicator.MethodId.HasValue)
            {
                var method = await _testMethodRepository.FindAsync(taskIndicator.MethodId.Value);
                methodName = method?.Name;
            }
        }

        await _notificationCreator.NotifyIndicatorSubmittedToTechnicalManagerAsync(
            task.TechnicalManagerId!.Value,
            testRequest.ContractNumber ?? task.ContractNumber ?? "",
            task.TestRequestId,
            senderName,
            indicatorCount,
            task.RequestCompletionDate,
            sampleName,
            indicatorName,
            methodName
        );
    }

    /// <summary>
    /// Tính trạng thái thử nghiệm của chỉ tiêu (Indicator level)
    /// </summary>
    private IndicatorTestingStatus CalculateIndicatorTestingStatus(LaboratoryTaskIndicator indicator)
    {
        if (indicator.ApprovalStatus == IndicatorApprovalStatus.CustomerCancelled)
        {
            return IndicatorTestingStatus.Cancelled; // KH đã hủy
        }

        if (indicator.ApprovalStatus == IndicatorApprovalStatus.Approved)
        {
            return IndicatorTestingStatus.Done;
        }

        if (indicator.ApprovalStatus == IndicatorApprovalStatus.Rejected)
        {
            return IndicatorTestingStatus.Rejected;
        }

        if (indicator.ApprovalStatus == IndicatorApprovalStatus.Entered)
        {
            return IndicatorTestingStatus.PendingApproval;
        }

        // Pending status
        if (indicator.TestDate.HasValue)
        {
            return IndicatorTestingStatus.Testing; // Đã check-in
        }

        return IndicatorTestingStatus.Todo; // Chưa check-in
    }

    /// <summary>
    /// Kiểm tra xem giá trị có phải là số hoặc công thức toán học không
    /// Các dạng được coi là số/công thức:
    /// - Số thuần: 0.5, 1.2, 100, -5
    /// - Công thức mũ dạng caret: 2^2, 10^-3, 1.5^2
    /// - Unicode superscript: 2², 10⁻³, 5⁺², xⁿ
    /// - Ký hiệu khoa học: 1.5×10², 2×10⁻⁶, 1.5x10^-3
    /// - Phân số/tỷ lệ: 1/2, 3:4, 1÷2
    /// - Khoảng: 1-5, 10~20, 5±2
    /// - Phần trăm/phần nghìn: 50%, 5‰
    /// - Bất đẳng thức: >5, <10, ≥3, ≤8, >=5, <=10
    /// </summary>
    private bool IsNumericOrFormulaValue(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;

        // Trim whitespace
        value = value.Trim();

        // 1. Số thuần (bao gồm số âm và số thập phân)
        if (decimal.TryParse(value, out _)) return true;

        // 2. Ký tự superscript unicode (số và chữ)
        const string superscriptChars = "⁰¹²³⁴⁵⁶⁷⁸⁹⁺⁻ⁿˣʸᵃᵇᶜᵈᵉᶠᵍʰⁱʲᵏˡᵐᵒᵖʳˢᵗᵘᵛʷᶻ";

        // 3. Ký tự subscript unicode
        const string subscriptChars = "₀₁₂₃₄₅₆₇₈₉₊₋ₐₑₕᵢⱼₖₗₘₙₒₚᵣₛₜᵤᵥₓ";

        // 4. Các ký tự toán học đặc biệt
        const string mathOperators = "×÷±∓√∛∜∞≈≠≤≥<>≪≫∝∑∏∫∂∇";

        // 5. Các ký tự số và toán học cơ bản
        const string basicMathChars = "0123456789.,-+*/^:~%‰()[]{}";

        // Kết hợp tất cả các ký tự hợp lệ
        var allValidChars = superscriptChars + subscriptChars + mathOperators + basicMathChars + "xXeE";

        // Kiểm tra xem chuỗi chỉ chứa các ký tự hợp lệ
        bool containsOnlyValidChars = value.All(c => allValidChars.Contains(c) || char.IsWhiteSpace(c));

        // Kiểm tra xem có ít nhất một số trong chuỗi
        bool containsDigit = value.Any(c => char.IsDigit(c) || superscriptChars.Contains(c) || subscriptChars.Contains(c));

        // Nếu chỉ chứa ký tự hợp lệ VÀ có ít nhất một số → là công thức
        if (containsOnlyValidChars && containsDigit)
        {
            return true;
        }

        return false;
    }

    #endregion

    #region Sample Testing Image APIs

    /// <summary>
    /// Upload sample testing image (before/after testing)
    /// </summary>
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public async Task<SampleTestingImageDto> UploadSampleTestingImageAsync(
        Guid laboratoryTaskId,
        UploadSampleTestingImageInput input,
        string fileId,
        string fileName,
        string filePath,
        long fileSize)
    {
        var task = await _laboratoryTaskRepository.GetAsync(laboratoryTaskId);
        var currentUserId = CurrentUser.Id;

        // Validate: Chỉ KTV được assign hoặc QLKT được upload
        var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => x.LaboratoryTaskId == laboratoryTaskId);
        var hasPermission = task.TechnicalManagerId == currentUserId ||
                           taskIndicators.Any(i => i.TechnicianId == currentUserId);

        if (!hasPermission)
        {
            throw new ForbiddenAccessException("Bạn không có quyền upload hình ảnh cho mẫu này");
        }

        // Validate: Max 5 images per type
        var existingImages = await _sampleTestingImageRepository.GetListAsync(x =>
            x.LaboratoryTaskId == laboratoryTaskId &&
            x.SampleId == input.SampleId &&
            x.ImageType == input.ImageType);

        if (existingImages.Count >= 5)
        {
            var imageTypeName = input.ImageType == SampleTestingImageType.BeforeTesting ? "trước thử nghiệm" : "sau thử nghiệm";
            throw new ValidationException("images", $"Đã đạt tối đa 5 hình ảnh {imageTypeName}");
        }

        var image = new SampleTestingImage(
            _guidGenerator.Create(),
            laboratoryTaskId,
            input.SampleId,
            fileId,
            fileName,
            filePath,
            fileSize,
            input.ImageType
        );

        await _sampleTestingImageRepository.InsertAsync(image);

        return new SampleTestingImageDto
        {
            Id = image.Id,
            SampleId = image.SampleId,
            FileId = image.FileId,
            FileName = image.FileName,
            FilePath = image.FilePath,
            FileSize = image.FileSize,
            ImageType = image.ImageType,
            CreationTime = image.CreationTime,
            CreatorId = image.CreatorId
        };
    }

    /// <summary>
    /// Delete sample testing image
    /// </summary>
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public async Task DeleteSampleTestingImageAsync(Guid imageId)
    {
        var image = await _sampleTestingImageRepository.GetAsync(imageId);
        var task = await _laboratoryTaskRepository.GetAsync(image.LaboratoryTaskId);
        var currentUserId = CurrentUser.Id;

        // Validate: Chỉ người tạo hoặc QLKT được xóa
        if (image.CreatorId != currentUserId && task.TechnicalManagerId != currentUserId)
        {
            throw new ForbiddenAccessException("Bạn không có quyền xóa hình ảnh này");
        }

        await _sampleTestingImageRepository.DeleteAsync(image);
    }

    /// <summary>
    /// Get sample testing images
    /// </summary>
    public async Task<List<SampleTestingImageDto>> GetSampleTestingImagesAsync(
        Guid laboratoryTaskId,
        Guid sampleId,
        SampleTestingImageType? imageType = null)
    {
        var images = await _sampleTestingImageRepository.GetListAsync(x =>
            x.LaboratoryTaskId == laboratoryTaskId &&
            x.SampleId == sampleId &&
            (!imageType.HasValue || x.ImageType == imageType.Value));

        return images.OrderBy(x => x.CreationTime).Select(x => new SampleTestingImageDto
        {
            Id = x.Id,
            SampleId = x.SampleId,
            FileId = x.FileId,
            FileName = x.FileName,
            FilePath = x.FilePath,
            FileSize = x.FileSize,
            ImageType = x.ImageType,
            CreationTime = x.CreationTime,
            CreatorId = x.CreatorId
        }).ToList();
    }

    #endregion

    #region Indicator Image APIs

    /// <summary>
    /// Upload indicator image (single)
    /// </summary>
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public async Task<IndicatorImageDto> UploadIndicatorImageAsync(
        Guid laboratoryTaskId,
        Guid indicatorId,
        string fileId,
        string fileName,
        string filePath,
        long fileSize)
    {
        var task = await _laboratoryTaskRepository.GetAsync(laboratoryTaskId);
        var currentUserId = CurrentUser.Id;

        // Get indicator
        var indicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicatorId);
        if (indicator.LaboratoryTaskId != laboratoryTaskId)
        {
            throw new ValidationException("indicatorId", "Chỉ tiêu không thuộc công việc này");
        }

        // Validate: Chỉ KTV được assign cho indicator này hoặc QLKT mới được upload
        var hasPermission = task.TechnicalManagerId == currentUserId ||
                           indicator.TechnicianId == currentUserId;

        if (!hasPermission)
        {
            throw new ForbiddenAccessException("Bạn không có quyền upload hình ảnh cho chỉ tiêu này");
        }

        // Validate: Chỉ được upload khi indicator đang ở trạng thái Testing (Entered = Đang thử nghiệm)
        // Hoặc Pending (chưa nhập kết quả) với test date đã được set (đã check-in)
        var canUpload = indicator.ApprovalStatus == IndicatorApprovalStatus.Entered ||
                       (indicator.ApprovalStatus == IndicatorApprovalStatus.Pending && indicator.TestDate.HasValue);
        if (!canUpload)
        {
            throw new ValidationException("status", "Chỉ được upload hình ảnh khi chỉ tiêu đang ở trạng thái thử nghiệm");
        }

        // Validate: Max 5 images per indicator
        var existingImages = await _indicatorImageRepository.GetListAsync(x =>
            x.LaboratoryTaskIndicatorId == indicatorId);

        if (existingImages.Count >= 5)
        {
            throw new ValidationException("images", "Đã đạt tối đa 5 hình ảnh cho chỉ tiêu này");
        }

        // Create new image
        var image = new IndicatorImage(
            _guidGenerator.Create(),
            indicatorId,
            fileId,
            fileName,
            filePath,
            fileSize);

        await _indicatorImageRepository.InsertAsync(image);

        return new IndicatorImageDto
        {
            Id = image.Id,
            LaboratoryTaskIndicatorId = image.LaboratoryTaskIndicatorId,
            FileId = image.FileId,
            FileName = image.FileName,
            FileSize = image.FileSize,
            Url = $"/api/v1/files/{image.FileId}",
            CreationTime = image.CreationTime,
            CreatorId = image.CreatorId
        };
    }

    /// <summary>
    /// Delete indicator image
    /// </summary>
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public async Task DeleteIndicatorImageAsync(Guid imageId)
    {
        var image = await _indicatorImageRepository.GetAsync(imageId);
        var indicator = await _laboratoryTaskIndicatorRepository.GetAsync(image.LaboratoryTaskIndicatorId);
        var task = await _laboratoryTaskRepository.GetAsync(indicator.LaboratoryTaskId);
        var currentUserId = CurrentUser.Id;

        // Validate: Chỉ người tạo hoặc QLKT mới được xóa
        var canDelete = image.CreatorId == currentUserId || task.TechnicalManagerId == currentUserId;
        if (!canDelete)
        {
            throw new ForbiddenAccessException("Bạn không có quyền xóa hình ảnh này");
        }

        await _indicatorImageRepository.DeleteAsync(image);
    }

    /// <summary>
    /// Get indicator images
    /// </summary>
    public async Task<List<IndicatorImageDto>> GetIndicatorImagesAsync(Guid indicatorId)
    {
        var images = await _indicatorImageRepository.GetListAsync(x =>
            x.LaboratoryTaskIndicatorId == indicatorId);

        return images.OrderBy(x => x.CreationTime).Select(x => new IndicatorImageDto
        {
            Id = x.Id,
            LaboratoryTaskIndicatorId = x.LaboratoryTaskIndicatorId,
            FileId = x.FileId,
            FileName = x.FileName,
            FileSize = x.FileSize,
            Url = $"/api/v1/files/{x.FileId}",
            CreationTime = x.CreationTime,
            CreatorId = x.CreatorId
        }).ToList();
    }

    #endregion
}
