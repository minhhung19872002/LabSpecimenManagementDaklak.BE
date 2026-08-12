using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.Products;
using LabSpecimenManagement.WarehouseImports;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.StockAdjustments;

[RemoteService(IsEnabled = false)]
[Authorize(LabSpecimenManagementPermissions.StockAdjustments.Default)]
public class StockAdjustmentAppService : ApplicationService, IStockAdjustmentAppService
{
    private readonly IRepository<StockAdjustment, Guid> _repository;
    private readonly IRepository<StockAdjustmentItem, Guid> _itemRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<ProductSku, Guid> _skuRepository;
    private readonly IRepository<Unit, Guid> _unitRepository;
    private readonly IRepository<Warehouse, Guid> _warehouseRepository;
    private readonly IRepository<StockMovementLog, Guid> _stockMovementLogRepository;
    private readonly IRepository<ProductWarehouseInventory, Guid> _productWarehouseInventoryRepository;
    private readonly IRepository<WarehouseImportReceiptItem, Guid> _importReceiptItemRepository;
    private readonly IRepository<WarehouseImportReceipt, Guid> _importReceiptRepository;
    private readonly ICodeGeneratorService _codeGenerator;
    private readonly IDataFilter _dataFilter;
    private readonly IUserNameProvider _userNameProvider;
    private readonly IFifoStockDeductionService _fifoStockService;
    private readonly IConsumableStockCalculator _consumableStockCalculator;

    public StockAdjustmentAppService(
        IRepository<StockAdjustment, Guid> repository,
        IRepository<StockAdjustmentItem, Guid> itemRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<ProductSku, Guid> skuRepository,
        IRepository<Unit, Guid> unitRepository,
        IRepository<Warehouse, Guid> warehouseRepository,
        IRepository<StockMovementLog, Guid> stockMovementLogRepository,
        IRepository<ProductWarehouseInventory, Guid> productWarehouseInventoryRepository,
        IRepository<WarehouseImportReceiptItem, Guid> importReceiptItemRepository,
        IRepository<WarehouseImportReceipt, Guid> importReceiptRepository,
        ICodeGeneratorService codeGenerator,
        IDataFilter dataFilter,
        IUserNameProvider userNameProvider,
        IFifoStockDeductionService fifoStockService,
        IConsumableStockCalculator consumableStockCalculator)
    {
        _repository = repository;
        _itemRepository = itemRepository;
        _productRepository = productRepository;
        _skuRepository = skuRepository;
        _unitRepository = unitRepository;
        _warehouseRepository = warehouseRepository;
        _stockMovementLogRepository = stockMovementLogRepository;
        _productWarehouseInventoryRepository = productWarehouseInventoryRepository;
        _importReceiptItemRepository = importReceiptItemRepository;
        _importReceiptRepository = importReceiptRepository;
        _codeGenerator = codeGenerator;
        _dataFilter = dataFilter;
        _userNameProvider = userNameProvider;
        _fifoStockService = fifoStockService;
        _consumableStockCalculator = consumableStockCalculator;
    }

    public async Task<PagedResultDto<StockAdjustmentListDto>> GetListAsync(GetStockAdjustmentListInput input)
    {
        var query = await _repository.GetQueryableAsync();

        if (input.Status.HasValue)
            query = query.Where(x => x.Status == input.Status.Value);

        if (input.FromDate.HasValue)
            query = query.Where(x => x.CreationTime >= input.FromDate.Value);

        if (input.ToDate.HasValue)
            query = query.Where(x => x.CreationTime <= input.ToDate.Value.AddDays(1));

        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var search = input.Search.Trim().ToLower();
            var itemQuery = await _itemRepository.GetQueryableAsync();
            var matchingAdjustmentIds = itemQuery
                .Where(i => i.ProductName.ToLower().Contains(search))
                .Select(i => i.StockAdjustmentId)
                .Distinct();

            query = query.Where(x =>
                x.Code.ToLower().Contains(search) ||
                matchingAdjustmentIds.Contains(x.Id));
        }

