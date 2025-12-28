using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.MasterData.Indicators;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.QualityManagement;
using LabSpecimenManagement.TestRequests;
using LabSpecimenManagement.Users;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace LabSpecimenManagement.LaboratoryTasks;

[Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Default)]
public class LaboratoryTaskAppService : ApplicationService, ILaboratoryTaskAppService
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
    private readonly IIdentityRoleRepository _roleRepository;
    private readonly ITestRequestAppService _testRequestAppService;
    private readonly IRepository<QualityManagementTask, Guid> _qualityTaskRepository;
    private readonly IRepository<QualityManagementIndicator, Guid> _qualityIndicatorRepository;
    private readonly IRepository<QualityIndicatorStatusHistory, Guid> _qualityIndicatorHistoryRepository;

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
        IIdentityRoleRepository roleRepository,
        ITestRequestAppService testRequestAppService,
        IRepository<QualityManagementTask, Guid> qualityTaskRepository,
        IRepository<QualityManagementIndicator, Guid> qualityIndicatorRepository,
        IRepository<QualityIndicatorStatusHistory, Guid> qualityIndicatorHistoryRepository)
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
        _roleRepository = roleRepository;
        _testRequestAppService = testRequestAppService;
        _qualityTaskRepository = qualityTaskRepository;
        _qualityIndicatorRepository = qualityIndicatorRepository;
        _qualityIndicatorHistoryRepository = qualityIndicatorHistoryRepository;
    }

    public async Task<LaboratoryTaskDto> GetAsync(Guid id)
    {
        var task = await _laboratoryTaskRepository.GetAsync(id);
        return await MapToDetailDtoAsync(task);
    }

    public async Task<PagedResultDto<LaboratoryTaskDto>> GetFilteredListAsync(GetLaboratoryTaskListDto input)
    {
        var queryable = await _laboratoryTaskRepository.GetQueryableAsync();

        // Apply search filter - search by ContractNumber and TechnicalManager name
        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var searchLower = input.Search.ToLower();

            // Get list of TechnicalManagerIds that match the search term
            var matchingUserIds = (await _userRepository.GetListAsync())
                .Where(u => (u.Name != null && u.Name.ToLower().Contains(searchLower)) ||
                           (u.Surname != null && u.Surname.ToLower().Contains(searchLower)) ||
                           (u.UserName != null && u.UserName.ToLower().Contains(searchLower)))
                .Select(u => u.Id)
                .ToList();

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
            // Simple sorting implementation
            queryable = input.Sorting.ToLower() switch
            {
                "transferdate" => queryable.OrderBy(x => x.TransferDate),
                "transferdate desc" => queryable.OrderByDescending(x => x.TransferDate),
                "requestcompletiondate" => queryable.OrderBy(x => x.RequestCompletionDate),
                "requestcompletiondate desc" => queryable.OrderByDescending(x => x.RequestCompletionDate),
                "status" => queryable.OrderBy(x => x.Status),
                "status desc" => queryable.OrderByDescending(x => x.Status),
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
            TestDate = task.TestDate,
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
        if (task.TechnicalManagerId.HasValue)
        {
            var user = await _userRepository.FindAsync(task.TechnicalManagerId.Value);
            if (user != null)
            {
                dto.TechnicalManagerName = user.Name ?? user.UserName;
            }
        }

        return dto;
    }

    private async Task<LaboratoryTaskDto> MapToDetailDtoAsync(LaboratoryTask task)
    {
        var dto = await MapToListDtoAsync(task);

        // Get indicators
        var taskIndicators = await _laboratoryTaskIndicatorRepository
            .GetListAsync(x => x.LaboratoryTaskId == task.Id);

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

            // Get user names for workflow fields
            string? technicianName = null;
            string? enteredByName = null;
            string? approvedByName = null;
            string? methodName = null;

            if (taskIndicator.TechnicianId.HasValue)
            {
                var technician = await _userRepository.FindAsync(taskIndicator.TechnicianId.Value);
                technicianName = technician?.Name ?? technician?.UserName;
            }
            if (taskIndicator.EnteredById.HasValue)
            {
                var enteredBy = await _userRepository.FindAsync(taskIndicator.EnteredById.Value);
                enteredByName = enteredBy?.Name ?? enteredBy?.UserName;
            }
            if (taskIndicator.ApprovedById.HasValue)
            {
                var approvedBy = await _userRepository.FindAsync(taskIndicator.ApprovedById.Value);
                approvedByName = approvedBy?.Name ?? approvedBy?.UserName;
            }
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
                RejectionReason = taskIndicator.RejectionReason
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
            throw new UserFriendlyException($"Không thể xóa công việc khi trạng thái là '{GetStatusLabel(task.Status)}'");
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

        // Validation: Không cho assign lại khi đang thí nghiệm trở đi
        if (!CanEditTask(task.Status))
        {
            throw new UserFriendlyException($"Không thể giao việc khi trạng thái là '{GetStatusLabel(task.Status)}'");
        }

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

        // Validate all indicators have assignments
        foreach (var ti in indicatorsForSample)
        {
            var assignment = assignments.FirstOrDefault(a => a.IndicatorId == ti.Id);
            if (assignment == null || !assignment.MethodId.HasValue || !assignment.TechnicianId.HasValue)
            {
                throw new UserFriendlyException("Vui lòng chọn phương pháp và kỹ thuật viên cho tất cả chỉ tiêu");
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

        return await MapToDetailDtoAsync(task);
    }

    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public async Task<LaboratoryTaskIndicatorDto> UpdateIndicatorResultAsync(Guid indicatorId, UpdateIndicatorResultDto input)
    {
        var taskIndicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicatorId);

        // Validation: Must have method assigned
        if (!taskIndicator.MethodId.HasValue)
        {
            throw new UserFriendlyException("Chỉ tiêu chưa được gán phương pháp");
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
            throw new UserFriendlyException("Bạn không có quyền phê duyệt kết quả");
        }

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
                throw new UserFriendlyException("Vui lòng nhập lý do từ chối");
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
            throw new UserFriendlyException("Không có chỉ tiêu nào để gửi kết quả");
        }

        // Validate: All active indicators must be approved
        var allApproved = activeIndicators.All(ti => ti.ApprovalStatus == IndicatorApprovalStatus.Approved);
        if (!allApproved)
        {
            throw new UserFriendlyException("Tất cả chỉ tiêu phải được duyệt trước khi gửi kết quả");
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
    private async Task CreateQualityManagementTaskAsync(LaboratoryTask labTask, List<LaboratoryTaskIndicator> indicators)
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
    /// Logic MỚI:
    /// - Chưa gắn phương pháp (0): Có chỉ tiêu chưa gắn method HOẶC chưa assign KTV
    /// - Đã gán phương pháp (1): Tất cả chỉ tiêu đã gắn method VÀ KTV
    /// - Đang thí nghiệm (2): Có ≥1 chỉ tiêu đã gửi duyệt (Entered)
    /// - Chờ duyệt kết quả (3): TẤT CẢ chỉ tiêu đã gửi duyệt (Entered hoặc Approved)
    /// - Đã duyệt (4): Tất cả chỉ tiêu đã được Approved
    /// - Đã gửi kết quả (5): QL đã gửi kết quả (trạng thái cuối cùng)
    /// </summary>
    private async Task UpdateTaskStatusAsync(LaboratoryTask task)
    {
        var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => x.LaboratoryTaskId == task.Id);

        if (!taskIndicators.Any())
        {
            return;
        }

        // Don't change status if already SentToQuality (final state)
        if (task.Status == LaboratoryTaskStatus.SentToQuality)
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

        // Check conditions - LOGIC MỚI (only check active indicators)
        var allAssigned = activeIndicators.All(ti => ti.MethodId.HasValue && ti.TechnicianId.HasValue);
        var anyEntered = activeIndicators.Any(ti => ti.ApprovalStatus == IndicatorApprovalStatus.Entered);
        var allEnteredOrApproved = activeIndicators.All(ti =>
            ti.ApprovalStatus == IndicatorApprovalStatus.Entered ||
            ti.ApprovalStatus == IndicatorApprovalStatus.Approved);
        var allApproved = activeIndicators.All(ti => ti.ApprovalStatus == IndicatorApprovalStatus.Approved);

        LaboratoryTaskStatus newStatus;

        if (!allAssigned)
        {
            // Có chỉ tiêu chưa gán KTV hoặc phương pháp
            newStatus = LaboratoryTaskStatus.NotAssigned;
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
            newStatus = LaboratoryTaskStatus.Assigned;
        }

        if (task.Status != newStatus)
        {
            task.Status = newStatus;
            await _laboratoryTaskRepository.UpdateAsync(task);
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
            throw new UserFriendlyException("Không tìm thấy thông tin chỉ tiêu mẫu");
        }

        var sample = await _sampleRepository.FindAsync(sampleIndicator.TestRequestSampleId);
        var indicator = await _indicatorRepository.FindAsync(sampleIndicator.IndicatorId);

        string? indicatorGroupName = null;
        if (indicator != null)
        {
            var indicatorGroup = await _indicatorGroupRepository.FindAsync(indicator.IndicatorGroupId);
            indicatorGroupName = indicatorGroup?.Name;
        }

        // Get user names for workflow fields
        string? technicianName = null;
        string? enteredByName = null;
        string? approvedByName = null;
        string? methodName = null;

        if (taskIndicator.TechnicianId.HasValue)
        {
            var technician = await _userRepository.FindAsync(taskIndicator.TechnicianId.Value);
            technicianName = technician?.Name ?? technician?.UserName;
        }
        if (taskIndicator.EnteredById.HasValue)
        {
            var enteredBy = await _userRepository.FindAsync(taskIndicator.EnteredById.Value);
            enteredByName = enteredBy?.Name ?? enteredBy?.UserName;
        }
        if (taskIndicator.ApprovedById.HasValue)
        {
            var approvedBy = await _userRepository.FindAsync(taskIndicator.ApprovedById.Value);
            approvedByName = approvedBy?.Name ?? approvedBy?.UserName;
        }
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
            RejectionReason = taskIndicator.RejectionReason
        };
    }

    #endregion

    #region Supporter Management

    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Default)]
    public async Task<List<LaboratoryTaskSupporterDto>> GetSupportersAsync(Guid taskId)
    {
        var supporters = await _supporterRepository.GetListAsync(x => x.LaboratoryTaskId == taskId);
        var dtos = new List<LaboratoryTaskSupporterDto>();

        foreach (var supporter in supporters)
        {
            var user = await _userRepository.FindAsync(supporter.UserId);
            dtos.Add(new LaboratoryTaskSupporterDto
            {
                Id = supporter.Id,
                LaboratoryTaskId = supporter.LaboratoryTaskId,
                UserId = supporter.UserId,
                UserName = user?.UserName,
                FullName = user != null ? $"{user.Surname} {user.Name}".Trim() : null,
                Email = user?.Email,
                CreationTime = supporter.CreationTime
            });
        }

        return dtos.OrderBy(x => x.FullName).ToList();
    }

    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public async Task<List<LaboratoryTaskSupporterDto>> AddSupportersAsync(Guid taskId, ManageSupportersDto input)
    {
        var task = await _laboratoryTaskRepository.GetAsync(taskId);

        // Validate: Only QLKT or existing supporter can add supporters
        if (!await CanManageTaskAsync(task))
        {
            throw new UserFriendlyException("Bạn không có quyền thêm người hỗ trợ");
        }

        // Get existing supporter user IDs
        var existingSupporters = await _supporterRepository.GetListAsync(x => x.LaboratoryTaskId == taskId);
        var existingUserIds = existingSupporters.Select(x => x.UserId).ToHashSet();

        foreach (var userId in input.UserIds)
        {
            // Skip if already a supporter
            if (existingUserIds.Contains(userId))
            {
                continue;
            }

            // Skip if user is the technical manager
            if (task.TechnicalManagerId == userId)
            {
                continue;
            }

            var supporter = new LaboratoryTaskSupporter(
                GuidGenerator.Create(),
                taskId,
                userId
            );
            supporter.CreatorId = CurrentUser.Id;

            await _supporterRepository.InsertAsync(supporter);
        }

        return await GetSupportersAsync(taskId);
    }

    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public async Task RemoveSupporterAsync(Guid taskId, Guid supporterId)
    {
        var task = await _laboratoryTaskRepository.GetAsync(taskId);

        // Validate: Only QLKT or the supporter themselves can remove
        if (!await CanManageTaskAsync(task))
        {
            throw new UserFriendlyException("Bạn không có quyền xóa người hỗ trợ");
        }

        var supporter = await _supporterRepository.FindAsync(supporterId);
        if (supporter == null || supporter.LaboratoryTaskId != taskId)
        {
            throw new UserFriendlyException("Không tìm thấy người hỗ trợ");
        }

        await _supporterRepository.DeleteAsync(supporter);
    }

    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Default)]
    public async Task<ListResultDto<UserLookupDto>> GetAvailableSupportersAsync(Guid taskId)
    {
        var task = await _laboratoryTaskRepository.GetAsync(taskId);

        // Get existing supporter user IDs
        var existingSupporters = await _supporterRepository.GetListAsync(x => x.LaboratoryTaskId == taskId);
        var existingUserIds = existingSupporters.Select(x => x.UserId).ToHashSet();

        // Add QLKT to excluded list
        if (task.TechnicalManagerId.HasValue)
        {
            existingUserIds.Add(task.TechnicalManagerId.Value);
        }

        // Get all active users and exclude existing supporters and QLKT
        var users = await _userRepository.GetListAsync();
        var availableUsers = users
            .Where(u => u.IsActive && !existingUserIds.Contains(u.Id))
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

        return new ListResultDto<UserLookupDto>(availableUsers);
    }

    /// <summary>
    /// Check if current user can manage task (admin, QLKT or supporter)
    /// </summary>
    private async Task<bool> CanManageTaskAsync(LaboratoryTask task)
    {
        var currentUserId = CurrentUser.Id;
        if (!currentUserId.HasValue)
        {
            return false;
        }

        // Admin can manage all tasks
        if (CurrentUser.IsInRole("admin"))
        {
            return true;
        }

        // QLKT can manage
        if (task.TechnicalManagerId == currentUserId.Value)
        {
            return true;
        }

        // Supporters can manage
        var supporters = await _supporterRepository.GetListAsync(x => x.LaboratoryTaskId == task.Id);
        return supporters.Any(x => x.UserId == currentUserId.Value);
    }

    #endregion

    #region Bulk Approval

    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public async Task<List<LaboratoryTaskIndicatorDto>> BulkApproveIndicatorsAsync(Guid taskId, BulkApproveIndicatorsDto input)
    {
        var task = await _laboratoryTaskRepository.GetAsync(taskId);

        // Validate: Only QLKT or supporter can approve
        if (!await CanManageTaskAsync(task))
        {
            throw new UserFriendlyException("Bạn không có quyền phê duyệt kết quả");
        }

        if (!input.Approve && string.IsNullOrWhiteSpace(input.RejectionReason))
        {
            throw new UserFriendlyException("Vui lòng nhập lý do từ chối");
        }

        var results = new List<LaboratoryTaskIndicatorDto>();

        foreach (var indicatorId in input.IndicatorIds)
        {
            var taskIndicator = await _laboratoryTaskIndicatorRepository.FindAsync(indicatorId);
            if (taskIndicator == null || taskIndicator.LaboratoryTaskId != taskId)
            {
                continue;
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

        return results;
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
            throw new UserFriendlyException("Bạn không có quyền gửi kết quả");
        }

        // Get selected indicators
        var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => x.LaboratoryTaskId == taskId);
        var selectedIndicators = taskIndicators
            .Where(ti => input.IndicatorIds.Contains(ti.Id))
            .ToList();

        if (!selectedIndicators.Any())
        {
            throw new UserFriendlyException("Vui lòng chọn ít nhất một chỉ tiêu để gửi");
        }

        // Validate: All selected indicators must be approved
        var notApproved = selectedIndicators.Where(ti => ti.ApprovalStatus != IndicatorApprovalStatus.Approved).ToList();
        if (notApproved.Any())
        {
            throw new UserFriendlyException("Tất cả chỉ tiêu được chọn phải được duyệt trước khi gửi");
        }

        // Create or update QualityManagementTask
        await CreateQualityManagementTaskAsync(task, selectedIndicators);

        // Update task status to SentToQuality when indicators are sent
        if (task.Status != LaboratoryTaskStatus.SentToQuality)
        {
            task.Status = LaboratoryTaskStatus.SentToQuality;
            await _laboratoryTaskRepository.UpdateAsync(task);
        }

        // Update TestRequest status if all tasks sent
        await TryUpdateTestRequestToPendingApprovalAsync(task.TestRequestId);

        // Reload task to ensure we return the latest data
        task = await _laboratoryTaskRepository.GetAsync(taskId);
        return await MapToDetailDtoAsync(task);
    }

    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Default)]
    public async Task<ListResultDto<UserLookupDto>> GetQualityManagersAsync()
    {
        // Get users with QLCL role
        var users = await _userRepository.GetListAsync();
        var qualityManagers = new List<UserLookupDto>();

        foreach (var user in users.Where(u => u.IsActive))
        {
            // For now, return all active users - in real scenario, filter by role
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
}
