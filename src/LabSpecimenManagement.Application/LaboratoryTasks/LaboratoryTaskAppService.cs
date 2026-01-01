using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.Common.Exceptions;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.MasterData.Indicators;
using LabSpecimenManagement.Notifications;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.QualityManagement;
using LabSpecimenManagement.TestRequests;
using LabSpecimenManagement.Users;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace LabSpecimenManagement.LaboratoryTasks;

[Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Default)]
public partial class LaboratoryTaskAppService : ApplicationService, ILaboratoryTaskAppService
{
    private readonly IRepository<LaboratoryTask, Guid> _laboratoryTaskRepository;
    private readonly IRepository<LaboratoryTaskIndicator, Guid> _laboratoryTaskIndicatorRepository;
    private readonly IRepository<LaboratoryTaskSupporter, Guid> _supporterRepository;
    private readonly IRepository<IndicatorRejectionHistory, Guid> _rejectionHistoryRepository;
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;
    private readonly IRepository<TestRequestSample, Guid> _sampleRepository;
    private readonly IRepository<TestRequestSampleIndicator, Guid> _sampleIndicatorRepository;
    private readonly IRepository<Laboratory, Guid> _laboratoryRepository;
    private readonly IRepository<Indicator, Guid> _indicatorRepository;
    private readonly IRepository<IndicatorGroup, Guid> _indicatorGroupRepository;
    private readonly IRepository<TestMethod, Guid> _testMethodRepository;
    private readonly IIdentityUserRepository _userRepository;
    private readonly IUserNameProvider _userNameProvider;
    private readonly IIdentityRoleRepository _roleRepository;
    private readonly ITestRequestAppService _testRequestAppService;
    private readonly IRepository<QualityManagementTask, Guid> _qualityTaskRepository;
    private readonly IRepository<QualityManagementIndicator, Guid> _qualityIndicatorRepository;
    private readonly IRepository<QualityIndicatorStatusHistory, Guid> _qualityIndicatorHistoryRepository;
    private readonly INotificationCreator _notificationCreator;
    private readonly IRepository<Standard, Guid> _standardRepository;
    private readonly IRepository<StandardIndicator, Guid> _standardIndicatorRepository;

    public LaboratoryTaskAppService(
        IRepository<LaboratoryTask, Guid> laboratoryTaskRepository,
        IRepository<LaboratoryTaskIndicator, Guid> laboratoryTaskIndicatorRepository,
        IRepository<LaboratoryTaskSupporter, Guid> supporterRepository,
        IRepository<IndicatorRejectionHistory, Guid> rejectionHistoryRepository,
        IRepository<TestRequest, Guid> testRequestRepository,
        IRepository<TestRequestSample, Guid> sampleRepository,
        IRepository<TestRequestSampleIndicator, Guid> sampleIndicatorRepository,
        IRepository<Laboratory, Guid> laboratoryRepository,
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<IndicatorGroup, Guid> indicatorGroupRepository,
        IRepository<TestMethod, Guid> testMethodRepository,
        IIdentityUserRepository userRepository,
        IUserNameProvider userNameProvider,
        IIdentityRoleRepository roleRepository,
        ITestRequestAppService testRequestAppService,
        IRepository<QualityManagementTask, Guid> qualityTaskRepository,
        IRepository<QualityManagementIndicator, Guid> qualityIndicatorRepository,
        IRepository<QualityIndicatorStatusHistory, Guid> qualityIndicatorHistoryRepository,
        INotificationCreator notificationCreator,
        IRepository<Standard, Guid> standardRepository,
        IRepository<StandardIndicator, Guid> standardIndicatorRepository)
    {
        _laboratoryTaskRepository = laboratoryTaskRepository;
        _laboratoryTaskIndicatorRepository = laboratoryTaskIndicatorRepository;
        _supporterRepository = supporterRepository;
        _rejectionHistoryRepository = rejectionHistoryRepository;
        _testRequestRepository = testRequestRepository;
        _sampleRepository = sampleRepository;
        _sampleIndicatorRepository = sampleIndicatorRepository;
        _laboratoryRepository = laboratoryRepository;
        _indicatorRepository = indicatorRepository;
        _indicatorGroupRepository = indicatorGroupRepository;
        _testMethodRepository = testMethodRepository;
        _userRepository = userRepository;
        _userNameProvider = userNameProvider;
        _roleRepository = roleRepository;
        _testRequestAppService = testRequestAppService;
        _qualityTaskRepository = qualityTaskRepository;
        _qualityIndicatorRepository = qualityIndicatorRepository;
        _qualityIndicatorHistoryRepository = qualityIndicatorHistoryRepository;
        _notificationCreator = notificationCreator;
        _standardRepository = standardRepository;
        _standardIndicatorRepository = standardIndicatorRepository;
    }

    public async Task<LaboratoryTaskDto> GetAsync(Guid id)
    {
        var task = await _laboratoryTaskRepository.GetAsync(id);

        // Recalculate status mỗi khi GET để đảm bảo status luôn đúng
        await UpdateTaskStatusAsync(task);

        // Đồng bộ trạng thái TestRequest
        await UpdateTestRequestStatusAsync(task.TestRequestId);

        return await MapToDetailDtoAsync(task);
    }

    public async Task<PagedResultDto<LaboratoryTaskDto>> GetFilteredListAsync(GetLaboratoryTaskListDto input)
    {
        var queryable = await _laboratoryTaskRepository.GetQueryableAsync();

        // Filter by current user's access (admin sees all, others see tasks where they are QLKT or supporter)
        var currentUserId = CurrentUser.Id;
        if (currentUserId.HasValue && !CurrentUser.IsInRole(AppConstants.RoleAdmin))
        {
            // Get task IDs where current user is a supporter
            var supporterTaskIds = (await _supporterRepository.GetListAsync(s => s.UserId == currentUserId.Value))
                .Select(s => s.LaboratoryTaskId)
                .ToHashSet();

            // Filter: current user is QLKT OR current user is supporter
            queryable = queryable.Where(x =>
                x.TechnicalManagerId == currentUserId.Value ||
                supporterTaskIds.Contains(x.Id));
        }

        // Apply search filter - search by ContractNumber and TechnicalManager name
        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var search = input.Search.Trim();

            // Get list of TechnicalManagerIds that match the search term
            var matchingUserIds = (await _userRepository.GetListAsync())
                .Where(u => u.Name.ContainsIgnoreCase(search) ||
                           u.Surname.ContainsIgnoreCase(search) ||
                           u.UserName.ContainsIgnoreCase(search))
                .Select(u => u.Id)
                .ToList();

            var searchLower = search.ToLower();
            queryable = queryable.Where(x =>
                (x.ContractNumber != null && x.ContractNumber.ToLower().Contains(searchLower)) ||
                (x.TechnicalManagerId.HasValue && matchingUserIds.Contains(x.TechnicalManagerId.Value)));
        }

        if (input.LaboratoryId.HasValue)
        {
            queryable = queryable.Where(x => x.LaboratoryId == input.LaboratoryId.Value);
        }

        if (input.TechnicalManagerId.HasValue)
        {
            queryable = queryable.Where(x => x.TechnicalManagerId == input.TechnicalManagerId.Value);
        }

        if (input.Status.HasValue)
        {
            queryable = queryable.Where(x => x.Status == input.Status.Value);
        }

        if (input.TransferDateFrom.HasValue)
        {
            queryable = queryable.Where(x => x.TransferDate >= input.TransferDateFrom.Value);
        }

        if (input.TransferDateTo.HasValue)
        {
            queryable = queryable.Where(x => x.TransferDate <= input.TransferDateTo.Value);
        }

        if (input.RequestCompletionDateFrom.HasValue)
        {
            queryable = queryable.Where(x => x.RequestCompletionDate >= input.RequestCompletionDateFrom.Value);
        }

        if (input.RequestCompletionDateTo.HasValue)
        {
            queryable = queryable.Where(x => x.RequestCompletionDate <= input.RequestCompletionDateTo.Value);
        }

        // Get total count
        var totalCount = queryable.Count();

        // Apply sorting
        if (!string.IsNullOrWhiteSpace(input.Sorting))
        {
            queryable = input.Sorting.ToLower() switch
            {
                SortingFields.TransferDate => queryable.OrderBy(x => x.TransferDate),
                SortingFields.TransferDateDesc => queryable.OrderByDescending(x => x.TransferDate),
                SortingFields.RequestCompletionDate => queryable.OrderBy(x => x.RequestCompletionDate),
                SortingFields.RequestCompletionDateDesc => queryable.OrderByDescending(x => x.RequestCompletionDate),
                SortingFields.Status => queryable.OrderBy(x => x.Status),
                SortingFields.StatusDesc => queryable.OrderByDescending(x => x.Status),
                _ => queryable.OrderByDescending(x => x.CreationTime)
            };
        }
        else
        {
            queryable = queryable.OrderByDescending(x => x.CreationTime);
        }

        // Apply paging
        var tasks = queryable
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        // Map to DTOs
        var dtos = new List<LaboratoryTaskDto>();
        foreach (var task in tasks)
        {
            dtos.Add(await MapToListDtoAsync(task));
        }

