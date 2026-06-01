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
using LabSpecimenManagement.Testing;
using LabSpecimenManagement.TestRequests;
using LabSpecimenManagement.Users;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;

namespace LabSpecimenManagement.LaboratoryTasks;

[Authorize]
public partial class LaboratoryTaskAppService : ApplicationService, ILaboratoryTaskAppService
{
    // Support both English role names (from seed data) and Vietnamese role names (from production)
    private static readonly string[] QualityManagerRoleNames = { "QualityManager", "Quản lý chất lượng" };

    // Positions that are considered as technicians (for dropdown filtering)
    private static readonly string[] TechnicianPositions = { "Nhân viên", "Thử nghiệm viên" };

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
    private readonly IRepository<IndicatorImage, Guid> _indicatorImageRepository;
    private readonly IRepository<SampleIndicatorNote, Guid> _sampleIndicatorNoteRepository;
    private readonly IRepository<MethodChangeHistory, Guid> _methodChangeHistoryRepository;
    private readonly IRepository<Note, Guid> _noteRepository;
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IRepository<LaboratoryDepartmentSupporter, Guid> _departmentSupporterRepository;

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
        IRepository<StandardIndicator, Guid> standardIndicatorRepository,
        IRepository<IndicatorImage, Guid> indicatorImageRepository,
        IRepository<SampleIndicatorNote, Guid> sampleIndicatorNoteRepository,
        IRepository<MethodChangeHistory, Guid> methodChangeHistoryRepository,
        IRepository<Note, Guid> noteRepository,
        IRepository<Department, Guid> departmentRepository,
        IRepository<LaboratoryDepartmentSupporter, Guid> departmentSupporterRepository)
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
        _indicatorImageRepository = indicatorImageRepository;
        _sampleIndicatorNoteRepository = sampleIndicatorNoteRepository;
        _methodChangeHistoryRepository = methodChangeHistoryRepository;
        _noteRepository = noteRepository;
        _departmentRepository = departmentRepository;
        _departmentSupporterRepository = departmentSupporterRepository;
    }

    public async Task<LaboratoryTaskDto> GetAsync(Guid id)
    {
        var task = await _laboratoryTaskRepository.GetAsync(id);
        return await MapToDetailDtoAsync(task);
    }

    public async Task<PagedResultDto<LaboratoryTaskDto>> GetFilteredListAsync(GetLaboratoryTaskListDto input)
    {
        var queryable = await _laboratoryTaskRepository.GetQueryableAsync();

        // Filter by current user's access (admin sees all, others see tasks where they are QLKT or supporter)
        var currentUserId = CurrentUser.Id;
        if (currentUserId.HasValue && !CurrentUser.IsInRole(AppConstants.RoleAdmin))
        {
            // Get task IDs where current user is a supporter (chỉ cho incomplete tasks)
            var supporterTaskIds = (await _supporterRepository.GetListAsync(s => s.UserId == currentUserId.Value))
                .Select(s => s.LaboratoryTaskId)
                .ToHashSet();

            // Lấy danh sách LaboratoryId mà user là QLKT của phòng ban
            var managedLaboratoryIds = (await _laboratoryRepository.GetListAsync(
                lab => lab.TechnicalManagerId == currentUserId.Value))
                .Select(lab => lab.Id)
                .ToHashSet();

            // Lấy danh sách LaboratoryId mà user là department supporter (chưa hết hạn)
            var departmentSupporterLabIds = (await _departmentSupporterRepository.GetListAsync(
                ds => ds.UserId == currentUserId.Value && ds.ExpirationDate > DateTime.UtcNow))
                .Select(ds => ds.LaboratoryId)
                .ToHashSet();

            // Filter: current user is QLKT của phòng ban OR QLKT của task OR supporter OR department supporter
            // Supporter chỉ thấy task chưa hoàn thành (status != SentToQuality)
            queryable = queryable.Where(x =>
                managedLaboratoryIds.Contains(x.LaboratoryId) ||
                x.TechnicalManagerId == currentUserId.Value ||
                (supporterTaskIds.Contains(x.Id) && x.Status != LaboratoryTaskStatus.SentToQuality) ||
                departmentSupporterLabIds.Contains(x.LaboratoryId));
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

        if (input.TestRequestStatus.HasValue)
        {
            var testRequestQueryable = await _testRequestRepository.GetQueryableAsync();
            var testRequestIds = testRequestQueryable
                .Where(tr => tr.Status == input.TestRequestStatus.Value)
                .Select(tr => tr.Id);

            queryable = queryable.Where(x => testRequestIds.Contains(x.TestRequestId));
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

    [Authorize]
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

    [Authorize]
    public async Task<LaboratoryTaskDto> UpdateStatusAsync(Guid id, LaboratoryTaskStatus newStatus)
    {
        var task = await _laboratoryTaskRepository.GetAsync(id);
        task.Status = newStatus;
        await _laboratoryTaskRepository.UpdateAsync(task);
        return await MapToDetailDtoAsync(task);
    }

    [Authorize]
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
        // Lấy ngày thí nghiệm từ các indicators
        var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => x.LaboratoryTaskId == task.Id);

        // Tính Min/Max TestDate giống báo cáo
        var indicatorsWithTestDate = taskIndicators.Where(ti => ti.TestDate.HasValue).ToList();
        var earliestTestDate = indicatorsWithTestDate.OrderBy(ti => ti.TestDate).FirstOrDefault()?.TestDate;
        var latestTestDate = indicatorsWithTestDate.OrderByDescending(ti => ti.TestDate).FirstOrDefault()?.TestDate;

        // Calculate TestDateRange: "dd/MM/yyyy - dd/MM/yyyy"
        var testDateRange = CalculateTestDateRange(earliestTestDate, latestTestDate);

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
            TestDateRange = testDateRange, // Khoảng ngày: Min - Max
            Status = task.Status,
            Notes = task.Notes,
            ResultRejectionReason = task.ResultRejectionReason,
            CreationTime = task.CreationTime,
            LastModificationTime = task.LastModificationTime,
            // Shared Sample Fields
            IsSharedSampleTask = task.IsSharedSampleTask,
            IsWaitingForPriorLab = task.IsWaitingForPriorLab,
            SampleSentToNextLab = task.SampleSentToNextLab,
            SampleSentDate = task.SampleSentDate,
            SampleReceivedDate = task.SampleReceivedDate,
            SampleReceivedNote = task.SampleReceivedNote,
            PriorLabTaskId = task.PriorLabTaskId,
            // Check if prior lab has sent sample (for Lab Hóa waiting for Vi Sinh)
            PriorLabSentSample = false // Will be set below if applicable
        };

        // Check if prior lab has sent sample (for Lab Hóa to know when Vi Sinh sent)
        if (task.PriorLabTaskId.HasValue)
        {
            var priorTask = await _laboratoryTaskRepository.FindAsync(task.PriorLabTaskId.Value);
            if (priorTask != null)
            {
                dto.PriorLabSentSample = priorTask.SampleSentToNextLab;
            }
        }

        // Get TestRequest info
        var testRequest = await _testRequestRepository.FindAsync(task.TestRequestId);
        if (testRequest != null)
        {
            dto.TestRequestNumber = testRequest.RequestNumber;
            dto.TestRequestStatus = testRequest.Status;
            dto.CustomerName = testRequest.CustomerName;
            dto.SampleSentDate = testRequest.SampleSentDate ?? task.SampleSentDate; // Ưu tiên TestRequest.SampleSentDate (ngày BPGN xác nhận gửi mẫu)
        }

        // Get Laboratory info
        var laboratory = await _laboratoryRepository.FindAsync(task.LaboratoryId);
        if (laboratory != null)
        {
            dto.LaboratoryCode = laboratory.Code;
            var department = await _departmentRepository.FindAsync(laboratory.DepartmentId);
            dto.LaboratoryName = department?.Name ?? "";
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

            // Check if current user is a department supporter (not expired)
            dto.IsCurrentUserDepartmentSupporter = (await _departmentSupporterRepository.GetListAsync(
                ds => ds.UserId == currentUserId.Value &&
                      ds.LaboratoryId == task.LaboratoryId &&
                      ds.ExpirationDate > DateTime.UtcNow)).Any();
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

        // Batch load all Master Data Notes
        var allNoteIds = taskIndicators
            .Where(ti => !string.IsNullOrEmpty(ti.MasterDataNoteIds))
            .SelectMany(ti => ParseNoteIds(ti.MasterDataNoteIds))
            .Distinct()
            .ToList();
        var masterDataNotes = allNoteIds.Count > 0
            ? (await _noteRepository.GetListAsync(n => allNoteIds.Contains(n.Id))).ToDictionary(n => n.Id)
            : new Dictionary<Guid, Note>();

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
            string? methodFormattedName = null;
            var isSubContractMethod = !string.IsNullOrWhiteSpace(taskIndicator.SubContractMethodName);
            if (taskIndicator.MethodId.HasValue)
            {
                var method = await _testMethodRepository.FindAsync(taskIndicator.MethodId.Value);
                if (!isSubContractMethod)
                {
                    methodName = method?.Name;
                    methodFormattedName = method?.FormattedName;
                }
                isSubContractMethod = isSubContractMethod || IsDashMethodName(method?.Name);
                // Fallback to TestMethod.Unit if LaboratoryTaskIndicator.Unit is not set
                if (string.IsNullOrEmpty(unit) && method != null)
                {
                    unit = method.FormattedUnit ?? method.Unit ?? "";
                }
            }

            if (!string.IsNullOrWhiteSpace(taskIndicator.SubContractMethodName))
            {
                methodName = taskIndicator.SubContractMethodName;
                methodFormattedName = null;
            }

            // Get standard info if StandardId is set
            string? standardName = null;
            string? comparisonOperator = null;
            string? allowedLimit = null;
            string? lowerOperator = null;
            string? lowerLimit = null;
            string? upperOperator = null;
            string? upperLimit = null;
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
                    lowerOperator = standardIndicator.LowerOperator;
                    lowerLimit = standardIndicator.LowerLimit;
                    upperOperator = standardIndicator.UpperOperator;
                    upperLimit = standardIndicator.UpperLimit;
                }
            }

            // Get indicator images
            var indicatorImages = await _indicatorImageRepository.GetListAsync(x =>
                x.LaboratoryTaskIndicatorId == taskIndicator.Id);

            // Get method change histories for this indicator
            var methodChangeHistories = await _methodChangeHistoryRepository.GetListAsync(h => h.IndicatorId == taskIndicator.Id);

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
                IndicatorFormattedName = indicator?.FormattedName,
                IndicatorGroupName = indicatorGroupName,
                ParentIndicatorId = indicator?.ParentIndicatorId,
                UnitPrice = sampleIndicator.UnitPrice,
                // Phase 6 workflow fields
                MethodId = taskIndicator.MethodId,
                MethodName = methodName,
                MethodFormattedName = methodFormattedName,
                IsSubContractMethod = isSubContractMethod,
                SubContractMethodName = taskIndicator.SubContractMethodName,
                SubContractNumber = taskIndicator.SubContractNumber,
                SubContractCost = taskIndicator.SubContractCost,
                TechnicianId = taskIndicator.TechnicianId,
                TechnicianName = technicianName,
                AssignedDate = taskIndicator.AssignedDate,
                TestDate = taskIndicator.TestDate,
                Result = taskIndicator.Result,
                FreeTextNote = taskIndicator.FreeTextNote,
                MasterDataNoteIds = taskIndicator.MasterDataNoteIds,
                MasterDataNoteContents = GetMasterDataNoteContentsFromCache(taskIndicator.MasterDataNoteIds, masterDataNotes),
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
                AllowedLimit = allowedLimit,
                LowerOperator = lowerOperator,
                LowerLimit = lowerLimit,
                UpperOperator = upperOperator,
                UpperLimit = upperLimit,
                // Indicator images
                Images = indicatorImages.OrderBy(x => x.CreationTime).Select(x => new Testing.IndicatorImageDto
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
                // Workflow Redesign - Testing Rejection Fields
                TestingRejectionReason = taskIndicator.TestingRejectionReason,
                TestingRejectionDate = taskIndicator.TestingRejectionDate,
                TestingRejectedById = taskIndicator.TestingRejectedById,
                TestingRejectedByName = taskIndicator.TestingRejectedByName,
                // Workflow Redesign - Customer Confirmation Fields
                CustomerConfirmationDate = taskIndicator.CustomerConfirmationDate,
                CustomerConfirmedById = taskIndicator.CustomerConfirmedById,
                CustomerConfirmedByName = taskIndicator.CustomerConfirmedByName,
                // Workflow Redesign - Method Change Histories
                MethodChangeHistories = methodChangeHistories
                    .OrderByDescending(h => h.RequestedDate)
                    .Select(h => new MethodChangeHistoryDto
                    {
                        Id = h.Id,
                        IndicatorId = h.IndicatorId,
                        OldMethodId = h.OldMethodId,
                        OldMethodName = h.OldMethodName,
                        NewMethodId = h.NewMethodId,
                        NewMethodName = h.NewMethodName,
                        Reason = h.Reason,
                        RequestedDate = h.RequestedDate,
                        RequestedByUserId = h.RequestedByUserId,
                        RequestedByUserName = h.RequestedByUserName,
                        ConfirmedDate = h.ConfirmedDate,
                        ConfirmedByUserId = h.ConfirmedByUserId,
                        ConfirmedByUserName = h.ConfirmedByUserName,
                        IsApproved = h.IsApproved
                    })
                    .ToList(),
                // Shared Sample - waiting flag
                IsWaitingForSharedSample = taskIndicator.IsWaitingForSharedSample
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

    [Authorize]
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

        // Filter assignments: chỉ lấy các chỉ tiêu có thể giao việc
        // Bỏ qua (skip) các chỉ tiêu đã được duyệt, đang chờ duyệt, hoặc đã bị hủy
        var validAssignments = new List<AssignIndicatorDto>();
        foreach (var assignment in assignments)
        {
            // Validate that indicator exists in this sample
            var taskIndicator = indicatorsForSample.FirstOrDefault(ti => ti.Id == assignment.IndicatorId);
            if (taskIndicator == null)
            {
                // Chỉ tiêu không thuộc mẫu này - bỏ qua
                continue;
            }

            // Bỏ qua chỉ tiêu đã được duyệt, đang chờ duyệt, hoặc đã bị hủy
            if (taskIndicator.ApprovalStatus == IndicatorApprovalStatus.Approved ||
                taskIndicator.ApprovalStatus == IndicatorApprovalStatus.Entered ||
                taskIndicator.ApprovalStatus == IndicatorApprovalStatus.CustomerCancelled)
            {
                continue;
            }

            // Chỉ validate method + technician cho các chỉ tiêu hợp lệ
            if (!assignment.MethodId.HasValue || !assignment.TechnicianId.HasValue)
            {
                throw new ValidationException("assignment", "Vui lòng chọn phương pháp và Thử nghiệm viên cho tất cả chỉ tiêu được chọn");
            }

            validAssignments.Add(assignment);
        }

        // Apply assignments - chỉ xử lý các chỉ tiêu hợp lệ
        foreach (var assignment in validAssignments)
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

        // Notify technicians about assigned indicators (Case 1 & 2 KTV)
        // Chỉ notify cho các chỉ tiêu thực sự được giao việc (validAssignments)
        await NotifyTechniciansOnAssignAsync(task, indicatorsForSample, validAssignments);

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
                        task.Id,
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
                    task.Id,
                    indicatorCount,
                    task.RequestCompletionDate
                );
            }
        }
    }

    [Authorize]
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

            var senderName = CurrentUser.Name ?? CurrentUser.UserName ?? "Thử nghiệm viên";

            await _notificationCreator.NotifyIndicatorSubmittedToTechnicalManagerAsync(
                task.TechnicalManagerId.Value,
                testRequest.ContractNumber ?? task.ContractNumber ?? "",
                task.Id,
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

    [Authorize]
    public async Task<LaboratoryTaskIndicatorDto> ApproveIndicatorAsync(Guid indicatorId, ApproveIndicatorDto input)
    {
        var taskIndicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicatorId);
        var task = await _laboratoryTaskRepository.GetAsync(taskIndicator.LaboratoryTaskId);

        // Validate: Only QLKT or supporter can approve
        if (!await CanManageTaskAsync(task))
        {
            throw new ForbiddenAccessException("Bạn không có quyền phê duyệt kết quả");
        }

        // Validate: Check if there are any quality managers before approving
        if (input.Approve)
        {
            var qualityManagerIds = await GetQualityManagerIdsAsync(task);
            if (!qualityManagerIds.Any())
            {
                throw new Common.Exceptions.InvalidOperationException(
                    "Không thể duyệt kết quả vì không có Quản lý chất lượng nào trong hệ thống. Vui lòng liên hệ quản trị viên để thêm người dùng có vai trò Quản lý chất lượng.", "qualityManager");
            }
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

            // Auto send to QLCL - set IsSentToQuality = true
            taskIndicator.IsSentToQuality = true;
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
            // Không set ApprovalDate khi từ chối - chỉ set khi duyệt thành công
            taskIndicator.ApprovedById = CurrentUser.Id;
            // Giữ lại Result và Unit để user có thể sửa và gửi lại

            // Save rejection history
            var rejectionHistory = new IndicatorRejectionHistory(
                GuidGenerator.Create(),
                indicatorId,
                input.RejectionReason,
                CurrentUser.Id!.Value,
                Clock.Now
            );
            await _rejectionHistoryRepository.InsertAsync(rejectionHistory, autoSave: true);
        }

        await _laboratoryTaskIndicatorRepository.UpdateAsync(taskIndicator, autoSave: true);

        // Nếu approve, tạo QualityManagementIndicator và gửi thông báo đến tất cả QLCL
        if (input.Approve)
        {
            // Auto-approve parent indicator nếu đây là sub-indicator
            var indicatorsToSendToQuality = new List<LaboratoryTaskIndicator> { taskIndicator };
            var autoApprovedParent = await AutoApproveParentIndicatorIfNeededAsync(taskIndicator, task);
            if (autoApprovedParent != null)
            {
                indicatorsToSendToQuality.Add(autoApprovedParent);
            }

            await CreateQualityManagementTaskAsync(task, indicatorsToSendToQuality);

            // Update task status to SentToQuality nếu tất cả đã được gửi
            var allTaskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => x.LaboratoryTaskId == task.Id);
            var hasUnsentApproved = allTaskIndicators.Any(ti =>
                ti.ApprovalStatus == IndicatorApprovalStatus.Approved && !ti.IsSentToQuality);

            if (!hasUnsentApproved && task.Status != LaboratoryTaskStatus.SentToQuality)
            {
                task.Status = LaboratoryTaskStatus.SentToQuality;
                await _laboratoryTaskRepository.UpdateAsync(task);
            }
            else
            {
                // Update task status bình thường
                await UpdateTaskStatusAsync(task);
            }

            // Update TestRequest status
            await TryUpdateTestRequestToPendingApprovalAsync(task.TestRequestId);

            // Gửi thông báo đến tất cả QLCL
            await NotifyAllQualityManagersOnApproveAsync(task, taskIndicator);
        }
        else
        {
            // Update task status
            await UpdateTaskStatusAsync(task);

            // Đồng bộ trạng thái TestRequest
            await UpdateTestRequestStatusAsync(task.TestRequestId);

            // Notify technician when result is rejected (Case 5 KTV - single rejection)
            if (taskIndicator.EnteredById.HasValue)
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
                    task.Id,
                    technicalManagerName,
                    sample?.SampleName ?? "",
                    indicator?.Name ?? "",
                    method?.Name ?? ""
                );
            }
        }

        return await MapToIndicatorDtoAsync(taskIndicator);
    }

    /// <summary>
    /// Cập nhật tiêu chuẩn so sánh cho chỉ tiêu
    /// </summary>
    [Authorize]
    public async Task<LaboratoryTaskIndicatorDto> UpdateIndicatorStandardAsync(Guid indicatorId, Guid? standardId)
    {
        var taskIndicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicatorId);
        var task = await _laboratoryTaskRepository.GetAsync(taskIndicator.LaboratoryTaskId);

        // Validate: Only QLKT or supporter can update standard
        if (!await CanManageTaskAsync(task))
        {
            throw new ForbiddenAccessException("Bạn không có quyền cập nhật tiêu chuẩn");
        }

        // Validate: Not allow update when indicator is in certain statuses
        var notAllowedStatuses = new[]
        {
            IndicatorApprovalStatus.Approved,              // Đã duyệt - không cho đổi
            IndicatorApprovalStatus.CustomerCancelled,     // KH hủy - không cho đổi
            IndicatorApprovalStatus.MethodChangeRequested, // Đang yêu cầu đổi PP - không cho đổi
            IndicatorApprovalStatus.TestingRejected        // QLKT từ chối thử nghiệm - không cho đổi
        };
        if (notAllowedStatuses.Contains(taskIndicator.ApprovalStatus))
        {
            throw new Common.Exceptions.InvalidOperationException("Không thể thay đổi tiêu chuẩn ở trạng thái hiện tại", "indicator");
        }

        taskIndicator.StandardId = standardId;
        await _laboratoryTaskIndicatorRepository.UpdateAsync(taskIndicator);

        return await MapToIndicatorDtoAsync(taskIndicator);
    }

    /// <summary>
    /// Cập nhật phương pháp cho chỉ tiêu
    /// </summary>
    [Authorize]
    public async Task<LaboratoryTaskIndicatorDto> UpdateIndicatorMethodAsync(Guid indicatorId, Guid? methodId)
    {
        var taskIndicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicatorId);
        var task = await _laboratoryTaskRepository.GetAsync(taskIndicator.LaboratoryTaskId);
        TestMethod? selectedMethod = null;

        // Validate: Only QLKT or supporter can update method
        if (!await CanManageTaskAsync(task))
        {
            throw new ForbiddenAccessException("Bạn không có quyền cập nhật phương pháp");
        }

        // Validate: Cannot update when indicator is Approved or CustomerCancelled
        if (taskIndicator.ApprovalStatus == IndicatorApprovalStatus.Approved)
        {
            throw new Common.Exceptions.InvalidOperationException("Không thể cập nhật phương pháp cho chỉ tiêu đã duyệt", "indicator");
        }
        if (taskIndicator.ApprovalStatus == IndicatorApprovalStatus.CustomerCancelled)
        {
            throw new Common.Exceptions.InvalidOperationException("Không thể cập nhật phương pháp cho chỉ tiêu đã bị hủy", "indicator");
        }

        // Validate method existence when provided
        if (methodId.HasValue)
        {
            selectedMethod = await _testMethodRepository.FindAsync(methodId.Value);
            if (selectedMethod == null)
            {
                throw new ValidationException("methodId", "Phương pháp không tồn tại");
            }
        }

        var keepSubContractMethod = IsDashMethodName(selectedMethod?.Name);
        if (!keepSubContractMethod)
        {
            taskIndicator.SubContractMethodName = null;
            taskIndicator.SubContractNumber = null;
            taskIndicator.SubContractCost = null;
        }

        taskIndicator.MethodId = methodId;
        await _laboratoryTaskIndicatorRepository.UpdateAsync(taskIndicator);

        // Recalculate task status after changing method
        await UpdateTaskStatusAsync(task);

        return await MapToIndicatorDtoAsync(taskIndicator);
    }

    /// <summary>
    /// Cập nhật thông tin phương pháp phụ cho chỉ tiêu có phương pháp gốc là "-"
    /// </summary>
    [Authorize]
    public async Task<LaboratoryTaskIndicatorDto> UpdateSubContractMethodAsync(Guid indicatorId, UpdateSubContractMethodDto input)
    {
        var taskIndicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicatorId);
        var task = await _laboratoryTaskRepository.GetAsync(taskIndicator.LaboratoryTaskId);

        if (!await CanManageTaskAsync(task))
        {
            throw new ForbiddenAccessException("Bạn không có quyền cập nhật phương pháp phụ");
        }

        if (taskIndicator.ApprovalStatus == IndicatorApprovalStatus.Approved)
        {
            throw new Common.Exceptions.InvalidOperationException("Không thể cập nhật phương pháp phụ cho chỉ tiêu đã duyệt", "indicator");
        }

        var subContractMethodName = input.SubContractMethodName?.Trim();
        var subContractNumber = input.SubContractNumber?.Trim();

        if (string.IsNullOrWhiteSpace(subContractMethodName))
        {
            throw new ValidationException("subContractMethodName", "Tên phương pháp không được để trống");
        }

        if (string.IsNullOrWhiteSpace(subContractNumber))
        {
            throw new ValidationException("subContractNumber", "Số hợp đồng phụ không được để trống");
        }

        // Chỉ cho phép cập nhật cho chỉ tiêu có phương pháp gốc "-" hoặc đã có dữ liệu phương pháp phụ
        var canUpdateSubContractMethod = !string.IsNullOrWhiteSpace(taskIndicator.SubContractMethodName);
        if (!canUpdateSubContractMethod && taskIndicator.MethodId.HasValue)
        {
            var method = await _testMethodRepository.FindAsync(taskIndicator.MethodId.Value);
            canUpdateSubContractMethod = IsDashMethodName(method?.Name);
        }

        if (!canUpdateSubContractMethod)
        {
            throw new Common.Exceptions.InvalidOperationException("Chỉ tiêu này không thuộc luồng cập nhật phương pháp phụ", "indicator");
        }

        taskIndicator.SubContractMethodName = subContractMethodName;
        taskIndicator.SubContractNumber = subContractNumber;
        taskIndicator.SubContractCost = input.SubContractCost;

        await _laboratoryTaskIndicatorRepository.UpdateAsync(taskIndicator);

        return await MapToIndicatorDtoAsync(taskIndicator);
    }

    /// <summary>
    /// Cập nhật đơn vị tính cho chỉ tiêu (không cập nhật ngược master data)
    /// </summary>
    [Authorize]
    public async Task<LaboratoryTaskIndicatorDto> UpdateIndicatorUnitAsync(Guid indicatorId, string? unit)
    {
        var taskIndicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicatorId);
        var task = await _laboratoryTaskRepository.GetAsync(taskIndicator.LaboratoryTaskId);

        // Validate: Only QLKT or supporter can update unit
        if (!await CanManageTaskAsync(task))
        {
            throw new ForbiddenAccessException("Bạn không có quyền cập nhật đơn vị tính");
        }

        // Validate: Cannot update when indicator is Approved or CustomerCancelled
        if (taskIndicator.ApprovalStatus == IndicatorApprovalStatus.Approved)
        {
            throw new Common.Exceptions.InvalidOperationException("Không thể cập nhật đơn vị tính cho chỉ tiêu đã duyệt", "indicator");
        }
        if (taskIndicator.ApprovalStatus == IndicatorApprovalStatus.CustomerCancelled)
        {
            throw new Common.Exceptions.InvalidOperationException("Không thể cập nhật đơn vị tính cho chỉ tiêu đã bị hủy", "indicator");
        }

        taskIndicator.Unit = unit;
        await _laboratoryTaskIndicatorRepository.UpdateAsync(taskIndicator);

        return await MapToIndicatorDtoAsync(taskIndicator);
    }

    /// <summary>
    /// Cập nhật ghi chú cho chỉ tiêu
    /// </summary>
    [Authorize]
    public async Task<LaboratoryTaskIndicatorDto> UpdateIndicatorNoteAsync(Guid indicatorId, string? note)
    {
        var taskIndicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicatorId);
        var task = await _laboratoryTaskRepository.GetAsync(taskIndicator.LaboratoryTaskId);

        // Validate: Only QLKT or supporter can update note
        if (!await CanManageTaskAsync(task))
        {
            throw new ForbiddenAccessException("Bạn không có quyền cập nhật ghi chú");
        }

        // Validate: Cannot update when indicator is Approved
        if (taskIndicator.ApprovalStatus == IndicatorApprovalStatus.Approved)
        {
            throw new Common.Exceptions.InvalidOperationException("Không thể cập nhật ghi chú cho chỉ tiêu đã duyệt", "indicator");
        }

        taskIndicator.FreeTextNote = note;
        await _laboratoryTaskIndicatorRepository.UpdateAsync(taskIndicator);

        return await MapToIndicatorDtoAsync(taskIndicator);
    }

    /// <summary>
    /// Cập nhật ghi chú cho nhiều chỉ tiêu cùng lúc
    /// </summary>
    [Authorize]
    public async Task BulkUpdateIndicatorNoteAsync(Guid taskId, BulkUpdateNoteDto input)
    {
        var task = await _laboratoryTaskRepository.GetAsync(taskId);

        // Validate: Only QLKT or supporter can update note
        if (!await CanManageTaskAsync(task))
        {
            throw new ForbiddenAccessException("Bạn không có quyền cập nhật ghi chú");
        }

        // Get indicators belonging to this task - chỉ cần lấy selectedIndicatorIds
        // Vì cả FreeTextNote và MasterDataNoteIds đều áp dụng cho từng chỉ tiêu được chọn
        var indicators = await _laboratoryTaskIndicatorRepository
            .GetListAsync(x => input.SelectedIndicatorIds.Contains(x.Id) && x.LaboratoryTaskId == taskId);

        // Convert Master Data Note IDs to comma-separated string
        var masterDataNoteIdsString = input.MasterDataNoteIds != null && input.MasterDataNoteIds.Any()
            ? string.Join(",", input.MasterDataNoteIds)
            : null;

        foreach (var indicator in indicators)
        {
            // Skip approved/cancelled indicators
            if (indicator.ApprovalStatus == IndicatorApprovalStatus.Approved ||
                indicator.ApprovalStatus == IndicatorApprovalStatus.CustomerCancelled)
                continue;

            // 1. Update FreeTextNote for selected indicators only
            if (input.AppendMode && !string.IsNullOrEmpty(indicator.FreeTextNote) && !string.IsNullOrEmpty(input.FreeTextNote))
            {
                indicator.FreeTextNote = $"{indicator.FreeTextNote}\n{input.FreeTextNote}";
            }
            else
            {
                indicator.FreeTextNote = input.FreeTextNote;
            }

            // 2. Update MasterDataNoteIds for selected indicators
            // Skip if SkipMasterDataUpdate is true (giữ nguyên MasterDataNoteIds)
            if (!input.SkipMasterDataUpdate)
            {
                if (input.AppendMode && !string.IsNullOrEmpty(indicator.MasterDataNoteIds) && masterDataNoteIdsString != null)
                {
                    // Append: Merge existing and new note IDs (avoid duplicates)
                    var existingIds = indicator.MasterDataNoteIds.Split(',').ToHashSet();
                    var newIds = input.MasterDataNoteIds!.Select(id => id.ToString()).ToList();
                    foreach (var id in newIds)
                    {
                        existingIds.Add(id);
                    }
                    indicator.MasterDataNoteIds = string.Join(",", existingIds);
                }
                else
                {
                    // Replace mode hoặc không có MasterDataNoteIds cũ
                    // Nếu masterDataNoteIdsString là null hoặc empty thì xóa MasterDataNoteIds
                    indicator.MasterDataNoteIds = masterDataNoteIdsString;
                }
            }
        }

        await _laboratoryTaskIndicatorRepository.UpdateManyAsync(indicators);
    }

    [Authorize]
    public async Task<LaboratoryTaskDto> SendResultsAsync(Guid taskId)
    {
        var task = await _laboratoryTaskRepository.GetAsync(taskId);
        var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => x.LaboratoryTaskId == taskId);

        // Filter out finalized indicators - they should not block sending results
        // - CustomerCancelled: KH đã hủy chỉ tiêu
        // - TestingRejected với CustomerConfirmationDate: QLKT từ chối TN và KH đã xác nhận
        var activeIndicators = taskIndicators
            .Where(ti => ti.ApprovalStatus != IndicatorApprovalStatus.CustomerCancelled &&
                        !(ti.ApprovalStatus == IndicatorApprovalStatus.TestingRejected && ti.CustomerConfirmationDate.HasValue))
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
    /// <returns>QualityManagementTask ID</returns>
    private async Task<Guid> CreateQualityManagementTaskAsync(LaboratoryTask labTask, List<LaboratoryTaskIndicator> indicators, Guid? qualityManagerId = null)
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
                await _qualityIndicatorRepository.UpdateAsync(qualityIndicator, autoSave: true);

                // Create history record for resubmission
                var history = new QualityIndicatorStatusHistory(
                    GuidGenerator.Create(),
                    qualityIndicator.Id,
                    oldStatus,
                    QualityIndicatorApprovalStatus.PendingApproval,
                    "Gửi lại kết quả sau khi bị từ chối"
                );
                await _qualityIndicatorHistoryRepository.InsertAsync(history, autoSave: true);
            }

            // Update QualityManagementTask status back to PendingApproval
            if (existingTask != null && existingTask.Status == QualityManagementStatus.Rejected)
            {
                existingTask.Status = QualityManagementStatus.PendingApproval;
                await _qualityTaskRepository.UpdateAsync(existingTask, autoSave: true);
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
                await _qualityTaskRepository.UpdateAsync(qualityTask, autoSave: true);
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
                await _qualityTaskRepository.InsertAsync(qualityTask, autoSave: true);
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

                // Copy StandardId from LaboratoryTaskIndicator (ReferenceLimit not used - we use comparisonOperator + allowedLimit from LaboratoryTaskIndicator)
                if (labIndicator.StandardId.HasValue)
                {
                    qualityIndicator.StandardId = labIndicator.StandardId;
                }

                await _qualityIndicatorRepository.InsertAsync(qualityIndicator, autoSave: true);
            }

            return qualityTask.Id;
        }

        // Return existing task ID (if no new indicators were created)
        if (existingTask != null)
        {
            return existingTask.Id;
        }

        // Should not reach here, but return Guid.Empty as fallback
        return Guid.Empty;
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
        // Get all active users filtered by Position (Nhân viên, Thử nghiệm viên)
        var users = await _userRepository.GetListAsync();
        var technicians = new List<UserLookupDto>();

        foreach (var user in users.Where(u => u.IsActive))
        {
            // Filter by position
            var position = user.GetProperty<string>("Position");
            if (string.IsNullOrWhiteSpace(position) || !TechnicianPositions.Contains(position))
            {
                continue;
            }

            technicians.Add(new UserLookupDto
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

        return new ListResultDto<UserLookupDto>(technicians.OrderBy(u => u.DisplayName).ToList());
    }

    public async Task<ListResultDto<UserLookupDto>> GetTechniciansByTaskAsync(Guid taskId)
    {
        // Get task to find QLKT's department
        var task = await _laboratoryTaskRepository.GetAsync(taskId);
        Guid? qlktDepartmentId = null;

        if (task.TechnicalManagerId.HasValue)
        {
            var qlktUser = await _userRepository.GetAsync(task.TechnicalManagerId.Value);
            qlktDepartmentId = qlktUser.GetProperty<Guid?>("DepartmentId");
        }

        // Get all active users
        var users = await _userRepository.GetListAsync();
        var technicians = new List<UserLookupDto>();

        foreach (var user in users.Where(u => u.IsActive))
        {
            // Filter by position: only "Nhân viên" or "Thử nghiệm viên"
            var position = user.GetProperty<string>("Position");
            if (string.IsNullOrWhiteSpace(position) || !TechnicianPositions.Contains(position))
            {
                continue;
            }

            // Filter by same department as QLKT (if QLKT has department)
            if (qlktDepartmentId.HasValue)
            {
                var userDepartmentId = user.GetProperty<Guid?>("DepartmentId");
                if (!userDepartmentId.HasValue || userDepartmentId.Value != qlktDepartmentId.Value)
                {
                    continue;
                }
            }

            technicians.Add(new UserLookupDto
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

        return new ListResultDto<UserLookupDto>(technicians.OrderBy(u => u.DisplayName).ToList());
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Tự động cập nhật trạng thái task dựa trên trạng thái các indicators
    /// Logic: Ưu tiên trạng thái THẤP NHẤT (phải hoàn thành TẤT CẢ mới lên status cao hơn)
    /// Ngoại trừ PendingApproval - ưu tiên hiển thị khi có chỉ tiêu chờ duyệt (cần action từ QLKT)
    ///
    /// Thứ tự kiểm tra:
    /// 1. Chờ duyệt kết quả (3): Có ≥1 chỉ tiêu Entered → hiển thị ngay (cần QLKT action)
    /// 2. Đã gửi QLCL (5): TẤT CẢ chỉ tiêu đã gửi QLCL
    /// 3. Đã duyệt (4): TẤT CẢ chỉ tiêu đã được Approved
    /// 4. Đang thử nghiệm (2): TẤT CẢ đã giao việc + có ≥1 đang test
    /// 5. Đã giao việc (1): TẤT CẢ chỉ tiêu đã có method + KTV
    /// 6. Chưa giao việc (0): Còn chỉ tiêu chưa được giao việc
    /// </summary>
    private async Task UpdateTaskStatusAsync(LaboratoryTask task)
    {
        var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => x.LaboratoryTaskId == task.Id);

        if (!taskIndicators.Any())
        {
            return;
        }

        // Filter out CustomerCancelled and TestingRejected (with customer confirmation) indicators
        // - CustomerCancelled: KH đã hủy chỉ tiêu
        // - TestingRejected with CustomerConfirmationDate: QLKT từ chối TN và KH đã xác nhận đồng ý
        // These should not affect task status calculation (considered as "completed/finalized")
        var filteredIndicators = taskIndicators
            .Where(ti => ti.ApprovalStatus != IndicatorApprovalStatus.CustomerCancelled &&
                        !(ti.ApprovalStatus == IndicatorApprovalStatus.TestingRejected && ti.CustomerConfirmationDate.HasValue))
            .ToList();

        // Filter out parent indicators with children (they are display-only, not for processing)
        // Only count sub-indicators and standalone indicators for status calculation
        var activeIndicators = await FilterIndicatorsForStatusCalculationAsync(filteredIndicators);

        // If all indicators are cancelled/rejected or are parents, keep current status or set to a default
        if (!activeIndicators.Any())
        {
            return;
        }

        // Check conditions for status calculation
        // Logic mới: Ưu tiên trạng thái THẤP NHẤT (phải hoàn thành tất cả mới lên status cao hơn)
        // Ngoại trừ PendingApproval - cần hiển thị ngay khi có chỉ tiêu chờ duyệt (cần action từ QLKT)
        // Ngoại trừ CannotTest/SupplementaryRequested - cần QLKT đánh giá

        // TẤT CẢ chỉ tiêu đã gửi QLCL
        var allSentToQuality = activeIndicators.All(ti =>
            ti.ApprovalStatus == IndicatorApprovalStatus.Approved && ti.IsSentToQuality);

        // TẤT CẢ chỉ tiêu đã được duyệt (Approved)
        var allApproved = activeIndicators.All(ti => ti.ApprovalStatus == IndicatorApprovalStatus.Approved);

        // Có ≥1 chỉ tiêu đã nhập KQ (Entered - chờ duyệt) - ưu tiên hiển thị vì cần action
        var anyEntered = activeIndicators.Any(ti => ti.ApprovalStatus == IndicatorApprovalStatus.Entered);

        // Có ≥1 chỉ tiêu CannotTest hoặc SupplementaryRequested - cần QLKT đánh giá
        var anyNeedEvaluation = activeIndicators.Any(ti =>
            ti.ApprovalStatus == IndicatorApprovalStatus.CannotTest ||
            ti.ApprovalStatus == IndicatorApprovalStatus.SupplementaryRequested);

        // TẤT CẢ chỉ tiêu đã được giao việc (có method + KTV)
        // Chỉ xét các chỉ tiêu AcceptedForTesting hoặc MethodChanged (đã được QLKT chấp nhận)
        var acceptedIndicators = activeIndicators.Where(ti =>
            ti.ApprovalStatus == IndicatorApprovalStatus.AcceptedForTesting ||
            ti.ApprovalStatus == IndicatorApprovalStatus.MethodChanged ||
            ti.ApprovalStatus == IndicatorApprovalStatus.Pending ||
            ti.ApprovalStatus == IndicatorApprovalStatus.Rejected ||
            ti.ApprovalStatus == IndicatorApprovalStatus.Entered ||
            ti.ApprovalStatus == IndicatorApprovalStatus.Approved).ToList();

        var allAssigned = acceptedIndicators.Any() && acceptedIndicators.All(ti => ti.MethodId.HasValue && ti.TechnicianId.HasValue);

        // Có ≥1 chỉ tiêu đang thử nghiệm (có testDate, KTV đã check-in, status = Pending hoặc AcceptedForTesting)
        // Chỉ tính những chỉ tiêu đang "testing" thực sự, không tính Approved/Entered/Rejected
        var anyTesting = activeIndicators.Any(ti =>
            ti.TestDate.HasValue &&
            (ti.ApprovalStatus == IndicatorApprovalStatus.Pending ||
             ti.ApprovalStatus == IndicatorApprovalStatus.AcceptedForTesting));

        // Check trạng thái workflow đánh giá
        // Tất cả chỉ tiêu đã được QLKT đánh giá (AcceptedForTesting, MethodChanged, TestingRejected, hoặc đã approved)
        var allEvaluated = activeIndicators.All(ti =>
            ti.ApprovalStatus == IndicatorApprovalStatus.AcceptedForTesting ||
            ti.ApprovalStatus == IndicatorApprovalStatus.MethodChanged ||
            ti.ApprovalStatus == IndicatorApprovalStatus.TestingRejected ||
            ti.ApprovalStatus == IndicatorApprovalStatus.Approved);

        // Tất cả được chấp nhận thử nghiệm (không có TestingRejected)
        var allAcceptedForTesting = activeIndicators.All(ti =>
            ti.ApprovalStatus == IndicatorApprovalStatus.AcceptedForTesting ||
            ti.ApprovalStatus == IndicatorApprovalStatus.MethodChanged ||
            ti.ApprovalStatus == IndicatorApprovalStatus.Approved);

        // Có ít nhất 1 chỉ tiêu được chấp nhận (cho PartialAccepted)
        var anyAcceptedForTesting = activeIndicators.Any(ti =>
            ti.ApprovalStatus == IndicatorApprovalStatus.AcceptedForTesting ||
            ti.ApprovalStatus == IndicatorApprovalStatus.MethodChanged ||
            ti.ApprovalStatus == IndicatorApprovalStatus.Approved);

        // Có chỉ tiêu đang chờ KH xác nhận (MethodChangeRequested hoặc TestingRejected chưa confirm)
        var anyWaitingCustomer = activeIndicators.Any(ti =>
            ti.ApprovalStatus == IndicatorApprovalStatus.MethodChangeRequested);

        // Có ≥1 chỉ tiêu KTV báo không TN được - cần QLKT xử lý
        var anyCannotTest = activeIndicators.Any(ti =>
            ti.ApprovalStatus == IndicatorApprovalStatus.CannotTest);

        LaboratoryTaskStatus newStatus;

        // Logic ưu tiên:
        // 1. CannotTest/SupplementaryRequested ưu tiên cao nhất - cần QLKT đánh giá ngay
        // 2. PendingApproval được ưu tiên hiển thị khi có chỉ tiêu chờ duyệt (cần QLKT action)
        // 3. Các status khác phải TẤT CẢ chỉ tiêu đạt được mới chuyển lên

        if (anyNeedEvaluation)
        {
            // Có ≥1 chỉ tiêu CannotTest/SupplementaryRequested - cần QLKT đánh giá
            newStatus = LaboratoryTaskStatus.UnderEvaluation;
        }
        else if (anyEntered)
        {
            // Có ≥1 chỉ tiêu chờ duyệt - ưu tiên hiển thị vì cần QLKT duyệt
            newStatus = LaboratoryTaskStatus.WaitingApproval;
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
            newStatus = LaboratoryTaskStatus.InProgress;
        }
        else if (allAssigned)
        {
            // TẤT CẢ chỉ tiêu đã được giao việc (chưa có ai test)
            newStatus = LaboratoryTaskStatus.Assigned;
        }
        else if (anyWaitingCustomer)
        {
            // Có chỉ tiêu đang chờ KH xác nhận
            newStatus = LaboratoryTaskStatus.WaitingCustomerResponse;
        }
        else if (allEvaluated && allAcceptedForTesting)
        {
            // Tất cả chỉ tiêu đã được đánh giá và chấp nhận thử nghiệm
            newStatus = LaboratoryTaskStatus.FullyAccepted;
        }
        else if (allEvaluated && anyAcceptedForTesting)
        {
            // Một phần được chấp nhận, một phần bị từ chối
            newStatus = LaboratoryTaskStatus.PartialAccepted;
        }
        else if (allEvaluated)
        {
            // Tất cả bị từ chối thử nghiệm
            newStatus = LaboratoryTaskStatus.TestingRejected;
        }
        else
        {
            // Còn chỉ tiêu chưa được đánh giá
            newStatus = LaboratoryTaskStatus.UnderEvaluation;
        }

        if (task.Status != newStatus)
        {
            var oldStatus = task.Status;
            task.Status = newStatus;
            await _laboratoryTaskRepository.UpdateAsync(task);

            // Send notifications for shared sample status changes
            await SendSharedSampleStatusNotificationAsync(task, oldStatus, newStatus);
        }
    }

    /// <summary>
    /// Gửi notification cho Lab Hóa khi Lab Vi Sinh thay đổi trạng thái (shared sample)
    /// </summary>
    private async Task SendSharedSampleStatusNotificationAsync(LaboratoryTask task, LaboratoryTaskStatus oldStatus, LaboratoryTaskStatus newStatus)
    {
        // Only send for shared sample tasks from microbiology lab
        if (!task.IsSharedSampleTask)
        {
            return;
        }

        // Check if this is the microbiology lab (priority lab)
        var laboratory = await _laboratoryRepository.GetAsync(task.LaboratoryId);
        var labDepartment = await _departmentRepository.FindAsync(laboratory.DepartmentId);
        var labName = labDepartment?.Name ?? "";
        var isMicrobiologyLab = laboratory.Code?.Equals("LVS", StringComparison.OrdinalIgnoreCase) == true ||
                                labName.Contains("Vi sinh", StringComparison.OrdinalIgnoreCase) ||
                                labName.Contains("Vi Sinh", StringComparison.OrdinalIgnoreCase);

        if (!isMicrobiologyLab)
        {
            return; // Only microbiology lab sends notifications to other labs
        }

        // Get waiting tasks (Lab Hóa) that reference this task
        var waitingTasks = await _laboratoryTaskRepository.GetListAsync(
            t => t.PriorLabTaskId == task.Id);

        if (!waitingTasks.Any())
        {
            return;
        }

        // Get sample codes for notification message
        var testRequest = await _testRequestRepository.GetAsync(task.TestRequestId);
        var samples = await _sampleRepository.GetListAsync(s => s.TestRequestId == testRequest.Id && s.IsSharedSample);
        var sampleCodes = string.Join(", ", samples.Select(s => s.SampleCode));

        // Determine notification message based on status change
        string? notificationType = null;
        string? title = null;
        string? message = null;

        if (newStatus == LaboratoryTaskStatus.UnderEvaluation && oldStatus != LaboratoryTaskStatus.UnderEvaluation)
        {
            notificationType = "SharedSampleUnderReview";
            title = "Mẫu chung đang được xem xét";
            message = $"{sampleCodes} mẫu chung đang được xem xét yêu cầu thử nghiệm";
        }
        else if (newStatus == LaboratoryTaskStatus.InProgress)
        {
            notificationType = "SharedSampleInProgress";
            title = "Mẫu chung đang được thử nghiệm";
            message = $"{sampleCodes} mẫu chung đang được thử nghiệm";
        }
        else if (newStatus == LaboratoryTaskStatus.Approved)
        {
            notificationType = "SharedSampleCompleted";
            title = "Mẫu chung đã thử nghiệm xong";
            message = $"{sampleCodes} mẫu chung đã thử nghiệm xong";
        }

        // TODO: Add notification for shared sample status change when INotificationCreator interface is updated
        // Notification types: SharedSampleUnderReview, SharedSampleInProgress, SharedSampleCompleted
    }

    /// <summary>
    /// Đồng bộ trạng thái TestRequest dựa trên trạng thái các indicators của tất cả tasks
    /// Logic: Chỉ chuyển SampleSent → InProgress khi có KTV check-in (có testDate)
    /// PendingApproval chỉ được set khi QLKT gửi KQ cho QLCL (TryUpdateTestRequestToPendingApprovalAsync)
    /// </summary>
    private async Task UpdateTestRequestStatusAsync(Guid testRequestId)
    {
        var testRequest = await _testRequestRepository.FindAsync(testRequestId);
        if (testRequest == null) return;

        // Chỉ cập nhật khi status đang ở SampleSent
        // Không thay đổi nếu đã ở: InProgress, PendingChangeApproval, PendingApproval, Approved, ResultSent, Completed
        if (testRequest.Status != TestRequestStatus.SampleSent)
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

        // Loại trừ các chỉ tiêu đã "finalized":
        // - CustomerCancelled: KH đã hủy chỉ tiêu
        // - TestingRejected với CustomerConfirmationDate: QLKT từ chối TN và KH đã xác nhận
        var activeIndicators = allIndicators
            .Where(ti => ti.ApprovalStatus != IndicatorApprovalStatus.CustomerCancelled &&
                        !(ti.ApprovalStatus == IndicatorApprovalStatus.TestingRejected && ti.CustomerConfirmationDate.HasValue))
            .ToList();

        if (!activeIndicators.Any()) return;

        // Chỉ chuyển sang InProgress khi có ≥1 KTV đã check-in (có testDate)
        var anyTesting = activeIndicators.Any(ti => ti.TestDate.HasValue);

        if (anyTesting)
        {
            testRequest.Status = TestRequestStatus.InProgress;
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
        return status == LaboratoryTaskStatus.UnderEvaluation ||
               status == LaboratoryTaskStatus.Assigned;
    }

    /// <summary>
    /// Lấy nhãn tiếng Việt cho trạng thái
    /// </summary>
    private static string GetStatusLabel(LaboratoryTaskStatus status)
    {
        return status switch
        {
            LaboratoryTaskStatus.UnderEvaluation => "Chưa giao việc",
            LaboratoryTaskStatus.Assigned => "Đã giao việc",
            LaboratoryTaskStatus.InProgress => "Đang thử nghiệm",
            LaboratoryTaskStatus.WaitingApproval => "Chờ duyệt kết quả",
            LaboratoryTaskStatus.Approved => "Đã duyệt",
            LaboratoryTaskStatus.SentToQuality => "Đã gửi QL Chất lượng",
            _ => status.ToString()
        };
    }

    private static bool IsDashMethodName(string? methodName)
    {
        return string.Equals(methodName?.Trim(), "-", StringComparison.Ordinal);
    }

    /// <summary>
    /// Cập nhật TestRequest sang PendingApproval khi có chỉ tiêu được gửi đến QLCL
    /// Chỉ cần gửi một số chỉ tiêu là chuyển sang PendingApproval ngay
    /// </summary>
    private async Task TryUpdateTestRequestToPendingApprovalAsync(Guid testRequestId)
    {
        // Chuyển sang PendingApproval ngay khi có chỉ tiêu được gửi đến QLCL
        await _testRequestAppService.UpdateToPendingApprovalAsync(testRequestId);
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
        string? methodFormattedName = null;
        string? unit = taskIndicator.Unit;
        var isSubContractMethod = !string.IsNullOrWhiteSpace(taskIndicator.SubContractMethodName);
        if (taskIndicator.MethodId.HasValue)
        {
            var method = await _testMethodRepository.FindAsync(taskIndicator.MethodId.Value);
            if (!isSubContractMethod)
            {
                methodName = method?.Name;
                methodFormattedName = method?.FormattedName;
            }
            isSubContractMethod = isSubContractMethod || IsDashMethodName(method?.Name);
            // Fallback to TestMethod.FormattedUnit/Unit if LaboratoryTaskIndicator.Unit is not set
            if (string.IsNullOrEmpty(unit) && method != null)
            {
                unit = method.FormattedUnit ?? method.Unit;
            }
        }

        if (!string.IsNullOrWhiteSpace(taskIndicator.SubContractMethodName))
        {
            methodName = taskIndicator.SubContractMethodName;
            methodFormattedName = null;
        }

        // Get indicator images
        var indicatorImages = await _indicatorImageRepository.GetListAsync(x =>
            x.LaboratoryTaskIndicatorId == taskIndicator.Id);

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
            IndicatorFormattedName = indicator?.FormattedName,
            IndicatorGroupName = indicatorGroupName,
            ParentIndicatorId = indicator?.ParentIndicatorId,
            UnitPrice = sampleIndicator.UnitPrice,
            // Phase 6 workflow fields
            MethodId = taskIndicator.MethodId,
            MethodName = methodName,
            MethodFormattedName = methodFormattedName,
            IsSubContractMethod = isSubContractMethod,
            SubContractMethodName = taskIndicator.SubContractMethodName,
            SubContractNumber = taskIndicator.SubContractNumber,
            SubContractCost = taskIndicator.SubContractCost,
            TechnicianId = taskIndicator.TechnicianId,
            TechnicianName = technicianName,
            AssignedDate = taskIndicator.AssignedDate,
            TestDate = taskIndicator.TestDate,
            Result = taskIndicator.Result,
            FreeTextNote = taskIndicator.FreeTextNote,
            MasterDataNoteIds = taskIndicator.MasterDataNoteIds,
            MasterDataNoteContents = await GetMasterDataNoteContentsAsync(taskIndicator.MasterDataNoteIds),
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
            // Supplementary Request fields
            SupplementaryRequestReason = taskIndicator.SupplementaryRequestReason,
            SupplementaryRequestDate = taskIndicator.SupplementaryRequestDate,
            SupplementaryRequestedById = taskIndicator.SupplementaryRequestedById,
            SupplementaryRequestedByName = taskIndicator.SupplementaryRequestedByName,
            Images = indicatorImages.OrderBy(x => x.CreationTime).Select(x => new Testing.IndicatorImageDto
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
            // Shared Sample - waiting flag
            IsWaitingForSharedSample = taskIndicator.IsWaitingForSharedSample
        };
    }

    /// <summary>
    /// Lấy danh sách nội dung Master Data Notes từ comma-separated IDs
    /// </summary>
    private async Task<List<string>?> GetMasterDataNoteContentsAsync(string? masterDataNoteIds)
    {
        if (string.IsNullOrEmpty(masterDataNoteIds))
            return null;

        var noteIdStrings = masterDataNoteIds.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var noteIds = noteIdStrings
            .Select(s => Guid.TryParse(s.Trim(), out var id) ? id : (Guid?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();

        if (!noteIds.Any())
            return null;

        var notes = await _noteRepository.GetListAsync(n => noteIds.Contains(n.Id));
        return notes.OrderBy(n => noteIds.IndexOf(n.Id)).Select(n => n.Content).ToList();
    }

    /// <summary>
    /// Parse comma-separated note IDs string to list of Guids
    /// </summary>
    private static List<Guid> ParseNoteIds(string? noteIdsString)
    {
        if (string.IsNullOrEmpty(noteIdsString))
            return new List<Guid>();

        return noteIdsString
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => Guid.TryParse(s.Trim(), out var id) ? id : (Guid?)null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .ToList();
    }

    /// <summary>
    /// Get Master Data Note contents from pre-loaded cache dictionary
    /// </summary>
    private static List<string>? GetMasterDataNoteContentsFromCache(string? masterDataNoteIds, Dictionary<Guid, Note> notesCache)
    {
        if (string.IsNullOrEmpty(masterDataNoteIds))
            return null;

        var noteIds = ParseNoteIds(masterDataNoteIds);
        if (noteIds.Count == 0)
            return null;

        var contents = noteIds
            .Where(id => notesCache.ContainsKey(id))
            .Select(id => notesCache[id].Content)
            .ToList();

        return contents.Count > 0 ? contents : null;
    }

    /// <summary>
    /// Calculate Test Date Range from Min/Max test dates
    /// Returns: "dd/MM/yyyy - dd/MM/yyyy" where:
    /// - Start date = Min(TestDate) of all indicators (thời gian thử nghiệm sớm nhất)
    /// - End date = Max(TestDate) of all indicators (thời gian thử nghiệm muộn nhất)
    /// Always displays both dates even if they are the same (e.g., "07/01/2025 - 07/01/2025")
    /// Giống logic trong ResultFormAppService.CalculateTimeOfTesting
    /// </summary>
    private static string? CalculateTestDateRange(DateTime? minTestDate, DateTime? maxTestDate)
    {
        if (!minTestDate.HasValue && !maxTestDate.HasValue)
        {
            return null;
        }

        if (!minTestDate.HasValue)
        {
            // Only have max date, show it twice
            return $"{maxTestDate:dd/MM/yyyy} - {maxTestDate:dd/MM/yyyy}";
        }

        if (!maxTestDate.HasValue)
        {
            // Only have min date, show it twice
            return $"{minTestDate:dd/MM/yyyy} - {minTestDate:dd/MM/yyyy}";
        }

        // Always show both dates (even if same day: "07/01/2025 - 07/01/2025")
        return $"{minTestDate:dd/MM/yyyy} - {maxTestDate:dd/MM/yyyy}";
    }

    /// <summary>
    /// Tự động duyệt chỉ tiêu cha khi chỉ tiêu phụ được duyệt.
    /// Logic: Khi duyệt sub-indicator, cần auto-approve parent để QLCL có thể group đúng.
    /// </summary>
    /// <param name="subIndicatorTaskIndicator">LaboratoryTaskIndicator của chỉ tiêu phụ vừa được duyệt</param>
    /// <param name="task">LaboratoryTask hiện tại</param>
    /// <returns>Parent LaboratoryTaskIndicator nếu được auto-approve, null nếu không cần</returns>
    private async Task<LaboratoryTaskIndicator?> AutoApproveParentIndicatorIfNeededAsync(
        LaboratoryTaskIndicator subIndicatorTaskIndicator,
        LaboratoryTask task)
    {
        // 1. Lấy TestRequestSampleIndicator để biết IndicatorId và SampleId
        var sampleIndicator = await _sampleIndicatorRepository.FindAsync(subIndicatorTaskIndicator.TestRequestSampleIndicatorId);
        if (sampleIndicator == null) return null;

        // Lưu lại SampleId của sub-indicator để tìm parent cùng sample
        var subIndicatorSampleId = sampleIndicator.TestRequestSampleId;

        // 2. Lấy Indicator và kiểm tra có ParentIndicatorId không
        var indicator = await _indicatorRepository.FindAsync(sampleIndicator.IndicatorId);
        if (indicator == null || !indicator.ParentIndicatorId.HasValue) return null;

        var parentIndicatorId = indicator.ParentIndicatorId.Value;

        // 3. Tìm tất cả LaboratoryTaskIndicator của task với cùng method
        var allTaskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(
            ti => ti.LaboratoryTaskId == task.Id &&
                  ti.MethodId == subIndicatorTaskIndicator.MethodId);

        // 4. Tìm parent's LaboratoryTaskIndicator CÙNG SAMPLE với sub-indicator
        foreach (var taskIndicator in allTaskIndicators)
        {
            var si = await _sampleIndicatorRepository.FindAsync(taskIndicator.TestRequestSampleIndicatorId);
            // Phải check cả IndicatorId VÀ cùng SampleId
            if (si?.IndicatorId == parentIndicatorId && si?.TestRequestSampleId == subIndicatorSampleId)
            {
                // Nếu parent chưa được approve, auto-approve nó
                if (taskIndicator.ApprovalStatus != IndicatorApprovalStatus.Approved)
                {
                    taskIndicator.ApprovalStatus = IndicatorApprovalStatus.Approved;
                    taskIndicator.ApprovalDate = DateTime.Now;
                    taskIndicator.ApprovedById = CurrentUser.Id;
                    taskIndicator.RejectionReason = null;
                    taskIndicator.IsSentToQuality = true;

                    await _laboratoryTaskIndicatorRepository.UpdateAsync(taskIndicator, autoSave: true);
                    return taskIndicator;
                }
                break;
            }
        }

        return null;
    }

    /// <summary>
    /// Lọc ra các chỉ tiêu cần tính toán status (loại bỏ parent indicators có children).
    /// Parent indicators có children chỉ để hiển thị, không cần xử lý (testing, approve).
    /// Chỉ tính: sub-indicators và standalone indicators (không có children).
    /// </summary>
    /// <param name="taskIndicators">Danh sách tất cả LaboratoryTaskIndicator của task</param>
    /// <returns>Danh sách các indicators cần tính toán status</returns>
    private async Task<List<LaboratoryTaskIndicator>> FilterIndicatorsForStatusCalculationAsync(
        List<LaboratoryTaskIndicator> taskIndicators)
    {
        if (!taskIndicators.Any())
        {
            return taskIndicators;
        }

        // Bước 1: Lấy tất cả TestRequestSampleIndicator IDs
        var sampleIndicatorIds = taskIndicators.Select(ti => ti.TestRequestSampleIndicatorId).Distinct().ToList();

        // Bước 2: Query tất cả SampleIndicators một lần
        var sampleIndicators = await _sampleIndicatorRepository.GetListAsync(
            si => sampleIndicatorIds.Contains(si.Id));
        var sampleIndicatorMap = sampleIndicators.ToDictionary(si => si.Id, si => si.IndicatorId);

        // Bước 3: Lấy tất cả IndicatorIds
        var indicatorIds = sampleIndicatorMap.Values.Distinct().ToList();

        // Bước 4: Query tất cả Indicators một lần
        var indicators = await _indicatorRepository.GetListAsync(ind => indicatorIds.Contains(ind.Id));
        var indicatorMap = indicators.ToDictionary(ind => ind.Id, ind => ind);

        // Bước 5: Xây dựng mapping taskIndicatorId -> indicatorId
        var taskIndicatorToIndicatorId = new Dictionary<Guid, Guid>();
        foreach (var ti in taskIndicators)
        {
            if (sampleIndicatorMap.TryGetValue(ti.TestRequestSampleIndicatorId, out var indicatorId))
            {
                taskIndicatorToIndicatorId[ti.Id] = indicatorId;
            }
        }

        // Bước 6: Xác định các parent indicators có children trong cùng task
        // Composite key: indicatorId + methodId
        var parentIndicatorKeys = new HashSet<string>();

        // Tìm tất cả các parent indicator IDs từ các sub-indicators
        foreach (var ti in taskIndicators)
        {
            if (!taskIndicatorToIndicatorId.TryGetValue(ti.Id, out var indicatorId)) continue;
            if (!indicatorMap.TryGetValue(indicatorId, out var indicator)) continue;

            if (indicator.ParentIndicatorId.HasValue)
            {
                // Đây là sub-indicator, đánh dấu parent của nó
                var parentKey = $"{indicator.ParentIndicatorId.Value}_{ti.MethodId}";
                parentIndicatorKeys.Add(parentKey);
            }
        }

        // Bước 7: Lọc ra các indicators cần tính status
        // Loại bỏ các parent indicators có children (tức là có trong parentIndicatorKeys)
        var result = new List<LaboratoryTaskIndicator>();
        foreach (var ti in taskIndicators)
        {
            if (!taskIndicatorToIndicatorId.TryGetValue(ti.Id, out var indicatorId))
            {
                // Không tìm thấy mapping, vẫn giữ lại (edge case)
                result.Add(ti);
                continue;
            }

            var key = $"{indicatorId}_{ti.MethodId}";
            if (parentIndicatorKeys.Contains(key))
            {
                // Đây là parent indicator có children -> skip
                continue;
            }

            // Đây là sub-indicator hoặc standalone indicator -> giữ lại
            result.Add(ti);
        }

        return result;
    }

    #endregion

    // Supporter Management methods moved to LaboratoryTaskAppService.Supporters.cs

    #region Bulk Approval

    [Authorize]
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

        // Validate: Check if there are any quality managers before approving
        if (input.Approve)
        {
            var qualityManagerIds = await GetQualityManagerIdsAsync(task);
            if (!qualityManagerIds.Any())
            {
                throw new Common.Exceptions.InvalidOperationException(
                    "Không thể duyệt kết quả vì không có Quản lý chất lượng nào trong hệ thống. Vui lòng liên hệ quản trị viên để thêm người dùng có vai trò Quản lý chất lượng.", "qualityManager");
            }
        }

        var results = new List<LaboratoryTaskIndicatorDto>();
        var approvedIndicators = new List<LaboratoryTaskIndicator>();
        var autoApprovedParentIds = new HashSet<Guid>(); // Track đã auto-approve parents nào để tránh duplicate

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

                // Auto send to QLCL - set IsSentToQuality = true
                taskIndicator.IsSentToQuality = true;

                // Add to approvedIndicators list for later processing
                approvedIndicators.Add(taskIndicator);

                // Auto-approve parent indicator nếu đây là sub-indicator
                var autoApprovedParent = await AutoApproveParentIndicatorIfNeededAsync(taskIndicator, task);
                if (autoApprovedParent != null && !autoApprovedParentIds.Contains(autoApprovedParent.Id))
                {
                    autoApprovedParentIds.Add(autoApprovedParent.Id);
                    approvedIndicators.Add(autoApprovedParent);
                }
            }
            else
            {
                taskIndicator.ApprovalStatus = IndicatorApprovalStatus.Rejected;
                taskIndicator.RejectionReason = input.RejectionReason;
                // Không set ApprovalDate khi từ chối - chỉ set khi duyệt thành công
                taskIndicator.ApprovedById = CurrentUser.Id;

                // Save rejection history
                var rejectionHistory = new IndicatorRejectionHistory(
                    GuidGenerator.Create(),
                    indicatorId,
                    input.RejectionReason!,
                    CurrentUser.Id!.Value,
                    Clock.Now
                );
                await _rejectionHistoryRepository.InsertAsync(rejectionHistory, autoSave: true);
            }

            await _laboratoryTaskIndicatorRepository.UpdateAsync(taskIndicator, autoSave: true);
            results.Add(await MapToIndicatorDtoAsync(taskIndicator));
        }

        // Nếu approve, tạo QualityManagementIndicator và gửi thông báo đến tất cả QLCL
        if (input.Approve && approvedIndicators.Any())
        {
            await CreateQualityManagementTaskAsync(task, approvedIndicators);

            // Update task status to SentToQuality nếu tất cả đã được gửi
            var allTaskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => x.LaboratoryTaskId == task.Id);
            var hasUnsentApproved = allTaskIndicators.Any(ti =>
                ti.ApprovalStatus == IndicatorApprovalStatus.Approved && !ti.IsSentToQuality);

            if (!hasUnsentApproved && task.Status != LaboratoryTaskStatus.SentToQuality)
            {
                task.Status = LaboratoryTaskStatus.SentToQuality;
                await _laboratoryTaskRepository.UpdateAsync(task);
            }
            else
            {
                // Update task status bình thường
                await UpdateTaskStatusAsync(task);
            }

            // Update TestRequest status
            await TryUpdateTestRequestToPendingApprovalAsync(task.TestRequestId);

            // Gửi 1 thông báo tổng hợp đến tất cả QLCL
            await NotifyAllQualityManagersOnBulkApproveAsync(task, approvedIndicators);
        }
        else
        {
            // Update task status
            await UpdateTaskStatusAsync(task);

            // Đồng bộ trạng thái TestRequest
            await UpdateTestRequestStatusAsync(task.TestRequestId);

            // Notify technicians when results are rejected (Case 4 & 5 KTV)
            if (!input.Approve)
            {
                await NotifyTechniciansOnRejectAsync(task, input.IndicatorIds);
            }
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
                    task.Id,
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
                    task.Id,
                    technicalManagerName
                );
            }
        }
    }

    #endregion

    #region Send to Quality Management

    [Authorize]
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

        // Mark selected indicators as sent to quality and save/update notes
        foreach (var indicator in selectedIndicators)
        {
            indicator.IsSentToQuality = true;

            // Check if note already exists for this TestRequestSampleIndicatorId
            var existingNote = await _sampleIndicatorNoteRepository.FirstOrDefaultAsync(
                x => x.TestRequestSampleIndicatorId == indicator.TestRequestSampleIndicatorId
            );

            if (existingNote != null)
            {
                // Update existing note
                existingNote.NoteContent = input.NoteContent;
                existingNote.SelectedNoteIds = input.SelectedNoteIds != null && input.SelectedNoteIds.Any()
                    ? System.Text.Json.JsonSerializer.Serialize(input.SelectedNoteIds)
                    : null;
                existingNote.CreatedDate = Clock.Now;
                existingNote.CreatedBy = CurrentUser.Id.Value;

                await _sampleIndicatorNoteRepository.UpdateAsync(existingNote);
            }
            else
            {
                // Create new note
                var sampleIndicatorNote = new SampleIndicatorNote(
                    GuidGenerator.Create(),
                    indicator.TestRequestSampleIndicatorId,
                    input.NoteContent,
                    input.SelectedNoteIds != null && input.SelectedNoteIds.Any()
                        ? System.Text.Json.JsonSerializer.Serialize(input.SelectedNoteIds)
                        : null,
                    Clock.Now,
                    CurrentUser.Id.Value
                );
                await _sampleIndicatorNoteRepository.InsertAsync(sampleIndicatorNote);
            }
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

        // Get QualityManagementTask ID for navigation
        var qualityTask = (await _qualityTaskRepository.GetListAsync(
            t => t.TestRequestId == task.TestRequestId && t.TaskType == QualityTaskType.ResultApproval))
            .FirstOrDefault();
        var qualityTaskId = qualityTask?.Id ?? Guid.Empty;

        await _notificationCreator.NotifyResultSubmittedToQualityManagerAsync(
            qualityManagerId,
            testRequest.ContractNumber ?? task.ContractNumber ?? "",
            qualityTaskId,
            senderName,
            indicatorCount,
            sampleName,
            indicatorName,
            methodName
        );
    }

    /// <summary>
    /// Gửi thông báo cho TẤT CẢ QLCL trong nhóm khi QLKT duyệt kết quả (auto send)
    /// Chỉ cần 1 người trong nhóm QLCL duyệt/từ chối là xong
    /// </summary>
    private async Task NotifyAllQualityManagersOnApproveAsync(LaboratoryTask task, LaboratoryTaskIndicator indicator)
    {
        // Get all quality managers for this task
        var qualityManagers = await GetQualityManagerIdsAsync(task);
        if (!qualityManagers.Any())
        {
            return;
        }

        var testRequest = await _testRequestRepository.GetAsync(task.TestRequestId);
        var senderName = CurrentUser.Name ?? CurrentUser.UserName ?? "Quản lý kỹ thuật";

        // Get indicator details
        string? sampleName = null;
        string? indicatorName = null;
        string? methodName = null;

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

        // Get QualityManagementTask ID for navigation
        var qualityTask = (await _qualityTaskRepository.GetListAsync(
            t => t.TestRequestId == task.TestRequestId && t.TaskType == QualityTaskType.ResultApproval))
            .FirstOrDefault();
        var qualityTaskId = qualityTask?.Id ?? Guid.Empty;

        // Notify all quality managers
        foreach (var qualityManagerId in qualityManagers)
        {
            await _notificationCreator.NotifyResultSubmittedToQualityManagerAsync(
                qualityManagerId,
                testRequest.ContractNumber ?? task.ContractNumber ?? "",
                qualityTaskId,
                senderName,
                1, // Single indicator
                sampleName,
                indicatorName,
                methodName
            );
        }
    }

    /// <summary>
    /// Gửi thông báo cho TẤT CẢ QLCL khi duyệt nhiều chỉ tiêu (bulk approve)
    /// </summary>
    private async Task NotifyAllQualityManagersOnBulkApproveAsync(LaboratoryTask task, List<LaboratoryTaskIndicator> indicators)
    {
        var qualityManagers = await GetQualityManagerIdsAsync(task);
        if (!qualityManagers.Any())
        {
            return;
        }

        var testRequest = await _testRequestRepository.GetAsync(task.TestRequestId);
        var senderName = CurrentUser.Name ?? CurrentUser.UserName ?? "Quản lý kỹ thuật";
        var indicatorCount = indicators.Count;

        string? sampleName = null;
        string? indicatorName = null;
        string? methodName = null;

        // For single indicator, get details
        if (indicatorCount == 1)
        {
            var indicator = indicators.First();
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

        // Get QualityManagementTask ID for navigation
        var qualityTask = (await _qualityTaskRepository.GetListAsync(
            t => t.TestRequestId == task.TestRequestId && t.TaskType == QualityTaskType.ResultApproval))
            .FirstOrDefault();
        var qualityTaskId = qualityTask?.Id ?? Guid.Empty;

        // Notify all quality managers
        foreach (var qualityManagerId in qualityManagers)
        {
            await _notificationCreator.NotifyResultSubmittedToQualityManagerAsync(
                qualityManagerId,
                testRequest.ContractNumber ?? task.ContractNumber ?? "",
                qualityTaskId,
                senderName,
                indicatorCount,
                sampleName,
                indicatorName,
                methodName
            );
        }
    }

    /// <summary>
    /// Lấy danh sách ID của tất cả QLCL (role QualityManager và IsActive)
    /// </summary>
    private async Task<List<Guid>> GetQualityManagerIdsAsync(LaboratoryTask task)
    {
        // TODO: Nếu cần lọc theo phòng ban trong tương lai, uncomment đoạn code bên dưới
        // Guid? qlktDepartmentId = null;
        // if (task.TechnicalManagerId.HasValue)
        // {
        //     var qlktUser = await _userRepository.GetAsync(task.TechnicalManagerId.Value);
        //     qlktDepartmentId = qlktUser.GetProperty<Guid?>("DepartmentId");
        // }

        var users = await _userRepository.GetListAsync();
        var qualityManagerIds = new List<Guid>();

        foreach (var user in users.Where(u => u.IsActive))
        {
            var userRoles = await _userRepository.GetRolesAsync(user.Id);
            if (!userRoles.Any(r => QualityManagerRoleNames.Contains(r.Name)))
            {
                continue;
            }

            // TODO: Nếu cần lọc theo phòng ban trong tương lai, uncomment đoạn code bên dưới
            // if (qlktDepartmentId.HasValue)
            // {
            //     var userDepartmentId = user.GetProperty<Guid?>("DepartmentId");
            //     if (userDepartmentId != qlktDepartmentId)
            //     {
            //         continue;
            //     }
            // }

            qualityManagerIds.Add(user.Id);
        }

        return qualityManagerIds;
    }

    [Authorize]
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
            if (!userRoles.Any(r => QualityManagerRoleNames.Contains(r.Name)))
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
                x.Status == LaboratoryTaskStatus.UnderEvaluation ||
                // Condition 2: Cần duyệt (PendingApproval)
                x.Status == LaboratoryTaskStatus.WaitingApproval ||
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

            if (task.Status == LaboratoryTaskStatus.WaitingApproval)
            {
                taskName = "Cần duyệt kết quả";
            }
            else if (task.Status == LaboratoryTaskStatus.UnderEvaluation)
            {
                taskName = "Cần phân công công việc";
            }
            else if (daysUntilDeadline.HasValue && daysUntilDeadline.Value <= 5 &&
                     task.Status != LaboratoryTaskStatus.Approved &&
                     task.Status != LaboratoryTaskStatus.SentToQuality)
            {
                taskName = "Sắp tới ngày trả kết quả thử nghiệm";
            }
            else
            {
                continue;
            }

            result.Add(new LabTechManagerPendingTaskDto
            {
                Id = task.Id,
                ContractNumber = task.ContractNumber ?? "",
                TaskName = taskName,
                TransferDate = task.TransferDate,
                RequestCompletionDate = task.RequestCompletionDate,
                Status = task.Status,
                DaysUntilDeadline = daysUntilDeadline,
                CreationTime = task.CreationTime,
            });
        }

        // --- Indicators needing method setup (only have "-" placeholder method) ---
        await AppendIndicatorsNeedingMethodSetupAsync(currentUserId.Value, result);

        return result
            .OrderByDescending(x => x.CreationTime)
            .ToList();
    }

    private async Task AppendIndicatorsNeedingMethodSetupAsync(
        Guid currentUserId, List<LabTechManagerPendingTaskDto> result)
    {
        var managedLabIds = (await _laboratoryRepository.GetQueryableAsync())
            .Where(l => l.TechnicalManagerId == currentUserId)
            .Select(l => l.Id)
            .ToList();

        if (managedLabIds.Count == 0) return;

        var managedGroupIds = (await _indicatorGroupRepository.GetQueryableAsync())
            .Where(g => g.LaboratoryId.HasValue && managedLabIds.Contains(g.LaboratoryId.Value))
            .Select(g => g.Id)
            .ToList();

        if (managedGroupIds.Count == 0) return;

        var indicators = (await _indicatorRepository.GetQueryableAsync())
            .Where(i => i.NeedsMethodSetup
                        && i.IsActive
                        && i.ParentIndicatorId == null
                        && managedGroupIds.Contains(i.IndicatorGroupId))
            .Select(i => new { i.Id, i.Code, i.Name, i.CreationTime })
            .ToList();

        foreach (var ind in indicators)
        {
            result.Add(new LabTechManagerPendingTaskDto
            {
                Id = ind.Id,
                ContractNumber = ind.Code,
                TaskName = $"Cần cập nhật phương pháp: {ind.Name}",
                TaskType = "IndicatorMethodSetup",
                CreationTime = ind.CreationTime,
            });
        }
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

        // Get all indicators assigned to current user with actionable status
        var indicatorQueryable = await _laboratoryTaskIndicatorRepository.GetQueryableAsync();
        var taskQueryable = await _laboratoryTaskRepository.GetQueryableAsync();

        var indicators = indicatorQueryable
            .Where(x => x.TechnicianId == currentUserId.Value &&
                        (x.ApprovalStatus == IndicatorApprovalStatus.Pending ||
                         x.ApprovalStatus == IndicatorApprovalStatus.AcceptedForTesting ||
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

            var pendingCount = group.Count(x => x.ApprovalStatus == IndicatorApprovalStatus.Pending ||
                                                x.ApprovalStatus == IndicatorApprovalStatus.AcceptedForTesting);
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

    #region Sample Assignment Log (Sổ nhận mẫu giao việc)

    /// <summary>
    /// Lấy thống kê số lượng công việc theo trạng thái (không filter, chỉ filter theo quyền truy cập)
    /// </summary>
    [Authorize]
    public async Task<TaskStatusStatisticsDto> GetTaskStatusStatisticsAsync()
    {
        var queryable = await _laboratoryTaskRepository.GetQueryableAsync();

        // Filter by current user's access (same logic as GetFilteredListAsync)
        var currentUserId = CurrentUser.Id;
        if (currentUserId.HasValue && !CurrentUser.IsInRole(AppConstants.RoleAdmin))
        {
            var supporterTaskIds = (await _supporterRepository.GetListAsync(s => s.UserId == currentUserId.Value))
                .Select(s => s.LaboratoryTaskId)
                .ToHashSet();

            var managedLaboratoryIds = (await _laboratoryRepository.GetListAsync(
                lab => lab.TechnicalManagerId == currentUserId.Value))
                .Select(lab => lab.Id)
                .ToHashSet();

            var departmentSupporterLabIds = (await _departmentSupporterRepository.GetListAsync(
                ds => ds.UserId == currentUserId.Value && ds.ExpirationDate > DateTime.UtcNow))
                .Select(ds => ds.LaboratoryId)
                .ToHashSet();

            queryable = queryable.Where(x =>
                managedLaboratoryIds.Contains(x.LaboratoryId) ||
                x.TechnicalManagerId == currentUserId.Value ||
                (supporterTaskIds.Contains(x.Id) && x.Status != LaboratoryTaskStatus.SentToQuality) ||
                departmentSupporterLabIds.Contains(x.LaboratoryId));
        }

        var tasks = queryable.ToList();

        return new TaskStatusStatisticsDto
        {
            Total = tasks.Count,
            UnderEvaluation = tasks.Count(t => t.Status == LaboratoryTaskStatus.UnderEvaluation),
            WaitingCustomerResponse = tasks.Count(t => t.Status == LaboratoryTaskStatus.WaitingCustomerResponse),
            PartialAccepted = tasks.Count(t => t.Status == LaboratoryTaskStatus.PartialAccepted),
            FullyAccepted = tasks.Count(t => t.Status == LaboratoryTaskStatus.FullyAccepted),
            Assigned = tasks.Count(t => t.Status == LaboratoryTaskStatus.Assigned),
            InProgress = tasks.Count(t => t.Status == LaboratoryTaskStatus.InProgress),
            WaitingApproval = tasks.Count(t => t.Status == LaboratoryTaskStatus.WaitingApproval),
            Approved = tasks.Count(t => t.Status == LaboratoryTaskStatus.Approved),
            SentToQuality = tasks.Count(t => t.Status == LaboratoryTaskStatus.SentToQuality),
        };
    }

    /// <summary>
    /// Lấy danh sách Sổ nhận mẫu giao việc cho QLKT - hiển thị tất cả công việc của Lab mà user quản lý
    /// </summary>
    [Authorize]
    public async Task<PagedResultDto<SampleAssignmentLogDto>> GetSampleAssignmentLogAsync(GetSampleAssignmentLogInput input)
    {
        var currentUserId = CurrentUser.Id;
        var isAdmin = CurrentUser.IsInRole(AppConstants.RoleAdmin);

        // Get queryables
        var labIndicatorQuery = await _laboratoryTaskIndicatorRepository.GetQueryableAsync();
        var labTaskQuery = await _laboratoryTaskRepository.GetQueryableAsync();
        var sampleIndicatorQuery = await _sampleIndicatorRepository.GetQueryableAsync();
        var sampleQuery = await _sampleRepository.GetQueryableAsync();
        var testRequestQuery = await _testRequestRepository.GetQueryableAsync();
        var masterIndicatorQuery = await _indicatorRepository.GetQueryableAsync();
        var testMethodQuery = await _testMethodRepository.GetQueryableAsync();
        var qualityIndicatorQuery = await _qualityIndicatorRepository.GetQueryableAsync();

        // Build base query from LaboratoryTaskIndicator
        var query = from li in labIndicatorQuery
                    join lt in labTaskQuery on li.LaboratoryTaskId equals lt.Id
                    join si in sampleIndicatorQuery on li.TestRequestSampleIndicatorId equals si.Id
                    join s in sampleQuery on si.TestRequestSampleId equals s.Id
                    join tr in testRequestQuery on lt.TestRequestId equals tr.Id
                    join mi in masterIndicatorQuery on si.IndicatorId equals mi.Id
                    join tm in testMethodQuery on li.MethodId equals tm.Id into tmJoin
                    from tm in tmJoin.DefaultIfEmpty()
                    join qi in qualityIndicatorQuery on li.Id equals qi.LaboratoryTaskIndicatorId into qiJoin
                    from qi in qiJoin.DefaultIfEmpty()
                    select new
                    {
                        // TestRequest info
                        TestRequestId = tr.Id,
                        LaboratoryTaskId = lt.Id,
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
                        TestRequestSampleIndicatorId = si.Id,
                        LaboratoryTaskIndicatorId = li.Id,
                        IndicatorId = li.Id,
                        IndicatorName = mi.Name,
                        IndicatorFormattedName = mi.FormattedName,
                        MethodId = li.MethodId,
                        MethodName = tm != null ? tm.Name : null,
                        MethodFormattedName = tm != null ? tm.FormattedName : null,
                        Unit = li.Unit,
                        TransferDate = si.TransferDate,
                        TestDate = li.TestDate,
                        Result = li.Result,

                        // Assignment info
                        TechnicalManagerId = lt.TechnicalManagerId,
                        TechnicianId = li.TechnicianId,
                        EntryDate = li.EntryDate,
                        QlktApprovalDate = li.ApprovalDate,

                        // Status
                        IndicatorApprovalStatus = li.ApprovalStatus,
                        QualityApprovalStatus = qi != null ? (QualityIndicatorApprovalStatus?)qi.ApprovalStatus : null,

                        // Laboratory info (for filtering)
                        LaboratoryId = lt.LaboratoryId
                    };

        // Apply access control for QLKT: Show all tasks of Labs that user manages
        if (currentUserId.HasValue && !isAdmin)
        {
            // Get labs where current user is Technical Manager
            var managedLaboratoryIds = (await _laboratoryRepository.GetListAsync(
                lab => lab.TechnicalManagerId == currentUserId.Value))
                .Select(lab => lab.Id)
                .ToHashSet();

            // Get task IDs where current user is a supporter
            var supporterTaskIds = (await _supporterRepository.GetListAsync(s => s.UserId == currentUserId.Value))
                .Select(s => s.LaboratoryTaskId)
                .ToHashSet();

            // Get lab IDs where current user is a department supporter (not expired)
            var departmentSupporterLabIds = (await _departmentSupporterRepository.GetListAsync(
                ds => ds.UserId == currentUserId.Value && ds.ExpirationDate > DateTime.UtcNow))
                .Select(ds => ds.LaboratoryId)
                .ToHashSet();

            // Filter: Show tasks from labs user manages OR tasks where user is supporter OR department supporter labs
            query = query.Where(x =>
                managedLaboratoryIds.Contains(x.LaboratoryId) ||
                supporterTaskIds.Contains(x.LaboratoryTaskId) ||
                departmentSupporterLabIds.Contains(x.LaboratoryId));
        }

        // Apply filters
        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var searchLower = input.Search.ToLower();
            query = query.Where(x =>
                (x.ContractNumber != null && x.ContractNumber.ToLower().Contains(searchLower)) ||
                (x.SampleCode != null && x.SampleCode.ToLower().Contains(searchLower)) ||
                (x.SampleName != null && x.SampleName.ToLower().Contains(searchLower)) ||
                (x.IndicatorName != null && x.IndicatorName.ToLower().Contains(searchLower)) ||
                (x.MethodName != null && x.MethodName.ToLower().Contains(searchLower)));
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

        if (input.LaboratoryIds != null && input.LaboratoryIds.Count > 0)
        {
            query = query.Where(x => input.LaboratoryIds.Contains(x.LaboratoryId));
        }

        // Filter by IndicatorIds (multi-select)
        if (input.IndicatorIds != null && input.IndicatorIds.Count > 0)
        {
            var indicatorIdSet = input.IndicatorIds.ToHashSet();
            var matchingSampleIndicatorIds = sampleIndicatorQuery
                .Where(si => indicatorIdSet.Contains(si.IndicatorId))
                .Select(si => si.Id)
                .ToHashSet();
            query = query.Where(x => matchingSampleIndicatorIds.Contains(x.TestRequestSampleIndicatorId));
        }

        // Filter by TechnicianIds (multi-select)
        if (input.TechnicianIds != null && input.TechnicianIds.Count > 0)
        {
            query = query.Where(x => x.TechnicianId.HasValue && input.TechnicianIds.Contains(x.TechnicianId.Value));
        }

        // Apply sorting - CRITICAL: Must sort by ContractNumber then SampleId first to ensure row spanning works correctly
        var sortingKey = string.IsNullOrWhiteSpace(input.Sorting) ? "contractnumber desc" : input.Sorting.ToLower();

        var sortedQuery = sortingKey switch
        {
            "requestcompletiondate desc" => query.OrderByDescending(x => x.RequestCompletionDate).ThenBy(x => x.ContractNumber).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName),
            "requestcompletiondate asc" => query.OrderBy(x => x.RequestCompletionDate).ThenBy(x => x.ContractNumber).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName),
            "contractnumber asc" => query.OrderBy(x => x.ContractNumber).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName),
            "contractnumber desc" => query.OrderByDescending(x => x.ContractNumber).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName),
            "transferdate asc" => query.OrderBy(x => x.TransferDate).ThenBy(x => x.ContractNumber).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName),
            "transferdate desc" => query.OrderByDescending(x => x.TransferDate).ThenBy(x => x.ContractNumber).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName),
            _ => query.OrderByDescending(x => x.ContractNumber).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName)
        };

        // Pagination by contract count (not row count)
        // skipCount and maxResultCount refer to number of contracts, not rows
        var allItems = sortedQuery.ToList();

        // Filter by QLKTDisplayStatus if specified (computed in-memory)
        if (input.QlktDisplayStatuses != null && input.QlktDisplayStatuses.Count > 0)
        {
            var statusSet = new HashSet<int>(input.QlktDisplayStatuses);
            allItems = allItems.Where(x =>
            {
                var status = MapQLKTDisplayStatus(
                    x.IndicatorApprovalStatus,
                    x.MethodId,
                    x.TechnicianId,
                    x.TestDate,
                    x.Result,
                    x.QualityApprovalStatus);
                return statusSet.Contains((int)status);
            }).ToList();
        }

        // Count unique contracts AFTER filtering
        var totalContractCount = allItems.Select(x => x.ContractNumber).Distinct().Count();

        var items = GetItemsByContractPagination(allItems, input.SkipCount, input.MaxResultCount);

        // Resolve user names
        var userIds = items
            .SelectMany(x => new[] { x.TechnicalManagerId, x.TechnicianId })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var userNameMap = new Dictionary<Guid, string>();
        foreach (var userId in userIds)
        {
            var userName = await _userNameProvider.GetUserNameAsync(userId);
            if (userName != null) userNameMap[userId] = userName;
        }

        // Map to DTOs
        var dtos = items.Select(x => new SampleAssignmentLogDto
        {
            TestRequestId = x.TestRequestId,
            QualityTaskId = Guid.Empty,
            LaboratoryTaskId = x.LaboratoryTaskId,
            ContractNumber = x.ContractNumber,
            TestRequestStatus = x.TestRequestStatus,
            RequestCompletionDate = x.RequestCompletionDate,
            SampleId = x.SampleId,
            SampleCode = x.SampleCode,
            SampleName = x.SampleName,
            SampleQuantity = x.SampleQuantity,
            SampleStatusText = x.SampleStatusText,
            TestRequestSampleIndicatorId = x.TestRequestSampleIndicatorId,
            LaboratoryTaskIndicatorId = x.LaboratoryTaskIndicatorId,
            IndicatorId = x.IndicatorId,
            IndicatorName = x.IndicatorName,
            IndicatorFormattedName = x.IndicatorFormattedName,
            MethodName = x.MethodName,
            MethodFormattedName = x.MethodFormattedName,
            Unit = x.Unit,
            TransferDate = x.TransferDate,
            TestDate = x.TestDate,
            Result = x.Result,
            TechnicalManagerName = x.TechnicalManagerId.HasValue && userNameMap.ContainsKey(x.TechnicalManagerId.Value)
                ? userNameMap[x.TechnicalManagerId.Value] : null,
            TechnicianName = x.TechnicianId.HasValue && userNameMap.ContainsKey(x.TechnicianId.Value)
                ? userNameMap[x.TechnicianId.Value] : null,
            EntryDate = x.EntryDate,
            QlktApprovalDate = x.QlktApprovalDate,
            IndicatorApprovalStatus = x.IndicatorApprovalStatus,
            TestingStatus = MapSampleAssignmentTestingStatus(x.IndicatorApprovalStatus, x.TestDate, x.Result),
            QLKTDisplayStatus = MapQLKTDisplayStatus(
                x.IndicatorApprovalStatus,
                x.MethodId,
                x.TechnicianId,
                x.TestDate,
                x.Result,
                x.QualityApprovalStatus),
            QualityApprovalStatus = x.QualityApprovalStatus,
            IsReportPublished = false
        }).ToList();

        return new PagedResultDto<SampleAssignmentLogDto>(totalContractCount, dtos);
    }

    /// <summary>
    /// Thống kê Sổ nhận mẫu giao việc cho QLKT theo QLKTDisplayStatus (không phụ thuộc filter)
    /// </summary>
    [Authorize]
    public async Task<SampleAssignmentLogStatisticsDto> GetSampleAssignmentLogStatisticsAsync()
    {
        var currentUserId = CurrentUser.Id;
        var isAdmin = CurrentUser.IsInRole(AppConstants.RoleAdmin);

        // Get queryables
        var labIndicatorQuery = await _laboratoryTaskIndicatorRepository.GetQueryableAsync();
        var labTaskQuery = await _laboratoryTaskRepository.GetQueryableAsync();
        var sampleIndicatorQuery = await _sampleIndicatorRepository.GetQueryableAsync();
        var qualityIndicatorQuery = await _qualityIndicatorRepository.GetQueryableAsync();

        // Build base query
        var query = from li in labIndicatorQuery
                    join lt in labTaskQuery on li.LaboratoryTaskId equals lt.Id
                    join qi in qualityIndicatorQuery on li.Id equals qi.LaboratoryTaskIndicatorId into qiJoin
                    from qi in qiJoin.DefaultIfEmpty()
                    select new
                    {
                        LaboratoryTaskId = lt.Id,
                        LaboratoryId = lt.LaboratoryId,
                        IndicatorApprovalStatus = li.ApprovalStatus,
                        MethodId = li.MethodId,
                        TechnicianId = li.TechnicianId,
                        TestDate = li.TestDate,
                        Result = li.Result,
                        QualityApprovalStatus = qi != null ? (QualityIndicatorApprovalStatus?)qi.ApprovalStatus : null,
                    };

        // Apply same access control as GetSampleAssignmentLogAsync (QLKT)
        if (currentUserId.HasValue && !isAdmin)
        {
            var managedLaboratoryIds = (await _laboratoryRepository.GetListAsync(
                lab => lab.TechnicalManagerId == currentUserId.Value))
                .Select(lab => lab.Id)
                .ToHashSet();

            var supporterTaskIds = (await _supporterRepository.GetListAsync(s => s.UserId == currentUserId.Value))
                .Select(s => s.LaboratoryTaskId)
                .ToHashSet();

            var departmentSupporterLabIds = (await _departmentSupporterRepository.GetListAsync(
                ds => ds.UserId == currentUserId.Value && ds.ExpirationDate > DateTime.UtcNow))
                .Select(ds => ds.LaboratoryId)
                .ToHashSet();

            query = query.Where(x =>
                managedLaboratoryIds.Contains(x.LaboratoryId) ||
                supporterTaskIds.Contains(x.LaboratoryTaskId) ||
                departmentSupporterLabIds.Contains(x.LaboratoryId));
        }

        // Materialize and compute display status
        var allItems = query.ToList();
        var statusCounts = allItems
            .Select(x => MapQLKTDisplayStatus(
                x.IndicatorApprovalStatus, x.MethodId, x.TechnicianId,
                x.TestDate, x.Result, x.QualityApprovalStatus))
            .ToList();

        return new SampleAssignmentLogStatisticsDto
        {
            Total = statusCounts.Count,
            UnderReview = statusCounts.Count(s => s == QLKTDisplayStatus.UnderReview),
            ToDo = statusCounts.Count(s => s == QLKTDisplayStatus.ToDo),
            Testing = statusCounts.Count(s => s == QLKTDisplayStatus.Testing),
            WaitingApproval = statusCounts.Count(s => s == QLKTDisplayStatus.WaitingQLKTApproval),
            ResultRejected = statusCounts.Count(s => s == QLKTDisplayStatus.ResultRejected),
            Completed = statusCounts.Count(s => s == QLKTDisplayStatus.Completed),
        };
    }

    /// <summary>
    /// Helper method to paginate by contract (not by row)
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

    private static TestingStatus? MapSampleAssignmentTestingStatus(
        IndicatorApprovalStatus approvalStatus,
        DateTime? testDate,
        string? result)
    {
        // Handle Rejected and CannotTest first - these are terminal states
        if (approvalStatus == IndicatorApprovalStatus.Rejected)
        {
            return TestingStatus.Rejected;
        }

        if (approvalStatus == IndicatorApprovalStatus.CannotTest)
        {
            return TestingStatus.CannotTest;
        }

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

        // Pending or AcceptedForTesting → NotStarted
        // (indicator is waiting to be processed/tested)
        return TestingStatus.NotStarted;
    }

    /// <summary>
    /// Map indicator data to QLKTDisplayStatus for "Sổ nhận việc" tab
    /// Based on KH flow diagram
    /// </summary>
    private static QLKTDisplayStatus MapQLKTDisplayStatus(
        IndicatorApprovalStatus approvalStatus,
        Guid? methodId,
        Guid? technicianId,
        DateTime? testDate,
        string? result,
        QualityIndicatorApprovalStatus? qualityApprovalStatus)
    {
        // 1. Handle special statuses first (terminal or special states)
        switch (approvalStatus)
        {
            case IndicatorApprovalStatus.MethodChangeRequested:
                return QLKTDisplayStatus.MethodChangeRequested;

            case IndicatorApprovalStatus.MethodChanged:
                // Theo flow KH: sau khi đổi PP xong → quay lại xem xét YC
                return QLKTDisplayStatus.UnderReview;

            case IndicatorApprovalStatus.TestingRejected:
                return QLKTDisplayStatus.TestingRejected;

            case IndicatorApprovalStatus.SupplementaryRequested:
                return QLKTDisplayStatus.SupplementaryRequested;

            case IndicatorApprovalStatus.CustomerCancelled:
                return QLKTDisplayStatus.CustomerCancelled;

            case IndicatorApprovalStatus.CannotTest:
                return QLKTDisplayStatus.CannotTest;

            case IndicatorApprovalStatus.Rejected:
                return QLKTDisplayStatus.ResultRejected;

            case IndicatorApprovalStatus.Pending:
                // Đang xem xét YC - QLKT chưa đánh giá
                return QLKTDisplayStatus.UnderReview;
        }

        // 2. Handle Approved status - check QLCL approval
        if (approvalStatus == IndicatorApprovalStatus.Approved)
        {
            if (qualityApprovalStatus == QualityIndicatorApprovalStatus.Approved)
            {
                return QLKTDisplayStatus.Completed;
            }
            if (qualityApprovalStatus == QualityIndicatorApprovalStatus.Rejected)
            {
                return QLKTDisplayStatus.QLCLRejected;
            }
            // PendingApproval or null (not yet sent to QLCL)
            return QLKTDisplayStatus.WaitingQLCLApproval;
        }

        // 3. Handle Entered status - Chờ QLKT phê duyệt
        if (approvalStatus == IndicatorApprovalStatus.Entered)
        {
            return QLKTDisplayStatus.WaitingQLKTApproval;
        }

        // 4. Handle AcceptedForTesting - check assignment and testing progress
        if (approvalStatus == IndicatorApprovalStatus.AcceptedForTesting)
        {
            // Chưa có KTV hoặc PP → Chờ giao việc
            if (!technicianId.HasValue || !methodId.HasValue)
            {
                return QLKTDisplayStatus.WaitingForAssignment;
            }

            // Có KTV + PP nhưng chưa có testDate → Cần làm
            if (!testDate.HasValue)
            {
                return QLKTDisplayStatus.ToDo;
            }

            // Có testDate → Đang thử nghiệm
            return QLKTDisplayStatus.Testing;
        }

        // Fallback (should not reach here)
        return QLKTDisplayStatus.UnderReview;
    }

    #endregion
}