        var totalCount = query.Count();

        var items = query
            .OrderByDescending(x => x.CreationTime)
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        var dtos = items.Select(x => new StockAdjustmentListDto
        {
            Id = x.Id,
            Code = x.Code,
            Status = x.Status,
            CreationTime = x.CreationTime,
            InventoryDate = x.InventoryDate,
            InventoryUserName = x.InventoryUserName,
        }).ToList();

        await _userNameProvider.EnrichWithUserNamesAsync(
            dtos,
            dto => items.First(x => x.Id == dto.Id).CreatorId,
            (dto, name) => dto.CreatorName = name);

        return new PagedResultDto<StockAdjustmentListDto>(totalCount, dtos);
    }

    public async Task<StockAdjustmentDto> GetAsync(Guid id)
    {
        var adjustment = await _repository.GetAsync(id);
        return await MapToDetailDtoAsync(adjustment);
    }

    [Authorize(LabSpecimenManagementPermissions.StockAdjustments.Create)]
    public async Task<StockAdjustmentDto> CreateAsync(CreateUpdateStockAdjustmentDto input)
    {
        if (input.Items == null || !input.Items.Any())
            throw new UserFriendlyException("Vui lòng thêm ít nhất một sản phẩm.");

        var code = await GenerateCodeAsync();
        var inventoryUserName = await _userNameProvider.GetUserNameAsync(CurrentUser.Id);

        var adjustment = new StockAdjustment(GuidGenerator.Create(), code)
        {
            Notes = input.Notes,
            InventoryDate = input.InventoryDate ?? DateTime.Now,
            InventoryUserId = CurrentUser.Id,
            InventoryUserName = inventoryUserName,
        };

        await _repository.InsertAsync(adjustment, autoSave: true);

        await CreateItemsAsync(adjustment.Id, input.Items);

        return await GetAsync(adjustment.Id);
    }

    [Authorize(LabSpecimenManagementPermissions.StockAdjustments.Edit)]
    public async Task<StockAdjustmentDto> UpdateAsync(Guid id, CreateUpdateStockAdjustmentDto input)
    {
        var adjustment = await _repository.GetAsync(id);

        if (adjustment.Status != StockAdjustmentStatus.Draft)
            throw new UserFriendlyException("Chỉ có thể sửa phiếu ở trạng thái Lưu tạm.");

        if (input.Items == null || !input.Items.Any())
            throw new UserFriendlyException("Vui lòng thêm ít nhất một sản phẩm.");

        adjustment.Notes = input.Notes;
        if (input.InventoryDate.HasValue)
            adjustment.InventoryDate = input.InventoryDate.Value;

        await _repository.UpdateAsync(adjustment, autoSave: true);

        var existingItems = (await _itemRepository.GetQueryableAsync())
            .Where(x => x.StockAdjustmentId == id)
            .ToList();

        foreach (var existing in existingItems)
        {
            await _itemRepository.DeleteAsync(existing, autoSave: true);
        }

        await CreateItemsAsync(adjustment.Id, input.Items);

        return await GetAsync(id);
    }

    [Authorize(LabSpecimenManagementPermissions.StockAdjustments.Delete)]
    public async Task DeleteAsync(Guid id)
    {
        var adjustment = await _repository.GetAsync(id);

        if (adjustment.Status != StockAdjustmentStatus.Draft)
            throw new UserFriendlyException("Chỉ có thể xóa phiếu ở trạng thái Lưu tạm.");

        var items = (await _itemRepository.GetQueryableAsync())
            .Where(x => x.StockAdjustmentId == id)
            .ToList();

        foreach (var item in items)
        {
            await _itemRepository.DeleteAsync(item, autoSave: true);
        }

        await _repository.DeleteAsync(id);
    }

    [Authorize(LabSpecimenManagementPermissions.StockAdjustments.Confirm)]
    public async Task<StockAdjustmentDto> ConfirmAsync(Guid id)
    {
        var adjustment = await _repository.GetAsync(id);

        if (adjustment.Status != StockAdjustmentStatus.Draft)
            throw new UserFriendlyException("Chỉ có thể xác nhận phiếu ở trạng thái Lưu tạm.");

        var items = (await _itemRepository.GetQueryableAsync())
            .Where(x => x.StockAdjustmentId == id)
            .ToList();

        var affectedProductIds = items.Select(x => x.ProductId).Distinct();
        await _consumableStockCalculator.FreezeAsync(affectedProductIds);

        var importItemQ = await _importReceiptItemRepository.GetQueryableAsync();
        var importReceiptQ = await _importReceiptRepository.GetQueryableAsync();

        foreach (var item in items)
        {
            var product = await _productRepository.GetAsync(item.ProductId);

            // Sync product.AvailableQuantity with ALL lot data (global, prevents drift)
            var allLotData = (from ii in importItemQ
                              join ir in importReceiptQ on ii.WarehouseImportReceiptId equals ir.Id
                              where ii.ProductId == product.Id
                                    && ir.Status == WarehouseImportReceiptStatus.Completed
                                    && ii.Status == WarehouseImportLineStatus.Checked
                              select new { ii.ReceivedBaseQuantity, ii.ConsumedBaseQuantity }).ToList();

            if (allLotData.Any())
            {
                var globalLotQty = allLotData.Sum(l => Math.Max(0, l.ReceivedBaseQuantity - l.ConsumedBaseQuantity));
                if (product.AvailableQuantity != globalLotQty)
                    product.AvailableQuantity = globalLotQty;
            }

            // quantityBefore = warehouse-filtered lot remaining (not global)
            decimal quantityBefore;
            if (item.WarehouseId.HasValue)
            {
                var whLotData = (from ii in importItemQ
                                 join ir in importReceiptQ on ii.WarehouseImportReceiptId equals ir.Id
                                 where ii.ProductId == product.Id
                                       && ir.Status == WarehouseImportReceiptStatus.Completed
                                       && ii.Status == WarehouseImportLineStatus.Checked
                                 where (ii.StorageWarehouseId.HasValue && ii.StorageWarehouseId == item.WarehouseId.Value)
                                       || (!ii.StorageWarehouseId.HasValue && ir.WarehouseId.HasValue && ir.WarehouseId == item.WarehouseId.Value)
                                 select new { ii.ReceivedBaseQuantity, ii.ConsumedBaseQuantity }).ToList();
                quantityBefore = whLotData.Sum(l => Math.Max(0, l.ReceivedBaseQuantity - l.ConsumedBaseQuantity));
            }
            else
            {
                quantityBefore = product.AvailableQuantity;
            }

            var actualInBaseUnit = item.ActualQuantity;
            if (item.ActualUnitId.HasValue && item.ActualUnitId != product.BaseUnitId)
            {
                var volumeUnitId = product.VolumeUnitId;
                int? volumePerUnit = product.VolumePerUnit;

                if (!volumeUnitId.HasValue || !volumePerUnit.HasValue || volumePerUnit.Value <= 0)
                {
                    var sku = (await _skuRepository.GetQueryableAsync())
                        .Where(s => s.ProductId == product.Id && s.VolumePerUnit.HasValue && s.VolumePerUnit > 0)
                        .FirstOrDefault();
                    if (sku != null)
                    {
                        volumeUnitId ??= sku.VolumeUnitId;
                        if (!volumePerUnit.HasValue || volumePerUnit.Value <= 0)
                            volumePerUnit = sku.VolumePerUnit;
                    }
                }

                if (volumeUnitId.HasValue && item.ActualUnitId == volumeUnitId
                    && volumePerUnit.HasValue && volumePerUnit.Value > 0)
                {
                    actualInBaseUnit = item.ActualQuantity / volumePerUnit.Value;
                }
                else if (product.PackagingUnitId.HasValue && item.ActualUnitId == product.PackagingUnitId
                         && product.PackagingQuantity.HasValue && product.PackagingQuantity.Value > 0)
                {
                    actualInBaseUnit = item.ActualQuantity / product.PackagingQuantity.Value;
                }
            }

            item.QuantityBefore = quantityBefore;
            item.AdjustmentQuantity = quantityBefore - actualInBaseUnit;
            await _itemRepository.UpdateAsync(item, autoSave: true);

            List<Guid> adjustWhIds;
            if (item.WarehouseId.HasValue)
            {
                adjustWhIds = new List<Guid> { item.WarehouseId.Value };
            }
            else
            {
                adjustWhIds = (await _warehouseRepository.GetQueryableAsync())
                    .Where(w => w.IsActive && w.Level > 0 && !w.IsDefault)
                    .Select(w => w.Id)
                    .ToList();
            }

            if (actualInBaseUnit < quantityBefore)
            {
                var diff = quantityBefore - actualInBaseUnit;
                await _fifoStockService.DeductAsync(
                    product, diff,
                    StockMovementType.StockAdjustment,
                    adjustment.Id, adjustment.Code, null,
                    adjustWhIds);
            }
            else
            {
                if (actualInBaseUnit > quantityBefore)
                {
                    product.AvailableQuantity = actualInBaseUnit;
                    await _productRepository.UpdateAsync(product, autoSave: true);
                }

                var log = new StockMovementLog(
                    GuidGenerator.Create(),
                    product.Id,
                    product.Name,
                    quantityBefore,
                    actualInBaseUnit,
                    StockMovementType.StockAdjustment,
                    adjustment.Id,
                    adjustment.Code,
                    null,
                    item.WarehouseId);
                await _stockMovementLogRepository.InsertAsync(log, autoSave: true);
            }

            if (item.WarehouseId.HasValue)
            {
                await UpsertProductWarehouseInventoryAsync(
                    item.ProductId, item.WarehouseId.Value, Clock.Now);
            }
        }

        adjustment.Status = StockAdjustmentStatus.Confirmed;
        await _repository.UpdateAsync(adjustment, autoSave: true);

        return await GetAsync(id);
    }

    public async Task<ConfirmPreviewDto> GetConfirmPreviewAsync(Guid id)
    {
        var adjustment = await _repository.GetAsync(id);

        if (adjustment.Status != StockAdjustmentStatus.Draft)
            throw new UserFriendlyException("Chỉ có thể kiểm tra phiếu ở trạng thái Lưu tạm.");

        var items = (await _itemRepository.GetQueryableAsync())
            .Where(x => x.StockAdjustmentId == id)
            .ToList();

        var result = new ConfirmPreviewDto();

        foreach (var item in items)
        {
            var product = await _productRepository.GetAsync(item.ProductId);
            var currentQty = product.AvailableQuantity;
            var changed = currentQty != item.QuantityBefore;
            if (changed) result.HasChanges = true;

            result.Items.Add(new ConfirmPreviewItemDto
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName,
                UnitName = item.UnitName,
                SnapshotQuantity = item.QuantityBefore,
                CurrentQuantity = currentQty,
                ActualQuantity = item.ActualQuantity,
                HasChanged = changed,
            });
        }

        return result;
    }

    #region Private Methods

    private async Task<string> GenerateCodeAsync()
    {
        using (_dataFilter.Disable<Volo.Abp.ISoftDelete>())
        {
            var todayPrefix = _codeGenerator.GetTodayPrefix("WR", "ddMMyyyy");
            var query = await _repository.GetQueryableAsync();
            var count = query.Count(x => x.Code.StartsWith(todayPrefix));
            return _codeGenerator.GenerateCodeWithDate("WR", count, "ddMMyyyy", 2);
        }
    }

    private async Task CreateItemsAsync(Guid adjustmentId, List<CreateUpdateStockAdjustmentItemDto> inputItems)
    {
        var unitQuery = await _unitRepository.GetQueryableAsync();
        var warehouseQuery = await _warehouseRepository.GetQueryableAsync();

        foreach (var itemInput in inputItems)
        {
            var product = await _productRepository.GetAsync(itemInput.ProductId);
            var unitName = unitQuery.Where(u => u.Id == product.BaseUnitId).Select(u => u.Name).FirstOrDefault() ?? "";

            string? warehouseName = null;
            if (itemInput.WarehouseId.HasValue)
            {
                warehouseName = warehouseQuery
                    .Where(w => w.Id == itemInput.WarehouseId.Value)
                    .Select(w => w.Name)
                    .FirstOrDefault();
            }

            string? actualUnitName = null;
            if (itemInput.ActualUnitId.HasValue)
            {
                actualUnitName = unitQuery
                    .Where(u => u.Id == itemInput.ActualUnitId.Value)
                    .Select(u => u.Name)
                    .FirstOrDefault();
            }

            var actualInBaseUnit = itemInput.ActualQuantity;
            if (itemInput.ActualUnitId.HasValue && itemInput.ActualUnitId != product.BaseUnitId)
            {
                var volumeUnitId = product.VolumeUnitId;
                int? volumePerUnit = product.VolumePerUnit;

                if (!volumeUnitId.HasValue || !volumePerUnit.HasValue || volumePerUnit.Value <= 0)
                {
                    var sku = (await _skuRepository.GetQueryableAsync())
                        .Where(s => s.ProductId == product.Id && s.VolumePerUnit.HasValue && s.VolumePerUnit > 0)
                        .FirstOrDefault();
                    if (sku != null)
                    {
                        volumeUnitId ??= sku.VolumeUnitId;
                        if (!volumePerUnit.HasValue || volumePerUnit.Value <= 0)
                            volumePerUnit = sku.VolumePerUnit;
                    }
                }

                if (volumeUnitId.HasValue && itemInput.ActualUnitId == volumeUnitId
                    && volumePerUnit.HasValue && volumePerUnit.Value > 0)
                {
                    actualInBaseUnit = itemInput.ActualQuantity / volumePerUnit.Value;
                }
                else if (product.PackagingUnitId.HasValue && itemInput.ActualUnitId == product.PackagingUnitId
                         && product.PackagingQuantity.HasValue && product.PackagingQuantity.Value > 0)
                {
                    actualInBaseUnit = itemInput.ActualQuantity / product.PackagingQuantity.Value;
                }
            }

            var item = new StockAdjustmentItem(
                GuidGenerator.Create(),
                adjustmentId,
                product.Id,
                product.Name,
                product.BaseUnitId ?? Guid.Empty,
                unitName,
                product.AvailableQuantity,
                actualInBaseUnit)
            {
                WarehouseId = itemInput.WarehouseId,
                WarehouseName = warehouseName,
                ActualUnitId = itemInput.ActualUnitId,
                ActualUnitName = actualUnitName,
                ActualQuantity = itemInput.ActualQuantity,
                Reason = itemInput.Reason,
                ExpectedStock = itemInput.ExpectedStock,
            };

            await _itemRepository.InsertAsync(item, autoSave: true);
        }
    }

    private async Task UpsertProductWarehouseInventoryAsync(
        Guid productId, Guid warehouseId, DateTime inventoryDate)
    {
        var query = await _productWarehouseInventoryRepository.GetQueryableAsync();
        var existing = query
            .FirstOrDefault(x => x.ProductId == productId && x.WarehouseId == warehouseId);

        if (existing != null)
        {
            existing.LastInventoryDate = inventoryDate;
            await _productWarehouseInventoryRepository.UpdateAsync(existing, autoSave: true);
        }
        else
        {
            var entity = new ProductWarehouseInventory(
                GuidGenerator.Create(), productId, warehouseId, inventoryDate);
            await _productWarehouseInventoryRepository.InsertAsync(entity, autoSave: true);
        }
    }

    private async Task<StockAdjustmentDto> MapToDetailDtoAsync(StockAdjustment adjustment)
    {
        var items = (await _itemRepository.GetQueryableAsync())
            .Where(x => x.StockAdjustmentId == adjustment.Id)
            .ToList();

        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        var products = (await _productRepository.GetQueryableAsync())
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.VolumePerUnit, p.PackagingUnitId, p.VolumeUnitId, p.ProductType, p.BaseUnitId })
            .ToList();

        var skus = (await _skuRepository.GetQueryableAsync())
            .Where(s => productIds.Contains(s.ProductId) && s.VolumePerUnit.HasValue && s.VolumePerUnit > 0)
            .Select(s => new { s.ProductId, s.VolumePerUnit, s.VolumeUnitId, s.PackagingUnitId })
            .ToList();
        var skuLookup = skus.GroupBy(s => s.ProductId).ToDictionary(g => g.Key, g => g.First());

        var unitIds = products
            .SelectMany(p => new Guid?[] { p.PackagingUnitId, p.VolumeUnitId, p.BaseUnitId })
            .Concat(skus.SelectMany(s => new[] { s.PackagingUnitId, s.VolumeUnitId }))
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        var unitNames = unitIds.Count > 0
            ? (await _unitRepository.GetQueryableAsync())
                .Where(u => unitIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Name })
                .ToDictionary(u => u.Id, u => u.Name)
            : new Dictionary<Guid, string>();

        var productLookup = products.ToDictionary(p => p.Id);

        var dto = new StockAdjustmentDto
        {
            Id = adjustment.Id,
            Code = adjustment.Code,
            Status = adjustment.Status,
            Notes = adjustment.Notes,
            CreationTime = adjustment.CreationTime,
            InventoryDate = adjustment.InventoryDate,
            InventoryUserId = adjustment.InventoryUserId,
            InventoryUserName = adjustment.InventoryUserName,
            Items = items.Select(i =>
            {
                var itemDto = new StockAdjustmentItemDto
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    UnitId = i.UnitId,
                    UnitName = i.UnitName,
                    QuantityBefore = i.QuantityBefore,
                    ActualQuantity = i.ActualQuantity,
                    AdjustmentQuantity = i.AdjustmentQuantity,
                    WarehouseId = i.WarehouseId,
                    WarehouseName = i.WarehouseName,
                    ActualUnitId = i.ActualUnitId,
                    ActualUnitName = i.ActualUnitName,
                    Reason = i.Reason,
                    ExpectedStock = i.ExpectedStock,
                };
                if (productLookup.TryGetValue(i.ProductId, out var prod))
                {
                    var volumePerUnit = prod.VolumePerUnit;
                    var packagingUnitId = prod.PackagingUnitId;
                    var volumeUnitId = prod.VolumeUnitId;

                    if ((!volumePerUnit.HasValue || volumePerUnit <= 0) && skuLookup.TryGetValue(i.ProductId, out var sku))
                    {
                        volumePerUnit = sku.VolumePerUnit;
                        volumeUnitId ??= sku.VolumeUnitId;
                        packagingUnitId ??= sku.PackagingUnitId;
                    }

                    itemDto.VolumePerUnit = volumePerUnit;
                    if (packagingUnitId.HasValue && unitNames.TryGetValue(packagingUnitId.Value, out var packagingName))
                        itemDto.PackagingUnitName = packagingName;
                    if (volumeUnitId.HasValue && unitNames.TryGetValue(volumeUnitId.Value, out var volumeName))
                        itemDto.VolumeUnitName = volumeName;

                    itemDto.ProductType = prod.ProductType;
                    if (prod.BaseUnitId.HasValue && unitNames.TryGetValue(prod.BaseUnitId.Value, out var baseUnitName))
                        itemDto.BaseUnitName = baseUnitName;
                }
                return itemDto;
            }).ToList(),
        };

        await _userNameProvider.EnrichWithUserNamesAsync(
            new List<StockAdjustmentDto> { dto },
            _ => adjustment.CreatorId,
            (d, name) => d.CreatorName = name);

        return dto;
    }

    #endregion
}
