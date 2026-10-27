using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.Equipments;
using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.PreparedSolutions;
using LabSpecimenManagement.Products;
using LabSpecimenManagement.TestBatches;
using LabSpecimenManagement.Testing;
using LabSpecimenManagement.TestRequests;
using LabSpecimenManagement.StockAdjustments;
using LabSpecimenManagement.WarehouseImports;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace LabSpecimenManagement.TestBatches;

[Authorize]
public class TestBatchAppService : ApplicationService, ITestBatchAppService
{
    private readonly IRepository<TestBatch, Guid> _testBatchRepository;
    private readonly IRepository<TestBatchItem, Guid> _testBatchItemRepository;
    private readonly IRepository<TestBatchConsumption, Guid> _testBatchConsumptionRepository;
    private readonly IRepository<LaboratoryTaskIndicator, Guid> _laboratoryTaskIndicatorRepository;
    private readonly IRepository<LaboratoryTask, Guid> _laboratoryTaskRepository;
    private readonly IRepository<TestRequestSampleIndicator, Guid> _sampleIndicatorRepository;
    private readonly IRepository<TestRequest, Guid> _testRequestRepository;
    private readonly IRepository<TestRequestSample, Guid> _sampleRepository;
    private readonly IRepository<Indicator, Guid> _indicatorRepository;
    private readonly IRepository<TestMethod, Guid> _testMethodRepository;
    private readonly IRepository<SpecimenType, Guid> _specimenTypeRepository;
    private readonly IRepository<TestMethodIndicator, Guid> _testMethodIndicatorRepository;
    private readonly IRepository<MaterialConsumptionItem, Guid> _materialConsumptionRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<ProductUnitConversion, Guid> _productUnitConversionRepository;
    private readonly IRepository<Equipment, Guid> _equipmentRepository;
    private readonly IRepository<Unit, Guid> _unitRepository;
    private readonly IRepository<ProductSku, Guid> _productSkuRepository;
    private readonly IRepository<PreparedSolutionBatch, Guid> _preparedSolutionBatchRepository;
    private readonly IRepository<WarehouseImportReceiptItem, Guid> _importReceiptItemRepository;
    private readonly IRepository<WarehouseImportReceipt, Guid> _importReceiptRepository;
    private readonly IRepository<IndicatorConsumption, Guid> _indicatorConsumptionRepository;
    private readonly IRepository<Warehouse, Guid> _warehouseRepository;
    private readonly IUserNameProvider _userNameProvider;
    private readonly ICodeGeneratorService _codeGenerator;
    private readonly IDataFilter _dataFilter;
    private readonly IAsyncQueryableExecuter AsyncExecuter;
    private readonly IFifoStockDeductionService _fifoStockService;
    private readonly IConsumableStockCalculator _consumableStockCalculator;
    private readonly IInventoryBarcodeRepository _inventoryBarcodeRepository;
    private readonly InventoryBarcodeStatusService _barcodeStatusService;

    public TestBatchAppService(
        IRepository<TestBatch, Guid> testBatchRepository,
        IRepository<TestBatchItem, Guid> testBatchItemRepository,
        IRepository<TestBatchConsumption, Guid> testBatchConsumptionRepository,
        IRepository<LaboratoryTaskIndicator, Guid> laboratoryTaskIndicatorRepository,
        IRepository<LaboratoryTask, Guid> laboratoryTaskRepository,
        IRepository<TestRequestSampleIndicator, Guid> sampleIndicatorRepository,
        IRepository<TestRequest, Guid> testRequestRepository,
        IRepository<TestRequestSample, Guid> sampleRepository,
        IRepository<Indicator, Guid> indicatorRepository,
        IRepository<TestMethod, Guid> testMethodRepository,
        IRepository<SpecimenType, Guid> specimenTypeRepository,
        IRepository<TestMethodIndicator, Guid> testMethodIndicatorRepository,
        IRepository<MaterialConsumptionItem, Guid> materialConsumptionRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<ProductUnitConversion, Guid> productUnitConversionRepository,
        IRepository<Equipment, Guid> equipmentRepository,
        IRepository<Unit, Guid> unitRepository,
        IRepository<ProductSku, Guid> productSkuRepository,
        IRepository<PreparedSolutionBatch, Guid> preparedSolutionBatchRepository,
        IRepository<WarehouseImportReceiptItem, Guid> importReceiptItemRepository,
        IRepository<WarehouseImportReceipt, Guid> importReceiptRepository,
        IRepository<IndicatorConsumption, Guid> indicatorConsumptionRepository,
        IRepository<Warehouse, Guid> warehouseRepository,
        IUserNameProvider userNameProvider,
        ICodeGeneratorService codeGenerator,
        IDataFilter dataFilter,
        IAsyncQueryableExecuter asyncQueryableExecuter,
        IFifoStockDeductionService fifoStockService,
        IConsumableStockCalculator consumableStockCalculator,
        IInventoryBarcodeRepository inventoryBarcodeRepository,
        InventoryBarcodeStatusService barcodeStatusService)
    {
        _testBatchRepository = testBatchRepository;
        _testBatchItemRepository = testBatchItemRepository;
        _testBatchConsumptionRepository = testBatchConsumptionRepository;
        _laboratoryTaskIndicatorRepository = laboratoryTaskIndicatorRepository;
        _laboratoryTaskRepository = laboratoryTaskRepository;
        _sampleIndicatorRepository = sampleIndicatorRepository;
        _testRequestRepository = testRequestRepository;
        _sampleRepository = sampleRepository;
        _indicatorRepository = indicatorRepository;
        _testMethodRepository = testMethodRepository;
        _specimenTypeRepository = specimenTypeRepository;
        _testMethodIndicatorRepository = testMethodIndicatorRepository;
        _materialConsumptionRepository = materialConsumptionRepository;
        _productRepository = productRepository;
        _productUnitConversionRepository = productUnitConversionRepository;
        _equipmentRepository = equipmentRepository;
        _unitRepository = unitRepository;
        _productSkuRepository = productSkuRepository;
        _preparedSolutionBatchRepository = preparedSolutionBatchRepository;
        _importReceiptItemRepository = importReceiptItemRepository;
        _importReceiptRepository = importReceiptRepository;
        _indicatorConsumptionRepository = indicatorConsumptionRepository;
        _warehouseRepository = warehouseRepository;
        _userNameProvider = userNameProvider;
        _codeGenerator = codeGenerator;
        _dataFilter = dataFilter;
        AsyncExecuter = asyncQueryableExecuter;
        _fifoStockService = fifoStockService;
        _consumableStockCalculator = consumableStockCalculator;
        _inventoryBarcodeRepository = inventoryBarcodeRepository;
        _barcodeStatusService = barcodeStatusService;
    }

    public async Task<PagedResultDto<TestBatchListDto>> GetListAsync(GetTestBatchListInput input)
    {
        var query = await _testBatchRepository.GetQueryableAsync();

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            var filter = input.Filter.Trim().ToLower();
            query = query.Where(x => x.Code.ToLower().Contains(filter) || x.Name.ToLower().Contains(filter));
        }

        if (input.Status.HasValue)
        {
            query = query.Where(x => x.Status == input.Status.Value);
        }

        if ((input.IndicatorIds != null && input.IndicatorIds.Any()) || input.IndicatorId.HasValue
            || input.MethodId.HasValue || input.SpecimenTypeId.HasValue)
        {
            var batchItemQuery = await _testBatchItemRepository.GetQueryableAsync();
            var taskIndicatorQuery = await _laboratoryTaskIndicatorRepository.GetQueryableAsync();
            var sampleIndicatorQuery = await _sampleIndicatorRepository.GetQueryableAsync();

            var joined = batchItemQuery
                .Join(taskIndicatorQuery,
                    bi => bi.LaboratoryTaskIndicatorId, ti => ti.Id,
                    (bi, ti) => new { bi.TestBatchId, ti.TestRequestSampleIndicatorId, ti.MethodId })
                .Join(sampleIndicatorQuery,
                    x => x.TestRequestSampleIndicatorId, si => si.Id,
                    (x, si) => new { x.TestBatchId, x.MethodId, si.IndicatorId, si.SpecimenTypeId });

            if (input.IndicatorIds != null && input.IndicatorIds.Any())
                joined = joined.Where(x => input.IndicatorIds.Contains(x.IndicatorId));
            else if (input.IndicatorId.HasValue)
                joined = joined.Where(x => x.IndicatorId == input.IndicatorId.Value);

            if (input.MethodId.HasValue)
                joined = joined.Where(x => x.MethodId == input.MethodId.Value);

            if (input.SpecimenTypeId.HasValue)
                joined = joined.Where(x => x.SpecimenTypeId == input.SpecimenTypeId.Value);

            var matchingBatchIds = joined.Select(x => x.TestBatchId).Distinct();
            query = query.Where(x => matchingBatchIds.Contains(x.Id));
        }

        if (input.FromDate.HasValue)
        {
            query = query.Where(x => x.CreationTime >= input.FromDate.Value);
        }

        if (input.ToDate.HasValue)
        {
            query = query.Where(x => x.CreationTime <= input.ToDate.Value);
        }

        if (input.TechnicianIds != null && input.TechnicianIds.Any())
        {
            var batchItemQuery = await _testBatchItemRepository.GetQueryableAsync();
            var taskIndicatorQuery = await _laboratoryTaskIndicatorRepository.GetQueryableAsync();

            var matchingBatchIds = batchItemQuery
                .Join(taskIndicatorQuery,
                    bi => bi.LaboratoryTaskIndicatorId,
                    ti => ti.Id,
                    (bi, ti) => new { bi.TestBatchId, ti.TechnicianId })
                .Where(x => x.TechnicianId.HasValue && input.TechnicianIds.Contains(x.TechnicianId.Value))
                .Select(x => x.TestBatchId)
                .Distinct();

            query = query.Where(x => matchingBatchIds.Contains(x.Id));
        }

        var totalCount = await AsyncExecuter.CountAsync(query);

        var sorting = input.Sorting?.Trim().ToLower() ?? "";
        query = sorting switch
        {
            "creationtime desc" => query.OrderByDescending(x => x.CreationTime),
            "creationtime" or "creationtime asc" => query.OrderBy(x => x.CreationTime),
            "testdate desc" => query.OrderByDescending(x => x.TestDate),
            "testdate" or "testdate asc" => query.OrderBy(x => x.TestDate),
            _ => query.OrderBy(x => x.Status).ThenByDescending(x => x.CreationTime),
        };

