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

        // Apply tab filter
        if (input.Tab == TestingTabType.MyAssignments && currentUserId.HasValue)
        {
            // Only tasks where current user is assigned as technician
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
            dtos.Add(await MapToListDtoAsync(item.Task, item.Indicators, item.TestingStatus, item.IsUrgent));
        }

        return new PagedResultDto<TestingListDto>(totalCount, dtos);
    }

    public async Task<TestingDetailDto> GetAsync(Guid laboratoryTaskId)
    {
        var task = await _laboratoryTaskRepository.GetAsync(laboratoryTaskId);
        return await MapToDetailDtoAsync(task);
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
        if (!indicators.Any())
        {
            return TestingStatus.Todo;
        }

        var anyEntered = indicators.Any(i =>
            i.ApprovalStatus >= IndicatorApprovalStatus.Entered ||
            !string.IsNullOrWhiteSpace(i.Result));

        var allSubmittedOrApproved = indicators.All(i =>
            i.ApprovalStatus == IndicatorApprovalStatus.Entered ||
            i.ApprovalStatus == IndicatorApprovalStatus.Approved);

        if (allSubmittedOrApproved)
        {
            return TestingStatus.Done;
        }
        else if (anyEntered)
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
        var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => x.LaboratoryTaskId == task.Id);

        if (!taskIndicators.Any())
        {
            return;
        }

        // Don't change status if already ResultSent
        if (task.Status == LaboratoryTaskStatus.ResultSent)
        {
            return;
        }

        // Check conditions
        var allHaveMethod = taskIndicators.All(ti => ti.MethodId.HasValue);
        var anyHaveResult = taskIndicators.Any(ti => !string.IsNullOrEmpty(ti.Result));
        var allHaveResult = taskIndicators.All(ti => !string.IsNullOrEmpty(ti.Result));
        var anyEntered = taskIndicators.Any(ti => ti.ApprovalStatus == IndicatorApprovalStatus.Entered);
        var allApproved = taskIndicators.All(ti => ti.ApprovalStatus == IndicatorApprovalStatus.Approved);

        LaboratoryTaskStatus newStatus;

        if (!allHaveMethod)
        {
            newStatus = LaboratoryTaskStatus.NotAssignedMethod;
        }
        else if (allApproved)
        {
            newStatus = LaboratoryTaskStatus.Approved;
        }
        else if (anyEntered || (allHaveResult && !allApproved))
        {
            newStatus = LaboratoryTaskStatus.PendingApproval;
        }
        else if (anyHaveResult)
        {
            newStatus = LaboratoryTaskStatus.Testing;
        }
        else
        {
            newStatus = LaboratoryTaskStatus.AssignedMethod;
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
        bool isUrgent)
    {
        var dto = new TestingListDto
        {
            Id = task.Id,
            LaboratoryTaskId = task.Id,
            ContractNumber = task.ContractNumber,
            TotalSampleCount = task.TotalSampleCount,
            TotalIndicatorCount = indicators.Count,
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

        // Get technician names (distinct)
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

        return dto;
    }

    private async Task<TestingDetailDto> MapToDetailDtoAsync(LaboratoryTask task)
    {
        var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => x.LaboratoryTaskId == task.Id);
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

        // Count completed indicators
        dto.CompletedIndicatorCount = taskIndicators.Count(i => i.ApprovalStatus == IndicatorApprovalStatus.Approved);
        dto.TotalIndicatorCount = taskIndicators.Count;

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

            if (taskIndicator.MethodId.HasValue)
            {
                var method = await _testMethodRepository.FindAsync(taskIndicator.MethodId.Value);
                methodName = method?.Name;
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
                Unit = taskIndicator.Unit,
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