        return new PagedResultDto<LaboratoryTaskDto>(totalCount, dtos);
    }

    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public async Task<LaboratoryTaskDto> UpdateAsync(Guid id, UpdateLaboratoryTaskDto input)
    {
        var task = await _laboratoryTaskRepository.GetAsync(id);

        task.TestDate = input.TestDate;
        task.Status = input.Status;
        task.Notes = input.Notes;

        await _laboratoryTaskRepository.UpdateAsync(task);

        return await MapToDetailDtoAsync(task);
    }

    public async Task<ListResultDto<LaboratoryTaskDto>> GetMyLaboratoryTasksAsync()
    {
        var currentUserId = CurrentUser.Id;
        if (!currentUserId.HasValue)
        {
            return new ListResultDto<LaboratoryTaskDto>();
        }

        var queryable = await _laboratoryTaskRepository.GetQueryableAsync();
        var tasks = queryable
            .Where(x => x.TechnicalManagerId == currentUserId.Value)
            .OrderByDescending(x => x.CreationTime)
            .ToList();

        var dtos = new List<LaboratoryTaskDto>();
        foreach (var task in tasks)
        {
            dtos.Add(await MapToListDtoAsync(task));
        }

        return new ListResultDto<LaboratoryTaskDto>(dtos);
    }

    public async Task<ListResultDto<LaboratoryTaskDto>> GetByLaboratoryAsync(Guid laboratoryId)
    {
        var queryable = await _laboratoryTaskRepository.GetQueryableAsync();
        var tasks = queryable
            .Where(x => x.LaboratoryId == laboratoryId)
            .OrderByDescending(x => x.CreationTime)
            .ToList();

        var dtos = new List<LaboratoryTaskDto>();
        foreach (var task in tasks)
        {
            dtos.Add(await MapToListDtoAsync(task));
        }

        return new ListResultDto<LaboratoryTaskDto>(dtos);
    }

    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public async Task<LaboratoryTaskDto> UpdateStatusAsync(Guid id, LaboratoryTaskStatus newStatus)
    {
        var task = await _laboratoryTaskRepository.GetAsync(id);
        task.Status = newStatus;
        await _laboratoryTaskRepository.UpdateAsync(task);
        return await MapToDetailDtoAsync(task);
    }

    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public async Task<LaboratoryTaskDto> SetTestDateAsync(Guid id, DateTime testDate)
    {
        var task = await _laboratoryTaskRepository.GetAsync(id);
        task.TestDate = testDate;
        await _laboratoryTaskRepository.UpdateAsync(task);
        return await MapToDetailDtoAsync(task);
    }

    #region Private Methods

    private async Task<LaboratoryTaskDto> MapToListDtoAsync(LaboratoryTask task)
    {
        // Lấy ngày thí nghiệm sớm nhất từ các indicators (ngày KTV bắt đầu thực hiện chỉ tiêu đầu tiên)
        var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => x.LaboratoryTaskId == task.Id);
        var earliestTestDate = taskIndicators
            .Where(ti => ti.TestDate.HasValue)
            .OrderBy(ti => ti.TestDate)
            .FirstOrDefault()?.TestDate;

        var dto = new LaboratoryTaskDto
        {
            Id = task.Id,
            TestRequestId = task.TestRequestId,
            LaboratoryId = task.LaboratoryId,
            ContractNumber = task.ContractNumber,
            TotalSampleCount = task.TotalSampleCount,
            TechnicalManagerId = task.TechnicalManagerId,
            TransferDate = task.TransferDate,
            RequestCompletionDate = task.RequestCompletionDate,
            TestDate = earliestTestDate ?? task.TestDate, // Ưu tiên ngày sớm nhất từ indicators, fallback về task.TestDate
            Status = task.Status,
            Notes = task.Notes,
            ResultRejectionReason = task.ResultRejectionReason,
            CreationTime = task.CreationTime,
            LastModificationTime = task.LastModificationTime
        };

        // Get TestRequest info
        var testRequest = await _testRequestRepository.FindAsync(task.TestRequestId);
        if (testRequest != null)
        {
            dto.TestRequestNumber = testRequest.RequestNumber;
            dto.CustomerName = testRequest.CustomerName;
        }

        // Get Laboratory info
        var laboratory = await _laboratoryRepository.FindAsync(task.LaboratoryId);
        if (laboratory != null)
        {
            dto.LaboratoryCode = laboratory.Code;
            dto.LaboratoryName = laboratory.Name;
        }

        // Get Technical Manager name
        dto.TechnicalManagerName = await _userNameProvider.GetUserNameAsync(task.TechnicalManagerId);

        // Set current user role flags
        var currentUserId = CurrentUser.Id;
        if (currentUserId.HasValue)
        {
            dto.IsCurrentUserTechnicalManager = task.TechnicalManagerId == currentUserId.Value;

            // Check if current user is a supporter
            var supporters = await _supporterRepository.GetListAsync(s => s.LaboratoryTaskId == task.Id);
            dto.IsCurrentUserSupporter = supporters.Any(s => s.UserId == currentUserId.Value);
        }

        return dto;
    }

    private async Task<LaboratoryTaskDto> MapToDetailDtoAsync(LaboratoryTask task)
    {
        var dto = await MapToListDtoAsync(task);

        // Get indicators
        var taskIndicators = await _laboratoryTaskIndicatorRepository
            .GetListAsync(x => x.LaboratoryTaskId == task.Id);

        // Batch fetch all user names needed for indicators
        var allUserIds = taskIndicators
            .SelectMany(ti => new[] { ti.TechnicianId, ti.EnteredById, ti.ApprovedById })
            .ToList();
        var userNames = await _userNameProvider.GetUserNamesAsync(allUserIds);

        foreach (var taskIndicator in taskIndicators)
        {
            var sampleIndicator = await _sampleIndicatorRepository.FindAsync(taskIndicator.TestRequestSampleIndicatorId);
            if (sampleIndicator == null) continue;

            var sample = await _sampleRepository.FindAsync(sampleIndicator.TestRequestSampleId);
            var indicator = await _indicatorRepository.FindAsync(sampleIndicator.IndicatorId);

            string? indicatorGroupName = null;
            if (indicator != null)
            {
                var indicatorGroup = await _indicatorGroupRepository.FindAsync(indicator.IndicatorGroupId);
                indicatorGroupName = indicatorGroup?.Name;
            }

            // Get user names from batch-fetched dictionary
            string? technicianName = taskIndicator.TechnicianId.HasValue && userNames.TryGetValue(taskIndicator.TechnicianId.Value, out var tn) ? tn : null;
            string? enteredByName = taskIndicator.EnteredById.HasValue && userNames.TryGetValue(taskIndicator.EnteredById.Value, out var en) ? en : null;
            string? approvedByName = taskIndicator.ApprovedById.HasValue && userNames.TryGetValue(taskIndicator.ApprovedById.Value, out var an) ? an : null;
            string? methodName = null;
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

            // Get standard info if StandardId is set
            string? standardName = null;
            string? comparisonOperator = null;
            string? allowedLimit = null;
            if (taskIndicator.StandardId.HasValue)
            {
                var standard = await _standardRepository.FindAsync(taskIndicator.StandardId.Value);
                standardName = standard?.Name;

                // Get StandardIndicator to get comparison operator and allowed limit
                var standardIndicator = await _standardIndicatorRepository.FirstOrDefaultAsync(
                    si => si.StandardId == taskIndicator.StandardId.Value &&
                          si.IndicatorId == sampleIndicator.IndicatorId &&
                          si.TestMethodId == taskIndicator.MethodId);
                if (standardIndicator != null)
                {
                    comparisonOperator = standardIndicator.ComparisonOperator;
                    allowedLimit = standardIndicator.AllowedLimit;
                }
            }

            dto.Indicators.Add(new LaboratoryTaskIndicatorDto
            {
                Id = taskIndicator.Id,
                TestRequestSampleIndicatorId = taskIndicator.TestRequestSampleIndicatorId,
                SampleId = sampleIndicator.TestRequestSampleId,
                SampleCode = sample?.SampleCode,
                SampleName = sample?.SampleName,
                IndicatorId = sampleIndicator.IndicatorId,
                IndicatorCode = indicator?.Code,
                IndicatorName = indicator?.Name,
                IndicatorGroupName = indicatorGroupName,
                UnitPrice = sampleIndicator.UnitPrice,
                // Phase 6 workflow fields
                MethodId = taskIndicator.MethodId,
                MethodName = methodName,
                TechnicianId = taskIndicator.TechnicianId,
                TechnicianName = technicianName,
                AssignedDate = taskIndicator.AssignedDate,
                TestDate = taskIndicator.TestDate,
                Result = taskIndicator.Result,
                Unit = unit,
                EntryDate = taskIndicator.EntryDate,
                EnteredById = taskIndicator.EnteredById,
                EnteredByName = enteredByName,
                ApprovalStatus = taskIndicator.ApprovalStatus,
                ApprovalDate = taskIndicator.ApprovalDate,
                ApprovedById = taskIndicator.ApprovedById,
                ApprovedByName = approvedByName,
                RejectionReason = taskIndicator.RejectionReason,
                IsSentToQuality = taskIndicator.IsSentToQuality,
                // Phase 7 - Standard comparison fields
                StandardId = taskIndicator.StandardId,
                StandardName = standardName,
                ComparisonOperator = comparisonOperator,
                AllowedLimit = allowedLimit
            });
        }

        // Get supporters
        dto.Supporters = await GetSupportersAsync(task.Id);

        return dto;
    }

    #endregion

    public async Task DeleteAsync(Guid id)
    {
        var task = await _laboratoryTaskRepository.GetAsync(id);

        // Validation: Không cho xóa khi đang thí nghiệm trở đi
        if (!CanEditTask(task.Status))
        {
            throw new Common.Exceptions.InvalidOperationException($"Không thể xóa công việc khi trạng thái là '{GetStatusLabel(task.Status)}'", "task");
        }

        // Delete junction records first
        var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(ti => ti.LaboratoryTaskId == id);
        foreach (var taskIndicator in taskIndicators)
        {
            await _laboratoryTaskIndicatorRepository.DeleteAsync(taskIndicator);
        }

        // Delete the task
        await _laboratoryTaskRepository.DeleteAsync(id);
    }

    #region Phase 6 - Workflow Methods

    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public async Task<LaboratoryTaskDto> AssignSampleIndicatorsAsync(Guid taskId, Guid sampleId, List<AssignIndicatorDto> assignments)
    {
        var task = await _laboratoryTaskRepository.GetAsync(taskId);

        // Get all task indicators for this sample
        var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => x.LaboratoryTaskId == taskId);

        // Filter by sampleId through junction
        var indicatorsForSample = new List<LaboratoryTaskIndicator>();
        foreach (var ti in taskIndicators)
        {
            var sampleIndicator = await _sampleIndicatorRepository.FindAsync(ti.TestRequestSampleIndicatorId);
            if (sampleIndicator != null && sampleIndicator.TestRequestSampleId == sampleId)
            {
                indicatorsForSample.Add(ti);
            }
        }

        // Validate only assignments that are sent (partial assignment is allowed now)
        // Each assignment must have both MethodId and TechnicianId
        // Validation: Chỉ cho phép giao việc cho indicator chưa được duyệt hoặc chưa bị hủy
        foreach (var assignment in assignments)
        {
            // Validate that indicator exists in this sample
            var taskIndicator = indicatorsForSample.FirstOrDefault(ti => ti.Id == assignment.IndicatorId);
            if (taskIndicator == null)
            {
                throw new ValidationException("indicator", "Chỉ tiêu không thuộc mẫu này");
            }

            // Không cho giao việc lại nếu indicator đã được duyệt hoặc bị hủy
            if (taskIndicator.ApprovalStatus == IndicatorApprovalStatus.Approved)
            {
                throw new Common.Exceptions.InvalidOperationException("Không thể giao việc lại cho chỉ tiêu đã được duyệt", "indicator");
            }
            if (taskIndicator.ApprovalStatus == IndicatorApprovalStatus.CustomerCancelled)
            {
                throw new Common.Exceptions.InvalidOperationException("Không thể giao việc cho chỉ tiêu đã bị hủy", "indicator");
            }

            if (!assignment.MethodId.HasValue || !assignment.TechnicianId.HasValue)
            {
                throw new ValidationException("assignment", "Vui lòng chọn phương pháp và kỹ thuật viên cho tất cả chỉ tiêu được chọn");
            }
        }

        // Apply assignments
        foreach (var assignment in assignments)
        {
            var taskIndicator = indicatorsForSample.FirstOrDefault(ti => ti.Id == assignment.IndicatorId);
            if (taskIndicator != null)
            {
                // Set AssignedDate when technician is assigned for the first time
                if (!taskIndicator.TechnicianId.HasValue && assignment.TechnicianId.HasValue)
                {
                    taskIndicator.AssignedDate = DateTime.Now;
                }
                taskIndicator.MethodId = assignment.MethodId;
                taskIndicator.TechnicianId = assignment.TechnicianId;
                await _laboratoryTaskIndicatorRepository.UpdateAsync(taskIndicator);
            }
        }

        // Update task status based on all indicators
        await UpdateTaskStatusAsync(task);

        // Update TestRequest status to InProgress when QLKT assigns PP + KTV
        // Only update if all tasks for this TestRequest have all indicators assigned
        await TryUpdateTestRequestToInProgressAsync(task.TestRequestId);

        // Notify technicians about assigned indicators (Case 1 & 2 KTV)
        await NotifyTechniciansOnAssignAsync(task, indicatorsForSample, assignments);

        return await MapToDetailDtoAsync(task);
    }

    /// <summary>
    /// Notify technicians when indicators are assigned to them
    /// Groups notifications by technician
    /// </summary>
    private async Task NotifyTechniciansOnAssignAsync(
        LaboratoryTask task,
        List<LaboratoryTaskIndicator> indicatorsForSample,
        List<AssignIndicatorDto> assignments)
    {
        var testRequest = await _testRequestRepository.GetAsync(task.TestRequestId);
        var contractNumber = testRequest.ContractNumber ?? task.ContractNumber ?? "";
        var technicalManagerName = CurrentUser.Name ?? CurrentUser.UserName ?? "Quản lý kỹ thuật";

        // Group assignments by TechnicianId
        var assignmentsByTechnician = assignments
            .Where(a => a.TechnicianId.HasValue)
            .GroupBy(a => a.TechnicianId!.Value)
            .ToList();

        foreach (var group in assignmentsByTechnician)
        {
            var technicianId = group.Key;
            var technicianAssignments = group.ToList();
            var indicatorCount = technicianAssignments.Count;

            if (indicatorCount == 1)
            {
                // Case 2: Single indicator - include details
                var assignment = technicianAssignments.First();
                var taskIndicator = indicatorsForSample.FirstOrDefault(ti => ti.Id == assignment.IndicatorId);
                if (taskIndicator != null)
                {
                    var sampleIndicator = await _sampleIndicatorRepository.FindAsync(taskIndicator.TestRequestSampleIndicatorId);
                    var sample = sampleIndicator != null ? await _sampleRepository.FindAsync(sampleIndicator.TestRequestSampleId) : null;
                    var indicator = sampleIndicator != null ? await _indicatorRepository.FindAsync(sampleIndicator.IndicatorId) : null;
                    var method = assignment.MethodId.HasValue ? await _testMethodRepository.FindAsync(assignment.MethodId.Value) : null;

                    await _notificationCreator.NotifySingleIndicatorAssignedToTechnicianAsync(
                        technicianId,
                        contractNumber,
                        task.TestRequestId,
                        technicalManagerName,
                        sample?.SampleName ?? "",
                        indicator?.Name ?? "",
                        method?.Name ?? "",
                        task.RequestCompletionDate
                    );
                }
            }
            else
            {
                // Case 1: Multiple indicators
                await _notificationCreator.NotifyMultipleIndicatorsAssignedToTechnicianAsync(
                    technicianId,
                    contractNumber,
                    task.TestRequestId,
                    indicatorCount,
                    task.RequestCompletionDate
                );
            }
        }
    }

    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public async Task<LaboratoryTaskIndicatorDto> UpdateIndicatorResultAsync(Guid indicatorId, UpdateIndicatorResultDto input)
    {
        var taskIndicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicatorId);

        // Validation: Must have method assigned
        if (!taskIndicator.MethodId.HasValue)
        {
            throw new ValidationException("method", "Chỉ tiêu chưa được gán phương pháp");
        }

        taskIndicator.Result = input.Result;
        taskIndicator.Unit = input.Unit;
        taskIndicator.EntryDate = DateTime.Now;
        taskIndicator.EnteredById = CurrentUser.Id;
        taskIndicator.ApprovalStatus = IndicatorApprovalStatus.Entered;

        // Set TestDate on first result entry (when technician starts testing)
        if (!taskIndicator.TestDate.HasValue)
        {
            taskIndicator.TestDate = DateTime.Now;
        }

        await _laboratoryTaskIndicatorRepository.UpdateAsync(taskIndicator);

        // Update task status
        var task = await _laboratoryTaskRepository.GetAsync(taskIndicator.LaboratoryTaskId);
        await UpdateTaskStatusAsync(task);

        // Đồng bộ trạng thái TestRequest
        await UpdateTestRequestStatusAsync(task.TestRequestId);

        // Notify QLKT about new result submitted (Case 1 & 6)
        if (task.TechnicalManagerId.HasValue)
        {
            var testRequest = await _testRequestRepository.GetAsync(task.TestRequestId);
            var sampleIndicator = await _sampleIndicatorRepository.FindAsync(taskIndicator.TestRequestSampleIndicatorId);
            var sample = sampleIndicator != null ? await _sampleRepository.FindAsync(sampleIndicator.TestRequestSampleId) : null;
            var indicator = sampleIndicator != null ? await _indicatorRepository.FindAsync(sampleIndicator.IndicatorId) : null;
            var method = taskIndicator.MethodId.HasValue ? await _testMethodRepository.FindAsync(taskIndicator.MethodId.Value) : null;

            var senderName = CurrentUser.Name ?? CurrentUser.UserName ?? "Kỹ thuật viên";

            await _notificationCreator.NotifyIndicatorSubmittedToTechnicalManagerAsync(
                task.TechnicalManagerId.Value,
                testRequest.ContractNumber ?? task.ContractNumber ?? "",
                task.TestRequestId,
                senderName,
                1, // Single indicator
                task.RequestCompletionDate,
                sample?.SampleName,
                indicator?.Name,
                method?.Name
            );
        }

        return await MapToIndicatorDtoAsync(taskIndicator);
    }

    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public async Task<LaboratoryTaskIndicatorDto> ApproveIndicatorAsync(Guid indicatorId, ApproveIndicatorDto input)
    {
        var taskIndicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicatorId);
        var task = await _laboratoryTaskRepository.GetAsync(taskIndicator.LaboratoryTaskId);

        // Validate: Only QLKT or supporter can approve
        if (!await CanManageTaskAsync(task))
        {
            throw new ForbiddenAccessException("Bạn không có quyền phê duyệt kết quả");
        }

        // Lưu StandardId nếu có
        taskIndicator.StandardId = input.StandardId;

        if (input.Approve)
        {
            // Approve
            taskIndicator.ApprovalStatus = IndicatorApprovalStatus.Approved;
            taskIndicator.ApprovalDate = DateTime.Now;
            taskIndicator.ApprovedById = CurrentUser.Id;
            taskIndicator.RejectionReason = null;
        }
        else
        {
            // Reject - reset to Pending for re-entry
            if (string.IsNullOrWhiteSpace(input.RejectionReason))
            {
                throw new ValidationException("reason", "Vui lòng nhập lý do từ chối");
            }

            taskIndicator.ApprovalStatus = IndicatorApprovalStatus.Rejected;
            taskIndicator.RejectionReason = input.RejectionReason;
            taskIndicator.ApprovalDate = DateTime.Now;
            taskIndicator.ApprovedById = CurrentUser.Id;
            // Giữ lại Result và Unit để user có thể sửa và gửi lại

            // Save rejection history
            var rejectionHistory = new IndicatorRejectionHistory(
                GuidGenerator.Create(),
                indicatorId,
                input.RejectionReason,
                CurrentUser.Id!.Value
            );
            await _rejectionHistoryRepository.InsertAsync(rejectionHistory);
        }

        await _laboratoryTaskIndicatorRepository.UpdateAsync(taskIndicator);

        // Update task status
        await UpdateTaskStatusAsync(task);

        // Đồng bộ trạng thái TestRequest
        await UpdateTestRequestStatusAsync(task.TestRequestId);

        // Notify technician when result is rejected (Case 5 KTV - single rejection)
        if (!input.Approve && taskIndicator.EnteredById.HasValue)
        {
            var testRequest = await _testRequestRepository.GetAsync(task.TestRequestId);
            var contractNumber = testRequest.ContractNumber ?? task.ContractNumber ?? "";
            var technicalManagerName = CurrentUser.Name ?? CurrentUser.UserName ?? "Quản lý kỹ thuật";

            var sampleIndicator = await _sampleIndicatorRepository.FindAsync(taskIndicator.TestRequestSampleIndicatorId);
            var sample = sampleIndicator != null ? await _sampleRepository.FindAsync(sampleIndicator.TestRequestSampleId) : null;
            var indicator = sampleIndicator != null ? await _indicatorRepository.FindAsync(sampleIndicator.IndicatorId) : null;
            var method = taskIndicator.MethodId.HasValue ? await _testMethodRepository.FindAsync(taskIndicator.MethodId.Value) : null;

            await _notificationCreator.NotifySingleResultRejectedByTechnicalManagerAsync(
                taskIndicator.EnteredById.Value,
                contractNumber,
                task.TestRequestId,
                technicalManagerName,
                sample?.SampleName ?? "",
                indicator?.Name ?? "",
                method?.Name ?? ""
            );
        }

        return await MapToIndicatorDtoAsync(taskIndicator);
    }

    /// <summary>
    /// Cập nhật tiêu chuẩn so sánh cho chỉ tiêu
    /// </summary>
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public async Task<LaboratoryTaskIndicatorDto> UpdateIndicatorStandardAsync(Guid indicatorId, Guid? standardId)
    {
        var taskIndicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicatorId);
        var task = await _laboratoryTaskRepository.GetAsync(taskIndicator.LaboratoryTaskId);

        // Validate: Only QLKT or supporter can update standard
        if (!await CanManageTaskAsync(task))
        {
            throw new ForbiddenAccessException("Bạn không có quyền cập nhật tiêu chuẩn");
        }

        // Validate: Only allow update when indicator status is Entered (Chờ phê duyệt)
        if (taskIndicator.ApprovalStatus != IndicatorApprovalStatus.Entered)
        {
            throw new Common.Exceptions.InvalidOperationException("Chỉ được chọn tiêu chuẩn khi chỉ tiêu đang ở trạng thái 'Chờ phê duyệt'", "indicator");
        }

        taskIndicator.StandardId = standardId;
        await _laboratoryTaskIndicatorRepository.UpdateAsync(taskIndicator);

        return await MapToIndicatorDtoAsync(taskIndicator);
    }

    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public async Task<LaboratoryTaskDto> SendResultsAsync(Guid taskId)
    {
        var task = await _laboratoryTaskRepository.GetAsync(taskId);
        var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => x.LaboratoryTaskId == taskId);

        // Filter out CustomerCancelled indicators - they should not block sending results
        var activeIndicators = taskIndicators
            .Where(ti => ti.ApprovalStatus != IndicatorApprovalStatus.CustomerCancelled)
            .ToList();

        // Validate: Must have at least one active indicator
        if (!activeIndicators.Any())
        {
            throw new ValidationException("indicator", "Không có chỉ tiêu nào để gửi kết quả");
        }

        // Validate: All active indicators must be approved
        var allApproved = activeIndicators.All(ti => ti.ApprovalStatus == IndicatorApprovalStatus.Approved);
        if (!allApproved)
        {
            throw new Common.Exceptions.InvalidOperationException("Tất cả chỉ tiêu phải được duyệt trước khi gửi kết quả", "indicator");
        }

        task.Status = LaboratoryTaskStatus.SentToQuality;
        await _laboratoryTaskRepository.UpdateAsync(task);

        // Create QualityManagementTask for QLCL to approve results
        await CreateQualityManagementTaskAsync(task, activeIndicators);

        // Update TestRequest status to PendingApproval when all tasks for this TestRequest have sent results
        // In new flow: InProgress → PendingApproval directly (no TestingCompleted status)
        await TryUpdateTestRequestToPendingApprovalAsync(task.TestRequestId);

        return await MapToDetailDtoAsync(task);
    }

    /// <summary>
    /// Tạo hoặc cập nhật QualityManagementTask khi QLKT gửi kết quả
    /// - Nếu chỉ tiêu đã có record QLCL (bị từ chối trước đó): update record hiện tại + lưu lịch sử
    /// - Nếu chưa có: thêm vào task hiện có hoặc tạo mới nếu chưa có task nào
    /// Mỗi TestRequest chỉ có DUY NHẤT 1 QualityManagementTask loại ResultApproval
    /// </summary>
    private async Task CreateQualityManagementTaskAsync(LaboratoryTask labTask, List<LaboratoryTaskIndicator> indicators, Guid? qualityManagerId = null)
    {
        var testRequest = await _testRequestRepository.GetAsync(labTask.TestRequestId);

        // Find existing ResultApproval task for this TestRequest (should be unique)
        var existingTask = (await _qualityTaskRepository.GetListAsync(
            t => t.TestRequestId == labTask.TestRequestId && t.TaskType == QualityTaskType.ResultApproval))
            .FirstOrDefault();

        // Check existing QualityManagementIndicators for these lab indicators (for resubmission case)
        var labIndicatorIds = indicators.Select(i => i.Id).ToList();
        var existingQualityIndicators = await _qualityIndicatorRepository.GetListAsync(
            qi => labIndicatorIds.Contains(qi.LaboratoryTaskIndicatorId));

        // Separate indicators: those that need resubmission vs new ones
        var indicatorsToResubmit = new List<(LaboratoryTaskIndicator LabIndicator, QualityManagementIndicator QualityIndicator)>();
        var indicatorsToCreate = new List<LaboratoryTaskIndicator>();

        foreach (var labIndicator in indicators)
        {
            var existingQI = existingQualityIndicators.FirstOrDefault(qi => qi.LaboratoryTaskIndicatorId == labIndicator.Id);
            if (existingQI != null && existingQI.ApprovalStatus == QualityIndicatorApprovalStatus.Rejected)
            {
                // This indicator was rejected before, need to resubmit (update existing record)
                indicatorsToResubmit.Add((labIndicator, existingQI));
            }
            else if (existingQI == null)
            {
                // New indicator, need to create
                indicatorsToCreate.Add(labIndicator);
            }
            // If existingQI exists with Approved/PendingApproval status, skip (already handled)
        }

        // Handle resubmission: update existing QualityManagementIndicator records
        if (indicatorsToResubmit.Any())
        {
            foreach (var (labIndicator, qualityIndicator) in indicatorsToResubmit)
            {
                var oldStatus = qualityIndicator.ApprovalStatus;

                // Update status back to PendingApproval
                qualityIndicator.ApprovalStatus = QualityIndicatorApprovalStatus.PendingApproval;
                qualityIndicator.RejectionReason = null; // Clear rejection reason
                qualityIndicator.ApprovalDate = null;
                qualityIndicator.ApprovedById = null;
                await _qualityIndicatorRepository.UpdateAsync(qualityIndicator);

                // Create history record for resubmission
                var history = new QualityIndicatorStatusHistory(
                    GuidGenerator.Create(),
                    qualityIndicator.Id,
                    oldStatus,
                    QualityIndicatorApprovalStatus.PendingApproval,
                    "Gửi lại kết quả sau khi bị từ chối"
                );
                await _qualityIndicatorHistoryRepository.InsertAsync(history);
            }

            // Update QualityManagementTask status back to PendingApproval
            if (existingTask != null && existingTask.Status == QualityManagementStatus.Rejected)
            {
                existingTask.Status = QualityManagementStatus.PendingApproval;
                await _qualityTaskRepository.UpdateAsync(existingTask);
            }
        }

        // Handle new indicators: add to existing task or create new task
        if (indicatorsToCreate.Any())
        {
            QualityManagementTask qualityTask;

            if (existingTask != null)
            {
                // Use existing task - update indicator count and status
                qualityTask = existingTask;
                qualityTask.TotalIndicatorCount += indicatorsToCreate.Count;

                // Update AssignedQualityManagerId if provided
                if (qualityManagerId.HasValue)
                {
                    qualityTask.AssignedQualityManagerId = qualityManagerId;
                }

                // If task was rejected/approved, reset to PendingApproval since new indicators added
                if (qualityTask.Status != QualityManagementStatus.PendingApproval)
                {
                    qualityTask.Status = QualityManagementStatus.PendingApproval;
                }
                await _qualityTaskRepository.UpdateAsync(qualityTask);
            }
            else
            {
                // Create new task (first time for this TestRequest)
                // Get all tasks for this TestRequest to calculate totals
                var allTasks = await _laboratoryTaskRepository.GetListAsync(t => t.TestRequestId == labTask.TestRequestId);

                // Get max RequestCompletionDate from all tasks
                DateTime? maxCompletionDate = allTasks
                    .Where(t => t.RequestCompletionDate.HasValue)
                    .Select(t => t.RequestCompletionDate!.Value)
                    .DefaultIfEmpty()
                    .Max();

                // Get samples count
                var samples = await _sampleRepository.GetListAsync(s => s.TestRequestId == labTask.TestRequestId);

                qualityTask = new QualityManagementTask(
                    GuidGenerator.Create(),
                    labTask.TestRequestId,
                    testRequest.ContractNumber ?? "",
                    QualityTaskType.ResultApproval,
                    samples.Count,
                    indicatorsToCreate.Count,
                    labTask.TransferDate,
                    maxCompletionDate
                );
                qualityTask.Status = QualityManagementStatus.PendingApproval;
                qualityTask.AssignedQualityManagerId = qualityManagerId;
                await _qualityTaskRepository.InsertAsync(qualityTask);
            }

            // Create QualityManagementIndicator for each new indicator
            foreach (var labIndicator in indicatorsToCreate)
            {
                var qualityIndicator = new QualityManagementIndicator(
                    GuidGenerator.Create(),
                    qualityTask.Id,
                    labIndicator.Id
                );
                qualityIndicator.ApprovalStatus = QualityIndicatorApprovalStatus.PendingApproval;
                await _qualityIndicatorRepository.InsertAsync(qualityIndicator);
            }
        }
    }

    public async Task<ListResultDto<MethodLookupDto>> GetMethodsByIndicatorAsync(Guid? indicatorId)
    {
        // For now, return all active methods
        // indicatorId is optional - later can add filtering logic based on indicator-method relationship
        var methods = await _testMethodRepository.GetListAsync(x => x.IsActive);
        var dtos = methods
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.Name)
            .Select(m => new MethodLookupDto
            {
                Id = m.Id,
                Code = m.Code,
                Name = m.Name,
                Description = ""  // Description removed from TestMethod entity
            })
            .ToList();

        return new ListResultDto<MethodLookupDto>(dtos);
    }

    public async Task<ListResultDto<UserLookupDto>> GetTechniciansByLaboratoryAsync(Guid? laboratoryId)
    {
        // Get all users - later can filter by role or laboratory assignment
        // If laboratoryId is null, return all technicians
        var users = await _userRepository.GetListAsync();
        var dtos = users
            .Where(u => u.IsActive)
            .Select(u => new UserLookupDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Name = u.Name,
                Surname = u.Surname,
                DisplayName = !string.IsNullOrWhiteSpace(u.Name)
                    ? $"{u.Surname} {u.Name}".Trim()
                    : u.UserName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                IsActive = u.IsActive
            })
            .OrderBy(u => u.DisplayName)
            .ToList();

        return new ListResultDto<UserLookupDto>(dtos);
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Tự động cập nhật trạng thái task dựa trên trạng thái các indicators
    /// Logic: Ưu tiên trạng thái CAO NHẤT (optimistic - hiển thị tiến độ cao nhất đạt được)
    /// Thứ tự ưu tiên từ CAO → THẤP:
    /// - Đã gửi QLCL (5): Có ≥1 chỉ tiêu đã gửi QLCL (IsSentToQuality = true)
    /// - Đã duyệt (4): Có ≥1 chỉ tiêu đã được Approved
    /// - Chờ duyệt kết quả (3): Có ≥1 chỉ tiêu đã nhập KQ (Entered)
    /// - Đang thử nghiệm (2): Có ≥1 chỉ tiêu có testDate (KTV đã check-in)
    /// - Đã giao việc (1): Có ≥1 chỉ tiêu đã có method + KTV
    /// - Chưa giao việc (0): Tất cả chỉ tiêu chưa được giao việc
    /// </summary>
    private async Task UpdateTaskStatusAsync(LaboratoryTask task)
    {
        var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => x.LaboratoryTaskId == task.Id);

        if (!taskIndicators.Any())
        {
            return;
        }

        // Filter out CustomerCancelled indicators - they should not affect task status calculation
        var activeIndicators = taskIndicators
            .Where(ti => ti.ApprovalStatus != IndicatorApprovalStatus.CustomerCancelled)
            .ToList();

        // If all indicators are cancelled, keep current status or set to a default
        if (!activeIndicators.Any())
        {
            return;
        }

        // Check conditions for status calculation
        // Có ≥1 chỉ tiêu đã gửi QLCL (Approved + IsSentToQuality)
        var anySentToQuality = activeIndicators.Any(ti =>
            ti.ApprovalStatus == IndicatorApprovalStatus.Approved && ti.IsSentToQuality);

        // Có ≥1 chỉ tiêu đã được duyệt (Approved)
        var anyApproved = activeIndicators.Any(ti => ti.ApprovalStatus == IndicatorApprovalStatus.Approved);

        // Có ≥1 chỉ tiêu đã nhập KQ (Entered - chờ duyệt)
        var anyEntered = activeIndicators.Any(ti => ti.ApprovalStatus == IndicatorApprovalStatus.Entered);

        // Có ≥1 chỉ tiêu đang thử nghiệm (có testDate, KTV đã check-in)
        var anyTesting = activeIndicators.Any(ti => ti.TestDate.HasValue);

        // Có ≥1 chỉ tiêu đã được giao việc (có method + KTV)
        var anyAssigned = activeIndicators.Any(ti => ti.MethodId.HasValue && ti.TechnicianId.HasValue);

        LaboratoryTaskStatus newStatus;

        // Logic ưu tiên theo tiến độ CAO NHẤT (từ cao xuống thấp)
        // Hiển thị trạng thái cao nhất mà task đã đạt được

        if (anySentToQuality)
        {
            // Có ≥1 chỉ tiêu đã gửi QLCL
            newStatus = LaboratoryTaskStatus.SentToQuality;
        }
        else if (anyApproved)
        {
            // Có ≥1 chỉ tiêu đã được duyệt
            newStatus = LaboratoryTaskStatus.Approved;
        }
        else if (anyEntered)
        {
            // Có ≥1 chỉ tiêu đã nhập KQ (chờ duyệt)
            newStatus = LaboratoryTaskStatus.PendingApproval;
        }
        else if (anyTesting)
        {
            // Có ≥1 chỉ tiêu đang thử nghiệm (KTV đã check-in)
            newStatus = LaboratoryTaskStatus.Testing;
        }
        else if (anyAssigned)
        {
            // Có ≥1 chỉ tiêu đã được giao việc
            newStatus = LaboratoryTaskStatus.Assigned;
        }
        else
        {
            // Tất cả chưa được giao việc
            newStatus = LaboratoryTaskStatus.NotAssigned;
        }

        if (task.Status != newStatus)
        {
            task.Status = newStatus;
            await _laboratoryTaskRepository.UpdateAsync(task);
        }
    }

    /// <summary>
    /// Đồng bộ trạng thái TestRequest dựa trên trạng thái các indicators của tất cả tasks
    /// Logic: Ưu tiên trạng thái CAO NHẤT (optimistic)
    /// - Đã phê duyệt: Có ≥1 chỉ tiêu đã được QLCL duyệt (IsSentToQuality + Approved)
    /// - Chờ phê duyệt KQ: Có ≥1 chỉ tiêu đã gửi QLKT (Entered) hoặc đã được QLKT duyệt (Approved nhưng chưa gửi QLCL)
    /// - Đang thử nghiệm: Có ≥1 chỉ tiêu có testDate (KTV đã check-in)
    /// - Đã gửi mẫu: Tất cả chỉ tiêu chưa có testDate
    /// </summary>
    private async Task UpdateTestRequestStatusAsync(Guid testRequestId)
    {
        var testRequest = await _testRequestRepository.FindAsync(testRequestId);
        if (testRequest == null) return;

        // Chỉ cập nhật khi status đang ở các trạng thái cho phép thay đổi tự động
        // Không thay đổi nếu đã ở: PendingChangeApproval, Approved, ResultSent, Completed
        if (testRequest.Status == TestRequestStatus.PendingChangeApproval ||
            testRequest.Status == TestRequestStatus.Approved ||
            testRequest.Status == TestRequestStatus.ResultSent ||
            testRequest.Status == TestRequestStatus.Completed)
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
            .Where(ti => ti.ApprovalStatus != IndicatorApprovalStatus.CustomerCancelled)
            .ToList();

        if (!activeIndicators.Any()) return;

        // Check conditions - logic ưu tiên CAO NHẤT
        // Có ≥1 chỉ tiêu đã được QLCL duyệt (đã gửi QLCL và Approved)
        var anySentToQuality = activeIndicators.Any(ti =>
            ti.ApprovalStatus == IndicatorApprovalStatus.Approved && ti.IsSentToQuality);

        // Có ≥1 chỉ tiêu đã được QLKT duyệt (Approved) hoặc đã nhập KQ (Entered)
        var anyEnteredOrApproved = activeIndicators.Any(ti =>
            ti.ApprovalStatus == IndicatorApprovalStatus.Entered ||
            ti.ApprovalStatus == IndicatorApprovalStatus.Approved);

        // Có ≥1 chỉ tiêu đang thử nghiệm (có testDate)
        var anyTesting = activeIndicators.Any(ti => ti.TestDate.HasValue);

        TestRequestStatus newStatus;

        if (anySentToQuality)
        {
            // Đã gửi QLCL → TestRequest chờ phê duyệt (QLCL duyệt)
            newStatus = TestRequestStatus.PendingApproval;
        }
        else if (anyEnteredOrApproved)
        {
            // Có indicator đã nhập KQ hoặc QLKT đã duyệt → Chờ phê duyệt
            newStatus = TestRequestStatus.PendingApproval;
        }
        else if (anyTesting)
        {
            // Có indicator đang thử nghiệm → Đang thử nghiệm
            newStatus = TestRequestStatus.InProgress;
        }
        else
        {
            // Tất cả chưa có testDate → giữ nguyên SampleSent (nếu đã gửi mẫu)
            if (testRequest.Status == TestRequestStatus.SampleSent)
            {
                return; // Giữ nguyên SampleSent
            }
            newStatus = testRequest.Status; // Giữ nguyên status hiện tại
        }

        if (testRequest.Status != newStatus)
        {
            testRequest.Status = newStatus;
            await _testRequestRepository.UpdateAsync(testRequest);
        }
    }

    /// <summary>
    /// Kiểm tra xem task có thể chỉnh sửa không dựa trên trạng thái
    /// Cho phép edit khi: Chưa giao việc, Đã giao việc
    /// Không cho edit khi đang Testing trở đi
    /// </summary>
    private static bool CanEditTask(LaboratoryTaskStatus status)
    {
        return status == LaboratoryTaskStatus.NotAssigned ||
               status == LaboratoryTaskStatus.Assigned;
    }

    /// <summary>
    /// Lấy nhãn tiếng Việt cho trạng thái
    /// </summary>
    private static string GetStatusLabel(LaboratoryTaskStatus status)
    {
        return status switch
        {
            LaboratoryTaskStatus.NotAssigned => "Chưa giao việc",
            LaboratoryTaskStatus.Assigned => "Đã giao việc",
            LaboratoryTaskStatus.Testing => "Đang thử nghiệm",
            LaboratoryTaskStatus.PendingApproval => "Chờ duyệt kết quả",
            LaboratoryTaskStatus.Approved => "Đã duyệt",
            LaboratoryTaskStatus.SentToQuality => "Đã gửi QL Chất lượng",
            _ => status.ToString()
        };
    }

    /// <summary>
    /// Kiểm tra và cập nhật TestRequest sang InProgress khi QLKT gán PP + KTV cho ≥1 chỉ tiêu
    /// </summary>
    private async Task TryUpdateTestRequestToInProgressAsync(Guid testRequestId)
    {
        // Get all tasks for this TestRequest
        var allTasks = await _laboratoryTaskRepository.GetListAsync(t => t.TestRequestId == testRequestId);

        // Check if at least one task has all indicators assigned (method + technician)
        foreach (var task in allTasks)
        {
            var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(ti => ti.LaboratoryTaskId == task.Id);
            if (taskIndicators.Any())
            {
                var allAssigned = taskIndicators.All(ti => ti.MethodId.HasValue && ti.TechnicianId.HasValue);
                if (allAssigned)
                {
                    // At least one task has all indicators assigned
                    await _testRequestAppService.UpdateToInProgressAsync(testRequestId);
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Kiểm tra và cập nhật TestRequest sang PendingApproval khi tất cả tasks đã gửi kết quả
    /// In new flow: InProgress → PendingApproval directly (no TestingCompleted status)
    /// </summary>
    private async Task TryUpdateTestRequestToPendingApprovalAsync(Guid testRequestId)
    {
        // Get all tasks for this TestRequest
        var allTasks = await _laboratoryTaskRepository.GetListAsync(t => t.TestRequestId == testRequestId);

        if (!allTasks.Any())
        {
            return;
        }

        // Check if all tasks have sent results
        var allResultsSent = allTasks.All(t => t.Status == LaboratoryTaskStatus.SentToQuality);
        if (allResultsSent)
        {
            // In new flow, go directly to PendingApproval
            await _testRequestAppService.UpdateToPendingApprovalAsync(testRequestId);
        }
    }

    private async Task<LaboratoryTaskIndicatorDto> MapToIndicatorDtoAsync(LaboratoryTaskIndicator taskIndicator)
    {
        var sampleIndicator = await _sampleIndicatorRepository.FindAsync(taskIndicator.TestRequestSampleIndicatorId);
        if (sampleIndicator == null)
        {
            throw new EntityNotFoundException("SampleIndicator", taskIndicator.TestRequestSampleIndicatorId);
        }

        var sample = await _sampleRepository.FindAsync(sampleIndicator.TestRequestSampleId);
        var indicator = await _indicatorRepository.FindAsync(sampleIndicator.IndicatorId);

        string? indicatorGroupName = null;
        if (indicator != null)
        {
            var indicatorGroup = await _indicatorGroupRepository.FindAsync(indicator.IndicatorGroupId);
            indicatorGroupName = indicatorGroup?.Name;
        }

        // Get user names for workflow fields (batch fetch)
        var userIds = new[] { taskIndicator.TechnicianId, taskIndicator.EnteredById, taskIndicator.ApprovedById };
        var userNames = await _userNameProvider.GetUserNamesAsync(userIds);

        string? technicianName = taskIndicator.TechnicianId.HasValue && userNames.TryGetValue(taskIndicator.TechnicianId.Value, out var tn) ? tn : null;
        string? enteredByName = taskIndicator.EnteredById.HasValue && userNames.TryGetValue(taskIndicator.EnteredById.Value, out var en) ? en : null;
        string? approvedByName = taskIndicator.ApprovedById.HasValue && userNames.TryGetValue(taskIndicator.ApprovedById.Value, out var an) ? an : null;
        string? methodName = null;
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

        return new LaboratoryTaskIndicatorDto
        {
            Id = taskIndicator.Id,
            TestRequestSampleIndicatorId = taskIndicator.TestRequestSampleIndicatorId,
            SampleId = sampleIndicator.TestRequestSampleId,
            SampleCode = sample?.SampleCode,
            SampleName = sample?.SampleName,
            IndicatorId = sampleIndicator.IndicatorId,
            IndicatorCode = indicator?.Code,
            IndicatorName = indicator?.Name,
            IndicatorGroupName = indicatorGroupName,
            UnitPrice = sampleIndicator.UnitPrice,
            // Phase 6 workflow fields
            MethodId = taskIndicator.MethodId,
            MethodName = methodName,
            TechnicianId = taskIndicator.TechnicianId,
            TechnicianName = technicianName,
            AssignedDate = taskIndicator.AssignedDate,
            TestDate = taskIndicator.TestDate,
            Result = taskIndicator.Result,
            Unit = unit,
            EntryDate = taskIndicator.EntryDate,
            EnteredById = taskIndicator.EnteredById,
            EnteredByName = enteredByName,
            ApprovalStatus = taskIndicator.ApprovalStatus,
            ApprovalDate = taskIndicator.ApprovalDate,
            ApprovedById = taskIndicator.ApprovedById,
            ApprovedByName = approvedByName,
            RejectionReason = taskIndicator.RejectionReason,
            IsSentToQuality = taskIndicator.IsSentToQuality
        };
    }

    #endregion

    // Supporter Management methods moved to LaboratoryTaskAppService.Supporters.cs

    #region Bulk Approval

    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public async Task<List<LaboratoryTaskIndicatorDto>> BulkApproveIndicatorsAsync(Guid taskId, BulkApproveIndicatorsDto input)
    {
        var task = await _laboratoryTaskRepository.GetAsync(taskId);

        // Validate: Only QLKT or supporter can approve
        if (!await CanManageTaskAsync(task))
        {
            throw new ForbiddenAccessException("Bạn không có quyền phê duyệt kết quả");
        }

        if (!input.Approve && string.IsNullOrWhiteSpace(input.RejectionReason))
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

            // Lưu StandardId nếu có trong mapping
            if (input.StandardIds != null && input.StandardIds.TryGetValue(indicatorId, out var standardId))
            {
                taskIndicator.StandardId = standardId;
            }

            if (input.Approve)
            {
                taskIndicator.ApprovalStatus = IndicatorApprovalStatus.Approved;
                taskIndicator.ApprovalDate = DateTime.Now;
                taskIndicator.ApprovedById = CurrentUser.Id;
                taskIndicator.RejectionReason = null;
            }
            else
            {
                taskIndicator.ApprovalStatus = IndicatorApprovalStatus.Rejected;
                taskIndicator.RejectionReason = input.RejectionReason;
                taskIndicator.ApprovalDate = DateTime.Now;
                taskIndicator.ApprovedById = CurrentUser.Id;

                // Save rejection history
                var rejectionHistory = new IndicatorRejectionHistory(
                    GuidGenerator.Create(),
                    indicatorId,
                    input.RejectionReason!,
                    CurrentUser.Id!.Value
                );
                await _rejectionHistoryRepository.InsertAsync(rejectionHistory);
            }

            await _laboratoryTaskIndicatorRepository.UpdateAsync(taskIndicator);
            results.Add(await MapToIndicatorDtoAsync(taskIndicator));
        }

        // Update task status
        await UpdateTaskStatusAsync(task);

        // Đồng bộ trạng thái TestRequest
        await UpdateTestRequestStatusAsync(task.TestRequestId);

        // Notify technicians when results are rejected (Case 4 & 5 KTV)
        if (!input.Approve)
        {
            await NotifyTechniciansOnRejectAsync(task, input.IndicatorIds);
        }

        return results;
    }

    /// <summary>
    /// Notify technicians when their results are rejected by QLKT
    /// Groups notifications by technician
    /// </summary>
    private async Task NotifyTechniciansOnRejectAsync(LaboratoryTask task, List<Guid> rejectedIndicatorIds)
    {
        var testRequest = await _testRequestRepository.GetAsync(task.TestRequestId);
        var contractNumber = testRequest.ContractNumber ?? task.ContractNumber ?? "";
        var technicalManagerName = CurrentUser.Name ?? CurrentUser.UserName ?? "Quản lý kỹ thuật";

        // Get rejected indicators with their EnteredById (the technician who entered the result)
        var rejectedIndicators = new List<(LaboratoryTaskIndicator Indicator, Guid TechnicianId)>();
        foreach (var indicatorId in rejectedIndicatorIds)
        {
            var taskIndicator = await _laboratoryTaskIndicatorRepository.FindAsync(indicatorId);
            if (taskIndicator?.EnteredById != null)
            {
                rejectedIndicators.Add((taskIndicator, taskIndicator.EnteredById.Value));
            }
        }

        // Group by technician
        var indicatorsByTechnician = rejectedIndicators
            .GroupBy(x => x.TechnicianId)
            .ToList();

        foreach (var group in indicatorsByTechnician)
        {
            var technicianId = group.Key;
            var technicianIndicators = group.ToList();
            var rejectedCount = technicianIndicators.Count;

            if (rejectedCount == 1)
            {
                // Case 5: Single rejection - include details
                var taskIndicator = technicianIndicators.First().Indicator;
                var sampleIndicator = await _sampleIndicatorRepository.FindAsync(taskIndicator.TestRequestSampleIndicatorId);
                var sample = sampleIndicator != null ? await _sampleRepository.FindAsync(sampleIndicator.TestRequestSampleId) : null;
                var indicator = sampleIndicator != null ? await _indicatorRepository.FindAsync(sampleIndicator.IndicatorId) : null;
                var method = taskIndicator.MethodId.HasValue ? await _testMethodRepository.FindAsync(taskIndicator.MethodId.Value) : null;

                await _notificationCreator.NotifySingleResultRejectedByTechnicalManagerAsync(
                    technicianId,
                    contractNumber,
                    task.TestRequestId,
                    technicalManagerName,
                    sample?.SampleName ?? "",
                    indicator?.Name ?? "",
                    method?.Name ?? ""
                );
            }
            else
            {
                // Case 4: Multiple rejections
                await _notificationCreator.NotifyMultipleResultsRejectedByTechnicalManagerAsync(
                    technicianId,
                    contractNumber,
                    task.TestRequestId,
                    technicalManagerName
                );
            }
        }
    }

    #endregion

    #region Send to Quality Management

    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public async Task<LaboratoryTaskDto> SendToQualityAsync(Guid taskId, SendToQualityDto input)
    {
        var task = await _laboratoryTaskRepository.GetAsync(taskId);

        // Validate: Only QLKT or supporter can send
        if (!await CanManageTaskAsync(task))
        {
            throw new ForbiddenAccessException("Bạn không có quyền gửi kết quả");
        }

        // Get selected indicators
        var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => x.LaboratoryTaskId == taskId);
        var selectedIndicators = taskIndicators
            .Where(ti => input.IndicatorIds.Contains(ti.Id))
            .ToList();

        if (!selectedIndicators.Any())
        {
            throw new ValidationException("indicatorIds", "Vui lòng chọn ít nhất một chỉ tiêu để gửi");
        }

        // Validate: All selected indicators must be approved
        var notApproved = selectedIndicators.Where(ti => ti.ApprovalStatus != IndicatorApprovalStatus.Approved).ToList();
        if (notApproved.Any())
        {
            throw new Common.Exceptions.InvalidOperationException("Tất cả chỉ tiêu được chọn phải được duyệt trước khi gửi", "indicator");
        }

        // Mark selected indicators as sent to quality
        foreach (var indicator in selectedIndicators)
        {
            indicator.IsSentToQuality = true;
        }
        await _laboratoryTaskIndicatorRepository.UpdateManyAsync(selectedIndicators);

        // Create or update QualityManagementTask with assigned quality manager
        await CreateQualityManagementTaskAsync(task, selectedIndicators, input.QualityManagerId);

        // Update task status to SentToQuality only when ALL approved indicators have been sent
        // Re-fetch all indicators to check if there are any remaining approved but not sent
        var allTaskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => x.LaboratoryTaskId == taskId);
        var hasUnsentApproved = allTaskIndicators.Any(ti =>
            ti.ApprovalStatus == IndicatorApprovalStatus.Approved && !ti.IsSentToQuality);

        if (!hasUnsentApproved && task.Status != LaboratoryTaskStatus.SentToQuality)
        {
            task.Status = LaboratoryTaskStatus.SentToQuality;
            await _laboratoryTaskRepository.UpdateAsync(task);
        }

        // Update TestRequest status if all tasks sent
        await TryUpdateTestRequestToPendingApprovalAsync(task.TestRequestId);

        // Notify QLCL about submitted results (Case 1 & 2 QLCL)
        await NotifyQualityManagerOnSendResultsAsync(task, selectedIndicators, input.QualityManagerId);

        // Reload task to ensure we return the latest data
        task = await _laboratoryTaskRepository.GetAsync(taskId);
        return await MapToDetailDtoAsync(task);
    }

    /// <summary>
    /// Gửi thông báo cho QLCL khi QLKT gửi kết quả
    /// </summary>
    private async Task NotifyQualityManagerOnSendResultsAsync(
        LaboratoryTask task,
        List<LaboratoryTaskIndicator> sentIndicators,
        Guid qualityManagerId)
    {
        var testRequest = await _testRequestRepository.GetAsync(task.TestRequestId);
        var senderName = CurrentUser.Name ?? CurrentUser.UserName ?? "Quản lý kỹ thuật";
        var indicatorCount = sentIndicators.Count;

        string? sampleName = null;
        string? indicatorName = null;
        string? methodName = null;

        // For single indicator, get details
        if (indicatorCount == 1)
        {
            var indicator = sentIndicators.First();
            var sampleIndicator = await _sampleIndicatorRepository.FindAsync(indicator.TestRequestSampleIndicatorId);
            if (sampleIndicator != null)
            {
                var sample = await _sampleRepository.FindAsync(sampleIndicator.TestRequestSampleId);
                var masterIndicator = await _indicatorRepository.FindAsync(sampleIndicator.IndicatorId);
                sampleName = sample?.SampleName;
                indicatorName = masterIndicator?.Name;
            }
            if (indicator.MethodId.HasValue)
            {
                var method = await _testMethodRepository.FindAsync(indicator.MethodId.Value);
                methodName = method?.Name;
            }
        }

        await _notificationCreator.NotifyResultSubmittedToQualityManagerAsync(
            qualityManagerId,
            testRequest.ContractNumber ?? task.ContractNumber ?? "",
            task.TestRequestId,
            senderName,
            indicatorCount,
            sampleName,
            indicatorName,
            methodName
        );
    }

    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Default)]
    public async Task<ListResultDto<UserLookupDto>> GetQualityManagersAsync(Guid taskId)
    {
        // Get task to find QLKT's department
        var task = await _laboratoryTaskRepository.GetAsync(taskId);
        Guid? qlktDepartmentId = null;

        if (task.TechnicalManagerId.HasValue)
        {
            var qlktUser = await _userRepository.GetAsync(task.TechnicalManagerId.Value);
            qlktDepartmentId = qlktUser.GetProperty<Guid?>("DepartmentId");
        }

        var users = await _userRepository.GetListAsync();
        var qualityManagers = new List<UserLookupDto>();

        foreach (var user in users.Where(u => u.IsActive))
        {
            // Check if user has QualityManager role
            var userRoles = await _userRepository.GetRolesAsync(user.Id);
            if (!userRoles.Any(r => r.Name == "QualityManager"))
            {
                continue;
            }

            // Filter by same department as QLKT (if QLKT has department)
            if (qlktDepartmentId.HasValue)
            {
                var userDepartmentId = user.GetProperty<Guid?>("DepartmentId");
                if (userDepartmentId != qlktDepartmentId)
                {
                    continue;
                }
            }

            qualityManagers.Add(new UserLookupDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Name = user.Name,
                Surname = user.Surname,
                DisplayName = !string.IsNullOrWhiteSpace(user.Name)
                    ? $"{user.Surname} {user.Name}".Trim()
                    : user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsActive = user.IsActive
            });
        }

        return new ListResultDto<UserLookupDto>(qualityManagers.OrderBy(u => u.DisplayName).ToList());
    }

    #endregion

    #region Work Reminder

    /// <summary>
    /// Lấy danh sách công việc cần làm cho Lab Technical Manager (popup reminder)
    /// Task name priority: "Cần duyệt kết quả" > "Cần phân công công việc" > "Sắp tới ngày trả kết quả"
    /// </summary>
    public async Task<List<LabTechManagerPendingTaskDto>> GetLabTechManagerPendingTasksAsync()
    {
        var currentUserId = CurrentUser.Id;
        if (!currentUserId.HasValue)
        {
            return new List<LabTechManagerPendingTaskDto>();
        }

        var today = DateTime.Today;
        var queryable = await _laboratoryTaskRepository.GetQueryableAsync();

        // Get tasks where current user is TechnicalManager
        var tasks = queryable
            .Where(x => x.TechnicalManagerId == currentUserId.Value)
            .Where(x =>
                // Condition 1: Cần phân công (NotAssigned)
                x.Status == LaboratoryTaskStatus.NotAssigned ||
                // Condition 2: Cần duyệt (PendingApproval)
                x.Status == LaboratoryTaskStatus.PendingApproval ||
                // Condition 3: Sắp tới ngày trả kết quả (trong 5 ngày) và chưa hoàn thành
                (x.RequestCompletionDate != null &&
                 x.Status != LaboratoryTaskStatus.Approved &&
                 x.Status != LaboratoryTaskStatus.SentToQuality))
            .ToList();

        var result = new List<LabTechManagerPendingTaskDto>();

        foreach (var task in tasks)
        {
            // Calculate days until deadline
            int? daysUntilDeadline = null;
            if (task.RequestCompletionDate.HasValue)
            {
                daysUntilDeadline = (int)(task.RequestCompletionDate.Value.Date - today).TotalDays;
            }

            // Determine task name based on priority
            // Priority: "Cần duyệt kết quả" > "Cần phân công công việc" > "Sắp tới ngày trả kết quả"
            string taskName;
            bool shouldInclude = false;

            if (task.Status == LaboratoryTaskStatus.PendingApproval)
            {
                taskName = "Cần duyệt kết quả";
                shouldInclude = true;
            }
            else if (task.Status == LaboratoryTaskStatus.NotAssigned)
            {
                taskName = "Cần phân công công việc";
                shouldInclude = true;
            }
            else if (daysUntilDeadline.HasValue && daysUntilDeadline.Value <= 5 &&
                     task.Status != LaboratoryTaskStatus.Approved &&
                     task.Status != LaboratoryTaskStatus.SentToQuality)
            {
                taskName = "Sắp tới ngày trả kết quả thử nghiệm";
                shouldInclude = true;
            }
            else
            {
                continue; // Skip tasks that don't match any condition
            }

            if (shouldInclude)
            {
                result.Add(new LabTechManagerPendingTaskDto
                {
                    Id = task.Id,
                    ContractNumber = task.ContractNumber ?? "",
                    TaskName = taskName,
                    TransferDate = task.TransferDate,
                    RequestCompletionDate = task.RequestCompletionDate,
                    Status = task.Status,
                    DaysUntilDeadline = daysUntilDeadline
                });
            }
        }

        // Sort: PendingApproval first, then NotAssigned, then by deadline
        return result
            .OrderBy(x => x.Status == LaboratoryTaskStatus.PendingApproval ? 0 :
                          x.Status == LaboratoryTaskStatus.NotAssigned ? 1 : 2)
            .ThenBy(x => x.RequestCompletionDate ?? DateTime.MaxValue)
            .ToList();
    }

    /// <summary>
    /// Lấy danh sách công việc cần làm cho Lab Technician (KTV) (popup reminder)
    /// Task name priority: "Có kết quả cần thực hiện lại" > "Có kết quả chỉ tiêu cần gửi" > "Có chỉ tiêu cần thực hiện"
    /// </summary>
    public async Task<List<LabTechnicianPendingTaskDto>> GetLabTechnicianPendingTasksAsync()
    {
        var currentUserId = CurrentUser.Id;
        if (!currentUserId.HasValue)
        {
            return new List<LabTechnicianPendingTaskDto>();
        }

        // Get all indicators assigned to current user with status Pending, Entered, or Rejected
        var indicatorQueryable = await _laboratoryTaskIndicatorRepository.GetQueryableAsync();
        var taskQueryable = await _laboratoryTaskRepository.GetQueryableAsync();

        var indicators = indicatorQueryable
            .Where(x => x.TechnicianId == currentUserId.Value &&
                        (x.ApprovalStatus == IndicatorApprovalStatus.Pending ||
                         x.ApprovalStatus == IndicatorApprovalStatus.Entered ||
                         x.ApprovalStatus == IndicatorApprovalStatus.Rejected))
            .ToList();

        if (!indicators.Any())
        {
            return new List<LabTechnicianPendingTaskDto>();
        }

        // Group by LaboratoryTaskId
        var groupedByTask = indicators
            .GroupBy(x => x.LaboratoryTaskId)
            .ToList();

        // Get task details
        var taskIds = groupedByTask.Select(g => g.Key).ToList();
        var tasks = taskQueryable
            .Where(x => taskIds.Contains(x.Id))
            .ToList()
            .ToDictionary(x => x.Id);

        var result = new List<LabTechnicianPendingTaskDto>();

        foreach (var group in groupedByTask)
        {
            if (!tasks.TryGetValue(group.Key, out var task))
            {
                continue;
            }

            var pendingCount = group.Count(x => x.ApprovalStatus == IndicatorApprovalStatus.Pending);
            var enteredCount = group.Count(x => x.ApprovalStatus == IndicatorApprovalStatus.Entered);
            var rejectedCount = group.Count(x => x.ApprovalStatus == IndicatorApprovalStatus.Rejected);

            // Determine task name based on priority
            // Priority: "Có kết quả cần thực hiện lại" > "Có kết quả chỉ tiêu cần gửi" > "Có chỉ tiêu cần thực hiện"
            string taskName;

            if (rejectedCount > 0)
            {
                taskName = "Có kết quả cần thực hiện lại";
            }
            else if (enteredCount > 0)
            {
                taskName = "Có kết quả chỉ tiêu cần gửi";
            }
            else
            {
                taskName = "Có chỉ tiêu cần thực hiện";
            }

            result.Add(new LabTechnicianPendingTaskDto
            {
                Id = task.Id,
                ContractNumber = task.ContractNumber ?? "",
                TaskName = taskName,
                TransferDate = task.TransferDate,
                RequestCompletionDate = task.RequestCompletionDate,
                PendingCount = pendingCount,
                EnteredCount = enteredCount,
                RejectedCount = rejectedCount
            });
        }

        // Sort: Rejected first, then Entered, then Pending, then by RequestCompletionDate
        return result
            .OrderBy(x => x.RejectedCount > 0 ? 0 : (x.EnteredCount > 0 ? 1 : 2))
            .ThenBy(x => x.RequestCompletionDate ?? DateTime.MaxValue)
            .ToList();
    }

    #endregion
}
