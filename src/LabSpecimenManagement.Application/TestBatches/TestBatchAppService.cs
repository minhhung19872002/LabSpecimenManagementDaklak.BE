using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.Equipments;
using LabSpecimenManagement.LaboratoryTasks;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Products;
using LabSpecimenManagement.TestBatches;
using LabSpecimenManagement.Testing;
using LabSpecimenManagement.TestRequests;
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
    private readonly IUserNameProvider _userNameProvider;
    private readonly ICodeGeneratorService _codeGenerator;
    private readonly IDataFilter _dataFilter;
    private readonly IAsyncQueryableExecuter AsyncExecuter;
    private readonly IFifoStockDeductionService _fifoStockService;

    public TestBatchAppService(
        IRepository<TestBatch, Guid> testBatchRepository,
        IRepository<TestBatchItem, Guid> testBatchItemRepository,
        IRepository<TestBatchConsumption, Guid> testBatchConsumptionRepository,
        IRepository<LaboratoryTaskIndicator, Guid> laboratoryTaskIndicatorRepository,
        IRepository<LaboratoryTask, Guid> laboratoryTaskRepository,
        IRepository<TestRequestSampleIndicator, Guid> sampleIndicatorRepository,
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
        IUserNameProvider userNameProvider,
        ICodeGeneratorService codeGenerator,
        IDataFilter dataFilter,
        IAsyncQueryableExecuter asyncQueryableExecuter,
        IFifoStockDeductionService fifoStockService)
    {
        _testBatchRepository = testBatchRepository;
        _testBatchItemRepository = testBatchItemRepository;
        _testBatchConsumptionRepository = testBatchConsumptionRepository;
        _laboratoryTaskIndicatorRepository = laboratoryTaskIndicatorRepository;
        _laboratoryTaskRepository = laboratoryTaskRepository;
        _sampleIndicatorRepository = sampleIndicatorRepository;
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
        _userNameProvider = userNameProvider;
        _codeGenerator = codeGenerator;
        _dataFilter = dataFilter;
        AsyncExecuter = asyncQueryableExecuter;
        _fifoStockService = fifoStockService;
    }

    public async Task<PagedResultDto<TestBatchListDto>> GetListAsync(GetTestBatchListInput input)
    {
        var query = await _testBatchRepository.GetQueryableAsync();

        if (!string.IsNullOrWhiteSpace(input.Filter))
        {
            query = query.Where(x => x.Code.Contains(input.Filter) || x.Name.Contains(input.Filter));
        }

        if (input.Status.HasValue)
        {
            query = query.Where(x => x.Status == input.Status.Value);
        }

        if (input.IndicatorIds != null && input.IndicatorIds.Any())
        {
            query = query.Where(x => input.IndicatorIds.Contains(x.IndicatorId));
        }
        else if (input.IndicatorId.HasValue)
        {
            query = query.Where(x => x.IndicatorId == input.IndicatorId.Value);
        }

        if (input.MethodId.HasValue)
        {
            query = query.Where(x => x.MethodId == input.MethodId.Value);
        }

        if (input.SpecimenTypeId.HasValue)
        {
            query = query.Where(x => x.SpecimenTypeId == input.SpecimenTypeId.Value);
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

        query = query
            .OrderBy(x => x.Status)
            .ThenByDescending(x => x.CreationTime)
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

        return new PagedResultDto<TestBatchListDto>(totalCount, dtos);
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

        foreach (var consumptionItem in consumptionItems)
        {
            var batchConsumption = new TestBatchConsumption(
                GuidGenerator.Create(),
                batch.Id,
                consumptionItem.ProductId,
                consumptionItem.EquipmentId,
                consumptionItem.UnitId,
                consumptionItem.Quantity * submittedItemCount);

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

        if (hasDeducted)
        {
            foreach (var c in existingConsumptions)
            {
                lines.Add(new ConsumptionPreviewLineDto
                {
                    ConsumptionId = c.Id,
                    ProductId = c.ProductId,
                    EquipmentId = c.EquipmentId,
                    UnitId = c.UnitId,
                    RecommendedQuantity = c.RecommendedQuantity,
                    ActualQuantity = c.ActualQuantity,
                    IsDeducted = c.IsDeducted,
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
        var units = unitIds.Any() ? await _unitRepository.GetListAsync(u => unitIds.Contains(u.Id)) : new List<Unit>();
        var conversions = productIds.Any()
            ? await _productUnitConversionRepository.GetListAsync(c => productIds.Contains(c.ProductId))
            : new List<ProductUnitConversion>();

        var productMap = products.ToDictionary(p => p.Id);
        var equipmentMap = equipments.ToDictionary(e => e.Id);
        var unitMap = units.ToDictionary(u => u.Id, u => u.Name);
        var conversionMap = conversions.ToDictionary(c => (c.ProductId, c.ConvertedUnitId));

        foreach (var line in lines)
        {
            if (line.ProductId.HasValue && productMap.TryGetValue(line.ProductId.Value, out var product))
            {
                line.ProductName = product.Name;
                line.ProductCode = product.Code;
                line.ProductType = (int)product.ProductType;

                // Convert AvailableQuantity from base unit to the consumption unit
                var availableInUnit = product.AvailableQuantity;
                if (line.UnitId.HasValue && line.UnitId != product.BaseUnitId
                    && conversionMap.TryGetValue((product.Id, line.UnitId.Value), out var conv)
                    && conv.Quantity > 0)
                {
                    availableInUnit = product.AvailableQuantity * (conv.ConvertedQuantity / conv.Quantity);
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

        // Step 2: Handle consumption (only on first submit)
        if (!batch.ConsumptionConfirmed && input.ConsumptionLines != null && input.ConsumptionLines.Any())
        {
            var existingConsumptions = await _testBatchConsumptionRepository.GetListAsync(x => x.TestBatchId == batch.Id);
            var nonDeducted = existingConsumptions.Where(c => !c.IsDeducted).ToList();
            if (nonDeducted.Any())
            {
                await _testBatchConsumptionRepository.DeleteManyAsync(nonDeducted);
            }

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

                batch.Consumptions.Add(consumption);
            }

            await _testBatchRepository.UpdateAsync(batch, autoSave: true);

            var allConsumptions = await _testBatchConsumptionRepository.GetListAsync(x => x.TestBatchId == batch.Id && !x.IsDeducted);
            await DeductStockAsync(allConsumptions);

            foreach (var consumption in allConsumptions)
            {
                consumption.IsDeducted = true;
                await _testBatchConsumptionRepository.UpdateAsync(consumption, autoSave: false);
            }

            batch.ConsumptionConfirmed = true;
            batch.ConsumptionConfirmedDate = Clock.Now;
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

        var sampleIndicatorIds = taskIndicators.Select(x => x.TestRequestSampleIndicatorId).Distinct().ToList();
        var sampleIndicators = await _sampleIndicatorRepository.GetListAsync(x => sampleIndicatorIds.Contains(x.Id));
        var sampleIndicatorMap = sampleIndicators.ToDictionary(x => x.Id);

        foreach (var taskIndicator in taskIndicators)
        {
            if (!sampleIndicatorMap.TryGetValue(taskIndicator.TestRequestSampleIndicatorId, out var sampleIndicator))
                continue;

            if (sampleIndicator.IndicatorId != batch.IndicatorId)
            {
                throw new UserFriendlyException($"Chỉ tiêu không khớp với mẻ (yêu cầu cùng chỉ tiêu)");
            }

            if (taskIndicator.MethodId != batch.MethodId)
            {
                throw new UserFriendlyException($"Phương pháp không khớp với mẻ (yêu cầu cùng phương pháp)");
            }

            if (sampleIndicator.SpecimenTypeId != batch.SpecimenTypeId)
            {
                throw new UserFriendlyException($"Nền mẫu không khớp với mẻ (yêu cầu cùng nền mẫu)");
            }

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

            var conversionQuery = await _productUnitConversionRepository.GetQueryableAsync();

            foreach (var consumption in productConsumptions)
            {
                if (!productMap.TryGetValue(consumption.ProductId!.Value, out var product))
                    continue;

                decimal baseQty;
                if (consumption.UnitId == product.BaseUnitId)
                {
                    baseQty = consumption.ActualQuantity;
                }
                else
                {
                    var conversion = await AsyncExecuter.FirstOrDefaultAsync(
                        conversionQuery.Where(c =>
                            c.ProductId == consumption.ProductId && c.ConvertedUnitId == consumption.UnitId));

                    if (conversion == null || conversion.ConvertedQuantity == 0)
                        continue;

                    decimal ratio = conversion.Quantity / conversion.ConvertedQuantity;
                    baseQty = consumption.ActualQuantity * ratio;
                }

                await _fifoStockService.DeductAsync(
                    product, baseQty,
                    StockMovementType.MaterialConsumption,
                    consumption.TestBatchId, null,
                    "Tiêu hao thử nghiệm");
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
                    IndicatorName = indicator?.Name,
                    MethodName = method?.Name,
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
}