        query = query
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount);

        var batches = await AsyncExecuter.ToListAsync(query);

        var indicatorIds = batches.Select(x => x.IndicatorId).Distinct().ToList();
        var methodIds = batches.Select(x => x.MethodId).Distinct().ToList();
        var specimenTypeIds = batches.Where(x => x.SpecimenTypeId.HasValue).Select(x => x.SpecimenTypeId!.Value).Distinct().ToList();
        var creatorIds = batches.Where(x => x.CreatorId.HasValue).Select(x => x.CreatorId!.Value).Distinct().ToList();

        var indicators = await _indicatorRepository.GetListAsync(x => indicatorIds.Contains(x.Id));
        var methods = await _testMethodRepository.GetListAsync(x => methodIds.Contains(x.Id));
        var specimenTypes = specimenTypeIds.Any() ? await _specimenTypeRepository.GetListAsync(x => specimenTypeIds.Contains(x.Id)) : new List<SpecimenType>();
        var creatorMap = await _userNameProvider.GetUserNamesAsync(creatorIds.Cast<Guid?>());

        var indicatorMap = indicators.ToDictionary(x => x.Id, x => x.Name);
        var methodMap = methods.ToDictionary(x => x.Id, x => x.Name);
        var specimenTypeMap = specimenTypes.ToDictionary(x => x.Id, x => x.Name);

        var batchIds = batches.Select(x => x.Id).ToList();
        var allItems = await _testBatchItemRepository.GetListAsync(x => batchIds.Contains(x.TestBatchId));
        var itemsByBatch = allItems.GroupBy(x => x.TestBatchId).ToDictionary(g => g.Key, g => g.ToList());

        var taskIds = allItems.Select(x => x.LaboratoryTaskId).Distinct().ToList();
        var tasksForItems = taskIds.Any()
            ? await _laboratoryTaskRepository.GetListAsync(t => taskIds.Contains(t.Id))
            : new List<LaboratoryTask>();
        var taskMapForItems = tasksForItems.ToDictionary(t => t.Id);

        var dtos = batches.Select(b =>
        {
            var items = itemsByBatch.GetValueOrDefault(b.Id, new List<TestBatchItem>());

            var contractCount = items
                .Select(i => i.ContractNumber)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct()
                .Count();

            DateTime? requestCompletionDate = null;
            foreach (var item in items)
            {
                if (taskMapForItems.TryGetValue(item.LaboratoryTaskId, out var task)
                    && task.RequestCompletionDate.HasValue)
                {
                    if (!requestCompletionDate.HasValue || task.RequestCompletionDate.Value < requestCompletionDate.Value)
                        requestCompletionDate = task.RequestCompletionDate;
                }
            }

            return new TestBatchListDto
            {
                Id = b.Id,
                Code = b.Code,
                Name = b.Name,
                IndicatorId = b.IndicatorId,
                IndicatorName = indicatorMap.GetValueOrDefault(b.IndicatorId),
                MethodId = b.MethodId,
                MethodName = methodMap.GetValueOrDefault(b.MethodId),
                SpecimenTypeId = b.SpecimenTypeId,
                SpecimenTypeName = b.SpecimenTypeId.HasValue ? specimenTypeMap.GetValueOrDefault(b.SpecimenTypeId.Value) : null,
                Status = b.Status,
                TestDate = b.TestDate,
                ItemCount = items.Count,
                SubmittedItemCount = items.Count(i => i.IsSubmitted),
                ContractCount = contractCount,
                RequestCompletionDate = requestCompletionDate,
                CreationTime = b.CreationTime,
                CreatorId = b.CreatorId,
                CreatorName = b.CreatorId.HasValue ? creatorMap.GetValueOrDefault(b.CreatorId.Value) : null,
            };
        }).ToList();

        // RequestCompletionDate is computed in-memory — apply sort here
        var sortedDtos = sorting switch
        {
            "requestcompletiondate desc" => [.. dtos.OrderByDescending(x => x.RequestCompletionDate)],
            "requestcompletiondate" or "requestcompletiondate asc" => [.. dtos.OrderBy(x => x.RequestCompletionDate)],
            _ => dtos,
        };

        return new PagedResultDto<TestBatchListDto>(totalCount, sortedDtos);
    }

    public async Task<TestBatchDto> GetAsync(Guid id)
    {
        var batch = await _testBatchRepository.GetAsync(id);
        return await MapToDetailDtoAsync(batch);
    }

    public async Task<TestBatchDto> CreateAsync(CreateTestBatchDto input)
    {
        var code = await GenerateBatchCodeAsync();

        var firstTaskIndicator = await _laboratoryTaskIndicatorRepository.GetAsync(input.LaboratoryTaskIndicatorIds.First());
        if (!firstTaskIndicator.MethodId.HasValue)
        {
            throw new UserFriendlyException("Chỉ tiêu chưa được gán phương pháp thử nghiệm");
        }
        var firstSampleIndicator = await _sampleIndicatorRepository.GetAsync(firstTaskIndicator.TestRequestSampleIndicatorId);

        var batch = new TestBatch(
            GuidGenerator.Create(),
            code,
            input.Name,
            firstSampleIndicator.IndicatorId,
            firstTaskIndicator.MethodId.Value,
            firstSampleIndicator.SpecimenTypeId);
        batch.Note = input.Note;

        await ValidateAndAddItemsAsync(batch, input.LaboratoryTaskIndicatorIds);

        await _testBatchRepository.InsertAsync(batch, autoSave: true);

        return await MapToDetailDtoAsync(batch);
    }

    public async Task DeleteAsync(Guid id)
    {
        var batch = await _testBatchRepository.GetAsync(id);

        if (batch.Status != TestBatchStatus.Draft)
        {
            throw new UserFriendlyException("Chỉ có thể xóa mẻ ở trạng thái Cần làm");
        }

        await _testBatchRepository.DeleteAsync(batch);
    }

    public async Task<TestBatchDto> AddItemsAsync(Guid id, AddItemsDto input)
    {
        var batch = await _testBatchRepository.GetAsync(id);

        if (batch.Status != TestBatchStatus.Draft && batch.Status != TestBatchStatus.Running)
        {
            throw new UserFriendlyException("Chỉ có thể thêm chỉ tiêu khi mẻ ở trạng thái Cần làm hoặc Đang thực hiện");
        }

        var items = await _testBatchItemRepository.GetListAsync(x => x.TestBatchId == batch.Id);
        batch.Items = items;

        await ValidateAndAddItemsAsync(batch, input.LaboratoryTaskIndicatorIds);

        await _testBatchRepository.UpdateAsync(batch, autoSave: true);

        return await MapToDetailDtoAsync(batch);
    }

    public async Task<TestBatchDto> RemoveItemsAsync(Guid id, RemoveItemsDto input)
    {
        var batch = await _testBatchRepository.GetAsync(id);

        if (batch.Status != TestBatchStatus.Draft && batch.Status != TestBatchStatus.Running)
        {
            throw new UserFriendlyException("Chỉ có thể xóa chỉ tiêu khi mẻ ở trạng thái Cần làm hoặc Đang thực hiện");
        }

        var itemsToRemove = await _testBatchItemRepository.GetListAsync(x =>
            x.TestBatchId == batch.Id && input.ItemIds.Contains(x.Id));

        if (itemsToRemove.Any(x => !string.IsNullOrWhiteSpace(x.Result)))
        {
            throw new UserFriendlyException("Không thể xóa chỉ tiêu đã có kết quả");
        }

        await _testBatchItemRepository.DeleteManyAsync(itemsToRemove, autoSave: true);

        return await MapToDetailDtoAsync(batch);
    }

    public async Task<TestBatchDto> SaveResultsAsync(Guid id, SaveTestBatchResultsDto input)
    {
        var batch = await _testBatchRepository.GetAsync(id);

        if (batch.Status == TestBatchStatus.Completed || batch.Status == TestBatchStatus.Cancelled)
        {
            throw new UserFriendlyException("Không thể nhập kết quả cho mẻ đã hoàn thành hoặc đã hủy");
        }

        if (batch.Status == TestBatchStatus.Draft)
        {
            batch.Status = TestBatchStatus.Running;
        }

        if (input.TestDate.HasValue)
        {
            batch.TestDate = input.TestDate;
        }

        var items = await _testBatchItemRepository.GetListAsync(x => x.TestBatchId == batch.Id);
        var itemMap = items.ToDictionary(x => x.Id);

        foreach (var itemInput in input.Items)
        {
            if (!itemMap.TryGetValue(itemInput.ItemId, out var item))
                continue;

            if (item.IsExternallyCompleted)
                continue;

            item.Result = itemInput.Result;
            if (itemInput.TestDate.HasValue)
            {
                item.TestDate = itemInput.TestDate;
            }
            else if (input.TestDate.HasValue)
            {
                item.TestDate = input.TestDate;
            }

            await _testBatchItemRepository.UpdateAsync(item, autoSave: false);
        }

        await _testBatchRepository.UpdateAsync(batch, autoSave: true);

        return await MapToDetailDtoAsync(batch);
    }

    public async Task<TestBatchDto> SubmitAsync(Guid id)
    {
        var batch = await _testBatchRepository.GetAsync(id);

        if (batch.Status != TestBatchStatus.Running && batch.Status != TestBatchStatus.Draft)
        {
            throw new UserFriendlyException("Chỉ có thể gửi kết quả khi mẻ đang thực hiện");
        }

        var items = await _testBatchItemRepository.GetListAsync(x => x.TestBatchId == batch.Id);
        var submittableItems = items.Where(x => !x.IsExternallyCompleted && !x.IsSubmitted && !string.IsNullOrWhiteSpace(x.Result)).ToList();

        if (!submittableItems.Any())
        {
            throw new UserFriendlyException("Không có chỉ tiêu nào có kết quả để gửi");
        }

        var taskIndicatorIds = submittableItems.Select(x => x.LaboratoryTaskIndicatorId).ToList();
        var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => taskIndicatorIds.Contains(x.Id));
        var taskIndicatorMap = taskIndicators.ToDictionary(x => x.Id);

        foreach (var item in submittableItems)
        {
            if (!taskIndicatorMap.TryGetValue(item.LaboratoryTaskIndicatorId, out var taskIndicator))
                continue;

            taskIndicator.Result = item.Result;
            taskIndicator.TestDate = item.TestDate ?? batch.TestDate;
            taskIndicator.SubmitDate = DateTime.Now;
            taskIndicator.EntryDate ??= DateTime.Now;
            taskIndicator.EnteredById ??= CurrentUser.Id;
            taskIndicator.ApprovalStatus = IndicatorApprovalStatus.Entered;

            await _laboratoryTaskIndicatorRepository.UpdateAsync(taskIndicator, autoSave: false);

            item.IsSubmitted = true;
            await _testBatchItemRepository.UpdateAsync(item, autoSave: false);
        }

        batch.Status = TestBatchStatus.Completed;
        batch.CompletedDate = DateTime.Now;
        batch.CompletedById = CurrentUser.Id;

        await _testBatchRepository.UpdateAsync(batch, autoSave: true);

        await UpdateTaskStatusesAsync(submittableItems);

        await DeductStockForSubmittedBatchItemsAsync(batch, submittableItems);

        return await MapToDetailDtoAsync(batch);
    }

    public async Task<TestBatchDto> SubmitItemsAsync(Guid id, SubmitItemsDto input)
    {
        var batch = await _testBatchRepository.GetAsync(id);

        if (batch.Status != TestBatchStatus.Running && batch.Status != TestBatchStatus.Draft)
        {
            throw new UserFriendlyException("Chỉ có thể gửi kết quả khi mẻ đang thực hiện");
        }

        if (input.ItemIds == null || !input.ItemIds.Any())
        {
            throw new UserFriendlyException("Vui lòng chọn ít nhất 1 chỉ tiêu để gửi");
        }

        var allItems = await _testBatchItemRepository.GetListAsync(x => x.TestBatchId == batch.Id);
        var selectedItems = allItems.Where(x => input.ItemIds.Contains(x.Id)).ToList();

        if (!selectedItems.Any())
        {
            throw new UserFriendlyException("Không tìm thấy chỉ tiêu được chọn");
        }

        var submittableItems = selectedItems
            .Where(x => !x.IsExternallyCompleted && !x.IsSubmitted && !string.IsNullOrWhiteSpace(x.Result))
            .ToList();

        if (!submittableItems.Any())
        {
            throw new UserFriendlyException("Không có chỉ tiêu nào có kết quả để gửi");
        }

        var taskIndicatorIds = submittableItems.Select(x => x.LaboratoryTaskIndicatorId).ToList();
        var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => taskIndicatorIds.Contains(x.Id));
        var taskIndicatorMap = taskIndicators.ToDictionary(x => x.Id);

        foreach (var item in submittableItems)
        {
            if (!taskIndicatorMap.TryGetValue(item.LaboratoryTaskIndicatorId, out var taskIndicator))
                continue;

            taskIndicator.Result = item.Result;
            taskIndicator.TestDate = item.TestDate ?? batch.TestDate;
            taskIndicator.SubmitDate = DateTime.Now;
            taskIndicator.EntryDate ??= DateTime.Now;
            taskIndicator.EnteredById ??= CurrentUser.Id;
            taskIndicator.ApprovalStatus = IndicatorApprovalStatus.Entered;

            await _laboratoryTaskIndicatorRepository.UpdateAsync(taskIndicator, autoSave: false);

            item.IsSubmitted = true;
            await _testBatchItemRepository.UpdateAsync(item, autoSave: false);
        }

        var allSubmitted = allItems.All(x => x.IsSubmitted || x.IsExternallyCompleted);
        if (allSubmitted)
        {
            batch.Status = TestBatchStatus.Completed;
            batch.CompletedDate = DateTime.Now;
            batch.CompletedById = CurrentUser.Id;
        }
        else if (batch.Status == TestBatchStatus.Draft)
        {
            batch.Status = TestBatchStatus.Running;
        }

        await _testBatchRepository.UpdateAsync(batch, autoSave: true);

        await UpdateTaskStatusesAsync(submittableItems);

        await DeductStockForSubmittedBatchItemsAsync(batch, submittableItems);

        return await MapToDetailDtoAsync(batch);
    }

    public async Task<TestBatchDto> CalculateConsumptionAsync(Guid id)
    {
        var batch = await _testBatchRepository.GetAsync(id);

        if (batch.Status != TestBatchStatus.Completed)
        {
            throw new UserFriendlyException("Chỉ tính tiêu hao cho mẻ sau khi đã gửi kết quả");
        }

        var existingConsumptions = await _testBatchConsumptionRepository.GetListAsync(x => x.TestBatchId == batch.Id);
        if (existingConsumptions.Any(c => c.IsDeducted))
        {
            throw new UserFriendlyException("Không thể tính lại tiêu hao đã xác nhận trừ kho");
        }

        if (existingConsumptions.Any())
        {
            await _testBatchConsumptionRepository.DeleteManyAsync(existingConsumptions);
        }

        var tmiQuery = await _testMethodIndicatorRepository.GetQueryableAsync();
        var tmi = await AsyncExecuter.FirstOrDefaultAsync(
            tmiQuery.Where(x =>
                x.TestMethodId == batch.MethodId &&
                x.IndicatorId == batch.IndicatorId &&
                x.SpecimenTypeId == batch.SpecimenTypeId));

        if (tmi == null)
        {
            await _testBatchRepository.UpdateAsync(batch, autoSave: true);
            return await MapToDetailDtoAsync(batch);
        }

        var consumptionQuery = await _materialConsumptionRepository.GetQueryableAsync();
        var consumptionItems = await AsyncExecuter.ToListAsync(
            consumptionQuery.Where(c => c.TestMethodIndicatorId == tmi.Id));

        var submittedItemCount = (await _testBatchItemRepository.GetListAsync(x => x.TestBatchId == batch.Id && x.IsSubmitted)).Count;

        var productIdsForWh = consumptionItems
            .Where(c => c.ProductId.HasValue).Select(c => c.ProductId!.Value).Distinct();
        var labId = await ResolveLaboratoryIdFromBatchAsync(batch.Id);
        var warehouseMap = await ResolveWarehouseForProductsAsync(productIdsForWh, labId);

        foreach (var consumptionItem in consumptionItems)
        {
            var batchConsumption = new TestBatchConsumption(
                GuidGenerator.Create(),
                batch.Id,
                consumptionItem.ProductId,
                consumptionItem.EquipmentId,
                consumptionItem.UnitId,
                consumptionItem.Quantity * submittedItemCount);
            if (consumptionItem.ProductId.HasValue)
                batchConsumption.WarehouseId = warehouseMap.GetValueOrDefault(consumptionItem.ProductId.Value);

            batch.Consumptions.Add(batchConsumption);
        }

        await _testBatchRepository.UpdateAsync(batch, autoSave: true);

        return await MapToDetailDtoAsync(batch);
    }

    public async Task<TestBatchDto> ConfirmConsumptionAsync(Guid id, ConfirmConsumptionDto input)
    {
        var batch = await _testBatchRepository.GetAsync(id);

        if (batch.Status != TestBatchStatus.Completed)
        {
            throw new UserFriendlyException("Chỉ xác nhận tiêu hao cho mẻ đã hoàn thành");
        }

        if (batch.ConsumptionConfirmed)
        {
            throw new UserFriendlyException("Tiêu hao đã được xác nhận trước đó");
        }

        var consumptions = await _testBatchConsumptionRepository.GetListAsync(x => x.TestBatchId == batch.Id);
        var consumptionMap = consumptions.ToDictionary(x => x.Id);

        foreach (var itemInput in input.Items)
        {
            if (!consumptionMap.TryGetValue(itemInput.ConsumptionId, out var consumption))
                continue;

            consumption.ActualQuantity = itemInput.ActualQuantity;
        }

        await DeductStockAsync(consumptions);

        foreach (var consumption in consumptions)
        {
            consumption.IsDeducted = true;
            await _testBatchConsumptionRepository.UpdateAsync(consumption, autoSave: false);
        }

        batch.ConsumptionConfirmed = true;
        batch.ConsumptionConfirmedDate = DateTime.Now;
        await _testBatchRepository.UpdateAsync(batch, autoSave: true);

        var barcodeTargets = consumptions
            .Where(c => c.ProductId.HasValue && c.WarehouseId.HasValue)
            .Select(c => (c.ProductId!.Value, c.WarehouseId!.Value));
        await _barcodeStatusService.RecalculateForProductsAsync(barcodeTargets);

        return await MapToDetailDtoAsync(batch);
    }

    public async Task<ConsumptionPreviewDto> GetConsumptionPreviewAsync(Guid id)
    {
        var batch = await _testBatchRepository.GetAsync(id);

        if (batch.Status != TestBatchStatus.Draft && batch.Status != TestBatchStatus.Running)
        {
            throw new UserFriendlyException("Chỉ có thể xem tiêu hao khi mẻ đang thực hiện");
        }

        var existingConsumptions = await _testBatchConsumptionRepository.GetListAsync(x => x.TestBatchId == batch.Id);
        var hasDeducted = existingConsumptions.Any(c => c.IsDeducted);

        var lines = new List<ConsumptionPreviewLineDto>();

        // Always show fresh lines for the new round: draft → template
        // Deducted lines from previous rounds are NOT shown as pre-fill (accumulate model)
        var draftConsumptions = existingConsumptions.Where(c => !c.IsDeducted).ToList();
        if (draftConsumptions.Any())
        {
            foreach (var c in draftConsumptions)
            {
                lines.Add(new ConsumptionPreviewLineDto
                {
                    ConsumptionId = c.Id,
                    ProductId = c.ProductId,
                    EquipmentId = c.EquipmentId,
                    UnitId = c.UnitId,
                    RecommendedQuantity = c.RecommendedQuantity,
                    ActualQuantity = c.ActualQuantity,
                    IsDeducted = false,
                });
            }
        }
        else
        {
            var tmiQuery = await _testMethodIndicatorRepository.GetQueryableAsync();
            var tmi = await AsyncExecuter.FirstOrDefaultAsync(
                tmiQuery.Where(x =>
                    x.TestMethodId == batch.MethodId &&
                    x.IndicatorId == batch.IndicatorId &&
                    x.SpecimenTypeId == batch.SpecimenTypeId));

            if (tmi != null)
            {
                var consumptionQuery = await _materialConsumptionRepository.GetQueryableAsync();
                var templates = await AsyncExecuter.ToListAsync(
                    consumptionQuery.Where(c => c.TestMethodIndicatorId == tmi.Id));

                foreach (var template in templates)
                {
                    lines.Add(new ConsumptionPreviewLineDto
                    {
                        ProductId = template.ProductId,
                        EquipmentId = template.EquipmentId,
                        UnitId = template.UnitId,
                        RecommendedQuantity = template.Quantity,
                        ActualQuantity = template.Quantity,
                        IsDeducted = false,
                    });
                }
            }
        }

        // Enrich with names and stock info
        var productIds = lines.Where(l => l.ProductId.HasValue).Select(l => l.ProductId!.Value).Distinct().ToList();
        var equipmentIds = lines.Where(l => l.EquipmentId.HasValue).Select(l => l.EquipmentId!.Value).Distinct().ToList();
        var unitIds = lines.Where(l => l.UnitId.HasValue).Select(l => l.UnitId!.Value).Distinct().ToList();

        var products = productIds.Any() ? await _productRepository.GetListAsync(p => productIds.Contains(p.Id)) : new List<Product>();
        var equipments = equipmentIds.Any() ? await _equipmentRepository.GetListAsync(e => equipmentIds.Contains(e.Id)) : new List<Equipment>();

        // Load SKUs for unit conversion fallback
        var skus = productIds.Any()
            ? await _productSkuRepository.GetListAsync(s => productIds.Contains(s.ProductId))
            : new List<ProductSku>();
        var skuByProduct = skus.GroupBy(s => s.ProductId).ToDictionary(g => g.Key, g => g.ToList());

        // Also load storage/purchase unit names for stock display
        var productUnitIds = products
            .SelectMany(p => new[] { p.PackagingUnitId, p.BaseUnitId })
            .Where(id => id.HasValue).Select(id => id!.Value);
        unitIds = unitIds.Union(productUnitIds).Distinct().ToList();

        var units = unitIds.Any() ? await _unitRepository.GetListAsync(u => unitIds.Contains(u.Id)) : new List<Unit>();
        var conversions = productIds.Any()
            ? await _productUnitConversionRepository.GetListAsync(c => productIds.Contains(c.ProductId))
            : new List<ProductUnitConversion>();

        var productMap = products.ToDictionary(p => p.Id);
        var equipmentMap = equipments.ToDictionary(e => e.Id);
        var unitMap = units.ToDictionary(u => u.Id, u => u.Name);
        var conversionMap = conversions.ToDictionary(c => (c.ProductId, c.ConvertedUnitId));

        var labId2 = await ResolveLaboratoryIdFromBatchAsync(batch.Id);
        var warehouseMap = await ResolveWarehouseForProductsAsync(productIds, labId2);
        var perWhAvailable = new Dictionary<Guid, decimal>();
        if (productIds.Any())
        {
            var itemQ = await _importReceiptItemRepository.GetQueryableAsync();
            var receiptQ = await _importReceiptRepository.GetQueryableAsync();
            perWhAvailable = (from item in itemQ
                join receipt in receiptQ on item.WarehouseImportReceiptId equals receipt.Id
                where productIds.Contains(item.ProductId)
                      && receipt.Status == WarehouseImportReceiptStatus.Completed
                      && item.Status == WarehouseImportLineStatus.Checked
                      && (item.ReceivedBaseQuantity - item.ConsumedBaseQuantity) > 0
                select new
                {
                    item.ProductId,
                    EffWh = item.StorageWarehouseId ?? receipt.WarehouseId,
                    Qty = item.ReceivedBaseQuantity - item.ConsumedBaseQuantity
                })
                .ToList()
                .Where(x => warehouseMap.TryGetValue(x.ProductId, out var wh) && wh.HasValue && x.EffWh == wh.Value)
                .GroupBy(x => x.ProductId)
                .ToDictionary(g => g.Key, g => g.Sum(x => x.Qty));
        }

        foreach (var line in lines)
        {
            if (line.ProductId.HasValue && productMap.TryGetValue(line.ProductId.Value, out var product))
            {
                line.ProductName = product.Name;
                line.ProductCode = product.Code;
                line.ProductType = (int)product.ProductType;

                line.StorageAvailableQuantity = perWhAvailable.GetValueOrDefault(product.Id, product.AvailableQuantity);
                var storageUnitId = product.PackagingUnitId ?? product.BaseUnitId;
                if (storageUnitId.HasValue && unitMap.TryGetValue(storageUnitId.Value, out var suName))
                    line.StorageUnitName = suName;

                var whAvailable = perWhAvailable.GetValueOrDefault(product.Id, product.AvailableQuantity);
                var availableInUnit = whAvailable;
                if (line.UnitId.HasValue)
                {
                    var consumeUnitId = line.UnitId.Value;
                    decimal? factor = null;

                    if (consumeUnitId == product.PackagingUnitId)
                        factor = 1;
                    else if (consumeUnitId == product.VolumeUnitId && product.VolumePerUnit > 0)
                        factor = product.VolumePerUnit.Value;
                    else if (consumeUnitId == product.BaseUnitId && product.PackagingQuantity > 0)
                        factor = 1m / product.PackagingQuantity.Value;

                    if (factor == null && skuByProduct.TryGetValue(product.Id, out var pSkus))
                    {
                        foreach (var sku in pSkus)
                        {
                            if (consumeUnitId == sku.PackagingUnitId) { factor = 1; break; }
                            if (consumeUnitId == sku.VolumeUnitId && sku.VolumePerUnit > 0) { factor = sku.VolumePerUnit.Value; break; }
                            if (consumeUnitId == sku.BaseUnitId && sku.PackagingQuantity > 0) { factor = 1m / sku.PackagingQuantity.Value; break; }
                        }
                    }

                    if (factor == null && conversionMap.TryGetValue((product.Id, consumeUnitId), out var conv) && conv.Quantity > 0)
                        factor = conv.ConvertedQuantity / conv.Quantity;

                    if (factor.HasValue)
                        availableInUnit = whAvailable * factor.Value;
                }
                line.AvailableQuantity = availableInUnit;
                line.ExceedsStock = line.ActualQuantity > availableInUnit;
            }
            if (line.EquipmentId.HasValue && equipmentMap.TryGetValue(line.EquipmentId.Value, out var equipment))
            {
                line.EquipmentName = equipment.Name;
            }
            if (line.UnitId.HasValue && unitMap.TryGetValue(line.UnitId.Value, out var unitName))
            {
                line.UnitName = unitName;
            }
        }

        return new ConsumptionPreviewDto
        {
            IsFirstSubmit = !hasDeducted,
            Lines = lines,
        };
    }

    public async Task<TestBatchDto> SaveConsumptionDraftAsync(Guid id, SaveConsumptionDraftDto input)
    {
        var batch = await _testBatchRepository.GetAsync(id);

        if (batch.Status == TestBatchStatus.Cancelled)
            throw new UserFriendlyException("Không thể lưu nháp tiêu hao cho mẻ đã hủy");

        var existingConsumptions = await _testBatchConsumptionRepository.GetListAsync(x => x.TestBatchId == batch.Id);
        var nonDeducted = existingConsumptions.Where(c => !c.IsDeducted).ToList();
        if (nonDeducted.Any())
        {
            await _testBatchConsumptionRepository.DeleteManyAsync(nonDeducted);
        }

        var labId3 = await ResolveLaboratoryIdFromBatchAsync(batch.Id);
        if (input.ConsumptionLines != null && input.ConsumptionLines.Any())
        {
            var productIdsForWh = input.ConsumptionLines
                .Where(l => l.ProductId.HasValue).Select(l => l.ProductId!.Value).Distinct();
            var warehouseMap = await ResolveWarehouseForProductsAsync(productIdsForWh, labId3);

            foreach (var line in input.ConsumptionLines)
            {
                var consumption = new TestBatchConsumption(
                    GuidGenerator.Create(),
                    batch.Id,
                    line.ProductId,
                    line.EquipmentId,
                    line.UnitId,
                    line.RecommendedQuantity);
                consumption.ActualQuantity = line.ActualQuantity;
                if (line.ProductId.HasValue)
                    consumption.WarehouseId = warehouseMap.GetValueOrDefault(line.ProductId.Value);
                batch.Consumptions.Add(consumption);
            }
        }

        await _testBatchRepository.UpdateAsync(batch, autoSave: true);

        return await MapToDetailDtoAsync(batch);
    }

    public async Task<TestBatchDto> SubmitWithConsumptionAsync(Guid id, SubmitWithConsumptionDto input)
    {
        var batch = await _testBatchRepository.GetAsync(id);

        if (batch.Status != TestBatchStatus.Running && batch.Status != TestBatchStatus.Draft)
        {
            throw new UserFriendlyException("Chỉ có thể gửi kết quả khi mẻ đang thực hiện");
        }

        if (input.ItemIds == null || !input.ItemIds.Any())
        {
            throw new UserFriendlyException("Vui lòng chọn ít nhất 1 chỉ tiêu để gửi");
        }

        // Step 1: Submit items
        var allItems = await _testBatchItemRepository.GetListAsync(x => x.TestBatchId == batch.Id);
        var selectedItems = allItems.Where(x => input.ItemIds.Contains(x.Id)).ToList();

        if (!selectedItems.Any())
        {
            throw new UserFriendlyException("Không tìm thấy chỉ tiêu được chọn");
        }

        var submittableItems = selectedItems
            .Where(x => !x.IsExternallyCompleted && !x.IsSubmitted && !string.IsNullOrWhiteSpace(x.Result))
            .ToList();

        if (!submittableItems.Any())
        {
            throw new UserFriendlyException("Không có chỉ tiêu nào có kết quả để gửi");
        }

        var taskIndicatorIds = submittableItems.Select(x => x.LaboratoryTaskIndicatorId).ToList();
        var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => taskIndicatorIds.Contains(x.Id));
        var taskIndicatorMap = taskIndicators.ToDictionary(x => x.Id);

        foreach (var item in submittableItems)
        {
            if (!taskIndicatorMap.TryGetValue(item.LaboratoryTaskIndicatorId, out var taskIndicator))
                continue;

            taskIndicator.Result = item.Result;
            taskIndicator.TestDate = item.TestDate ?? batch.TestDate;
            taskIndicator.SubmitDate = Clock.Now;
            taskIndicator.EntryDate ??= Clock.Now;
            taskIndicator.EnteredById ??= CurrentUser.Id;
            taskIndicator.ApprovalStatus = IndicatorApprovalStatus.Entered;

            await _laboratoryTaskIndicatorRepository.UpdateAsync(taskIndicator, autoSave: false);

            item.IsSubmitted = true;
            await _testBatchItemRepository.UpdateAsync(item, autoSave: false);
        }

        var allSubmitted = allItems.All(x => x.IsSubmitted || x.IsExternallyCompleted);
        if (allSubmitted)
        {
            batch.Status = TestBatchStatus.Completed;
            batch.CompletedDate = Clock.Now;
            batch.CompletedById = CurrentUser.Id;
        }
        else if (batch.Status == TestBatchStatus.Draft)
        {
            batch.Status = TestBatchStatus.Running;
        }

        // Step 2: Handle consumption — each submit round creates a new set of deducted consumptions (accumulate)
        var labId4 = submittableItems.Any()
            ? (await _laboratoryTaskRepository.FindAsync(submittableItems[0].LaboratoryTaskId))?.LaboratoryId
            : await ResolveLaboratoryIdFromBatchAsync(batch.Id);
        if (input.ConsumptionLines != null && input.ConsumptionLines.Any())
        {
            var existingConsumptions = await _testBatchConsumptionRepository.GetListAsync(x => x.TestBatchId == batch.Id);
            var nonDeducted = existingConsumptions.Where(c => !c.IsDeducted).ToList();
            if (nonDeducted.Any())
            {
                await _testBatchConsumptionRepository.DeleteManyAsync(nonDeducted);
            }

            var productIdsForWh = input.ConsumptionLines
                .Where(l => l.ProductId.HasValue).Select(l => l.ProductId!.Value).Distinct();
            var warehouseMap = await ResolveWarehouseForProductsAsync(productIdsForWh, labId4);

            foreach (var line in input.ConsumptionLines)
            {
                var consumption = new TestBatchConsumption(
                    GuidGenerator.Create(),
                    batch.Id,
                    line.ProductId,
                    line.EquipmentId,
                    line.UnitId,
                    line.RecommendedQuantity);
                consumption.ActualQuantity = line.ActualQuantity;
                if (line.ProductId.HasValue)
                    consumption.WarehouseId = warehouseMap.GetValueOrDefault(line.ProductId.Value);

                batch.Consumptions.Add(consumption);
            }

            await _testBatchRepository.UpdateAsync(batch, autoSave: true);

            var newConsumptions = await _testBatchConsumptionRepository.GetListAsync(x => x.TestBatchId == batch.Id && !x.IsDeducted);
            await DeductStockAsync(newConsumptions);

            foreach (var consumption in newConsumptions)
            {
                consumption.IsDeducted = true;
                await _testBatchConsumptionRepository.UpdateAsync(consumption, autoSave: false);
            }

            await CurrentUnitOfWork.SaveChangesAsync();

            var barcodeTargets = newConsumptions
                .Where(c => c.ProductId.HasValue && c.WarehouseId.HasValue)
                .Select(c => (c.ProductId!.Value, c.WarehouseId!.Value));
            await _barcodeStatusService.RecalculateForProductsAsync(barcodeTargets);

            // Mark confirmed on first round (informational only, not used as a guard)
            if (!batch.ConsumptionConfirmed)
            {
                batch.ConsumptionConfirmed = true;
                batch.ConsumptionConfirmedDate = Clock.Now;
            }
        }

        await _testBatchRepository.UpdateAsync(batch, autoSave: true);

        await UpdateTaskStatusesAsync(submittableItems);

        return await MapToDetailDtoAsync(batch);
    }

    public async Task<TestBatchDto> CancelAsync(Guid id)
    {
        var batch = await _testBatchRepository.GetAsync(id);

        if (batch.Status == TestBatchStatus.Cancelled)
        {
            throw new UserFriendlyException("Mẻ đã bị hủy trước đó");
        }

        var batchItems = await _testBatchItemRepository.GetListAsync(x => x.TestBatchId == batch.Id);
        var taskIndicatorIds = batchItems.Select(x => x.LaboratoryTaskIndicatorId).ToList();

        if (taskIndicatorIds.Any())
        {
            var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => taskIndicatorIds.Contains(x.Id));
            if (taskIndicators.Any(x => x.ApprovalStatus == IndicatorApprovalStatus.Approved))
            {
                throw new UserFriendlyException("Không thể hủy mẻ vì có chỉ tiêu đã được QLKT duyệt");
            }
        }

        batch.Status = TestBatchStatus.Cancelled;
        await _testBatchRepository.UpdateAsync(batch, autoSave: true);

        return await MapToDetailDtoAsync(batch);
    }

    public async Task<PagedResultDto<AvailableIndicatorDto>> GetAvailableIndicatorsAsync(GetAvailableIndicatorsInput input)
    {
        var taskIndicatorQuery = await _laboratoryTaskIndicatorRepository.GetQueryableAsync();
        var query = taskIndicatorQuery.Where(ti =>
            ti.ApprovalStatus != IndicatorApprovalStatus.Approved &&
            ti.ApprovalStatus != IndicatorApprovalStatus.Rejected &&
            ti.ApprovalStatus != IndicatorApprovalStatus.CustomerCancelled &&
            ti.ApprovalStatus != IndicatorApprovalStatus.TestingRejected &&
            ti.ApprovalStatus != IndicatorApprovalStatus.CannotTest &&
            ti.TechnicianId.HasValue);

        if (input.MethodId.HasValue)
        {
            query = query.Where(ti => ti.MethodId == input.MethodId.Value);
        }

        if (input.TechnicianId.HasValue)
        {
            query = query.Where(ti => ti.TechnicianId == input.TechnicianId.Value);
        }

        if (input.IndicatorId.HasValue || input.SpecimenTypeId.HasValue)
        {
            var sampleIndicatorQuery = await _sampleIndicatorRepository.GetQueryableAsync();
            var matchQuery = sampleIndicatorQuery.AsQueryable();

            if (input.IndicatorId.HasValue)
            {
                matchQuery = matchQuery.Where(si => si.IndicatorId == input.IndicatorId.Value);
            }
            if (input.SpecimenTypeId.HasValue)
            {
                matchQuery = matchQuery.Where(si => si.SpecimenTypeId == input.SpecimenTypeId.Value);
            }

            var matchingSiIds = matchQuery.Select(si => si.Id);
            query = query.Where(ti => matchingSiIds.Contains(ti.TestRequestSampleIndicatorId));
        }

        var totalCount = await AsyncExecuter.CountAsync(query);
        var taskIndicators = await AsyncExecuter.ToListAsync(
            query.Skip(input.SkipCount).Take(input.MaxResultCount));

        var existingBatchItemQuery = await _testBatchItemRepository.GetQueryableAsync();
        var tiIds = taskIndicators.Select(ti => ti.Id).ToList();
        var existingBatchIndicatorIds = await AsyncExecuter.ToListAsync(
            existingBatchItemQuery
                .Where(bi => tiIds.Contains(bi.LaboratoryTaskIndicatorId))
                .Select(bi => bi.LaboratoryTaskIndicatorId));
        var existingSet = existingBatchIndicatorIds.ToHashSet();

        var taskIds = taskIndicators.Select(ti => ti.LaboratoryTaskId).Distinct().ToList();
        var tasks = await _laboratoryTaskRepository.GetListAsync(t => taskIds.Contains(t.Id));
        var taskMap = tasks.ToDictionary(t => t.Id);

        var testRequestIds = tasks.Select(t => t.TestRequestId).Distinct().ToList();
        var testRequests = await _testRequestRepository.GetListAsync(r => testRequestIds.Contains(r.Id));
        var testRequestMap = testRequests.ToDictionary(r => r.Id);

        var sampleIndicatorIds = taskIndicators.Select(ti => ti.TestRequestSampleIndicatorId).Distinct().ToList();
        var sampleIndicators = await _sampleIndicatorRepository.GetListAsync(si => sampleIndicatorIds.Contains(si.Id));
        var sampleIndicatorMap = sampleIndicators.ToDictionary(si => si.Id);

        var sampleIds = sampleIndicators.Select(si => si.TestRequestSampleId).Distinct().ToList();
        var samples = await _sampleRepository.GetListAsync(s => sampleIds.Contains(s.Id));
        var sampleMap = samples.ToDictionary(s => s.Id);

        var techIds = taskIndicators.Where(ti => ti.TechnicianId.HasValue).Select(ti => ti.TechnicianId!.Value).Distinct().ToList();
        var techMap = await _userNameProvider.GetUserNamesAsync(techIds.Cast<Guid?>());

        var indicatorIds = sampleIndicators.Select(si => si.IndicatorId).Distinct().ToList();
        var indicatorEntities = await _indicatorRepository.GetListAsync(x => indicatorIds.Contains(x.Id));
        var indicatorNameMap = indicatorEntities.ToDictionary(x => x.Id, x => x.Name);

        var methodIds = taskIndicators.Where(ti => ti.MethodId.HasValue).Select(ti => ti.MethodId!.Value).Distinct().ToList();
        var methodEntities = methodIds.Any() ? await _testMethodRepository.GetListAsync(x => methodIds.Contains(x.Id)) : new List<TestMethod>();
        var methodNameMap = methodEntities.ToDictionary(x => x.Id, x => x.Name);

        var specimenTypeIds = sampleIndicators.Where(si => si.SpecimenTypeId.HasValue).Select(si => si.SpecimenTypeId!.Value).Distinct().ToList();
        var specimenTypeEntities = specimenTypeIds.Any() ? await _specimenTypeRepository.GetListAsync(x => specimenTypeIds.Contains(x.Id)) : new List<SpecimenType>();
        var specimenTypeNameMap = specimenTypeEntities.ToDictionary(x => x.Id, x => x.Name);

        var dtos = taskIndicators.Select(ti =>
        {
            var task = taskMap.GetValueOrDefault(ti.LaboratoryTaskId);
            var testRequest = task != null ? testRequestMap.GetValueOrDefault(task.TestRequestId) : null;
            var sampleIndicator = sampleIndicatorMap.GetValueOrDefault(ti.TestRequestSampleIndicatorId);
            var sample = sampleIndicator != null ? sampleMap.GetValueOrDefault(sampleIndicator.TestRequestSampleId) : null;

            return new AvailableIndicatorDto
            {
                Id = ti.Id,
                LaboratoryTaskId = ti.LaboratoryTaskId,
                ContractNumber = task?.ContractNumber,
                SampleCode = sample?.SampleCode,
                SampleName = sample?.SampleName,
                TechnicianId = ti.TechnicianId,
                TechnicianName = ti.TechnicianId.HasValue ? techMap.GetValueOrDefault(ti.TechnicianId.Value) : null,
                IndicatorId = sampleIndicator?.IndicatorId,
                IndicatorName = sampleIndicator != null ? indicatorNameMap.GetValueOrDefault(sampleIndicator.IndicatorId) : null,
                MethodId = ti.MethodId,
                MethodName = ti.MethodId.HasValue ? methodNameMap.GetValueOrDefault(ti.MethodId.Value) : null,
                SpecimenTypeId = sampleIndicator?.SpecimenTypeId,
                SpecimenTypeName = sampleIndicator?.SpecimenTypeId.HasValue == true
                    ? specimenTypeNameMap.GetValueOrDefault(sampleIndicator.SpecimenTypeId!.Value) : null,
                RequestCompletionDate = task?.RequestCompletionDate,
                SampleSentDate = task?.SampleSentDate,
                SampleReceivedDate = testRequest?.CreationTime,
                IsAlreadyInBatch = existingSet.Contains(ti.Id),
                TestingStatus = MapTestingStatus(ti.ApprovalStatus, ti.TestDate, ti.Result),
            };
        }).OrderBy(x => x.TechnicianName).ToList();

        return new PagedResultDto<AvailableIndicatorDto>(totalCount, dtos);
    }

    #region Private Methods

    private static int MapTestingStatus(IndicatorApprovalStatus approvalStatus, DateTime? testDate, string? result)
    {
        if (approvalStatus == IndicatorApprovalStatus.Rejected) return (int)TestingStatus.Rejected;
        if (approvalStatus == IndicatorApprovalStatus.CannotTest) return (int)TestingStatus.CannotTest;
        if (!string.IsNullOrWhiteSpace(result))
            return approvalStatus == IndicatorApprovalStatus.Approved
                ? (int)TestingStatus.Completed
                : (int)TestingStatus.PendingApproval;
        if (testDate.HasValue) return (int)TestingStatus.InProgress;
        return (int)TestingStatus.NotStarted;
    }

    private async Task<string> GenerateBatchCodeAsync()
    {
        var todayPrefix = _codeGenerator.GetTodayPrefix("Batch");

        using (_dataFilter.Disable<Volo.Abp.ISoftDelete>())
        {
            var query = await _testBatchRepository.GetQueryableAsync();
            var count = await AsyncExecuter.CountAsync(query.Where(x => x.Code.StartsWith(todayPrefix)));
            return _codeGenerator.GenerateCodeWithDate("Batch", count, sequencePadding: 3);
        }
    }

    private async Task ValidateAndAddItemsAsync(TestBatch batch, List<Guid> laboratoryTaskIndicatorIds)
    {
        var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x =>
            laboratoryTaskIndicatorIds.Contains(x.Id));

        if (!taskIndicators.Any())
        {
            throw new UserFriendlyException("Không tìm thấy chỉ tiêu nào");
        }

        var noMethod = taskIndicators.FirstOrDefault(x => !x.MethodId.HasValue);
        if (noMethod != null)
        {
            throw new UserFriendlyException("Chỉ tiêu chưa được gán phương pháp thử nghiệm");
        }

        var sampleIndicatorIds = taskIndicators.Select(x => x.TestRequestSampleIndicatorId).Distinct().ToList();
        var sampleIndicators = await _sampleIndicatorRepository.GetListAsync(x => sampleIndicatorIds.Contains(x.Id));
        var sampleIndicatorMap = sampleIndicators.ToDictionary(x => x.Id);

        foreach (var taskIndicator in taskIndicators)
        {
            if (!sampleIndicatorMap.TryGetValue(taskIndicator.TestRequestSampleIndicatorId, out var sampleIndicator))
                continue;

            if (batch.Items.Any(x => x.LaboratoryTaskIndicatorId == taskIndicator.Id))
                continue;

            var task = await _laboratoryTaskRepository.FindAsync(taskIndicator.LaboratoryTaskId);
            var sample = await _sampleRepository.FindAsync(sampleIndicator.TestRequestSampleId);

            var item = new TestBatchItem(
                GuidGenerator.Create(),
                batch.Id,
                taskIndicator.Id,
                taskIndicator.LaboratoryTaskId,
                task?.ContractNumber,
                sample?.SampleCode);

            if (taskIndicator.ApprovalStatus == IndicatorApprovalStatus.Approved)
            {
                item.IsExternallyCompleted = true;
                item.Result = taskIndicator.Result;
                item.TestDate = taskIndicator.TestDate;
                item.IsSubmitted = true;
            }

            batch.Items.Add(item);
        }
    }

    private async Task UpdateTaskStatusesAsync(List<TestBatchItem> submittedItems)
    {
        var taskIds = submittedItems.Select(x => x.LaboratoryTaskId).Distinct().ToList();

        foreach (var taskId in taskIds)
        {
            var task = await _laboratoryTaskRepository.FindAsync(taskId);
            if (task == null) continue;

            var allIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => x.LaboratoryTaskId == taskId);
            var allEntered = allIndicators.All(i =>
                i.ApprovalStatus == IndicatorApprovalStatus.Entered ||
                i.ApprovalStatus == IndicatorApprovalStatus.Approved ||
                i.ApprovalStatus == IndicatorApprovalStatus.CustomerCancelled ||
                i.ApprovalStatus == IndicatorApprovalStatus.TestingRejected);

            if (allEntered && task.Status != LaboratoryTaskStatus.Approved)
            {
                task.Status = LaboratoryTaskStatus.WaitingApproval;
                await _laboratoryTaskRepository.UpdateAsync(task, autoSave: false);
            }
        }
    }

    private async Task DeductStockAsync(List<TestBatchConsumption> consumptions)
    {
        var productConsumptions = consumptions.Where(c => c.ProductId.HasValue).ToList();
        var equipmentConsumptions = consumptions.Where(c => c.EquipmentId.HasValue).ToList();

        if (productConsumptions.Any())
        {
            var productIds = productConsumptions.Select(c => c.ProductId!.Value).Distinct().ToList();
            var products = await _productRepository.GetListAsync(p => productIds.Contains(p.Id));
            var productMap = products.ToDictionary(p => p.Id);

            var skus = await _productSkuRepository.GetListAsync(s => productIds.Contains(s.ProductId));
            var skuByProduct = skus.GroupBy(s => s.ProductId).ToDictionary(g => g.Key, g => g.ToList());

            var conversions = await _productUnitConversionRepository.GetListAsync(c => productIds.Contains(c.ProductId));
            var conversionMap = conversions.ToDictionary(c => (c.ProductId, c.ConvertedUnitId));

            foreach (var consumption in productConsumptions)
            {
                if (!productMap.TryGetValue(consumption.ProductId!.Value, out var product))
                    continue;

                var consumeUnitId = consumption.UnitId;
                decimal? unitsPerStorage = null;

                // 1. Product-level units
                if (consumeUnitId == product.PackagingUnitId)
                    unitsPerStorage = 1;
                else if (consumeUnitId == product.VolumeUnitId && product.VolumePerUnit > 0)
                    unitsPerStorage = product.VolumePerUnit.Value;
                else if (consumeUnitId == product.BaseUnitId && product.PackagingQuantity > 0)
                    unitsPerStorage = 1m / product.PackagingQuantity.Value;

                // 2. SKU-level units
                if (unitsPerStorage == null && skuByProduct.TryGetValue(product.Id, out var pSkus))
                {
                    foreach (var sku in pSkus)
                    {
                        if (consumeUnitId == sku.PackagingUnitId) { unitsPerStorage = 1; break; }
                        if (consumeUnitId == sku.VolumeUnitId && sku.VolumePerUnit > 0) { unitsPerStorage = sku.VolumePerUnit.Value; break; }
                        if (consumeUnitId == sku.BaseUnitId && sku.PackagingQuantity > 0) { unitsPerStorage = 1m / sku.PackagingQuantity.Value; break; }
                    }
                }

                // 3. ProductUnitConversion table
                if (unitsPerStorage == null && consumeUnitId.HasValue
                    && conversionMap.TryGetValue((product.Id, consumeUnitId.Value), out var conv)
                    && conv.ConvertedQuantity > 0)
                {
                    unitsPerStorage = conv.ConvertedQuantity / conv.Quantity;
                }

                if (unitsPerStorage == null || unitsPerStorage.Value == 0)
                    continue;

                var storageQty = consumption.ActualQuantity / unitsPerStorage.Value;

            }
        }

        if (equipmentConsumptions.Any())
        {
            var usageByEquipment = equipmentConsumptions
                .GroupBy(c => c.EquipmentId!.Value)
                .ToDictionary(g => g.Key, g => (int)g.Sum(c => c.ActualQuantity));

            var equipments = await _equipmentRepository.GetListAsync(e => usageByEquipment.Keys.Contains(e.Id));

            foreach (var equipment in equipments)
            {
                equipment.UsageCount += usageByEquipment[equipment.Id];
                await _equipmentRepository.UpdateAsync(equipment, autoSave: true);
            }
        }
    }

    private async Task DeductStockForSubmittedBatchItemsAsync(TestBatch batch, List<TestBatchItem> submittedItems)
    {
        var tmiQuery = await _testMethodIndicatorRepository.GetQueryableAsync();
        var tmi = await AsyncExecuter.FirstOrDefaultAsync(
            tmiQuery.Where(x =>
                x.TestMethodId == batch.MethodId &&
                x.IndicatorId == batch.IndicatorId &&
                x.SpecimenTypeId == batch.SpecimenTypeId));

        if (tmi == null) return;

        var consumptionQuery = await _materialConsumptionRepository.GetQueryableAsync();
        var consumptionItems = await AsyncExecuter.ToListAsync(
            consumptionQuery.Where(c => c.TestMethodIndicatorId == tmi.Id));

        if (consumptionItems.Count == 0) return;

        var productIdsForWh = consumptionItems
            .Where(c => c.ProductId.HasValue).Select(c => c.ProductId!.Value).Distinct().ToList();

        // Resolve lab filter from the first submitted item's task
        Guid? laboratoryId = null;
        if (submittedItems.Any())
        {
            var labTask = await _laboratoryTaskRepository.GetAsync(submittedItems[0].LaboratoryTaskId);
            laboratoryId = labTask.LaboratoryId;
        }

        var allLotsMap = await ResolveAllLotsForProductsAsync(productIdsForWh, laboratoryId);

        // Preload unit conversion data
        var products = productIdsForWh.Any()
            ? await _productRepository.GetListAsync(p => productIdsForWh.Contains(p.Id))
            : new List<Product>();
        var productMap = products.ToDictionary(p => p.Id);
        var skus = productIdsForWh.Any()
            ? await _productSkuRepository.GetListAsync(s => productIdsForWh.Contains(s.ProductId))
            : new List<ProductSku>();
        var skuByProduct = skus.GroupBy(s => s.ProductId).ToDictionary(g => g.Key, g => g.ToList());
        var conversions = productIdsForWh.Any()
            ? await _productUnitConversionRepository.GetListAsync(c => productIdsForWh.Contains(c.ProductId))
            : new List<ProductUnitConversion>();
        var conversionMap = conversions.ToDictionary(c => (c.ProductId, c.ConvertedUnitId));

        // Resolve barcode codes for all lots
        var allLots = allLotsMap.Values.SelectMany(l => l).ToList();
        var barcodeMap = await ResolveBarcodeCodesForLotsAsync(allLots);

        // Compute consumable stock
        var consumableProductWhMap = productIdsForWh
            .ToDictionary(pid => pid, pid =>
            {
                var first = allLotsMap.GetValueOrDefault(pid)?.FirstOrDefault();
                return first?.WarehouseId;
            });
        var consumableStocks = await _consumableStockCalculator.ComputeAsync(consumableProductWhMap);

        // Shared pool: track cumulative consumption from each lot across all submitted items
        var consumedFromLot = new Dictionary<Guid, decimal>();
        var allNewConsumptions = new List<IndicatorConsumption>();

        foreach (var submittedItem in submittedItems)
        {
            foreach (var item in consumptionItems)
            {
                if (item.AvgConsumptionPerSample is null or 0) continue;

                if (item.EquipmentId.HasValue || !item.ProductId.HasValue)
                {
                    var consumption = new IndicatorConsumption(
                        GuidGenerator.Create(),
                        submittedItem.LaboratoryTaskIndicatorId,
                        item.ProductId, item.EquipmentId, item.UnitId,
                        item.AvgConsumptionPerSample.Value);
                    consumption.IsDeducted = true;
                    consumption.SubmitDate = Clock.Now;
                    await _indicatorConsumptionRepository.InsertAsync(consumption, autoSave: false);
                    allNewConsumptions.Add(consumption);
                    continue;
                }

                var productLots = allLotsMap.GetValueOrDefault(item.ProductId.Value);
                if (productLots == null || !productLots.Any()
                    || !productMap.TryGetValue(item.ProductId.Value, out var product))
                {
                    var consumption = new IndicatorConsumption(
                        GuidGenerator.Create(),
                        submittedItem.LaboratoryTaskIndicatorId,
                        item.ProductId, null, item.UnitId,
                        item.AvgConsumptionPerSample.Value);
                    consumption.IsDeducted = true;
                    consumption.SubmitDate = Clock.Now;
                    consumption.ConsumableStockBefore = consumableStocks.GetValueOrDefault(item.ProductId.Value);
                    await _indicatorConsumptionRepository.InsertAsync(consumption, autoSave: false);
                    allNewConsumptions.Add(consumption);
                    continue;
                }

                var unitsPerStorage = ResolveUnitsPerStorage(item.UnitId, product, skuByProduct, conversionMap);
                if (unitsPerStorage == null || unitsPerStorage.Value == 0)
                {
                    var consumption = new IndicatorConsumption(
                        GuidGenerator.Create(),
                        submittedItem.LaboratoryTaskIndicatorId,
                        item.ProductId, null, item.UnitId,
                        item.AvgConsumptionPerSample.Value);
                    consumption.IsDeducted = true;
                    consumption.SubmitDate = Clock.Now;
                    consumption.ConsumableStockBefore = consumableStocks.GetValueOrDefault(item.ProductId.Value);
                    await _indicatorConsumptionRepository.InsertAsync(consumption, autoSave: false);
                    allNewConsumptions.Add(consumption);
                    continue;
                }

                var remainingQty = item.AvgConsumptionPerSample.Value;
                bool isFirst = true;
                foreach (var lot in productLots)
                {
                    if (remainingQty <= 0) break;

                    var alreadyConsumed = consumedFromLot.GetValueOrDefault(lot.WarehouseImportReceiptItemId);
                    var lotAvailableBase = lot.RemainingBaseQuantity - alreadyConsumed;
                    if (lotAvailableBase <= 0) continue;

                    var lotAvailableInConsumptionUnit = lotAvailableBase * unitsPerStorage.Value;
                    var consumeFromLot = Math.Min(remainingQty, lotAvailableInConsumptionUnit);
                    var consumeFromLotBase = consumeFromLot / unitsPerStorage.Value;

                    var consumption = new IndicatorConsumption(
                        GuidGenerator.Create(),
                        submittedItem.LaboratoryTaskIndicatorId,
                        item.ProductId, null, item.UnitId,
                        isFirst ? item.AvgConsumptionPerSample.Value : 0);
                    consumption.ActualQuantity = consumeFromLot;
                    consumption.IsDeducted = true;
                    consumption.SubmitDate = Clock.Now;
                    consumption.WarehouseId = lot.WarehouseId;
                    consumption.LotNumber = lot.LotNumber;
                    consumption.WarehouseImportReceiptItemId = lot.WarehouseImportReceiptItemId;
                    consumption.BarcodeCode = barcodeMap.GetValueOrDefault(lot.WarehouseImportReceiptItemId);
                    consumption.ConsumableStockBefore = consumableStocks.GetValueOrDefault(item.ProductId.Value);
                    await _indicatorConsumptionRepository.InsertAsync(consumption, autoSave: false);
                    allNewConsumptions.Add(consumption);

                    consumedFromLot[lot.WarehouseImportReceiptItemId] = alreadyConsumed + consumeFromLotBase;
                    remainingQty -= consumeFromLot;
                    isFirst = false;
                }

                if (remainingQty > 0)
                {
                    var lastLot = productLots.Last();
                    var consumption = new IndicatorConsumption(
                        GuidGenerator.Create(),
                        submittedItem.LaboratoryTaskIndicatorId,
                        item.ProductId, null, item.UnitId, 0);
                    consumption.ActualQuantity = remainingQty;
                    consumption.IsDeducted = true;
                    consumption.SubmitDate = Clock.Now;
                    consumption.WarehouseId = lastLot.WarehouseId;
                    consumption.LotNumber = lastLot.LotNumber;
                    consumption.WarehouseImportReceiptItemId = lastLot.WarehouseImportReceiptItemId;
                    consumption.BarcodeCode = barcodeMap.GetValueOrDefault(lastLot.WarehouseImportReceiptItemId);
                    consumption.ConsumableStockBefore = consumableStocks.GetValueOrDefault(item.ProductId.Value);
                    await _indicatorConsumptionRepository.InsertAsync(consumption, autoSave: false);
                    allNewConsumptions.Add(consumption);
                }
            }
        }

        var warehouseMap = allNewConsumptions
            .Where(c => c.ProductId.HasValue && c.WarehouseId.HasValue)
            .GroupBy(c => c.ProductId!.Value)
            .ToDictionary(g => g.Key, g => (Guid?)g.First().WarehouseId);

        var submittedCount = submittedItems.Count;
        var productItems = consumptionItems.Where(c => c.ProductId.HasValue).ToList();
        var equipmentItems = consumptionItems.Where(c => c.EquipmentId.HasValue).ToList();

        if (productItems.Count > 0)
            await DeductProductConsumptionsAsync(productItems, submittedCount, batch.Id, warehouseMap);

        if (equipmentItems.Count > 0)
            await IncrementEquipmentUsageAsync(equipmentItems, submittedCount);

        await CurrentUnitOfWork.SaveChangesAsync();

        var barcodeTargets = allNewConsumptions
            .Where(c => c.ProductId.HasValue && c.WarehouseId.HasValue)
            .Select(c => (c.ProductId!.Value, c.WarehouseId!.Value))
            .Distinct();
        await _barcodeStatusService.RecalculateForProductsAsync(barcodeTargets);
    }

    private async Task DeductProductConsumptionsAsync(
        List<MaterialConsumptionItem> productItems, int submittedCount, Guid sourceId,
        Dictionary<Guid, Guid?> warehouseMap)
    {
        var productIds = productItems.Select(c => c.ProductId!.Value).Distinct().ToList();
        var products = await _productRepository.GetListAsync(p => productIds.Contains(p.Id));
        var productMap = products.ToDictionary(p => p.Id);

        var skus = await _productSkuRepository.GetListAsync(s => productIds.Contains(s.ProductId));
        var skuByProduct = skus.GroupBy(s => s.ProductId).ToDictionary(g => g.Key, g => g.ToList());

        var whQuery = await _warehouseRepository.GetQueryableAsync();
        var deductibleWhIds = whQuery
            .Where(w => w.IsActive && w.Level > 0 && !w.IsDefault)
            .Select(w => w.Id)
            .ToList();

        foreach (var item in productItems)
        {
            if (!productMap.TryGetValue(item.ProductId!.Value, out var product))
                continue;

            if (item.AvgConsumptionPerSample is null or 0)
                continue;

            var deductQty = item.AvgConsumptionPerSample.Value * submittedCount;
            var baseQty = await ResolveBaseQuantityAsync(
                deductQty, item.UnitId, item.ProductId, product, skuByProduct);

            if (baseQty == null) continue;

            var whId = warehouseMap.GetValueOrDefault(item.ProductId!.Value);
            var whIds = whId.HasValue
                ? new List<Guid> { whId.Value }
                : deductibleWhIds;

        }
    }

    private async Task<decimal?> ResolveBaseQuantityAsync(
        decimal quantity, Guid? consumeUnitId, Guid? productId,
        Product product, Dictionary<Guid, List<ProductSku>> skuByProduct)
    {
        if (consumeUnitId == product.PackagingUnitId)
            return quantity;
        if (consumeUnitId == product.VolumeUnitId && product.VolumePerUnit > 0)
            return quantity / product.VolumePerUnit.Value;
        if (consumeUnitId == product.BaseUnitId && product.PackagingQuantity > 0)
            return quantity * product.PackagingQuantity.Value;

        decimal? factor = null;

        if (skuByProduct.TryGetValue(product.Id, out var pSkus))
        {
            foreach (var sku in pSkus)
            {
                if (consumeUnitId == sku.PackagingUnitId) { factor = 1; break; }
                if (consumeUnitId == sku.VolumeUnitId && sku.VolumePerUnit > 0) { factor = (decimal)sku.VolumePerUnit.Value; break; }
                if (consumeUnitId == sku.BaseUnitId && sku.PackagingQuantity > 0) { factor = 1m / sku.PackagingQuantity.Value; break; }
            }
        }

        if (factor == null && consumeUnitId.HasValue)
        {
            var conversionQuery = await _productUnitConversionRepository.GetQueryableAsync();
            var conversion = await AsyncExecuter.FirstOrDefaultAsync(
                conversionQuery.Where(c =>
                    c.ProductId == productId && c.ConvertedUnitId == consumeUnitId.Value));

            if (conversion != null && conversion.ConvertedQuantity > 0)
                factor = conversion.ConvertedQuantity / conversion.Quantity;
        }

        if (factor is null or 0) return null;

        return quantity / factor.Value;
    }

    private async Task IncrementEquipmentUsageAsync(
        List<MaterialConsumptionItem> equipmentItems, int submittedCount)
    {
        var usageByEquipment = equipmentItems
            .GroupBy(c => c.EquipmentId!.Value)
            .ToDictionary(g => g.Key, g => (int)g.Sum(c => c.Quantity) * submittedCount);

        var equipments = await _equipmentRepository.GetListAsync(e => usageByEquipment.Keys.Contains(e.Id));

        foreach (var equipment in equipments)
        {
            equipment.UsageCount += usageByEquipment[equipment.Id];
            await _equipmentRepository.UpdateAsync(equipment, autoSave: true);
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

    private async Task<TestBatchDto> MapToDetailDtoAsync(TestBatch batch)
    {
        var items = await _testBatchItemRepository.GetListAsync(x => x.TestBatchId == batch.Id);
        var consumptions = await _testBatchConsumptionRepository.GetListAsync(x => x.TestBatchId == batch.Id);

        var indicator = await _indicatorRepository.FindAsync(batch.IndicatorId);
        var method = await _testMethodRepository.FindAsync(batch.MethodId);
        var specimenType = batch.SpecimenTypeId.HasValue
            ? await _specimenTypeRepository.FindAsync(batch.SpecimenTypeId.Value)
            : null;

        var completedByName = await _userNameProvider.GetUserNameAsync(batch.CompletedById);

        var taskIndicatorIds = items.Select(i => i.LaboratoryTaskIndicatorId).ToList();
        var taskIndicators = await _laboratoryTaskIndicatorRepository.GetListAsync(x => taskIndicatorIds.Contains(x.Id));
        var taskIndicatorMap = taskIndicators.ToDictionary(x => x.Id);

        var allTechIds = taskIndicators.Where(ti => ti.TechnicianId.HasValue).Select(ti => ti.TechnicianId!.Value).Distinct().ToList();
        var techMap = await _userNameProvider.GetUserNamesAsync(allTechIds.Cast<Guid?>());

        var sampleIndicatorIds = taskIndicators.Select(ti => ti.TestRequestSampleIndicatorId).Distinct().ToList();
        var sampleIndicators = await _sampleIndicatorRepository.GetListAsync(si => sampleIndicatorIds.Contains(si.Id));
        var sampleIndicatorMap = sampleIndicators.ToDictionary(si => si.Id);

        var sampleIds = sampleIndicators.Select(si => si.TestRequestSampleId).Distinct().ToList();
        var samples = await _sampleRepository.GetListAsync(s => sampleIds.Contains(s.Id));
        var sampleMap = samples.ToDictionary(s => s.Id);

        var allIndicatorIds = sampleIndicators.Select(si => si.IndicatorId).Distinct().ToList();
        var allIndicators = await _indicatorRepository.GetListAsync(x => allIndicatorIds.Contains(x.Id));
        var indicatorNameMap = allIndicators.ToDictionary(x => x.Id, x => x.Name);

        var allMethodIds = taskIndicators.Where(ti => ti.MethodId.HasValue).Select(ti => ti.MethodId!.Value).Distinct().ToList();
        var allMethods = allMethodIds.Any() ? await _testMethodRepository.GetListAsync(x => allMethodIds.Contains(x.Id)) : new List<TestMethod>();
        var methodNameMap = allMethods.ToDictionary(x => x.Id, x => x.Name);

        var productIds = consumptions.Where(c => c.ProductId.HasValue).Select(c => c.ProductId!.Value).Distinct().ToList();
        var equipmentIds = consumptions.Where(c => c.EquipmentId.HasValue).Select(c => c.EquipmentId!.Value).Distinct().ToList();
        var unitIds = consumptions.Where(c => c.UnitId.HasValue).Select(c => c.UnitId!.Value).Distinct().ToList();

        var productsForConsumption = productIds.Any() ? await _productRepository.GetListAsync(p => productIds.Contains(p.Id)) : new List<Product>();
        var equipmentsForConsumption = equipmentIds.Any() ? await _equipmentRepository.GetListAsync(e => equipmentIds.Contains(e.Id)) : new List<Equipment>();
        var units = unitIds.Any() ? await _unitRepository.GetListAsync(u => unitIds.Contains(u.Id)) : new List<Unit>();

        var productNameMap = productsForConsumption.ToDictionary(p => p.Id, p => p.Name);
        var equipmentNameMap = equipmentsForConsumption.ToDictionary(e => e.Id, e => e.Name);
        var unitNameMap = units.ToDictionary(u => u.Id, u => u.Name);

        var dto = new TestBatchDto
        {
            Id = batch.Id,
            Code = batch.Code,
            Name = batch.Name,
            IndicatorId = batch.IndicatorId,
            IndicatorName = indicator?.Name,
            MethodId = batch.MethodId,
            MethodName = method?.Name,
            SpecimenTypeId = batch.SpecimenTypeId,
            SpecimenTypeName = specimenType?.Name,
            Status = batch.Status,
            TestDate = batch.TestDate,
            CompletedDate = batch.CompletedDate,
            CompletedById = batch.CompletedById,
            CompletedByName = completedByName,
            Note = batch.Note,
            ConsumptionConfirmed = batch.ConsumptionConfirmed,
            ConsumptionConfirmedDate = batch.ConsumptionConfirmedDate,
            HasApprovedIndicators = taskIndicators.Any(x => x.ApprovalStatus == IndicatorApprovalStatus.Approved),
            CreationTime = batch.CreationTime,
            CreatorId = batch.CreatorId,
            LastModificationTime = batch.LastModificationTime,
            LastModifierId = batch.LastModifierId,
            IsDeleted = batch.IsDeleted,
            DeleterId = batch.DeleterId,
            DeletionTime = batch.DeletionTime,
            Items = items.Select(i =>
            {
                var taskIndicator = taskIndicatorMap.GetValueOrDefault(i.LaboratoryTaskIndicatorId);
                var sampleIndicator2 = taskIndicator != null ? sampleIndicatorMap.GetValueOrDefault(taskIndicator.TestRequestSampleIndicatorId) : null;
                var sample = sampleIndicator2 != null ? sampleMap.GetValueOrDefault(sampleIndicator2.TestRequestSampleId) : null;

                return new TestBatchItemDto
                {
                    Id = i.Id,
                    TestBatchId = i.TestBatchId,
                    LaboratoryTaskIndicatorId = i.LaboratoryTaskIndicatorId,
                    LaboratoryTaskId = i.LaboratoryTaskId,
                    ContractNumber = i.ContractNumber,
                    SampleCode = i.SampleCode,
                    SampleName = sample?.SampleName,
                    TechnicianName = taskIndicator?.TechnicianId.HasValue == true ? techMap.GetValueOrDefault(taskIndicator.TechnicianId!.Value) : null,
                    IndicatorName = sampleIndicator2 != null ? indicatorNameMap.GetValueOrDefault(sampleIndicator2.IndicatorId) : null,
                    MethodName = taskIndicator?.MethodId.HasValue == true ? methodNameMap.GetValueOrDefault(taskIndicator.MethodId!.Value) : null,
                    Unit = taskIndicator?.Unit,
                    SubmitDate = taskIndicator?.SubmitDate,
                    RejectionReason = taskIndicator?.RejectionReason,
                    Result = i.Result,
                    TestDate = i.TestDate,
                    IsSubmitted = i.IsSubmitted,
                    IsExternallyCompleted = i.IsExternallyCompleted,
                    ApprovalStatus = taskIndicator != null ? (int)taskIndicator.ApprovalStatus : 0,
                };
            }).ToList(),
            Consumptions = consumptions.Select(c => new TestBatchConsumptionDto
            {
                Id = c.Id,
                TestBatchId = c.TestBatchId,
                ProductId = c.ProductId,
                ProductName = c.ProductId.HasValue ? productNameMap.GetValueOrDefault(c.ProductId.Value) : null,
                EquipmentId = c.EquipmentId,
                EquipmentName = c.EquipmentId.HasValue ? equipmentNameMap.GetValueOrDefault(c.EquipmentId.Value) : null,
                UnitId = c.UnitId,
                UnitName = c.UnitId.HasValue ? unitNameMap.GetValueOrDefault(c.UnitId.Value) : null,
                RecommendedQuantity = c.RecommendedQuantity,
                ActualQuantity = c.ActualQuantity,
                IsDeducted = c.IsDeducted,
            }).ToList(),
        };

        return dto;
    }

    #endregion

    private async Task<Guid?> ResolveLaboratoryIdFromBatchAsync(Guid batchId)
    {
        var batchItem = await AsyncExecuter.FirstOrDefaultAsync(
            (await _testBatchItemRepository.GetQueryableAsync())
                .Where(bi => bi.TestBatchId == batchId));
        if (batchItem == null) return null;
        var labTask = await _laboratoryTaskRepository.FindAsync(batchItem.LaboratoryTaskId);
        return labTask?.LaboratoryId;
    }

    private async Task<Dictionary<Guid, Guid?>> ResolveWarehouseForProductsAsync(
        IEnumerable<Guid> productIds, Guid? laboratoryId = null)
    {
        var resolved = await ResolveWarehouseAndLotForProductsAsync(productIds, laboratoryId);
        return resolved.ToDictionary(kv => kv.Key, kv => kv.Value.WarehouseId);
    }

    private async Task<Dictionary<Guid, (Guid? WarehouseId, string? LotNumber)>> ResolveWarehouseAndLotForProductsAsync(
        IEnumerable<Guid> productIds, Guid? laboratoryId = null)
    {
        var allLots = await ResolveAllLotsForProductsAsync(productIds, laboratoryId);
        return allLots.ToDictionary(
            kv => kv.Key,
            kv =>
            {
                var first = kv.Value.FirstOrDefault();
                return (first?.WarehouseId, first?.LotNumber);
            });
    }

    private async Task<Dictionary<Guid, List<LotAllocation>>> ResolveAllLotsForProductsAsync(
        IEnumerable<Guid> productIds, Guid? laboratoryId = null)
    {
        var ids = productIds.Distinct().ToList();
        if (!ids.Any()) return new Dictionary<Guid, List<LotAllocation>>();

        var warehouseQuery = await _warehouseRepository.GetQueryableAsync();
        var excludedWarehouseIds = warehouseQuery
            .Where(w => w.Level == 0 || w.IsDefault)
            .Select(w => w.Id)
            .ToHashSet();

        HashSet<Guid>? labWarehouseIds = null;
        if (laboratoryId.HasValue)
        {
            labWarehouseIds = warehouseQuery
                .Where(w => w.LaboratoryId == laboratoryId.Value)
                .Select(w => w.Id)
                .ToHashSet();
        }

        var itemQuery = await _importReceiptItemRepository.GetQueryableAsync();
        var receiptQuery = await _importReceiptRepository.GetQueryableAsync();

        var lots = (from item in itemQuery
                    join receipt in receiptQuery on item.WarehouseImportReceiptId equals receipt.Id
                    where ids.Contains(item.ProductId)
                          && receipt.Status == WarehouseImportReceiptStatus.Completed
                          && item.Status == WarehouseImportLineStatus.Checked
                          && (item.ReceivedBaseQuantity - item.ConsumedBaseQuantity) > 0
                    orderby item.LotExpiryDate == null ? 1 : 0,
                            item.LotExpiryDate,
                            receipt.ActualImportDate
                    select new LotAllocation
                    {
                        WarehouseImportReceiptItemId = item.Id,
                        ProductId = item.ProductId,
                        WarehouseId = item.StorageWarehouseId ?? receipt.WarehouseId,
                        LotNumber = item.LotNumber,
                        RemainingBaseQuantity = item.ReceivedBaseQuantity - item.ConsumedBaseQuantity,
                        LotExpiryDate = item.LotExpiryDate,
                        BarcodeValues = item.BarcodeValues,
                        SkuId = item.SkuId,
                    })
                    .ToList()
                    .Where(l => l.WarehouseId.HasValue
                                && !excludedWarehouseIds.Contains(l.WarehouseId.Value)
                                && (labWarehouseIds == null || labWarehouseIds.Contains(l.WarehouseId.Value)))
                    .ToList();

        if (labWarehouseIds != null && !lots.Any())
        {
            lots = (from item in itemQuery
                        join receipt in receiptQuery on item.WarehouseImportReceiptId equals receipt.Id
                        where ids.Contains(item.ProductId)
                              && receipt.Status == WarehouseImportReceiptStatus.Completed
                              && item.Status == WarehouseImportLineStatus.Checked
                              && (item.ReceivedBaseQuantity - item.ConsumedBaseQuantity) > 0
                        orderby item.LotExpiryDate == null ? 1 : 0,
                                item.LotExpiryDate,
                                receipt.ActualImportDate
                        select new LotAllocation
                        {
                            WarehouseImportReceiptItemId = item.Id,
                            ProductId = item.ProductId,
                            WarehouseId = item.StorageWarehouseId ?? receipt.WarehouseId,
                            LotNumber = item.LotNumber,
                            RemainingBaseQuantity = item.ReceivedBaseQuantity - item.ConsumedBaseQuantity,
                            LotExpiryDate = item.LotExpiryDate,
                            BarcodeValues = item.BarcodeValues,
                            SkuId = item.SkuId,
                        })
                        .ToList()
                        .Where(l => l.WarehouseId.HasValue
                                    && !excludedWarehouseIds.Contains(l.WarehouseId.Value))
                        .ToList();
        }

        return ids.ToDictionary(
            id => id,
            id => lots.Where(l => l.ProductId == id).ToList());
    }

    private async Task<Dictionary<Guid, string?>> ResolveBarcodeCodesForLotsAsync(
        List<LotAllocation> lots)
    {
        var receiptItemIds = lots
            .Select(l => l.WarehouseImportReceiptItemId)
            .Distinct()
            .ToList();
        if (!receiptItemIds.Any())
            return new Dictionary<Guid, string?>();

        var activeCodes = await _inventoryBarcodeRepository.GetActiveCodeByReceiptItemAsync(receiptItemIds);

        return receiptItemIds.ToDictionary(
            riId => riId,
            riId => activeCodes.TryGetValue(riId, out var active) ? active.Code : (string?)null);
    }

    private decimal? ResolveUnitsPerStorage(
        Guid? consumeUnitId,
        Product product,
        Dictionary<Guid, List<ProductSku>> skuByProduct,
        Dictionary<(Guid, Guid), ProductUnitConversion> conversionMap)
    {
        decimal? unitsPerStorage = null;

        if (consumeUnitId == product.PackagingUnitId)
            unitsPerStorage = 1;
        else if (consumeUnitId == product.VolumeUnitId && product.VolumePerUnit > 0)
            unitsPerStorage = product.VolumePerUnit.Value;
        else if (consumeUnitId == product.BaseUnitId && product.PackagingQuantity > 0)
            unitsPerStorage = 1m / product.PackagingQuantity.Value;

        if (unitsPerStorage == null && skuByProduct.TryGetValue(product.Id, out var pSkus))
        {
            foreach (var sku in pSkus)
            {
                if (consumeUnitId == sku.PackagingUnitId) { unitsPerStorage = 1; break; }
                if (consumeUnitId == sku.VolumeUnitId && sku.VolumePerUnit > 0) { unitsPerStorage = sku.VolumePerUnit.Value; break; }
                if (consumeUnitId == sku.BaseUnitId && sku.PackagingQuantity > 0) { unitsPerStorage = 1m / sku.PackagingQuantity.Value; break; }
            }
        }

        if (unitsPerStorage == null && consumeUnitId.HasValue
            && conversionMap.TryGetValue((product.Id, consumeUnitId.Value), out var conv)
            && conv.ConvertedQuantity > 0)
        {
            unitsPerStorage = conv.ConvertedQuantity / conv.Quantity;
        }

        return unitsPerStorage;
    }

    private class LotAllocation
    {
        public Guid WarehouseImportReceiptItemId { get; set; }
        public Guid ProductId { get; set; }
        public Guid? WarehouseId { get; set; }
        public string? LotNumber { get; set; }
        public decimal RemainingBaseQuantity { get; set; }
        public DateTime? LotExpiryDate { get; set; }
        public string? BarcodeValues { get; set; }
        public Guid? SkuId { get; set; }
    }
}
