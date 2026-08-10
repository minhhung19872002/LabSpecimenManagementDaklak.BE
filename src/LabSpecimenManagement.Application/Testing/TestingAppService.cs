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
using LabSpecimenManagement.Equipments;
using LabSpecimenManagement.PreparedSolutions;
using LabSpecimenManagement.Products;
using LabSpecimenManagement.QualityManagement;
using LabSpecimenManagement.TestRequests;
using LabSpecimenManagement.WarehouseImports;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Identity;

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
    private readonly IRepository<LaboratoryTaskSupporter, Guid> _supporterRepository;
    private readonly IRepository<LaboratoryTaskSampleReceived, Guid> _sampleReceivedRepository;
    private readonly IRepository<Standard, Guid> _standardRepository;
    private readonly IRepository<StandardIndicator, Guid> _standardIndicatorRepository;
    private readonly IRepository<RejectionHistoryImage, Guid> _rejectionHistoryImageRepository;
    private readonly IUserNameProvider _userNameProvider;
    private readonly IGuidGenerator _guidGenerator;
    private readonly INotificationCreator _notificationCreator;
    private readonly ITestResultValueAppService _testResultValueAppService;
    private readonly IRepository<Department, Guid> _departmentRepository;
    private readonly IIdentityUserRepository _identityUserRepository;
    private readonly IRepository<QualityManagementIndicator, Guid> _qualityIndicatorRepository;
    private readonly IRepository<TestMethodIndicator, Guid> _testMethodIndicatorRepository;
    private readonly IRepository<MaterialConsumptionItem, Guid> _materialConsumptionRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<Equipment, Guid> _equipmentRepository;
    private readonly IRepository<ProductUnitConversion, Guid> _productUnitConversionRepository;
    private readonly IRepository<TestBatchItem, Guid> _testBatchItemRepository;
    private readonly IRepository<PreparedSolutionBatch, Guid> _preparedSolutionBatchRepository;
    private readonly IRepository<WarehouseImportReceiptItem, Guid> _importReceiptItemRepository;
    private readonly IRepository<ProductSku, Guid> _productSkuRepository;
    private readonly IFifoStockDeductionService _fifoStockService;

    public TestingAppService(
        IRepository<LaboratoryTask, Guid> laboratoryTaskRepository,
        IRepository<LaboratoryTaskIndicator, Guid> laboratoryTaskIndicatorRepository,
        IRepository<IndicatorRejectionHistory, Guid> rejectionHistoryRepository,
        IRepository<TestRequest, Guid> testRequestRepository,
        IRepository<TestRequestSample, Guid> sampleRepository,
        IRepository<TestRequestSampleIndicator, Guid> sampleIndicatorRepository,
        IRepository<TestRequestSampleAttachment, Guid> sampleAttachmentRepository,
        IRepository<Laboratory, Guid> laboratoryRepository,
        IRepository<Department, Guid> departmentRepository,
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<TestMethod, Guid> testMethodRepository,
        IRepository<SampleTestingImage, Guid> sampleTestingImageRepository,
        IRepository<IndicatorImage, Guid> indicatorImageRepository,
        IRepository<LaboratoryTaskSupporter, Guid> supporterRepository,
        IRepository<LaboratoryTaskSampleReceived, Guid> sampleReceivedRepository,
        IRepository<Standard, Guid> standardRepository,
        IRepository<StandardIndicator, Guid> standardIndicatorRepository,
        IRepository<RejectionHistoryImage, Guid> rejectionHistoryImageRepository,
        IUserNameProvider userNameProvider,
        IGuidGenerator guidGenerator,
        INotificationCreator notificationCreator,
        ITestResultValueAppService testResultValueAppService,
        IIdentityUserRepository identityUserRepository,
        IRepository<QualityManagementIndicator, Guid> qualityIndicatorRepository,
        IRepository<TestMethodIndicator, Guid> testMethodIndicatorRepository,
        IRepository<MaterialConsumptionItem, Guid> materialConsumptionRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<Equipment, Guid> equipmentRepository,
        IRepository<ProductUnitConversion, Guid> productUnitConversionRepository,
        IRepository<TestBatchItem, Guid> testBatchItemRepository,
        IRepository<PreparedSolutionBatch, Guid> preparedSolutionBatchRepository,
        IRepository<WarehouseImportReceiptItem, Guid> importReceiptItemRepository,
        IRepository<ProductSku, Guid> productSkuRepository,
        IFifoStockDeductionService fifoStockService)
    {
        _laboratoryTaskRepository = laboratoryTaskRepository;
        _laboratoryTaskIndicatorRepository = laboratoryTaskIndicatorRepository;
        _rejectionHistoryRepository = rejectionHistoryRepository;
        _testRequestRepository = testRequestRepository;
        _sampleRepository = sampleRepository;
        _sampleIndicatorRepository = sampleIndicatorRepository;
        _sampleAttachmentRepository = sampleAttachmentRepository;
        _laboratoryRepository = laboratoryRepository;
        _departmentRepository = departmentRepository;
        _indicatorRepository = indicatorRepository;
        _testMethodRepository = testMethodRepository;
        _sampleTestingImageRepository = sampleTestingImageRepository;
        _indicatorImageRepository = indicatorImageRepository;
        _supporterRepository = supporterRepository;
        _sampleReceivedRepository = sampleReceivedRepository;
        _standardRepository = standardRepository;
        _standardIndicatorRepository = standardIndicatorRepository;
        _rejectionHistoryImageRepository = rejectionHistoryImageRepository;
        _userNameProvider = userNameProvider;
        _guidGenerator = guidGenerator;
        _notificationCreator = notificationCreator;
        _testResultValueAppService = testResultValueAppService;
        _identityUserRepository = identityUserRepository;
        _qualityIndicatorRepository = qualityIndicatorRepository;
        _testMethodIndicatorRepository = testMethodIndicatorRepository;
        _materialConsumptionRepository = materialConsumptionRepository;
        _productRepository = productRepository;
        _equipmentRepository = equipmentRepository;
        _productUnitConversionRepository = productUnitConversionRepository;
        _testBatchItemRepository = testBatchItemRepository;
        _preparedSolutionBatchRepository = preparedSolutionBatchRepository;
        _importReceiptItemRepository = importReceiptItemRepository;
        _productSkuRepository = productSkuRepository;
        _fifoStockService = fifoStockService;
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
        // - QLKT: xem được tất cả task của phòng ban họ quản lý (Laboratory.TechnicalManagerId) hoặc task họ được gán làm TechnicalManager
        // - Người hỗ trợ: xem được task mà họ là supporter
        // - KTV: chỉ xem được task của Lab họ thuộc về (qua DepartmentId)
        var isAdmin = CurrentUser.IsInRole(AppConstants.RoleAdmin);
        if (currentUserId.HasValue && !isAdmin)
        {
            // Lấy danh sách LaboratoryId mà user là TechnicalManager (QLKT của phòng ban)
            var managedLaboratoryIds = (await _laboratoryRepository.GetListAsync(
                lab => lab.TechnicalManagerId == currentUserId.Value))
                .Select(lab => lab.Id)
                .ToHashSet();

            // Lấy danh sách TaskId mà user là Supporter
            var supporterTaskIds = (await _supporterRepository.GetListAsync(
                s => s.UserId == currentUserId.Value))
                .Select(s => s.LaboratoryTaskId)
                .ToHashSet();

            // Lấy DepartmentId của user hiện tại để filter task của Lab họ thuộc về
            var currentUser = await _identityUserRepository.FindAsync(currentUserId.Value);
            var userDepartmentId = currentUser?.GetProperty<Guid?>(AppConstants.PropertyDepartmentId);

            // Lấy danh sách LaboratoryId có cùng DepartmentId với user (Lab họ thuộc về)
            var userLabIds = new HashSet<Guid>();
            if (userDepartmentId.HasValue)
            {
                userLabIds = (await _laboratoryRepository.GetListAsync(
                    lab => lab.DepartmentId == userDepartmentId.Value))
                    .Select(lab => lab.Id)
                    .ToHashSet();
            }

            // User có thể xem task nếu:
            // 1. Task thuộc phòng ban mà họ là QLKT (Laboratory.TechnicalManagerId)
            // 2. Họ là TechnicalManager của task cụ thể (LaboratoryTask.TechnicalManagerId)
            // 3. Họ là Supporter của task
            // 4. Task thuộc Lab có cùng DepartmentId với user (Lab họ thuộc về)
            taskQuery = taskQuery.Where(x =>
                managedLaboratoryIds.Contains(x.Task.LaboratoryId) ||
                x.Task.TechnicalManagerId == currentUserId.Value ||
                supporterTaskIds.Contains(x.Task.Id) ||
                userLabIds.Contains(x.Task.LaboratoryId));
        }

        // Apply Tab filter:
        // - MyAssignments: Chỉ hiển thị task mà user được assign làm KTV (có ít nhất 1 indicator)
        // - All: Hiển thị tất cả task mà user có quyền xem (đã filter ở trên)
        if (input.Tab == TestingTabType.MyAssignments && currentUserId.HasValue)
        {
            taskQuery = taskQuery.Where(x =>
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

        // Apply RequestCompletionDate range filter
        if (input.RequestCompletionDateFrom.HasValue)
        {
            taskQuery = taskQuery.Where(x => x.Task.RequestCompletionDate >= input.RequestCompletionDateFrom.Value);
        }
        if (input.RequestCompletionDateTo.HasValue)
        {
            taskQuery = taskQuery.Where(x => x.Task.RequestCompletionDate <= input.RequestCompletionDateTo.Value);
        }

        // Apply TransferDate (UnderReviewDate) range filter
        if (input.TransferDateFrom.HasValue || input.TransferDateTo.HasValue)
        {
            var trDateQuery = await _testRequestRepository.GetQueryableAsync();
            if (input.TransferDateFrom.HasValue)
            {
                trDateQuery = trDateQuery.Where(tr => tr.UnderReviewDate >= input.TransferDateFrom.Value);
            }
            if (input.TransferDateTo.HasValue)
            {
                var transferDateEnd = input.TransferDateTo.Value.Date.AddDays(1);
                trDateQuery = trDateQuery.Where(tr => tr.UnderReviewDate < transferDateEnd);
            }
            var matchingTrIds = trDateQuery.Select(tr => tr.Id);
            taskQuery = taskQuery.Where(x => matchingTrIds.Contains(x.Task.TestRequestId));
        }

        // Apply indicator filter (join through TestRequestSampleIndicator to get IndicatorId)
        if (input.IndicatorIds != null && input.IndicatorIds.Any())
        {
            var sampleIndicatorQueryable = await _sampleIndicatorRepository.GetQueryableAsync();
            var matchingSampleIndicatorIds = sampleIndicatorQueryable
                .Where(si => input.IndicatorIds.Contains(si.IndicatorId))
                .Select(si => si.Id)
                .ToHashSet();

            taskQuery = taskQuery.Where(x => x.Indicators.Any(i =>
                matchingSampleIndicatorIds.Contains(i.TestRequestSampleIndicatorId)));
        }

        // Get all tasks for status filtering and processing
        var allTasks = taskQuery.ToList();

        // Calculate testing status for each task
        // Note: Filter out parent indicators with children before calculating status
        var tasksWithStatus = new List<(LaboratoryTask Task, List<LaboratoryTaskIndicator> Indicators, TestingStatus TestingStatus, bool IsUrgent)>();
        foreach (var item in allTasks)
        {
            // Filter out parent indicators with children for status calculation
            var filteredIndicators = await FilterIndicatorsForStatusCalculationAsync(item.Indicators);
            var status = CalculateTestingStatus(filteredIndicators);
            var isUrgent = item.Task.RequestCompletionDate.HasValue &&
                           (item.Task.RequestCompletionDate.Value - DateTime.Today).TotalDays <= 5;
            tasksWithStatus.Add((item.Task, item.Indicators, status, isUrgent));
        }

        // Apply status filter
        if (input.Statuses != null && input.Statuses.Any())
        {
            tasksWithStatus = tasksWithStatus.Where(x => input.Statuses.Contains(x.TestingStatus)).ToList();
        }

        // Get total count after all filters
        var totalCount = tasksWithStatus.Count;

        // Load ContractDates for sorting
        var taskTestRequestIds = tasksWithStatus.Select(x => x.Task.TestRequestId).Distinct().ToList();
        var trQueryable = await _testRequestRepository.GetQueryableAsync();
        var testRequestDateMap = trQueryable
            .Where(tr => taskTestRequestIds.Contains(tr.Id))
            .Select(tr => new { tr.Id, tr.ContractDate, tr.UnderReviewDate })
            .ToDictionary(tr => tr.Id, tr => new { tr.ContractDate, tr.UnderReviewDate });

        var contractDateMap = testRequestDateMap.ToDictionary(x => x.Key, x => x.Value.ContractDate);

        // Apply sorting
        var sortedTasks = tasksWithStatus.AsEnumerable();
        var sortingKey = string.IsNullOrWhiteSpace(input.Sorting) ? SortingFields.ContractDateDesc : input.Sorting.ToLower();
        sortedTasks = sortingKey switch
        {
            SortingFields.TransferDate => sortedTasks.OrderBy(x => testRequestDateMap.GetValueOrDefault(x.Task.TestRequestId)?.UnderReviewDate),
            SortingFields.TransferDateDesc => sortedTasks.OrderByDescending(x => testRequestDateMap.GetValueOrDefault(x.Task.TestRequestId)?.UnderReviewDate),
            SortingFields.RequestCompletionDate => sortedTasks.OrderBy(x => x.Task.RequestCompletionDate),
            SortingFields.RequestCompletionDateDesc => sortedTasks.OrderByDescending(x => x.Task.RequestCompletionDate),
            SortingFields.TestDate => sortedTasks.OrderBy(x => x.Task.TestDate),
            SortingFields.TestDateDesc => sortedTasks.OrderByDescending(x => x.Task.TestDate),
            SortingFields.TestingStatus => sortedTasks.OrderBy(x => x.TestingStatus),
            SortingFields.TestingStatusDesc => sortedTasks.OrderByDescending(x => x.TestingStatus),
            SortingFields.ContractDate => sortedTasks.OrderBy(x => contractDateMap.GetValueOrDefault(x.Task.TestRequestId)),
            SortingFields.ContractDateDesc => sortedTasks.OrderByDescending(x => contractDateMap.GetValueOrDefault(x.Task.TestRequestId)),
            _ => sortedTasks.OrderByDescending(x => contractDateMap.GetValueOrDefault(x.Task.TestRequestId))
        };

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

    [Authorize]
    public async Task<TestingStatusStatisticsDto> GetStatusStatisticsAsync(TestingTabType tab)
    {
        var currentUserId = CurrentUser.Id;
        var queryable = await _laboratoryTaskRepository.GetQueryableAsync();
        var indicatorQueryable = await _laboratoryTaskIndicatorRepository.GetQueryableAsync();

        var taskQuery = queryable.Select(task => new
        {
            Task = task,
            Indicators = indicatorQueryable.Where(i => i.LaboratoryTaskId == task.Id).ToList()
        });

        // Apply user access filter (same logic as GetListAsync)
        var isAdmin = CurrentUser.IsInRole(AppConstants.RoleAdmin);
        if (currentUserId.HasValue && !isAdmin)
        {
            var managedLaboratoryIds = (await _laboratoryRepository.GetListAsync(
                lab => lab.TechnicalManagerId == currentUserId.Value))
                .Select(lab => lab.Id).ToHashSet();

            var supporterTaskIds = (await _supporterRepository.GetListAsync(
                s => s.UserId == currentUserId.Value))
                .Select(s => s.LaboratoryTaskId).ToHashSet();

            var currentUser = await _identityUserRepository.FindAsync(currentUserId.Value);
            var userDepartmentId = currentUser?.GetProperty<Guid?>(AppConstants.PropertyDepartmentId);

            var userLabIds = new HashSet<Guid>();
            if (userDepartmentId.HasValue)
            {
                userLabIds = (await _laboratoryRepository.GetListAsync(
                    lab => lab.DepartmentId == userDepartmentId.Value))
                    .Select(lab => lab.Id).ToHashSet();
            }

            taskQuery = taskQuery.Where(x =>
                managedLaboratoryIds.Contains(x.Task.LaboratoryId) ||
                x.Task.TechnicalManagerId == currentUserId.Value ||
                supporterTaskIds.Contains(x.Task.Id) ||
                userLabIds.Contains(x.Task.LaboratoryId));
        }

        // Apply Tab filter
        if (tab == TestingTabType.MyAssignments && currentUserId.HasValue)
        {
            taskQuery = taskQuery.Where(x =>
                x.Indicators.Any(i => i.TechnicianId == currentUserId.Value));
        }

        var allTasks = taskQuery.ToList();

        // Calculate testing status for each task
        var statusCounts = new TestingStatusStatisticsDto { Total = allTasks.Count };
        foreach (var item in allTasks)
        {
            var filteredIndicators = await FilterIndicatorsForStatusCalculationAsync(item.Indicators);
            var status = CalculateTestingStatus(filteredIndicators);
            switch (status)
            {
                case TestingStatus.NotStarted: statusCounts.NotStarted++; break;
                case TestingStatus.InProgress: statusCounts.InProgress++; break;
                case TestingStatus.PendingApproval: statusCounts.PendingApproval++; break;
                case TestingStatus.Rejected: statusCounts.Rejected++; break;
                case TestingStatus.Completed: statusCounts.Completed++; break;
            }
        }

        return statusCounts;
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

            // Validate: Only assigned technician, technical manager, or supporter can edit
            if (!await CanEditIndicatorAsync(indicator, task, currentUserId))
            {
                throw new ForbiddenAccessException("Bạn không có quyền chỉnh sửa chỉ tiêu này");
            }

            // Validate: Cannot edit if already approved
            if (indicator.ApprovalStatus == IndicatorApprovalStatus.Approved)
            {
                throw new Common.Exceptions.InvalidOperationException("Không thể chỉnh sửa chỉ tiêu đã được phê duyệt", "indicator");
            }

            // Validate: Cannot edit if customer cancelled (đồng bộ với UpdateIndicatorMethod/UnitAsync)
            if (indicator.ApprovalStatus == IndicatorApprovalStatus.CustomerCancelled)
            {
                throw new Common.Exceptions.InvalidOperationException("Không thể chỉnh sửa chỉ tiêu đã bị hủy", "indicator");
            }

            // Note: Result now accepts both numeric values, formulas (2^2, 10^-3),
            // unicode superscript (2², 10⁻³), and text values (Đạt, Không đạt, Âm tính)
            // Text values will be auto-created in master data when submitting for approval

            // Update indicator
            // MethodId/Unit KHÔNG còn được ghi ở đây — đổi PP/ĐVT đi qua endpoint riêng
            // UpdateIndicatorMethodAsync / UpdateIndicatorUnitAsync (PUT indicators/{id}/method|unit)
            indicator.Result = result.Result;

            // Chỉ update FreeTextNote nếu có giá trị mới được truyền vào
            // Nếu result.Note là null thì giữ nguyên FreeTextNote hiện tại
            if (result.Note != null)
            {
                indicator.FreeTextNote = result.Note;
            }

            // Update TestDate if provided
            if (result.TestDate.HasValue)
            {
                indicator.TestDate = result.TestDate;
            }

            // Auto-set entry date when result is entered or testDate is set
            if ((!string.IsNullOrWhiteSpace(result.Result) || result.TestDate.HasValue) && !indicator.EntryDate.HasValue)
            {
                indicator.EntryDate = DateTime.Now;
                indicator.EnteredById = currentUserId;
            }

            await _laboratoryTaskIndicatorRepository.UpdateAsync(indicator);
        }

        // Update task status
        await UpdateTaskStatusAsync(task);

        // Update TestRequest status to InProgress when KTV sets testDate
        await TryUpdateTestRequestToInProgressAsync(task.TestRequestId);
    }

    /// <summary>
    /// KTV cập nhật phương pháp cho 1 chỉ tiêu.
    /// Mirror LaboratoryTaskAppService.UpdateIndicatorMethodAsync nhưng dùng quyền
    /// CanEditIndicatorAsync (KTV được gán/QLKT/supporter) và chặn chỉ tiêu đã duyệt.
    /// </summary>
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public async Task<TestingIndicatorDto> UpdateIndicatorMethodAsync(Guid indicatorId, Guid? methodId)
    {
        var indicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicatorId);
        var task = await _laboratoryTaskRepository.GetAsync(indicator.LaboratoryTaskId);

        if (!await CanEditIndicatorAsync(indicator, task, CurrentUser.Id))
        {
            throw new ForbiddenAccessException("Bạn không có quyền chỉnh sửa chỉ tiêu này");
        }

        if (indicator.ApprovalStatus == IndicatorApprovalStatus.Approved)
        {
            throw new Common.Exceptions.InvalidOperationException("Không thể chỉnh sửa chỉ tiêu đã được phê duyệt", "indicator");
        }

        if (indicator.ApprovalStatus == IndicatorApprovalStatus.CustomerCancelled)
        {
            throw new Common.Exceptions.InvalidOperationException("Không thể cập nhật phương pháp cho chỉ tiêu đã bị hủy", "indicator");
        }

        TestMethod? selectedMethod = null;
        if (methodId.HasValue)
        {
            selectedMethod = await _testMethodRepository.FindAsync(methodId.Value);
            if (selectedMethod == null)
            {
                throw new ValidationException("methodId", "Phương pháp không tồn tại");
            }
        }

        // Đổi sang method thường -> clear thông tin thầu phụ (đồng bộ với LaboratoryTaskAppService)
        if (!IsDashMethodName(selectedMethod?.Name))
        {
            indicator.SubContractMethodName = null;
            indicator.SubContractNumber = null;
            indicator.SubContractCost = null;
        }

        // Auto-fill ĐVT theo PP mới khi: ĐVT đang trống (kể cả html rỗng "<p></p>")
        // hoặc đang đúng bằng ĐVT mặc định của PP cũ (chưa được nhập tay giá trị riêng).
        // ĐVT đã nhập tay thì giữ nguyên.
        var shouldAutoFillUnit = IsBlankUnit(indicator.Unit);
        if (!shouldAutoFillUnit && indicator.MethodId.HasValue)
        {
            var oldMethod = await _testMethodRepository.FindAsync(indicator.MethodId.Value);
            var oldDefaultUnit = oldMethod?.FormattedUnit ?? oldMethod?.Unit;
            shouldAutoFillUnit = !string.IsNullOrEmpty(oldDefaultUnit) && indicator.Unit == oldDefaultUnit;
        }

        indicator.MethodId = methodId;
        if (shouldAutoFillUnit)
        {
            indicator.Unit = selectedMethod?.FormattedUnit ?? selectedMethod?.Unit;
        }

        await _laboratoryTaskIndicatorRepository.UpdateAsync(indicator);

        // Recalculate task status after changing method
        await UpdateTaskStatusAsync(task);

        return await MapSingleIndicatorAsync(indicator, task);
    }

    /// <summary>
    /// KTV bulk cập nhật phương pháp cho nhiều chỉ tiêu.
    /// Cùng logic với UpdateIndicatorMethodAsync nhưng skip (không throw) từng chỉ tiêu
    /// không hợp lệ (đã duyệt/đã hủy/không thuộc task/không có quyền chỉnh sửa).
    /// </summary>
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public async Task<List<TestingIndicatorDto>> BulkUpdateIndicatorMethodAsync(Guid laboratoryTaskId, BulkUpdateIndicatorMethodInput input)
    {
        var task = await _laboratoryTaskRepository.GetAsync(laboratoryTaskId);

        TestMethod? selectedMethod = null;
        if (input.MethodId.HasValue)
        {
            selectedMethod = await _testMethodRepository.FindAsync(input.MethodId.Value);
            if (selectedMethod == null)
            {
                throw new ValidationException("methodId", "Phương pháp không tồn tại");
            }
        }

        var keepSubContract = IsDashMethodName(selectedMethod?.Name);
        var updatedDtos = new List<TestingIndicatorDto>();

        // Cùng semantic với CanEditIndicatorAsync nhưng check QLKT/supporter 1 lần cho cả batch
        // (2 điều kiện này chỉ phụ thuộc task + user), trong loop chỉ còn so TechnicianId
        var currentUserId = CurrentUser.Id;
        if (!currentUserId.HasValue)
        {
            return updatedDtos;
        }
        var canEditAllTaskIndicators =
            task.TechnicalManagerId == currentUserId.Value ||
            await _supporterRepository.AnyAsync(s =>
                s.LaboratoryTaskId == task.Id && s.UserId == currentUserId.Value);

        foreach (var indicatorId in input.IndicatorIds.Distinct())
        {
            var indicator = await _laboratoryTaskIndicatorRepository.FindAsync(indicatorId);
            if (indicator == null || indicator.LaboratoryTaskId != task.Id)
            {
                continue;
            }

            if (!canEditAllTaskIndicators && indicator.TechnicianId != currentUserId.Value)
            {
                continue;
            }

            if (indicator.ApprovalStatus == IndicatorApprovalStatus.Approved ||
                indicator.ApprovalStatus == IndicatorApprovalStatus.CustomerCancelled)
            {
                continue;
            }

            if (!keepSubContract)
            {
                indicator.SubContractMethodName = null;
                indicator.SubContractNumber = null;
                indicator.SubContractCost = null;
            }

            var shouldAutoFillUnit = IsBlankUnit(indicator.Unit);
            if (!shouldAutoFillUnit && indicator.MethodId.HasValue)
            {
                var oldMethod = await _testMethodRepository.FindAsync(indicator.MethodId.Value);
                var oldDefaultUnit = oldMethod?.FormattedUnit ?? oldMethod?.Unit;
                shouldAutoFillUnit = !string.IsNullOrEmpty(oldDefaultUnit) && indicator.Unit == oldDefaultUnit;
            }

            indicator.MethodId = input.MethodId;
            if (shouldAutoFillUnit)
            {
                indicator.Unit = selectedMethod?.FormattedUnit ?? selectedMethod?.Unit;
            }

            await _laboratoryTaskIndicatorRepository.UpdateAsync(indicator);
            updatedDtos.Add(await MapSingleIndicatorAsync(indicator, task));
        }

        await UpdateTaskStatusAsync(task);

        return updatedDtos;
    }

    /// <summary>
    /// KTV cập nhật đơn vị tính cho 1 chỉ tiêu.
    /// </summary>
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public async Task<TestingIndicatorDto> UpdateIndicatorUnitAsync(Guid indicatorId, string? unit)
    {
        var indicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicatorId);
        var task = await _laboratoryTaskRepository.GetAsync(indicator.LaboratoryTaskId);

        if (!await CanEditIndicatorAsync(indicator, task, CurrentUser.Id))
        {
            throw new ForbiddenAccessException("Bạn không có quyền chỉnh sửa chỉ tiêu này");
        }

        if (indicator.ApprovalStatus == IndicatorApprovalStatus.Approved)
        {
            throw new Common.Exceptions.InvalidOperationException("Không thể chỉnh sửa chỉ tiêu đã được phê duyệt", "indicator");
        }

        if (indicator.ApprovalStatus == IndicatorApprovalStatus.CustomerCancelled)
        {
            throw new Common.Exceptions.InvalidOperationException("Không thể cập nhật đơn vị tính cho chỉ tiêu đã bị hủy", "indicator");
        }

        indicator.Unit = unit;
        await _laboratoryTaskIndicatorRepository.UpdateAsync(indicator);

        return await MapSingleIndicatorAsync(indicator, task);
    }

    // Load dữ liệu phụ trợ rồi build DTO cho 1 chỉ tiêu (dùng cho các endpoint update lẻ)
    private async Task<TestingIndicatorDto> MapSingleIndicatorAsync(LaboratoryTaskIndicator taskIndicator, LaboratoryTask task)
    {
        var sampleIndicator = await _sampleIndicatorRepository.FindAsync(taskIndicator.TestRequestSampleIndicatorId);
        if (sampleIndicator == null)
        {
            throw new EntityNotFoundException("SampleIndicator", taskIndicator.TestRequestSampleIndicatorId);
        }

        var userNames = await _userNameProvider.GetUserNamesAsync(new List<Guid?> { taskIndicator.TechnicianId });
        return await MapIndicatorToDtoAsync(taskIndicator, sampleIndicator, task, userNames);
    }

    // Đồng bộ với LaboratoryTaskAppService.IsDashMethodName
    private static bool IsDashMethodName(string? methodName)
        => string.Equals(methodName?.Trim(), "-", StringComparison.Ordinal);

    // ĐVT coi như trống nếu null/khoảng trắng hoặc chỉ còn thẻ html rỗng (vd "<p></p>" từ RichTextEditor)
    private static bool IsBlankUnit(string? unit)
        => string.IsNullOrWhiteSpace(unit)
           || string.IsNullOrWhiteSpace(Regex.Replace(unit, "<[^>]*>", string.Empty));

    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public async Task SubmitForApprovalAsync(Guid laboratoryTaskId, SubmitForApprovalInput input)
    {
        var task = await _laboratoryTaskRepository.GetAsync(laboratoryTaskId);
        var currentUserId = CurrentUser.Id;

        foreach (var indicatorId in input.IndicatorIds)
        {
            var indicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicatorId);

            // Validate: Only assigned technician, technical manager, or supporter can submit
            if (!await CanEditIndicatorAsync(indicator, task, currentUserId))
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

            // Batch fetch images for all histories
            var historyIds = orderedHistories.Select(h => h.Id).ToList();
            var allImages = await _rejectionHistoryImageRepository.GetListAsync(x => historyIds.Contains(x.RejectionHistoryId));
            var imagesByHistory = allImages.GroupBy(x => x.RejectionHistoryId)
                .ToDictionary(g => g.Key, g => g.ToList());

            return orderedHistories.Select(history => new RejectionHistoryDto
            {
                Id = history.Id,
                IndicatorId = history.IndicatorId,
                RejectionReason = history.RejectionReason,
                RejectedDate = history.RejectedDate,
                RejectedById = history.RejectedById,
                RejectedByName = userNames.TryGetValue(history.RejectedById, out var name) ? name : null,
                IsCannotTest = history.IsCannotTest,
                ReportedByRole = history.ReportedByRole,
                Images = imagesByHistory.TryGetValue(history.Id, out var images)
                    ? images.OrderBy(x => x.CreationTime).Select(x => new RejectionHistoryImageDto
                    {
                        Id = x.Id,
                        RejectionHistoryId = x.RejectionHistoryId,
                        FileId = x.FileId,
                        FileName = x.FileName,
                        FileSize = x.FileSize,
                        Url = $"/{x.FilePath.Replace("\\", "/")}",
                        CreationTime = x.CreationTime,
                        CreatorId = x.CreatorId
                    }).ToList()
                    : new List<RejectionHistoryImageDto>()
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

        // "Bị từ chối" = Có ≥1 chỉ tiêu bị từ chối (kết quả)
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

        // "Không TN được" = Có ≥1 chỉ tiêu KTV báo không thể thử nghiệm
        var anyCannotTest = activeIndicators.Any(i =>
            i.ApprovalStatus == IndicatorApprovalStatus.CannotTest);

        if (anyCannotTest)
        {
            return TestingStatus.CannotTest;
        }

        // "Đang thử nghiệm" = Có ≥1 chỉ tiêu đã bắt đầu làm (TestDate hoặc EntryDate)
        // và đang ở các trạng thái cho phép thử nghiệm
        // - TestDate: KTV đã chọn ngày thử nghiệm
        // - EntryDate: KTV đã nhập kết quả
        var inProgressStatuses = new[]
        {
            IndicatorApprovalStatus.Pending,
            IndicatorApprovalStatus.MethodChanged,      // KH đã đồng ý đổi PP
            IndicatorApprovalStatus.AcceptedForTesting  // QLKT đã chấp nhận TN
        };
        var anyInProgress = activeIndicators.Any(i =>
            (i.TestDate.HasValue || i.EntryDate.HasValue) && inProgressStatuses.Contains(i.ApprovalStatus));

        if (anyInProgress)
        {
            return TestingStatus.InProgress;
        }

        // Mặc định: "Chưa bắt đầu"
        return TestingStatus.NotStarted;
    }

    private async Task<bool> CanEditIndicatorAsync(LaboratoryTaskIndicator indicator, LaboratoryTask task, Guid? currentUserId)
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

        // Supporter can edit (có quyền giao KTV, duyệt/từ chối, gửi QLCL)
        var isSupporter = await _supporterRepository.AnyAsync(s =>
            s.LaboratoryTaskId == task.Id && s.UserId == currentUserId.Value);
        if (isSupporter)
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

        // Loại trừ các chỉ tiêu đã "finalized" khi tính toán trạng thái:
        // - CustomerCancelled: KH đã hủy chỉ tiêu
        // - TestingRejected với CustomerConfirmationDate: QLKT từ chối TN và KH đã xác nhận đồng ý
        var activeIndicators = allTaskIndicators.Where(ti =>
            ti.ApprovalStatus != IndicatorApprovalStatus.CustomerCancelled &&
            !(ti.ApprovalStatus == IndicatorApprovalStatus.TestingRejected && ti.CustomerConfirmationDate.HasValue)).ToList();

        if (!activeIndicators.Any())
        {
            return;
        }

        // Check conditions for status calculation - ĐỒNG BỘ VỚI LaboratoryTaskAppService
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
        var allAssigned = activeIndicators.All(ti => ti.MethodId.HasValue && ti.TechnicianId.HasValue);

        // Có ≥1 chỉ tiêu đang thử nghiệm (có testDate, KTV đã check-in, status = Pending)
        // Chỉ tính những chỉ tiêu đang "testing" thực sự, không tính Approved/Entered/Rejected
        var anyTesting = activeIndicators.Any(ti =>
            ti.TestDate.HasValue && ti.ApprovalStatus == IndicatorApprovalStatus.Pending);

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
        else
        {
            // Còn chỉ tiêu chưa được giao việc
            newStatus = LaboratoryTaskStatus.UnderEvaluation;
        }

        if (task.Status != newStatus)
        {
            task.Status = newStatus;
            await _laboratoryTaskRepository.UpdateAsync(task);
        }
    }

    /// <summary>
    /// Tính status của task từ danh sách indicators đã có sẵn (không fetch lại từ DB)
    /// Dùng khi đã có indicators trong memory để tránh re-fetch và tracking issues
    /// </summary>
    private LaboratoryTaskStatus CalculateTaskStatusFromIndicators(List<LaboratoryTaskIndicator> allIndicators)
    {
        // Loại trừ các chỉ tiêu đã "finalized" khi tính toán trạng thái:
        // - CustomerCancelled: KH đã hủy chỉ tiêu
        // - TestingRejected với CustomerConfirmationDate: QLKT từ chối TN và KH đã xác nhận đồng ý
        var activeIndicators = allIndicators.Where(ti =>
            ti.ApprovalStatus != IndicatorApprovalStatus.CustomerCancelled &&
            !(ti.ApprovalStatus == IndicatorApprovalStatus.TestingRejected && ti.CustomerConfirmationDate.HasValue)).ToList();

        if (!activeIndicators.Any())
        {
            return LaboratoryTaskStatus.UnderEvaluation;
        }

        // TẤT CẢ chỉ tiêu đã gửi QLCL
        var allSentToQuality = activeIndicators.All(ti =>
            ti.ApprovalStatus == IndicatorApprovalStatus.Approved && ti.IsSentToQuality);

        // TẤT CẢ chỉ tiêu đã được duyệt (Approved)
        var allApproved = activeIndicators.All(ti => ti.ApprovalStatus == IndicatorApprovalStatus.Approved);

        // Có ≥1 chỉ tiêu đã nhập KQ (Entered - chờ duyệt) - ưu tiên hiển thị vì cần action
        var anyEntered = activeIndicators.Any(ti => ti.ApprovalStatus == IndicatorApprovalStatus.Entered);

        // Có ≥1 chỉ tiêu KTV báo không TN được - cần QLKT xử lý
        var anyCannotTest = activeIndicators.Any(ti => ti.ApprovalStatus == IndicatorApprovalStatus.CannotTest);

        // TẤT CẢ chỉ tiêu đã được giao việc (có method + KTV)
        var allAssigned = activeIndicators.All(ti => ti.MethodId.HasValue && ti.TechnicianId.HasValue);

        // Có ≥1 chỉ tiêu đang thử nghiệm (có testDate, KTV đã check-in, status = Pending)
        var anyTesting = activeIndicators.Any(ti =>
            ti.TestDate.HasValue && ti.ApprovalStatus == IndicatorApprovalStatus.Pending);

        // Logic ưu tiên (đồng bộ với UpdateTaskStatusAsync)
        // 1. CannotTest được ưu tiên CAO NHẤT - cần QLKT xử lý trước khi tiếp tục
        if (anyCannotTest)
        {
            return LaboratoryTaskStatus.UnderEvaluation;
        }
        else if (anyEntered)
        {
            return LaboratoryTaskStatus.WaitingApproval;
        }
        else if (allSentToQuality)
        {
            return LaboratoryTaskStatus.SentToQuality;
        }
        else if (allApproved)
        {
            return LaboratoryTaskStatus.Approved;
        }
        else if (allAssigned && anyTesting)
        {
            return LaboratoryTaskStatus.InProgress;
        }
        else if (allAssigned)
        {
            return LaboratoryTaskStatus.Assigned;
        }
        else
        {
            return LaboratoryTaskStatus.UnderEvaluation;
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
            TransferDate = null,
            RequestCompletionDate = task.RequestCompletionDate,
            TestDate = task.TestDate,
            TestingStatus = testingStatus,
            IsUrgent = isUrgent
        };

        // Get laboratory name
        var laboratory = await _laboratoryRepository.FindAsync(task.LaboratoryId);
        if (laboratory != null)
        {
            var labDept = await _departmentRepository.FindAsync(laboratory.DepartmentId);
            dto.LaboratoryName = labDept?.Name;
        }

        // Get customer name from test request
        var testRequest = await _testRequestRepository.FindAsync(task.TestRequestId);
        dto.CustomerName = testRequest?.CustomerName;
        dto.TestRequestStatus = testRequest?.Status;
        dto.TransferDate = testRequest?.UnderReviewDate;

        // Filter out parent indicators with children for counting
        var filteredIndicators = await FilterIndicatorsForStatusCalculationAsync(indicators);

        // Tab "MyAssignments": Show only current user's data
        if (tabType == TestingTabType.MyAssignments && currentUserId.HasValue)
        {
            // Loại trừ CustomerCancelled khỏi danh sách (từ filtered indicators)
            var userIndicators = filteredIndicators.Where(i =>
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
            // Tab "All": Show full data (loại trừ CustomerCancelled và parent indicators)
            var activeIndicators = filteredIndicators.Where(i =>
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
        // Admin, QLKT, và Người hỗ trợ: luôn xem tất cả indicators (bỏ qua viewMode = MyAssignments)
        // KTV với MyAssignments: chỉ xem indicators được assign cho họ
        // All (or null): Show all indicators
        List<LaboratoryTaskIndicator> taskIndicators;

        // Check nếu user là QLKT của task hoặc QLKT của phòng ban
        var isQLKTOfTask = task.TechnicalManagerId == currentUserId;
        var isQLKTOfLaboratory = false;
        var isSupporter = false;
        if (currentUserId.HasValue && !isAdmin && !isQLKTOfTask)
        {
            // Check xem user có phải QLKT của phòng ban (Laboratory) không
            var taskLaboratory = await _laboratoryRepository.FindAsync(task.LaboratoryId);
            isQLKTOfLaboratory = taskLaboratory?.TechnicalManagerId == currentUserId;

            // Check xem user có phải Supporter của task không
            if (!isQLKTOfLaboratory)
            {
                isSupporter = await _supporterRepository.AnyAsync(s =>
                    s.LaboratoryTaskId == task.Id && s.UserId == currentUserId.Value);
            }
        }
        var isQLKT = isQLKTOfTask || isQLKTOfLaboratory;

        if (viewMode == TestingTabType.MyAssignments && currentUserId.HasValue && !isAdmin && !isQLKT && !isSupporter)
        {
            // Chỉ KTV mới filter theo TechnicianId khi viewMode = MyAssignments
            taskIndicators = allTaskIndicators.Where(i => i.TechnicianId == currentUserId.Value).ToList();
        }
        else
        {
            taskIndicators = allTaskIndicators;
        }

        // Calculate testing status based on filtered indicators for display
        // Filter out parent indicators with children before calculating status
        var indicatorsForStatus = await FilterIndicatorsForStatusCalculationAsync(taskIndicators);
        var testingStatus = CalculateTestingStatus(indicatorsForStatus);

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
        if (laboratory != null)
        {
            var labDept = await _departmentRepository.FindAsync(laboratory.DepartmentId);
            dto.LaboratoryName = labDept?.Name;
        }

        // Get test request info (includes SampleSentDate - ngày BPGN gửi mẫu)
        var testRequest = await _testRequestRepository.FindAsync(task.TestRequestId);
        dto.CustomerName = testRequest?.CustomerName;
        dto.TestRequestStatus = testRequest?.Status;
        // Ngày nhận mẫu = ngày in phiếu thử nghiệm (BM05)
        dto.SampleReceivedDate = testRequest?.FormPrintedDate;
        dto.SampleSentDate = testRequest?.SampleSentDate ?? task.SampleSentDate;
        dto.UnderReviewDate = testRequest?.UnderReviewDate;

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

        // Set IsMy - true if current user is one of the assigned technicians
        dto.IsMy = currentUserId.HasValue && technicianIds.Contains(currentUserId.Value);

        // Count completed indicators (loại trừ CustomerCancelled và parent indicators có children)
        // Sử dụng indicatorsForStatus đã filter parent indicators
        var activeIndicators = indicatorsForStatus.Where(i => i.ApprovalStatus != IndicatorApprovalStatus.CustomerCancelled).ToList();
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
                    SampleQuantity = sample?.SampleQuantity,
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

                // Load sample received info
                var sampleReceived = await _sampleReceivedRepository.FirstOrDefaultAsync(x =>
                    x.LaboratoryTaskId == task.Id && x.SampleId == sampleId);
                if (sampleReceived != null)
                {
                    sampleDto.IsReceived = true;
                    sampleDto.ReceivedDate = sampleReceived.ReceivedDate;
                    sampleDto.ReceivedByUserName = sampleReceived.ReceivedByUserName;
                }

                sampleGroups[sampleId] = sampleDto;
            }

            // Map indicator (logic build DTO dùng chung với các endpoint update method/unit)
            sampleGroups[sampleId].Indicators.Add(
                await MapIndicatorToDtoAsync(taskIndicator, sampleIndicator, task, userNames));
        }

        dto.Samples = sampleGroups.Values.ToList();

        return dto;
    }

    /// <summary>
    /// Build TestingIndicatorDto cho 1 chỉ tiêu.
    /// Dùng chung cho MapToDetailDtoAsync (loop detail) và UpdateIndicatorMethodAsync/UpdateIndicatorUnitAsync.
    /// </summary>
    private async Task<TestingIndicatorDto> MapIndicatorToDtoAsync(
        LaboratoryTaskIndicator taskIndicator,
        TestRequestSampleIndicator sampleIndicator,
        LaboratoryTask task,
        Dictionary<Guid, string> userNames)
    {
        var indicator = await _indicatorRepository.FindAsync(sampleIndicator.IndicatorId);
        string? methodName = null;
        string? methodFormattedName = null;
        string? technicianName = null;
        string? unit = taskIndicator.Unit;

        if (taskIndicator.MethodId.HasValue)
        {
            var method = await _testMethodRepository.FindAsync(taskIndicator.MethodId.Value);
            methodName = method?.Name;
            methodFormattedName = method?.FormattedName;
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

        // Get standard info (set by QLKT)
        string? standardName = null;
        string? limitDescription = null;
        string? comparisonOperator = null;
        string? allowedLimit = null;
        string? lowerOperator = null;
        string? lowerLimit = null;
        string? upperOperator = null;
        string? upperLimit = null;
        if (taskIndicator.StandardId.HasValue)
        {
            var standard = await _standardRepository.FindAsync(taskIndicator.StandardId.Value);
            if (standard != null)
            {
                standardName = standard.Name;
                limitDescription = standard.LimitDescription;

                // Get StandardIndicator for ComparisonOperator and AllowedLimit
                if (taskIndicator.MethodId.HasValue)
                {
                    var standardIndicator = await _standardIndicatorRepository.FirstOrDefaultAsync(si =>
                        si.StandardId == taskIndicator.StandardId.Value &&
                        si.IndicatorId == sampleIndicator.IndicatorId &&
                        si.TestMethodId == taskIndicator.MethodId.Value);
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
            }
        }

        // Determine if can edit
        var hasPermission = await CanEditIndicatorAsync(taskIndicator, task, CurrentUser.Id);
        var canEdit = taskIndicator.ApprovalStatus != IndicatorApprovalStatus.Approved && hasPermission;

        // Determine if can check-in (Thực hiện)
        // Có thể check-in nếu: có quyền, status phù hợp, chưa có TestDate
        // Status phù hợp: Pending, Rejected, MethodChanged, AcceptedForTesting
        var canCheckInStatuses = new[]
        {
            IndicatorApprovalStatus.Pending,
            IndicatorApprovalStatus.Rejected,
            IndicatorApprovalStatus.MethodChanged,
            IndicatorApprovalStatus.AcceptedForTesting
        };
        var canCheckIn = hasPermission &&
                         canCheckInStatuses.Contains(taskIndicator.ApprovalStatus) &&
                         !taskIndicator.TestDate.HasValue;

        // Determine if can submit (Gửi kết quả)
        // Có thể submit nếu: có quyền, đã check-in (có TestDate), status = Pending/Rejected/Entered, có kết quả
        var canSubmit = hasPermission &&
                        taskIndicator.TestDate.HasValue &&
                        (taskIndicator.ApprovalStatus == IndicatorApprovalStatus.Pending ||
                         taskIndicator.ApprovalStatus == IndicatorApprovalStatus.Rejected ||
                         taskIndicator.ApprovalStatus == IndicatorApprovalStatus.Entered) && // Cho phép gửi lại khi đang chờ phê duyệt
                        !string.IsNullOrWhiteSpace(taskIndicator.Result);

        // Calculate indicator testing status
        var indicatorTestingStatus = CalculateIndicatorTestingStatus(taskIndicator);

        // Get indicator images
        var indicatorImages = await _indicatorImageRepository.GetListAsync(x =>
            x.LaboratoryTaskIndicatorId == taskIndicator.Id);

        return new TestingIndicatorDto
        {
            Id = taskIndicator.Id,
            TestRequestSampleIndicatorId = taskIndicator.TestRequestSampleIndicatorId,
            IndicatorId = sampleIndicator.IndicatorId,
            SpecimenTypeId = sampleIndicator.SpecimenTypeId,
            IndicatorName = indicator?.Name,
            IndicatorFormattedName = indicator?.FormattedName,
            MethodId = taskIndicator.MethodId,
            MethodName = methodName,
            MethodFormattedName = methodFormattedName,
            Unit = unit,
            Result = taskIndicator.Result,
            Note = taskIndicator.FreeTextNote,
            EntryDate = taskIndicator.EntryDate,
            AssignedDate = taskIndicator.AssignedDate,
            StartDate = taskIndicator.TestDate,  // Deprecated, use TestDate
            TestDate = taskIndicator.TestDate,
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
            }).ToList(),
            StandardId = taskIndicator.StandardId,
            StandardName = standardName,
            LimitDescription = limitDescription,
            ComparisonOperator = comparisonOperator,
            AllowedLimit = allowedLimit,
            LowerOperator = lowerOperator,
            LowerLimit = lowerLimit,
            UpperOperator = upperOperator,
            UpperLimit = upperLimit,
            ParentIndicatorId = indicator?.ParentIndicatorId
        };
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
            if (!await CanEditIndicatorAsync(indicator, task, currentUserId))
            {
                continue;
            }

            // Chỉ check-in indicators có status phù hợp cho thử nghiệm:
            // - Pending: chưa đánh giá hoặc QLKT đã chấp nhận
            // - Rejected: QLKT từ chối kết quả, cần thử nghiệm lại
            // - MethodChanged: KH đã đồng ý đổi PP, sẵn sàng thử nghiệm
            // - AcceptedForTesting: QLKT đã chấp nhận thử nghiệm
            var canCheckInStatuses = new[]
            {
                IndicatorApprovalStatus.Pending,
                IndicatorApprovalStatus.Rejected,
                IndicatorApprovalStatus.MethodChanged,
                IndicatorApprovalStatus.AcceptedForTesting
            };

            if (!canCheckInStatuses.Contains(indicator.ApprovalStatus))
            {
                continue;
            }

            // Nếu đang bị từ chối kết quả, reset ApprovalStatus về Pending để thử nghiệm lại
            if (indicator.ApprovalStatus == IndicatorApprovalStatus.Rejected)
            {
                indicator.ApprovalStatus = IndicatorApprovalStatus.Pending;
                // Clear SubmitDate khi thử nghiệm lại
                indicator.SubmitDate = null;
            }

            // Nếu là MethodChanged hoặc AcceptedForTesting, chuyển về Pending để bắt đầu TN
            if (indicator.ApprovalStatus == IndicatorApprovalStatus.MethodChanged ||
                indicator.ApprovalStatus == IndicatorApprovalStatus.AcceptedForTesting)
            {
                indicator.ApprovalStatus = IndicatorApprovalStatus.Pending;
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

        if (input.Indicators == null || !input.Indicators.Any())
        {
            throw new ValidationException("indicators", "Vui lòng chọn ít nhất 1 chỉ tiêu");
        }

        var errors = new List<string>();
        var submittedIndicators = new List<LaboratoryTaskIndicator>();

        // Fetch TẤT CẢ indicators của task một lần để:
        // 1. Tránh multiple queries
        // 2. Dùng để tính task status sau khi update
        // 3. Tránh EF tracking issues khi re-fetch
        var allTaskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => x.LaboratoryTaskId == laboratoryTaskId);
        var indicatorMap = allTaskIndicators.ToDictionary(i => i.Id);

        // Collect unique text values để tránh race condition khi batch submit có nhiều indicator với cùng text result
        var textValuesToCreate = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var inputIndicatorIds = input.Indicators.Select(i => i.IndicatorId).ToHashSet();

        foreach (var indicatorId in inputIndicatorIds)
        {
            if (indicatorMap.TryGetValue(indicatorId, out var indicator))
            {
                // Collect text values cần tạo (không phải numeric/formula)
                if (!string.IsNullOrWhiteSpace(indicator.Result) && !IsNumericOrFormulaValue(indicator.Result))
                {
                    textValuesToCreate.Add(indicator.Result.Trim());
                }
            }
        }

        // Pre-create all unique text values TRƯỚC khi process indicators
        // Điều này tránh race condition khi có nhiều indicator với cùng text value
        foreach (var textValue in textValuesToCreate)
        {
            await _testResultValueAppService.GetOrCreateByNameAsync(textValue);
        }

        foreach (var indicatorInput in input.Indicators)
        {
            if (!indicatorMap.TryGetValue(indicatorInput.IndicatorId, out var indicator))
            {
                continue;
            }

            // Kiểm tra quyền
            if (!await CanEditIndicatorAsync(indicator, task, currentUserId))
            {
                continue;
            }

            // Chỉ submit nếu status phù hợp (Pending, Rejected, Entered (cho phép gửi lại), hoặc các status cho phép thử nghiệm)
            var allowedStatuses = new[]
            {
                IndicatorApprovalStatus.Pending,
                IndicatorApprovalStatus.Rejected,
                IndicatorApprovalStatus.MethodChanged,
                IndicatorApprovalStatus.AcceptedForTesting,
                IndicatorApprovalStatus.Entered // Cho phép TNV gửi lại kết quả khi đang chờ phê duyệt
            };

            if (!allowedStatuses.Contains(indicator.ApprovalStatus))
            {
                continue;
            }

            // Kiểm tra đã nhập kết quả
            if (string.IsNullOrWhiteSpace(indicator.Result))
            {
                errors.Add($"Chỉ tiêu chưa có kết quả");
                continue;
            }

            // Validate TestDate - bắt buộc
            if (indicatorInput.TestDate == default)
            {
                errors.Add($"Vui lòng chọn ngày thử nghiệm");
                continue;
            }

            // Text values đã được tạo ở trên, không cần gọi GetOrCreateByNameAsync nữa

            // Set TestDate từ input
            indicator.TestDate = indicatorInput.TestDate;

            // Set SubmitDate và ApprovalStatus = Entered
            indicator.SubmitDate = DateTime.Now;
            indicator.EntryDate ??= DateTime.Now;
            indicator.EnteredById ??= currentUserId;
            indicator.ApprovalStatus = IndicatorApprovalStatus.Entered;

            await _laboratoryTaskIndicatorRepository.UpdateAsync(indicator, autoSave: false);
            submittedIndicators.Add(indicator);
        }

        if (errors.Any())
        {
            throw new ValidationException("indicators", string.Join("; ", errors.Distinct()));
        }

        if (!submittedIndicators.Any())
        {
            throw new ValidationException("indicators", "Không có chỉ tiêu nào được gửi duyệt");
        }

        // Deduct stock based on material consumption setup
        await DeductStockForSubmittedIndicatorsAsync(submittedIndicators, task.Id);

        // Mark corresponding batch items as externally completed
        await MarkBatchItemsAsExternallyCompletedAsync(submittedIndicators);

        // Update task status (sử dụng TẤT CẢ indicators đã cached, không fetch lại từ DB)
        // allTaskIndicators đã được update trong memory nên sẽ có ApprovalStatus mới
        var newTaskStatus = CalculateTaskStatusFromIndicators(allTaskIndicators);
        if (task.Status != newTaskStatus)
        {
            task.Status = newTaskStatus;
            await _laboratoryTaskRepository.UpdateAsync(task, autoSave: false);
        }

        // Update TestRequest status to InProgress when KTV submits results
        await TryUpdateTestRequestToInProgressAsync(task.TestRequestId);

        // Notify QLKT about submitted results (Case 1 & 6)
        if (task.TechnicalManagerId.HasValue && submittedIndicators.Any())
        {
            await NotifyTechnicalManagerOnSubmitAsync(task, submittedIndicators);
        }
    }

    private async Task DeductStockForSubmittedIndicatorsAsync(List<LaboratoryTaskIndicator> submittedIndicators, Guid taskId)
    {
        var indicatorsWithMethod = submittedIndicators.Where(i => i.MethodId.HasValue).ToList();
        if (!indicatorsWithMethod.Any()) return;

        var sampleIndicatorIds = indicatorsWithMethod
            .Select(i => i.TestRequestSampleIndicatorId).Distinct().ToList();
        var sampleIndicators = await _sampleIndicatorRepository.GetListAsync(
            si => sampleIndicatorIds.Contains(si.Id));
        var sampleIndicatorMap = sampleIndicators.ToDictionary(si => si.Id);

        var tmiQuery = await _testMethodIndicatorRepository.GetQueryableAsync();
        var consumptionQuery = await _materialConsumptionRepository.GetQueryableAsync();

        var consumptionByTmi = new Dictionary<Guid, List<MaterialConsumptionItem>>();

        foreach (var indicator in indicatorsWithMethod)
        {
            if (!sampleIndicatorMap.TryGetValue(indicator.TestRequestSampleIndicatorId, out var sampleIndicator))
                continue;

            var tmi = await AsyncExecuter.FirstOrDefaultAsync(
                tmiQuery.Where(x =>
                    x.TestMethodId == indicator.MethodId!.Value &&
                    x.IndicatorId == sampleIndicator.IndicatorId &&
                    x.SpecimenTypeId == sampleIndicator.SpecimenTypeId));

            if (tmi == null) continue;

            if (!consumptionByTmi.TryGetValue(tmi.Id, out var consumptionItems))
            {
                consumptionItems = await AsyncExecuter.ToListAsync(
                    consumptionQuery.Where(c => c.TestMethodIndicatorId == tmi.Id));
                consumptionByTmi[tmi.Id] = consumptionItems;
            }

            foreach (var item in consumptionItems)
            {
                if (item.AvgConsumptionPerSample == null || item.AvgConsumptionPerSample.Value == 0) continue;
                var consumption = new IndicatorConsumption(
                    GuidGenerator.Create(),
                    indicator.Id,
                    item.ProductId,
                    item.EquipmentId,
                    item.UnitId,
                    item.AvgConsumptionPerSample.Value);
                consumption.IsDeducted = true;
                await _indicatorConsumptionRepository.InsertAsync(consumption, autoSave: false);
            }
        }

        var allConsumptionItems = consumptionByTmi.Values.SelectMany(x => x).ToList();

        if (!allConsumptionItems.Any()) return;

        var productItems = allConsumptionItems.Where(c => c.ProductId.HasValue).ToList();
        var equipmentItems = allConsumptionItems.Where(c => c.EquipmentId.HasValue).ToList();

        if (productItems.Any())
        {
            var productIds = productItems.Select(c => c.ProductId!.Value).Distinct().ToList();
            var products = await _productRepository.GetListAsync(p => productIds.Contains(p.Id));
            var productMap = products.ToDictionary(p => p.Id);

            var skus = await _productSkuRepository.GetListAsync(s => productIds.Contains(s.ProductId));
            var skuByProduct = skus.GroupBy(s => s.ProductId).ToDictionary(g => g.Key, g => g.ToList());

            var conversionQuery = await _productUnitConversionRepository.GetQueryableAsync();

            foreach (var item in productItems)
            {
                if (!productMap.TryGetValue(item.ProductId!.Value, out var product))
                    continue;

                if (item.AvgConsumptionPerSample == null || item.AvgConsumptionPerSample.Value == 0)
                    continue;

                var deductQty = item.AvgConsumptionPerSample.Value;
                decimal baseQty;
                var consumeUnitId = item.UnitId;

                // 1. Product-level units
                if (consumeUnitId == product.PackagingUnitId)
                    baseQty = deductQty;
                else if (consumeUnitId == product.VolumeUnitId && product.VolumePerUnit > 0)
                    baseQty = deductQty / product.VolumePerUnit.Value;
                else if (consumeUnitId == product.BaseUnitId && product.PackagingQuantity > 0)
                    baseQty = deductQty * product.PackagingQuantity.Value;
                else
                {
                    decimal? factor = null;

                    // 2. SKU-level units
                    if (skuByProduct.TryGetValue(product.Id, out var pSkus))
                    {
                        foreach (var sku in pSkus)
                        {
                            if (consumeUnitId == sku.PackagingUnitId) { factor = 1; break; }
                            if (consumeUnitId == sku.VolumeUnitId && sku.VolumePerUnit > 0) { factor = (decimal)sku.VolumePerUnit.Value; break; }
                            if (consumeUnitId == sku.BaseUnitId && sku.PackagingQuantity > 0) { factor = 1m / sku.PackagingQuantity.Value; break; }
                        }
                    }

                    // 3. ProductUnitConversion table
                    if (factor == null && consumeUnitId.HasValue)
                    {
                        var conversion = await AsyncExecuter.FirstOrDefaultAsync(
                            conversionQuery.Where(c =>
                                c.ProductId == item.ProductId && c.ConvertedUnitId == consumeUnitId.Value));

                        if (conversion != null && conversion.ConvertedQuantity > 0)
                            factor = conversion.ConvertedQuantity / conversion.Quantity;
                    }

                    if (factor == null || factor.Value == 0)
                        continue;

                    baseQty = deductQty / factor.Value;
                }

                var deductions = await _fifoStockService.DeductAsync(
                    product, baseQty,
                    StockMovementType.MaterialConsumption,
                    taskId, null,
                    "Tiêu hao tự động theo chỉ tiêu");

                if (product.ProductType == ProductType.PreparedSolution && deductions.Any())
                {
                    var productSkus = skuByProduct.GetValueOrDefault(product.Id);
                    await UpdatePreparedSolutionRemainingVolumeAsync(product, productSkus, deductions);
                }
            }
        }

        if (equipmentItems.Any())
        {
            var usageByEquipment = equipmentItems
                .GroupBy(c => c.EquipmentId!.Value)
                .ToDictionary(g => g.Key, g => (int)g.Sum(c => c.Quantity));

            var equipments = await _equipmentRepository.GetListAsync(e => usageByEquipment.Keys.Contains(e.Id));

            foreach (var equipment in equipments)
            {
                equipment.UsageCount += usageByEquipment[equipment.Id];
                await _equipmentRepository.UpdateAsync(equipment, autoSave: true);
            }
        }
    }

    private async Task UpdatePreparedSolutionRemainingVolumeAsync(
        Product product, List<ProductSku>? productSkus, List<LotDeduction> deductions)
    {
        var receiptItemIds = deductions.Select(d => d.WarehouseImportReceiptItemId).ToList();
        var receiptItems = await _importReceiptItemRepository.GetListAsync(
            i => receiptItemIds.Contains(i.Id));
        var receiptIds = receiptItems.Select(i => i.WarehouseImportReceiptId).Distinct().ToList();

        var batches = await _preparedSolutionBatchRepository.GetListAsync(
            b => b.PreparedSolutionReceiptId.HasValue && receiptIds.Contains(b.PreparedSolutionReceiptId.Value));
        if (!batches.Any()) return;

        var batchByReceiptId = batches.ToDictionary(b => b.PreparedSolutionReceiptId!.Value);

        var vpu = ResolveVolumePerUnit(product, productSkus);

        foreach (var deduction in deductions)
        {
            var receiptItem = receiptItems.FirstOrDefault(i => i.Id == deduction.WarehouseImportReceiptItemId);
            if (receiptItem == null) continue;

            if (!batchByReceiptId.TryGetValue(receiptItem.WarehouseImportReceiptId, out var batch))
                continue;

            var deductedVolume = vpu > 0
                ? deduction.DeductedBaseQty * vpu
                : deduction.DeductedBaseQty;

            batch.RemainingVolume = Math.Max(0m, batch.RemainingVolume - deductedVolume);
            await _preparedSolutionBatchRepository.UpdateAsync(batch, autoSave: true);
        }
    }

    private static int ResolveVolumePerUnit(Product product, List<ProductSku>? skus)
    {
        if (product.VolumePerUnit is > 0) return product.VolumePerUnit.Value;
        if (skus != null)
        {
            foreach (var sku in skus)
            {
                if (sku.VolumePerUnit is > 0) return sku.VolumePerUnit.Value;
            }
        }
        return 0;
    }

    private async Task MarkBatchItemsAsExternallyCompletedAsync(List<LaboratoryTaskIndicator> submittedIndicators)
    {
        var indicatorIds = submittedIndicators.Select(i => i.Id).ToList();
        var batchItems = await _testBatchItemRepository.GetListAsync(x =>
            indicatorIds.Contains(x.LaboratoryTaskIndicatorId) && !x.IsExternallyCompleted);

        foreach (var item in batchItems)
        {
            var indicator = submittedIndicators.First(i => i.Id == item.LaboratoryTaskIndicatorId);
            item.IsExternallyCompleted = true;
            item.Result = indicator.Result;
            item.TestDate = indicator.TestDate;
            item.IsSubmitted = true;
            await _testBatchItemRepository.UpdateAsync(item, autoSave: false);
        }
    }

    private async Task NotifyTechnicalManagerOnSubmitAsync(LaboratoryTask task, List<LaboratoryTaskIndicator> submittedIndicators)
    {
        var testRequest = await _testRequestRepository.GetAsync(task.TestRequestId);
        var senderName = CurrentUser.Name ?? CurrentUser.UserName ?? "Thử nghiệm viên";
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

            if (!string.IsNullOrWhiteSpace(taskIndicator.SubContractMethodName))
            {
                methodName = taskIndicator.SubContractMethodName;
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

        if (indicator.ApprovalStatus == IndicatorApprovalStatus.CannotTest)
        {
            return IndicatorTestingStatus.CannotTest; // KTV báo không TN được
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

        // Các trạng thái có thể thử nghiệm: Pending, MethodChanged, AcceptedForTesting
        var testableStatuses = new[]
        {
            IndicatorApprovalStatus.Pending,
            IndicatorApprovalStatus.MethodChanged,
            IndicatorApprovalStatus.AcceptedForTesting
        };

        if (testableStatuses.Contains(indicator.ApprovalStatus))
        {
            // Đã bắt đầu nhập (có EntryDate hoặc TestDate) → Testing
            if (indicator.EntryDate.HasValue || indicator.TestDate.HasValue)
            {
                return IndicatorTestingStatus.Testing;
            }
            return IndicatorTestingStatus.Todo; // Chưa làm gì
        }

        // Các trạng thái khác (MethodChangeRequested, TestingRejected) - chờ KH xác nhận
        return IndicatorTestingStatus.Todo;
    }

    /// <summary>
    /// Lọc ra các chỉ tiêu cần tính toán status (loại bỏ parent indicators có children).
    /// Parent indicators có children chỉ để hiển thị, không cần xử lý (testing, approve).
    /// Chỉ tính: sub-indicators và standalone indicators (không có children).
    /// </summary>
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

    #region Sample Received

    /// <summary>
    /// KTV nhận mẫu - đánh dấu mẫu đã nhận trước khi thực hiện thí nghiệm
    /// </summary>
    [Authorize(LabSpecimenManagementPermissions.LaboratoryTasks.Edit)]
    public async Task ReceiveSampleAsync(Guid laboratoryTaskId, Guid sampleId)
    {
        var currentUserId = CurrentUser.Id;
        if (!currentUserId.HasValue)
        {
            throw new ForbiddenAccessException("Không xác định được người dùng hiện tại");
        }

        var task = await _laboratoryTaskRepository.GetAsync(laboratoryTaskId);
        if (task == null)
        {
            throw new EntityNotFoundException("LaboratoryTask", laboratoryTaskId);
        }

        // Kiểm tra sample có thuộc task này không (qua indicators)
        var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => x.LaboratoryTaskId == laboratoryTaskId);
        var sampleIds = new HashSet<Guid>();
        foreach (var ti in taskIndicators)
        {
            var si = await _sampleIndicatorRepository.FindAsync(ti.TestRequestSampleIndicatorId);
            if (si != null)
            {
                sampleIds.Add(si.TestRequestSampleId);
            }
        }

        if (!sampleIds.Contains(sampleId))
        {
            throw new ValidationException("sampleId", "Mẫu không thuộc công việc này");
        }

        // Kiểm tra quyền: phải là KTV được giao việc hoặc supporter
        var isAssignedTechnician = taskIndicators.Any(ti => ti.TechnicianId == currentUserId);
        var supporters = await _supporterRepository.GetListAsync(x => x.LaboratoryTaskId == laboratoryTaskId);
        var isSupporter = supporters.Any(s => s.UserId == currentUserId);

        if (!isAssignedTechnician && !isSupporter)
        {
            throw new ForbiddenAccessException("Bạn không có quyền nhận mẫu này");
        }

        // Kiểm tra đã nhận chưa
        var existingReceived = await _sampleReceivedRepository.FirstOrDefaultAsync(x =>
            x.LaboratoryTaskId == laboratoryTaskId && x.SampleId == sampleId);

        if (existingReceived != null)
        {
            throw new ValidationException("sampleId", "Mẫu này đã được nhận trước đó");
        }

        // Lấy tên người dùng hiện tại
        var userName = await _userNameProvider.GetUserNameAsync(currentUserId.Value);

        // Tạo bản ghi nhận mẫu
        var sampleReceived = new LaboratoryTaskSampleReceived(
            _guidGenerator.Create(),
            laboratoryTaskId,
            sampleId,
            DateTime.Now,
            currentUserId.Value,
            userName
        );

        await _sampleReceivedRepository.InsertAsync(sampleReceived);
    }

    /// <summary>
    /// Kiểm tra mẫu đã được nhận chưa
    /// </summary>
    public async Task<bool> IsSampleReceivedAsync(Guid laboratoryTaskId, Guid sampleId)
    {
        var received = await _sampleReceivedRepository.FirstOrDefaultAsync(x =>
            x.LaboratoryTaskId == laboratoryTaskId && x.SampleId == sampleId);
        return received != null;
    }

    /// <summary>
    /// KTV báo không thể thử nghiệm chỉ tiêu
    /// </summary>
    public async Task<TestingIndicatorDto> MarkCannotTestAsync(Guid laboratoryTaskId, Guid indicatorId, CannotTestDto input)
    {
        var currentUserId = CurrentUser.Id;
        if (!currentUserId.HasValue)
        {
            throw new Volo.Abp.Authorization.AbpAuthorizationException("Người dùng chưa đăng nhập");
        }

        // Get laboratory task
        var task = await _laboratoryTaskRepository.GetAsync(laboratoryTaskId);
        if (task == null)
        {
            throw new EntityNotFoundException("LaboratoryTask", laboratoryTaskId);
        }

        // Get indicator
        var indicator = await _laboratoryTaskIndicatorRepository.GetAsync(indicatorId);
        if (indicator == null || indicator.LaboratoryTaskId != laboratoryTaskId)
        {
            throw new EntityNotFoundException("LaboratoryTaskIndicator", indicatorId);
        }

        // Validate indicator can be marked as cannot test
        // Allow for: Pending, Rejected (KQ bị từ chối), MethodChanged, AcceptedForTesting, TestingRejected
        var allowedStatuses = new[]
        {
            IndicatorApprovalStatus.Pending,
            IndicatorApprovalStatus.Rejected,           // KQ bị từ chối - cần làm lại
            IndicatorApprovalStatus.MethodChanged,      // Đã đổi PP - đang làm
            IndicatorApprovalStatus.AcceptedForTesting, // Đã được chấp nhận TN - đang làm
            IndicatorApprovalStatus.TestingRejected     // QLKT từ chối TN - có thể KTV vẫn muốn báo lý do
        };

        if (!allowedStatuses.Contains(indicator.ApprovalStatus))
        {
            throw new ValidationException("indicatorId", $"Chỉ tiêu đang ở trạng thái không thể báo 'Không TN được'. Trạng thái hiện tại: {indicator.ApprovalStatus}");
        }

        // Create rejection history with IsCannotTest = true
        var rejectionHistory = new IndicatorRejectionHistory(
            _guidGenerator.Create(),
            indicatorId,
            input.Reason,
            currentUserId.Value,
            DateTime.Now,
            isCannotTest: true,
            reportedByRole: "Technician"
        );

        await _rejectionHistoryRepository.InsertAsync(rejectionHistory, autoSave: true);

        // Update indicator status to CannotTest
        indicator.ApprovalStatus = IndicatorApprovalStatus.CannotTest;
        indicator.RejectionReason = input.Reason;
        await _laboratoryTaskIndicatorRepository.UpdateAsync(indicator, autoSave: true);

        // Update task status to UnderEvaluation (chuyển về QLKT đánh giá)
        task.Status = LaboratoryTaskStatus.UnderEvaluation;
        await _laboratoryTaskRepository.UpdateAsync(task, autoSave: true);

        // Send notification to Technical Manager
        if (task.TechnicalManagerId.HasValue)
        {
            // Get indicator info through TestRequestSampleIndicator
            var sampleIndicator = await _sampleIndicatorRepository.FirstOrDefaultAsync(x => x.Id == indicator.TestRequestSampleIndicatorId);
            var indicatorEntity = sampleIndicator != null
                ? await _indicatorRepository.FirstOrDefaultAsync(x => x.Id == sampleIndicator.IndicatorId)
                : null;
            var indicatorName = indicatorEntity?.Name ?? "Không xác định";
            var senderName = await _userNameProvider.GetUserNameAsync(currentUserId.Value) ?? "KTV";

            // Get sample info for notification
            var sample = sampleIndicator != null
                ? await _sampleRepository.FirstOrDefaultAsync(x => x.Id == sampleIndicator.TestRequestSampleId)
                : null;
            var sampleName = sample?.SampleName ?? "";

            var methodName = indicator.MethodId.HasValue
                ? (await _testMethodRepository.FirstOrDefaultAsync(x => x.Id == indicator.MethodId.Value))?.Name ?? ""
                : "";

            // Use existing notification method for single indicator
            await _notificationCreator.NotifyIndicatorSubmittedToTechnicalManagerAsync(
                task.TechnicalManagerId.Value,
                task.ContractNumber ?? "",
                task.TestRequestId,
                senderName,
                1,
                task.RequestCompletionDate,
                sampleName,
                $"[Không TN được] {indicatorName}",
                methodName
            );
        }

        // Build and return indicator DTO
        return await BuildIndicatorDtoAsync(indicator, task);
    }

    /// <summary>
    /// Upload hình ảnh cho rejection history
    /// </summary>
    public async Task<RejectionHistoryImageDto> UploadRejectionHistoryImageAsync(
        Guid rejectionHistoryId,
        string fileId,
        string fileName,
        string filePath,
        long fileSize)
    {
        // Validate rejection history exists
        var rejectionHistory = await _rejectionHistoryRepository.FirstOrDefaultAsync(x => x.Id == rejectionHistoryId);
        if (rejectionHistory == null)
        {
            throw new EntityNotFoundException("IndicatorRejectionHistory", rejectionHistoryId);
        }

        // Check max images (5)
        var existingImages = await _rejectionHistoryImageRepository.GetListAsync(x => x.RejectionHistoryId == rejectionHistoryId);
        if (existingImages.Count >= 5)
        {
            throw new ValidationException("images", "Số lượng hình ảnh tối đa là 5");
        }

        // Create image record
        var image = new RejectionHistoryImage(
            _guidGenerator.Create(),
            rejectionHistoryId,
            fileId,
            fileName,
            filePath,
            fileSize
        );

        await _rejectionHistoryImageRepository.InsertAsync(image);

        return new RejectionHistoryImageDto
        {
            Id = image.Id,
            RejectionHistoryId = image.RejectionHistoryId,
            FileId = image.FileId,
            FileName = image.FileName,
            FileSize = image.FileSize,
            Url = $"/{image.FilePath.Replace("\\", "/")}",
            CreationTime = image.CreationTime,
            CreatorId = image.CreatorId
        };
    }

    /// <summary>
    /// Xóa hình ảnh rejection history
    /// </summary>
    public async Task DeleteRejectionHistoryImageAsync(Guid imageId)
    {
        var image = await _rejectionHistoryImageRepository.FirstOrDefaultAsync(x => x.Id == imageId);
        if (image == null)
        {
            throw new EntityNotFoundException("RejectionHistoryImage", imageId);
        }

        await _rejectionHistoryImageRepository.DeleteAsync(image);
    }

    /// <summary>
    /// Build indicator DTO from entity
    /// </summary>
    private async Task<TestingIndicatorDto> BuildIndicatorDtoAsync(LaboratoryTaskIndicator taskIndicator, LaboratoryTask task)
    {
        // Get related data
        var sampleIndicator = await _sampleIndicatorRepository.FirstOrDefaultAsync(x =>
            x.Id == taskIndicator.TestRequestSampleIndicatorId);

        var indicator = sampleIndicator != null
            ? await _indicatorRepository.FirstOrDefaultAsync(x => x.Id == sampleIndicator.IndicatorId)
            : null;

        var method = taskIndicator.MethodId.HasValue
            ? await _testMethodRepository.FirstOrDefaultAsync(x => x.Id == taskIndicator.MethodId.Value)
            : null;
        var methodName = method?.Name;
        var methodFormattedName = method?.FormattedName;

        if (!string.IsNullOrWhiteSpace(taskIndicator.SubContractMethodName))
        {
            methodName = taskIndicator.SubContractMethodName;
            methodFormattedName = null;
        }

        var technicianName = taskIndicator.TechnicianId.HasValue
            ? await _userNameProvider.GetUserNameAsync(taskIndicator.TechnicianId.Value)
            : null;

        // Get rejection histories
        var rejectionHistories = await _rejectionHistoryRepository.GetListAsync(x => x.IndicatorId == taskIndicator.Id);
        var latestRejection = rejectionHistories.OrderByDescending(h => h.RejectedDate).FirstOrDefault();

        // Get indicator images
        var indicatorImages = await _indicatorImageRepository.GetListAsync(x =>
            x.LaboratoryTaskIndicatorId == taskIndicator.Id);

        // Get standard info
        string? standardName = null;
        string? limitDescription = null;
        string? comparisonOperator = null;
        string? allowedLimit = null;
        string? lowerOperator = null;
        string? lowerLimit = null;
        string? upperOperator = null;
        string? upperLimit = null;

        if (taskIndicator.StandardId.HasValue && sampleIndicator != null)
        {
            var standard = await _standardRepository.FirstOrDefaultAsync(x => x.Id == taskIndicator.StandardId.Value);
            standardName = standard?.Name;

            var standardIndicator = await _standardIndicatorRepository.FirstOrDefaultAsync(x =>
                x.StandardId == taskIndicator.StandardId.Value && x.IndicatorId == sampleIndicator.IndicatorId);

            if (standardIndicator != null)
            {
                // Build limitDescription from ComparisonOperator and AllowedLimit
                if (!string.IsNullOrEmpty(standardIndicator.ComparisonOperator) && !string.IsNullOrEmpty(standardIndicator.AllowedLimit))
                {
                    limitDescription = $"{standardIndicator.ComparisonOperator} {standardIndicator.AllowedLimit}";
                }
                comparisonOperator = standardIndicator.ComparisonOperator;
                allowedLimit = standardIndicator.AllowedLimit;
                lowerOperator = standardIndicator.LowerOperator;
                lowerLimit = standardIndicator.LowerLimit;
                upperOperator = standardIndicator.UpperOperator;
                upperLimit = standardIndicator.UpperLimit;
            }
        }

        // Get unit from standardIndicator or taskIndicator
        string? unit = null;
        if (taskIndicator.StandardId.HasValue && sampleIndicator != null)
        {
            var standardIndicatorForUnit = await _standardIndicatorRepository.FirstOrDefaultAsync(x =>
                x.StandardId == taskIndicator.StandardId.Value && x.IndicatorId == sampleIndicator.IndicatorId);
            unit = standardIndicatorForUnit?.Unit;
        }

        // Calculate testing status
        var indicatorTestingStatus = CalculateIndicatorTestingStatus(taskIndicator);

        return new TestingIndicatorDto
        {
            Id = taskIndicator.Id,
            TestRequestSampleIndicatorId = taskIndicator.TestRequestSampleIndicatorId,
            IndicatorId = sampleIndicator?.IndicatorId ?? Guid.Empty,
            IndicatorName = indicator?.Name,
            IndicatorFormattedName = indicator?.FormattedName,
            MethodId = taskIndicator.MethodId,
            MethodName = methodName,
            MethodFormattedName = methodFormattedName,
            Unit = unit,
            Result = taskIndicator.Result,
            Note = taskIndicator.FreeTextNote,
            EntryDate = taskIndicator.EntryDate,
            AssignedDate = taskIndicator.AssignedDate,
            StartDate = taskIndicator.TestDate,
            TestDate = taskIndicator.TestDate,
            SubmitDate = taskIndicator.SubmitDate,
            CompletionDate = taskIndicator.ApprovalDate,
            ApprovalStatus = taskIndicator.ApprovalStatus,
            TestingStatus = indicatorTestingStatus,
            LatestRejectionReason = latestRejection?.RejectionReason ?? taskIndicator.RejectionReason,
            RejectionHistoryCount = rejectionHistories.Count,
            TechnicianId = taskIndicator.TechnicianId,
            TechnicianName = technicianName,
            CanEdit = false, // Cannot edit after marking cannot test
            CanCheckIn = false,
            CanSubmit = false,
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
            StandardId = taskIndicator.StandardId,
            StandardName = standardName,
            LimitDescription = limitDescription,
            ComparisonOperator = comparisonOperator,
            AllowedLimit = allowedLimit,
            LowerOperator = lowerOperator,
            LowerLimit = lowerLimit,
            UpperOperator = upperOperator,
            UpperLimit = upperLimit
        };
    }

    #endregion

    #region Sample Assignment Log

    /// <summary>
    /// Lấy danh sách Sổ nhận mẫu giao việc - query từ LaboratoryTaskIndicator
    /// Không yêu cầu QualityManagementTask đã tạo
    /// </summary>
    public async Task<PagedResultDto<QualityManagement.SampleAssignmentLogDto>> GetSampleAssignmentLogAsync(QualityManagement.GetSampleAssignmentLogInput input)
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

        // Build base query from LaboratoryTaskIndicator (without user joins - resolve names later)
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
                        CustomerName = tr.CustomerName,
                        ContractDate = tr.ContractDate,
                        TestRequestStatus = tr.Status,
                        RequestCompletionDate = si.RequestDate ?? tr.ExpectedCompletionDate ?? lt.RequestCompletionDate,

                        // Sample info
                        SampleId = s.Id,
                        SampleCode = s.SampleCode,
                        SampleName = s.SampleName,
                        SampleQuantity = s.SampleQuantity,
                        SampleStatusText = s.StatusText,

                        // Indicator info
                        TestRequestSampleIndicatorId = si.Id,
                        LaboratoryTaskIndicatorId = li.Id,
                        IndicatorId = si.IndicatorId,
                        IndicatorName = mi.Name,
                        IndicatorFormattedName = mi.FormattedName,
                        MethodId = li.MethodId,
                        MethodName = !string.IsNullOrWhiteSpace(li.SubContractMethodName)
                            ? li.SubContractMethodName
                            : (tm != null ? tm.Name : null),
                        MethodFormattedName = !string.IsNullOrWhiteSpace(li.SubContractMethodName)
                            ? null
                            : (tm != null ? tm.FormattedName : null),
                        Unit = li.Unit,
                        // Ngày nhận mẫu = ngày in phiếu thử nghiệm (BM05)
                        TransferDate = tr.FormPrintedDate,
                        TestDate = li.TestDate,
                        Result = li.Result,

                        // Assignment info (user IDs - resolve names later)
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

        // Apply user access control (same as GetListAsync)
        var supporterTaskIds = new HashSet<Guid>();
        if (currentUserId.HasValue && !isAdmin)
        {
            var managedLaboratoryIds = (await _laboratoryRepository.GetListAsync(
                lab => lab.TechnicalManagerId == currentUserId.Value))
                .Select(lab => lab.Id)
                .ToHashSet();

            supporterTaskIds = (await _supporterRepository.GetListAsync(
                s => s.UserId == currentUserId.Value))
                .Select(s => s.LaboratoryTaskId)
                .ToHashSet();

            var currentUser = await _identityUserRepository.FindAsync(currentUserId.Value);
            var userDepartmentId = currentUser?.GetProperty<Guid?>(AppConstants.PropertyDepartmentId);

            var userLabIds = new HashSet<Guid>();
            if (userDepartmentId.HasValue)
            {
                userLabIds = (await _laboratoryRepository.GetListAsync(
                    lab => lab.DepartmentId == userDepartmentId.Value))
                    .Select(lab => lab.Id)
                    .ToHashSet();
            }

            query = query.Where(x =>
                managedLaboratoryIds.Contains(x.LaboratoryId) ||
                supporterTaskIds.Contains(x.LaboratoryTaskId) ||
                userLabIds.Contains(x.LaboratoryId));
        }

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
            query = query.Where(x => input.LaboratoryIds.Contains(x.LaboratoryId));
        }

        if (input.IndicatorIds != null && input.IndicatorIds.Any())
        {
            var indicatorIdSet = input.IndicatorIds.ToHashSet();
            var matchingSampleIndicatorIds = sampleIndicatorQuery
                .Where(si => indicatorIdSet.Contains(si.IndicatorId))
                .Select(si => si.Id)
                .ToHashSet();

            query = query.Where(x => matchingSampleIndicatorIds.Contains(x.TestRequestSampleIndicatorId));
        }

        if (input.TechnicianIds != null && input.TechnicianIds.Any())
        {
            var technicianIdSet = input.TechnicianIds.ToHashSet();
            query = query.Where(x => x.TechnicianId.HasValue && technicianIdSet.Contains(x.TechnicianId.Value));
        }

        if (input.Results != null && input.Results.Any())
        {
            var resultSet = input.Results.ToHashSet();
            query = query.Where(x => x.Result != null && resultSet.Contains(x.Result));
        }

        // Apply sorting
        var sortingKey = string.IsNullOrWhiteSpace(input.Sorting) ? SortingFields.ContractDateDesc : input.Sorting.ToLower();

        var sortedQuery = sortingKey switch
        {
            SortingFields.RequestCompletionDate => query.OrderBy(x => x.RequestCompletionDate).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName),
            SortingFields.RequestCompletionDateDesc => query.OrderByDescending(x => x.RequestCompletionDate).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName),
            SortingFields.ContractNumber => query.OrderBy(x => x.ContractNumber).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName),
            SortingFields.ContractNumberDesc => query.OrderByDescending(x => x.ContractNumber).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName),
            SortingFields.TransferDate => query.OrderBy(x => x.TransferDate).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName),
            SortingFields.TransferDateDesc => query.OrderByDescending(x => x.TransferDate).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName),
            SortingFields.ContractDate => query.OrderBy(x => x.ContractDate).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName),
            SortingFields.ContractDateDesc => query.OrderByDescending(x => x.ContractDate).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName),
            _ => query.OrderByDescending(x => x.ContractDate).ThenBy(x => x.SampleId).ThenBy(x => x.IndicatorName)
        };

        // Pagination by contract count (not row count)
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
                    x.QualityApprovalStatus,
                    x.TestRequestStatus);
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
        var dtos = items.Select(x => new QualityManagement.SampleAssignmentLogDto
        {
            TestRequestId = x.TestRequestId,
            QualityTaskId = null,
            LaboratoryTaskId = x.LaboratoryTaskId,
            ContractNumber = x.ContractNumber,
            CustomerName = x.CustomerName,
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
            TestingStatus = MapTestingStatus(x.IndicatorApprovalStatus, x.TestDate, x.Result),
            QLKTDisplayStatus = MapQLKTDisplayStatus(
                x.IndicatorApprovalStatus,
                x.MethodId,
                x.TechnicianId,
                x.TestDate,
                x.Result,
                x.QualityApprovalStatus,
                x.TestRequestStatus),
            QualityApprovalStatus = x.QualityApprovalStatus,
            IsReportPublished = false,
            CanEdit = currentUserId.HasValue && x.TechnicianId == currentUserId
        }).ToList();

        return new PagedResultDto<QualityManagement.SampleAssignmentLogDto>(totalContractCount, dtos);
    }

    /// <summary>
    /// Lấy danh sách kết quả distinct cho dropdown filter
    /// </summary>
    public async Task<ListResultDto<string>> GetDistinctResultsAsync(QualityManagement.GetSampleAssignmentLogInput input)
    {
        var currentUserId = CurrentUser.Id;
        var isAdmin = CurrentUser.IsInRole(AppConstants.RoleAdmin);

        var labIndicatorQuery = await _laboratoryTaskIndicatorRepository.GetQueryableAsync();
        var labTaskQuery = await _laboratoryTaskRepository.GetQueryableAsync();
        var sampleIndicatorQuery = await _sampleIndicatorRepository.GetQueryableAsync();
        var sampleQuery = await _sampleRepository.GetQueryableAsync();
        var testRequestQuery = await _testRequestRepository.GetQueryableAsync();
        var masterIndicatorQuery = await _indicatorRepository.GetQueryableAsync();

        var query = from li in labIndicatorQuery
                    join lt in labTaskQuery on li.LaboratoryTaskId equals lt.Id
                    join si in sampleIndicatorQuery on li.TestRequestSampleIndicatorId equals si.Id
                    join s in sampleQuery on si.TestRequestSampleId equals s.Id
                    join tr in testRequestQuery on lt.TestRequestId equals tr.Id
                    join mi in masterIndicatorQuery on si.IndicatorId equals mi.Id
                    where li.Result != null && li.Result != ""
                    select new
                    {
                        Result = li.Result,
                        LaboratoryTaskId = lt.Id,
                        LaboratoryId = lt.LaboratoryId,
                        ContractNumber = tr.ContractNumber,
                        SampleCode = s.SampleCode,
                        SampleName = s.SampleName,
                        IndicatorName = mi.Name,
                        // Ngày nhận mẫu = ngày in phiếu thử nghiệm (BM05)
                        TransferDate = tr.FormPrintedDate,
                        TestRequestSampleIndicatorId = si.Id,
                        TechnicianId = li.TechnicianId,
                        IndicatorApprovalStatus = li.ApprovalStatus,
                        MethodId = li.MethodId,
                        TestDate = li.TestDate,
                        TestRequestStatus = tr.Status
                    };

        // Access control
        if (currentUserId.HasValue && !isAdmin)
        {
            var managedLaboratoryIds = (await _laboratoryRepository.GetListAsync(
                lab => lab.TechnicalManagerId == currentUserId.Value))
                .Select(lab => lab.Id)
                .ToHashSet();

            var supporterTaskIds = (await _supporterRepository.GetListAsync(
                s => s.UserId == currentUserId.Value))
                .Select(s => s.LaboratoryTaskId)
                .ToHashSet();

            var currentUser = await _identityUserRepository.FindAsync(currentUserId.Value);
            var userDepartmentId = currentUser?.GetProperty<Guid?>(AppConstants.PropertyDepartmentId);

            var userLabIds = new HashSet<Guid>();
            if (userDepartmentId.HasValue)
            {
                userLabIds = (await _laboratoryRepository.GetListAsync(
                    lab => lab.DepartmentId == userDepartmentId.Value))
                    .Select(lab => lab.Id)
                    .ToHashSet();
            }

            query = query.Where(x =>
                managedLaboratoryIds.Contains(x.LaboratoryId) ||
                supporterTaskIds.Contains(x.LaboratoryTaskId) ||
                userLabIds.Contains(x.LaboratoryId));
        }

        // Apply same filters as GetSampleAssignmentLogAsync (except Results)
        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var searchLower = input.Search.ToLower();
            query = query.Where(x =>
                (x.ContractNumber != null && x.ContractNumber.ToLower().Contains(searchLower)) ||
                (x.SampleCode != null && x.SampleCode.ToLower().Contains(searchLower)) ||
                (x.SampleName != null && x.SampleName.ToLower().Contains(searchLower)) ||
                (x.IndicatorName != null && x.IndicatorName.ToLower().Contains(searchLower)));
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
            query = query.Where(x => input.LaboratoryIds.Contains(x.LaboratoryId));
        }

        if (input.IndicatorIds != null && input.IndicatorIds.Any())
        {
            var indicatorIdSet = input.IndicatorIds.ToHashSet();
            var matchingSampleIndicatorIds = sampleIndicatorQuery
                .Where(si => indicatorIdSet.Contains(si.IndicatorId))
                .Select(si => si.Id)
                .ToHashSet();

            query = query.Where(x => matchingSampleIndicatorIds.Contains(x.TestRequestSampleIndicatorId));
        }

        if (input.TechnicianIds != null && input.TechnicianIds.Any())
        {
            var technicianIdSet = input.TechnicianIds.ToHashSet();
            query = query.Where(x => x.TechnicianId.HasValue && technicianIdSet.Contains(x.TechnicianId.Value));
        }

        var allItems = query.ToList();

        // Apply QlktDisplayStatuses in-memory filter
        if (input.QlktDisplayStatuses != null && input.QlktDisplayStatuses.Count > 0)
        {
            var qualityIndicatorQuery = await _qualityIndicatorRepository.GetQueryableAsync();
            var labIndicatorIds = allItems.Select(x => x.LaboratoryTaskId).Distinct().ToList();

            var statusSet = new HashSet<int>(input.QlktDisplayStatuses);
            allItems = allItems.Where(x =>
            {
                var status = MapQLKTDisplayStatus(
                    x.IndicatorApprovalStatus,
                    x.MethodId,
                    x.TechnicianId,
                    x.TestDate,
                    x.Result,
                    null,
                    x.TestRequestStatus);
                return statusSet.Contains((int)status);
            }).ToList();
        }

        var distinctResults = allItems
            .Select(x => x.Result!)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        return new ListResultDto<string>(distinctResults);
    }

    /// <summary>
    /// Thống kê Sổ nhận mẫu giao việc theo QLKTDisplayStatus (không phụ thuộc filter)
    /// </summary>
    public async Task<QualityManagement.SampleAssignmentLogStatisticsDto> GetSampleAssignmentLogStatisticsAsync()
    {
        var currentUserId = CurrentUser.Id;
        var isAdmin = CurrentUser.IsInRole(AppConstants.RoleAdmin);

        // Get queryables
        var labIndicatorQuery = await _laboratoryTaskIndicatorRepository.GetQueryableAsync();
        var labTaskQuery = await _laboratoryTaskRepository.GetQueryableAsync();
        var sampleIndicatorQuery = await _sampleIndicatorRepository.GetQueryableAsync();
        var testRequestQuery = await _testRequestRepository.GetQueryableAsync();
        var qualityIndicatorQuery = await _qualityIndicatorRepository.GetQueryableAsync();

        // Build base query
        var query = from li in labIndicatorQuery
                    join lt in labTaskQuery on li.LaboratoryTaskId equals lt.Id
                    join si in sampleIndicatorQuery on li.TestRequestSampleIndicatorId equals si.Id
                    join tr in testRequestQuery on lt.TestRequestId equals tr.Id
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
                        TestRequestStatus = tr.Status,
                    };

        // Apply user access control (same as GetSampleAssignmentLogAsync)
        if (currentUserId.HasValue && !isAdmin)
        {
            var managedLaboratoryIds = (await _laboratoryRepository.GetListAsync(
                lab => lab.TechnicalManagerId == currentUserId.Value))
                .Select(lab => lab.Id)
                .ToHashSet();

            var supporterTaskIds = (await _supporterRepository.GetListAsync(
                s => s.UserId == currentUserId.Value))
                .Select(s => s.LaboratoryTaskId)
                .ToHashSet();

            var currentUser = await _identityUserRepository.FindAsync(currentUserId.Value);
            var userDepartmentId = currentUser?.GetProperty<Guid?>(AppConstants.PropertyDepartmentId);

            var userLabIds = new HashSet<Guid>();
            if (userDepartmentId.HasValue)
            {
                userLabIds = (await _laboratoryRepository.GetListAsync(
                    lab => lab.DepartmentId == userDepartmentId.Value))
                    .Select(lab => lab.Id)
                    .ToHashSet();
            }

            query = query.Where(x =>
                managedLaboratoryIds.Contains(x.LaboratoryId) ||
                supporterTaskIds.Contains(x.LaboratoryTaskId) ||
                userLabIds.Contains(x.LaboratoryId));
        }

        // Statistics reflect the current user's own assigned indicators only
        if (currentUserId.HasValue)
        {
            query = query.Where(x => x.TechnicianId == currentUserId.Value);
        }

        // Materialize and compute display status
        var allItems = query.ToList();
        var statusCounts = allItems
            .Select(x => MapQLKTDisplayStatus(
                x.IndicatorApprovalStatus, x.MethodId, x.TechnicianId,
                x.TestDate, x.Result, x.QualityApprovalStatus, x.TestRequestStatus))
            .ToList();

        return new QualityManagement.SampleAssignmentLogStatisticsDto
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

    private static TestingStatus? MapTestingStatus(
        IndicatorApprovalStatus approvalStatus,
        DateTime? testDate,
        string? result)
    {
        if (!string.IsNullOrWhiteSpace(result))
        {
            return approvalStatus == IndicatorApprovalStatus.Approved
                ? TestingStatus.Completed
                : TestingStatus.PendingApproval;
        }

        if (testDate.HasValue)
        {
            return TestingStatus.InProgress;
        }

        return approvalStatus == IndicatorApprovalStatus.AcceptedForTesting
            ? TestingStatus.NotStarted
            : null;
    }

    /// <summary>
    /// Map indicator data to QLKTDisplayStatus for "Sổ nhận mẫu" tab (Testing page)
    /// Based on KH flow: Cần làm → Đang TN → Chờ QLKT phê duyệt → Hoàn thành
    /// </summary>
    private static QLKTDisplayStatus MapQLKTDisplayStatus(
        IndicatorApprovalStatus approvalStatus,
        Guid? methodId,
        Guid? technicianId,
        DateTime? testDate,
        string? result,
        QualityIndicatorApprovalStatus? qualityApprovalStatus,
        TestRequestStatus testRequestStatus = TestRequestStatus.New)
    {
        if (testRequestStatus == TestRequestStatus.Cancelled)
        {
            return QLKTDisplayStatus.RequestCancelled;
        }

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
                return QLKTDisplayStatus.UnderReview;
        }

        // 2. Handle Approved status - QLKT đã duyệt → Hoàn thành (không cần check QLCL ở trang Testing)
        if (approvalStatus == IndicatorApprovalStatus.Approved)
        {
            return QLKTDisplayStatus.Completed;
        }

        // 3. Handle Entered status - Chờ QLKT phê duyệt
        if (approvalStatus == IndicatorApprovalStatus.Entered)
        {
            return QLKTDisplayStatus.WaitingQLKTApproval;
        }

        // 4. Handle AcceptedForTesting - check assignment and testing progress
        if (approvalStatus == IndicatorApprovalStatus.AcceptedForTesting)
        {
            if (!technicianId.HasValue || !methodId.HasValue)
            {
                return QLKTDisplayStatus.WaitingForAssignment;
            }

            if (!testDate.HasValue)
            {
                return QLKTDisplayStatus.ToDo;
            }

            return QLKTDisplayStatus.Testing;
        }

        return QLKTDisplayStatus.UnderReview;
    }

    /// <summary>
    /// Helper to paginate items by contract count (not row count)
    /// Returns all items belonging to the contracts within the specified page
    /// </summary>
    private static List<T> GetItemsByContractPagination<T>(List<T> allItems, int skipContracts, int takeContracts) where T : class
    {
        // Use reflection to get ContractNumber property
        var contractProperty = typeof(T).GetProperty("ContractNumber");
        if (contractProperty == null) return allItems.Skip(skipContracts).Take(takeContracts).ToList();

        // Get unique contracts in order
        var uniqueContracts = allItems
            .Select(x => contractProperty.GetValue(x)?.ToString())
            .Distinct()
            .ToList();

        // Get contracts for current page
        var contractsInPage = uniqueContracts.Skip(skipContracts).Take(takeContracts).ToHashSet();

        // Return all items belonging to those contracts
        return allItems.Where(x => contractsInPage.Contains(contractProperty.GetValue(x)?.ToString())).ToList();
    }

    #endregion
}
