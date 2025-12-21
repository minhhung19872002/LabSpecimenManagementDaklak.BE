using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.MasterData.Indicators;
using LabSpecimenManagement.Permissions;
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
    private readonly IRepository<IndicatorRejectionHistory, Guid> _rejectionHistoryRepository;
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;
    private readonly IRepository<TestRequestSample, Guid> _sampleRepository;
    private readonly IRepository<TestRequestSampleIndicator, Guid> _sampleIndicatorRepository;
    private readonly IRepository<Laboratory, Guid> _laboratoryRepository;
    private readonly IRepository<Indicator, Guid> _indicatorRepository;
    private readonly IRepository<IndicatorGroup, Guid> _indicatorGroupRepository;
    private readonly IRepository<TestMethod, Guid> _testMethodRepository;
    private readonly IIdentityUserRepository _userRepository;

    public LaboratoryTaskAppService(
        IRepository<LaboratoryTask, Guid> laboratoryTaskRepository,
        IRepository<LaboratoryTaskIndicator, Guid> laboratoryTaskIndicatorRepository,
        IRepository<IndicatorRejectionHistory, Guid> rejectionHistoryRepository,
        IRepository<TestRequest, Guid> testRequestRepository,
        IRepository<TestRequestSample, Guid> sampleRepository,
        IRepository<TestRequestSampleIndicator, Guid> sampleIndicatorRepository,
        IRepository<Laboratory, Guid> laboratoryRepository,
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<IndicatorGroup, Guid> indicatorGroupRepository,
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
        _indicatorGroupRepository = indicatorGroupRepository;
        _testMethodRepository = testMethodRepository;
        _userRepository = userRepository;
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
            if (taskIndicator.MethodId.HasValue)
            {
                var method = await _testMethodRepository.FindAsync(taskIndicator.MethodId.Value);
                methodName = method?.Name;
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
                Result = taskIndicator.Result,
                Unit = taskIndicator.Unit,
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
        var task = await _laboratoryTaskRepository.GetAsync(taskIndicator.LaboratoryTaskId);
        await UpdateTaskStatusAsync(task);

        return await MapToIndicatorDtoAsync(taskIndicator);
    }

    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public async Task<LaboratoryTaskDto> SendResultsAsync(Guid taskId)
    {
        var task = await _laboratoryTaskRepository.GetAsync(taskId);
        var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => x.LaboratoryTaskId == taskId);

        // Validate: All indicators must be approved
        var allApproved = taskIndicators.All(ti => ti.ApprovalStatus == IndicatorApprovalStatus.Approved);
        if (!allApproved)
        {
            throw new UserFriendlyException("Tất cả chỉ tiêu phải được duyệt trước khi gửi kết quả");
        }

        task.Status = LaboratoryTaskStatus.ResultSent;
        await _laboratoryTaskRepository.UpdateAsync(task);

        return await MapToDetailDtoAsync(task);
    }

    public async Task<ListResultDto<MethodLookupDto>> GetMethodsByIndicatorAsync(Guid indicatorId)
    {
        // For now, return all active methods
        // Later can add filtering logic based on indicator-method relationship
        var methods = await _testMethodRepository.GetListAsync(x => x.IsActive);
        var dtos = methods
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.Name)
            .Select(m => new MethodLookupDto
            {
                Id = m.Id,
                Code = m.Code,
                Name = m.Name,
                Description = m.Description
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

        // Don't change status if already ResultSent (final state)
        if (task.Status == LaboratoryTaskStatus.ResultSent)
        {
            return;
        }

        // Check conditions - LOGIC MỚI
        var allAssigned = taskIndicators.All(ti => ti.MethodId.HasValue && ti.TechnicianId.HasValue);
        var anyEntered = taskIndicators.Any(ti => ti.ApprovalStatus == IndicatorApprovalStatus.Entered);
        var allEnteredOrApproved = taskIndicators.All(ti =>
            ti.ApprovalStatus == IndicatorApprovalStatus.Entered ||
            ti.ApprovalStatus == IndicatorApprovalStatus.Approved);
        var allApproved = taskIndicators.All(ti => ti.ApprovalStatus == IndicatorApprovalStatus.Approved);

        LaboratoryTaskStatus newStatus;

        if (!allAssigned)
        {
            // Có chỉ tiêu chưa gắn phương pháp HOẶC chưa assign KTV
            newStatus = LaboratoryTaskStatus.NotAssignedMethod;
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
            // Tất cả đã có method + KTV nhưng chưa gửi duyệt
            newStatus = LaboratoryTaskStatus.AssignedMethod;
        }

        if (task.Status != newStatus)
        {
            task.Status = newStatus;
            await _laboratoryTaskRepository.UpdateAsync(task);
        }
    }

    /// <summary>
    /// Kiểm tra xem task có thể chỉnh sửa không dựa trên trạng thái
    /// Chỉ cho phép edit khi: Chưa gắn PP hoặc Đã gán PP
    /// </summary>
    private static bool CanEditTask(LaboratoryTaskStatus status)
    {
        return status == LaboratoryTaskStatus.NotAssignedMethod ||
               status == LaboratoryTaskStatus.AssignedMethod;
    }

    /// <summary>
    /// Lấy nhãn tiếng Việt cho trạng thái
    /// </summary>
    private static string GetStatusLabel(LaboratoryTaskStatus status)
    {
        return status switch
        {
            LaboratoryTaskStatus.NotAssignedMethod => "Chưa gắn phương pháp",
            LaboratoryTaskStatus.AssignedMethod => "Đã gán phương pháp",
            LaboratoryTaskStatus.Testing => "Đang thí nghiệm",
            LaboratoryTaskStatus.PendingApproval => "Chờ duyệt kết quả",
            LaboratoryTaskStatus.Approved => "Đã duyệt",
            LaboratoryTaskStatus.ResultSent => "Đã gửi kết quả",
            _ => status.ToString()
        };
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
        if (taskIndicator.MethodId.HasValue)
        {
            var method = await _testMethodRepository.FindAsync(taskIndicator.MethodId.Value);
            methodName = method?.Name;
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
            Result = taskIndicator.Result,
            Unit = taskIndicator.Unit,
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
}
