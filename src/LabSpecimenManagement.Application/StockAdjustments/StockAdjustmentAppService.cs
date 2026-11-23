using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.Products;
using LabSpecimenManagement.Testing;
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
    private readonly IRepository<Manufacturer, Guid> _manufacturerRepository;
    private readonly ICodeGeneratorService _codeGenerator;
    private readonly IDataFilter _dataFilter;
    private readonly IUserNameProvider _userNameProvider;
    private readonly IFifoStockDeductionService _fifoStockService;
    private readonly IConsumableStockCalculator _consumableStockCalculator;
    private readonly InventoryBarcodeStatusService _barcodeStatusService;

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
        IRepository<Manufacturer, Guid> manufacturerRepository,
        ICodeGeneratorService codeGenerator,
        IDataFilter dataFilter,
        IUserNameProvider userNameProvider,
        IFifoStockDeductionService fifoStockService,
        IConsumableStockCalculator consumableStockCalculator,
        InventoryBarcodeStatusService barcodeStatusService)
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
        _manufacturerRepository = manufacturerRepository;
        _codeGenerator = codeGenerator;
        _dataFilter = dataFilter;
        _userNameProvider = userNameProvider;
        _fifoStockService = fifoStockService;
        _consumableStockCalculator = consumableStockCalculator;
        _barcodeStatusService = barcodeStatusService;
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

        if (input.WarehouseId.HasValue || input.ProductId.HasValue)
        {
            var itemQuery = await _itemRepository.GetQueryableAsync();
            if (input.WarehouseId.HasValue)
                itemQuery = itemQuery.Where(i => i.WarehouseId == input.WarehouseId.Value);
            if (input.ProductId.HasValue)
                itemQuery = itemQuery.Where(i => i.ProductId == input.ProductId.Value);

            var matchingAdjustmentIds = itemQuery
                .Select(i => i.StockAdjustmentId)
                .Distinct();

            query = query.Where(x => matchingAdjustmentIds.Contains(x.Id));
        }

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

        if (input.ProductId.HasValue)
            await FillProductAdjustmentAsync(dtos, input.ProductId.Value, input.WarehouseId);

        return new PagedResultDto<StockAdjustmentListDto>(totalCount, dtos);
    }

    /// <summary>
    /// Lịch sử kiểm kê trong chi tiết vật tư (KK-13): lượng điều chỉnh của riêng vật tư đó trên từng phiếu đã xác nhận.
    /// Dương = thừa, âm = thiếu (thực tế − sổ sách).
    /// </summary>
    private async Task FillProductAdjustmentAsync(List<StockAdjustmentListDto> dtos, Guid productId, Guid? warehouseId)
    {
        var confirmedIds = dtos.Where(d => d.Status == StockAdjustmentStatus.Confirmed).Select(d => d.Id).ToList();
        if (confirmedIds.Count == 0) return;

        var itemQuery = (await _itemRepository.GetQueryableAsync())
            .Where(i => confirmedIds.Contains(i.StockAdjustmentId) && i.ProductId == productId);
        if (warehouseId.HasValue)
            itemQuery = itemQuery.Where(i => i.WarehouseId == warehouseId.Value);
        var items = itemQuery.ToList();

        var product = await _productRepository.GetAsync(productId);
        var sku = (await _skuRepository.GetQueryableAsync())
            .Where(s => s.ProductId == productId && s.VolumePerUnit.HasValue && s.VolumePerUnit > 0)
            .FirstOrDefault();
        int? fallbackVpu = product.VolumePerUnit > 0 ? product.VolumePerUnit : sku?.VolumePerUnit;
        var packagingUnitId = product.PackagingUnitId ?? sku?.PackagingUnitId;
        var volumeUnitId = product.VolumeUnitId ?? sku?.VolumeUnitId;
        var unitIds = new[] { packagingUnitId, volumeUnitId, product.BaseUnitId }
            .Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
        var unitNames = (await _unitRepository.GetQueryableAsync())
            .Where(u => unitIds.Contains(u.Id))
            .ToDictionary(u => u.Id, u => u.Name);
        string? NameOf(Guid? uid) => uid.HasValue && unitNames.TryGetValue(uid.Value, out var n) ? n : null;
        var defaultStorageUnit = NameOf(packagingUnitId) ?? NameOf(product.BaseUnitId);

        // ĐV lưu kho theo SKU của từng lô — vật tư nhiều SKU (Lọ 3000 mL, Chai 6000 mL) không được cộng chung số lượng
        var rowWarehouseIds = items.Where(i => i.WarehouseId.HasValue).Select(i => i.WarehouseId!.Value).Distinct().ToList();
        var lotsByWarehouse = rowWarehouseIds.Count > 0
            ? await BuildLotsPerWarehouseAsync(rowWarehouseIds)
            : new Dictionary<Guid, List<WarehouseProductLotDto>>();
        string? StorageUnitOf(StockAdjustmentItem i)
        {
            var lot = i.WarehouseId.HasValue
                ? lotsByWarehouse.GetValueOrDefault(i.WarehouseId.Value)?
                    .FirstOrDefault(l => l.ProductId == productId && (l.LotNumber ?? "") == (i.LotNumber ?? ""))
                : null;
            return lot?.PackagingUnitName ?? lot?.BaseUnitName ?? defaultStorageUnit;
        }

        foreach (var dto in dtos.Where(d => d.Status == StockAdjustmentStatus.Confirmed))
        {
            var rows = items.Where(i => i.StockAdjustmentId == dto.Id).ToList();
            if (rows.Count == 0) continue;

            dto.AdjustmentDiffBase = -rows.Sum(r => r.AdjustmentQuantity);
            dto.AdjustmentDiffByUnit = rows
                .GroupBy(StorageUnitOf)
                .Select(g => new StockAdjustmentUnitDiffDto { UnitName = g.Key, Quantity = -g.Sum(r => r.AdjustmentQuantity) })
                .ToList();

            decimal volume = 0;
            var volumeKnown = true;
            foreach (var r in rows)
            {
                // Dung tích của lô = mL sổ sách / số chai sổ sách lúc xác nhận
                decimal? vpu = r.QuantityBefore > 0 && r.ConsumableQuantityBefore is > 0
                    ? r.ConsumableQuantityBefore.Value / r.QuantityBefore
                    : fallbackVpu;
                if (vpu is not > 0) { volumeKnown = false; break; }
                volume += -r.AdjustmentQuantity * vpu.Value;
            }
            dto.AdjustmentDiffVolume = volumeKnown ? volume : null;
            dto.StorageUnitName = dto.AdjustmentDiffByUnit.Count == 1 ? dto.AdjustmentDiffByUnit[0].UnitName : defaultStorageUnit;
            dto.VolumeUnitName = NameOf(volumeUnitId);
        }
    }

    public async Task<StockAdjustmentDto> GetAsync(Guid id)
    {
        var adjustment = await _repository.GetAsync(id);
        return await MapToDetailDtoAsync(adjustment);
    }

    [Authorize(LabSpecimenManagementPermissions.StockAdjustments.Create)]
    public async Task<StockAdjustmentDto> CreateAsync(CreateUpdateStockAdjustmentDto input)
    {
        ValidateItems(input);

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

        ValidateItems(input);

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

        // Global sync: run ONCE per product BEFORE the loop to prevent drift,
        // so surplus/deficit changes in the loop are not overwritten by a re-sync.
        var distinctProductIds = items.Select(x => x.ProductId).Distinct().ToList();
        foreach (var productId in distinctProductIds)
        {
            var product = await _productRepository.GetAsync(productId);
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
                {
                    product.AvailableQuantity = globalLotQty;
                    await _productRepository.UpdateAsync(product, autoSave: true);
                }
            }
        }

        var barcodeTargets = new List<(Guid ProductId, Guid WarehouseId)>();

        // Sổ sách + tiêu hao lý thuyết tính lại tại thời điểm xác nhận (BA Q18), theo từng lô
        var countedWarehouseIds = items
            .Where(x => x.WarehouseId.HasValue)
            .Select(x => x.WarehouseId!.Value)
            .Distinct()
            .ToList();
        var lotsByWarehouse = countedWarehouseIds.Count > 0
            ? await BuildLotsPerWarehouseAsync(countedWarehouseIds)
            : new Dictionary<Guid, List<WarehouseProductLotDto>>();

        foreach (var group in items.GroupBy(x => (x.ProductId, x.WarehouseId)).ToList())
        {
            var product = await _productRepository.GetAsync(group.Key.ProductId);

            if (group.Key.WarehouseId.HasValue)
            {
                var whId = group.Key.WarehouseId.Value;
                var productLots = (lotsByWarehouse.GetValueOrDefault(whId) ?? new List<WarehouseProductLotDto>())
                    .Where(l => l.ProductId == product.Id)
                    .ToList();

                await ConfirmWarehouseLotsAsync(adjustment, product, whId, group.ToList(), productLots);
                await UpsertProductWarehouseInventoryAsync(product.Id, whId, Clock.Now);
                barcodeTargets.Add((product.Id, whId));
            }
            else
            {
                var touchedWarehouseIds = await ConfirmWithoutWarehouseAsync(
                    adjustment, product, group.ToList(), importItemQ, importReceiptQ);
                foreach (var whId in touchedWarehouseIds)
                    barcodeTargets.Add((product.Id, whId));
            }
        }

        await _barcodeStatusService.RecalculateForProductsAsync(barcodeTargets);

        adjustment.Status = StockAdjustmentStatus.Confirmed;
        await _repository.UpdateAsync(adjustment, autoSave: true);

        return await GetAsync(id);
    }

    /// <summary>
    /// Xác nhận kiểm kê 1 vật tư trong 1 kho, tính riêng từng lô:
    /// - Lô có đếm: chênh lệch = sổ sách lô lúc xác nhận − thực tế, quy đổi mL theo dung tích SKU của chính lô;
    ///   phần thiếu trừ đúng lô đó (không trừ FEFO cả kho), phần thừa cộng vào sổ sách của chính lô đó.
    /// - Lô không đếm (dòng để trống, BA Q3): không điều chỉnh kiểm kê, nhưng mốc kiểm kê của vật tư trong kho
    ///   sẽ chuyển sang thời điểm xác nhận nên tiêu hao lý thuyết của lô đó được trừ vào chính lô để không bị mất.
    /// </summary>
    private async Task ConfirmWarehouseLotsAsync(
        StockAdjustment adjustment,
        Product product,
        Guid warehouseId,
        List<StockAdjustmentItem> items,
        List<WarehouseProductLotDto> productLots)
    {
        var lotByKey = productLots
            .GroupBy(l => l.LotNumber ?? "")
            .ToDictionary(g => g.Key, g => g.First());
        var countedLotKeys = new HashSet<string>();

        foreach (var item in items)
        {
            var lotKey = item.LotNumber ?? "";
            countedLotKeys.Add(lotKey);
            lotByKey.TryGetValue(lotKey, out var lot);

            var bookBase = lot?.RemainingBaseQuantity ?? 0;
            var actualBase = ConvertActualToBase(product, item.ActualQuantity, item.ActualUnitId,
                lot?.VolumePerUnit, lot?.VolumeUnitId);

            item.QuantityBefore = bookBase;
            item.ConsumableQuantityBefore = lot?.ConsumableQuantity ?? 0;
            if (lot != null)
                item.ExpectedStock = lot.ConsumableQuantity - lot.TheoreticalConsumption;
            item.AdjustmentQuantity = bookBase - actualBase;
            await _itemRepository.UpdateAsync(item, autoSave: true);

            if (actualBase < bookBase)
            {
                await _fifoStockService.DeductAsync(
                    product, bookBase - actualBase,
                    StockMovementType.StockAdjustment,
                    adjustment.Id, adjustment.Code, null,
                    new List<Guid> { warehouseId },
                    lotNumber: item.LotNumber);
                continue;
            }

            // Phần thừa: cộng vào chính lô đã đếm (K6/K32), không in thêm tem
            if (actualBase > bookBase
                && await AddSurplusToLotAsync(adjustment, product, warehouseId, item.LotNumber, actualBase - bookBase))
                continue;

            if (actualBase > bookBase)
            {
                // Không tìm thấy dòng nhập kho nào của lô trong kho: giữ cách ghi nhận cũ
                product.AvailableQuantity += actualBase - bookBase;
                await _productRepository.UpdateAsync(product, autoSave: true);
            }

            await _stockMovementLogRepository.InsertAsync(new StockMovementLog(
                GuidGenerator.Create(),
                product.Id,
                product.Name,
                bookBase,
                actualBase,
                StockMovementType.StockAdjustment,
                adjustment.Id,
                adjustment.Code,
                null,
                warehouseId,
                item.LotNumber), autoSave: true);
        }

        foreach (var lot in productLots.Where(l => !countedLotKeys.Contains(l.LotNumber ?? "")))
        {
            if (lot.TheoreticalConsumption <= 0 || lot.RemainingBaseQuantity <= 0) continue;

            var consumedBase = lot.VolumePerUnit is > 0
                ? lot.TheoreticalConsumption / lot.VolumePerUnit.Value
                : lot.TheoreticalConsumption;
            consumedBase = Math.Min(consumedBase, lot.RemainingBaseQuantity);

            await _fifoStockService.DeductAsync(
                product, consumedBase,
                StockMovementType.StockAdjustment,
                adjustment.Id, adjustment.Code,
                "Trừ tiêu hao của lô không kiểm kê",
                new List<Guid> { warehouseId },
                lotNumber: lot.LotNumber);
        }
    }

    /// <summary>
    /// Cộng phần thừa kiểm kê vào sổ sách của chính lô trong kho: giảm lượng đã xuất (ConsumedBaseQuantity)
    /// của các dòng nhập thuộc lô, dòng nhập mới nhất trước. Nếu thừa nhiều hơn tổng đã xuất (thực tế &gt; tổng nhập),
    /// phần còn lại ghi âm vào dòng nhập mới nhất để không đổi số lượng trên phiếu nhập.
    /// Trả về false nếu kho không có dòng nhập nào của lô.
    /// </summary>
    private async Task<bool> AddSurplusToLotAsync(
        StockAdjustment adjustment, Product product, Guid warehouseId, string? lotNumber, decimal surplusBase)
    {
        var importItemQ = await _importReceiptItemRepository.GetQueryableAsync();
        var importReceiptQ = await _importReceiptRepository.GetQueryableAsync();
        var lotKey = lotNumber ?? "";

        var lotLines = (from ii in importItemQ
                        join ir in importReceiptQ on ii.WarehouseImportReceiptId equals ir.Id
                        where ii.ProductId == product.Id
                              && ir.Status == WarehouseImportReceiptStatus.Completed
                              && ii.Status == WarehouseImportLineStatus.Checked
                              && !ii.IsEquipment
                              && (ii.LotNumber ?? "") == lotKey
                              && (ii.StorageWarehouseId == warehouseId
                                  || (ii.StorageWarehouseId == null && ir.WarehouseId == warehouseId))
                        orderby ir.ActualImportDate descending, ir.CreationTime descending
                        select ii).ToList();

        if (lotLines.Count == 0) return false;

        var remaining = surplusBase;
        var changes = new List<(WarehouseImportReceiptItem Line, decimal Qty)>();
        foreach (var line in lotLines.Where(l => l.ConsumedBaseQuantity > 0))
        {
            if (remaining <= 0) break;
            var give = Math.Min(line.ConsumedBaseQuantity, remaining);
            changes.Add((line, give));
            remaining -= give;
        }
        if (remaining > 0)
            changes.Add((lotLines[0], remaining));

        foreach (var (line, qty) in changes)
        {
            line.ConsumedBaseQuantity -= qty;
            await _importReceiptItemRepository.UpdateAsync(line, autoSave: true);

            var qtyBefore = product.AvailableQuantity;
            product.AvailableQuantity += qty;
            await _stockMovementLogRepository.InsertAsync(new StockMovementLog(
                GuidGenerator.Create(),
                product.Id,
                product.Name,
                qtyBefore,
                product.AvailableQuantity,
                StockMovementType.StockAdjustment,
                adjustment.Id,
                adjustment.Code,
                "Kiểm kê thừa — cộng vào lô",
                warehouseId,
                line.LotNumber,
                line.Id), autoSave: true);
        }

        await _productRepository.UpdateAsync(product, autoSave: true);
        return true;
    }

    /// <summary>
    /// Quy đổi số lượng thực tế về đơn vị lưu kho theo dung tích của lô (không dùng dung tích cấp vật tư / SKU đầu tiên).
    /// </summary>
    private static decimal ConvertActualToBase(
        Product product, decimal actualQuantity, Guid? actualUnitId, int? lotVolumePerUnit, Guid? lotVolumeUnitId)
    {
        if (!actualUnitId.HasValue) return actualQuantity;

        if (lotVolumePerUnit is > 0 && lotVolumeUnitId.HasValue && actualUnitId == lotVolumeUnitId)
            return actualQuantity / lotVolumePerUnit.Value;

        if (actualUnitId == product.BaseUnitId) return actualQuantity;

        if (product.PackagingUnitId.HasValue && actualUnitId == product.PackagingUnitId
            && product.PackagingQuantity is > 0)
            return actualQuantity / product.PackagingQuantity.Value;

        return actualQuantity;
    }

    /// <summary>Luồng cũ cho dòng kiểm kê không gắn kho (giữ nguyên hành vi trước đây).</summary>
    private async Task<List<Guid>> ConfirmWithoutWarehouseAsync(
        StockAdjustment adjustment,
        Product product,
        List<StockAdjustmentItem> group,
        IQueryable<WarehouseImportReceiptItem> importItemQ,
        IQueryable<WarehouseImportReceipt> importReceiptQ)
    {
        var productId = product.Id;
        Guid? warehouseId = null;
        {
            var (quantityBefore, consumableQuantityBefore) = await ComputeWarehouseStockAsync(
                importItemQ, importReceiptQ, product, warehouseId);

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

            var lotRemainingMap = new Dictionary<string, decimal>();
            var hasMultipleLots = group.Count() > 1;
            if (hasMultipleLots)
            {
                var lotQuery = from ii in importItemQ
                               join ir in importReceiptQ on ii.WarehouseImportReceiptId equals ir.Id
                               where ii.ProductId == productId
                                     && ir.Status == WarehouseImportReceiptStatus.Completed
                                     && ii.Status == WarehouseImportLineStatus.Checked
                               select new { ii, ir };

                if (warehouseId.HasValue)
                {
                    lotQuery = lotQuery.Where(x =>
                        (x.ii.StorageWarehouseId.HasValue && x.ii.StorageWarehouseId == warehouseId.Value)
                        || (!x.ii.StorageWarehouseId.HasValue && x.ir.WarehouseId.HasValue && x.ir.WarehouseId == warehouseId.Value));
                }

                var lotRows = lotQuery
                    .Select(x => new { x.ii.LotNumber, x.ii.ReceivedBaseQuantity, x.ii.ConsumedBaseQuantity })
                    .ToList();

                foreach (var lg in lotRows.GroupBy(l => l.LotNumber ?? ""))
                {
                    lotRemainingMap[lg.Key] = lg.Sum(l => Math.Max(0, l.ReceivedBaseQuantity - l.ConsumedBaseQuantity));
                }
            }

            decimal totalActualBase = 0;
            foreach (var item in group)
            {
                var actualInBaseUnit = item.ActualQuantity;
                if (item.ActualUnitId.HasValue && item.ActualUnitId != product.BaseUnitId)
                {
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

                totalActualBase += actualInBaseUnit;

                if (hasMultipleLots)
                {
                    var lotKey = item.LotNumber ?? "";
                    var lotBase = lotRemainingMap.TryGetValue(lotKey, out var lb) ? lb : 0;
                    item.QuantityBefore = lotBase;
                    item.ConsumableQuantityBefore = volumePerUnit.HasValue && volumePerUnit.Value > 0
                        ? lotBase * volumePerUnit.Value
                        : lotBase;
                    item.AdjustmentQuantity = lotBase - actualInBaseUnit;
                }
                else
                {
                    item.QuantityBefore = quantityBefore;
                    item.ConsumableQuantityBefore = consumableQuantityBefore;
                    item.AdjustmentQuantity = quantityBefore - actualInBaseUnit;
                }
                await _itemRepository.UpdateAsync(item, autoSave: true);
            }

            List<Guid> adjustWhIds;
            if (warehouseId.HasValue)
            {
                adjustWhIds = new List<Guid> { warehouseId.Value };
            }
            else
            {
                adjustWhIds = (await _warehouseRepository.GetQueryableAsync())
                    .Where(w => w.IsActive && w.Level > 0 && !w.IsDefault)
                    .Select(w => w.Id)
                    .ToList();
            }

            if (totalActualBase < quantityBefore)
            {
                var diff = quantityBefore - totalActualBase;
                await _fifoStockService.DeductAsync(
                    product, diff,
                    StockMovementType.StockAdjustment,
                    adjustment.Id, adjustment.Code, null,
                    adjustWhIds);
            }
            else
            {
                if (totalActualBase > quantityBefore)
                {
                    product.AvailableQuantity += (totalActualBase - quantityBefore);
                    await _productRepository.UpdateAsync(product, autoSave: true);
                }

                var log = new StockMovementLog(
                    GuidGenerator.Create(),
                    product.Id,
                    product.Name,
                    quantityBefore,
                    totalActualBase,
                    StockMovementType.StockAdjustment,
                    adjustment.Id,
                    adjustment.Code,
                    null,
                    warehouseId);
                await _stockMovementLogRepository.InsertAsync(log, autoSave: true);
            }

            return adjustWhIds;
        }
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

        var importItemQ = await _importReceiptItemRepository.GetQueryableAsync();
        var importReceiptQ = await _importReceiptRepository.GetQueryableAsync();

        foreach (var item in items)
        {
            var product = await _productRepository.GetAsync(item.ProductId);
            var (currentQty, _) = await ComputeWarehouseStockAsync(
                importItemQ, importReceiptQ, product, item.WarehouseId);
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

    public async Task<List<WarehouseProductLotDto>> GetWarehouseProductLotsAsync(Guid warehouseId)
    {
        var byWarehouse = await BuildLotsPerWarehouseAsync(new List<Guid> { warehouseId });
        return byWarehouse.GetValueOrDefault(warehouseId) ?? new List<WarehouseProductLotDto>();
    }

    public async Task<List<WarehouseWithLotsDto>> GetWarehouseWithChildrenProductLotsAsync(Guid warehouseId)
    {
        var allWarehouses = (await _warehouseRepository.GetQueryableAsync())
            .Where(w => w.IsActive)
            .Select(w => new { w.Id, w.Name, w.ParentWarehouseId })
            .ToList();

        var warehouseIds = CollectWarehouseIdsBfs(allWarehouses.Select(w => (w.Id, w.ParentWarehouseId)).ToList(), warehouseId);
        var warehouseNameMap = allWarehouses.ToDictionary(w => w.Id, w => w.Name);

        var byWarehouse = await BuildLotsPerWarehouseAsync(warehouseIds);

        return warehouseIds.Select(whId => new WarehouseWithLotsDto
        {
            WarehouseId = whId,
            WarehouseName = warehouseNameMap.TryGetValue(whId, out var name) ? name : whId.ToString(),
            Lots = byWarehouse.GetValueOrDefault(whId) ?? new List<WarehouseProductLotDto>(),
        }).ToList();
    }

    #region Private Methods

    private static void ValidateItems(CreateUpdateStockAdjustmentDto input)
    {
        if (input.Items == null || !input.Items.Any())
            throw new UserFriendlyException("Vui lòng thêm ít nhất một sản phẩm.");

        if (input.Items.Any(x => x.ActualQuantity < 0))
            throw new UserFriendlyException("Số lượng thực tế không được âm.");
    }

    private static List<Guid> CollectWarehouseIdsBfs(List<(Guid Id, Guid? ParentWarehouseId)> all, Guid rootId)
    {
        var result = new List<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(rootId);
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);
            foreach (var (id, parentId) in all.Where(w => w.ParentWarehouseId == current))
                queue.Enqueue(id);
        }
        return result;
    }

    private async Task<Dictionary<Guid, List<WarehouseProductLotDto>>> BuildLotsPerWarehouseAsync(List<Guid> warehouseIds)
    {
        var importItemQ = await _importReceiptItemRepository.GetQueryableAsync();
        var importReceiptQ = await _importReceiptRepository.GetQueryableAsync();

        var lotData = (from ii in importItemQ
                       join ir in importReceiptQ on ii.WarehouseImportReceiptId equals ir.Id
                       where ir.Status == WarehouseImportReceiptStatus.Completed
                             && ii.Status == WarehouseImportLineStatus.Checked
                             && !ii.IsEquipment
                             && ((ii.StorageWarehouseId.HasValue && warehouseIds.Contains(ii.StorageWarehouseId.Value))
                                 || (!ii.StorageWarehouseId.HasValue && ir.WarehouseId.HasValue && warehouseIds.Contains(ir.WarehouseId.Value)))
                       select new
                       {
                           ii.ProductId,
                           ii.LotNumber,
                           ii.SkuId,
                           ii.ReceivedBaseQuantity,
                           ii.ConsumedBaseQuantity,
                           EffectiveWarehouseId = ii.StorageWarehouseId.HasValue ? ii.StorageWarehouseId!.Value : ir.WarehouseId!.Value,
                       }).ToList();

        if (lotData.Count == 0)
            return warehouseIds.ToDictionary(id => id, _ => new List<WarehouseProductLotDto>());

        var productIds = lotData.Select(l => l.ProductId).Distinct().ToList();

        var products = (await _productRepository.GetQueryableAsync())
            .Where(p => productIds.Contains(p.Id)).ToList();
        var productMap = products.ToDictionary(p => p.Id);

        var skus = (await _skuRepository.GetQueryableAsync())
            .Where(s => productIds.Contains(s.ProductId)).ToList();
        var skuById = skus.ToDictionary(s => s.Id);
        var skusByProduct = skus.GroupBy(s => s.ProductId).ToDictionary(g => g.Key, g => g.ToList());

        var manufacturerIds = skus.Select(s => s.ManufacturerId).Distinct().ToList();
        var manufacturers = manufacturerIds.Count > 0
            ? (await _manufacturerRepository.GetQueryableAsync())
                .Where(m => manufacturerIds.Contains(m.Id))
                .ToDictionary(m => m.Id, m => m.Name)
            : new Dictionary<Guid, string>();

        var unitIds = products
            .SelectMany(p => new Guid?[] { p.PackagingUnitId, p.VolumeUnitId, p.BaseUnitId })
            .Concat(skus.SelectMany(s => new Guid?[] { s.PackagingUnitId, s.VolumeUnitId }))
            .Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var unitNames = unitIds.Count > 0
            ? (await _unitRepository.GetQueryableAsync())
                .Where(u => unitIds.Contains(u.Id))
                .ToDictionary(u => u.Id, u => u.Name)
            : new Dictionary<Guid, string>();

        var pwiQuery = await _productWarehouseInventoryRepository.GetQueryableAsync();
        var lastInventoryMap = pwiQuery
            .Where(pwi => productIds.Contains(pwi.ProductId) && warehouseIds.Contains(pwi.WarehouseId))
            .Select(pwi => new { pwi.ProductId, pwi.WarehouseId, pwi.LastInventoryDate })
            .ToList()
            .ToDictionary(x => (x.ProductId, x.WarehouseId), x => x.LastInventoryDate);

        var icRepo = LazyServiceProvider.LazyGetRequiredService<IRepository<IndicatorConsumption, Guid>>();
        var icQuery = await icRepo.GetQueryableAsync();
        var consumptionRaw = icQuery
            .Where(ic => productIds.Contains(ic.ProductId!.Value)
                         && ic.WarehouseId.HasValue && warehouseIds.Contains(ic.WarehouseId!.Value)
                         && (ic.IsDeducted || ic.PreparedSolutionBatchId != null)
                         && ic.SubmitDate.HasValue)
            .Select(ic => new { ic.ProductId, ic.LotNumber, ic.ActualQuantity, ic.UnitId, ic.SubmitDate, ic.PreparedSolutionBatchId, ic.WarehouseId })
            .ToList()
            .Where(ic => ic.WarehouseId.HasValue
                         && (!lastInventoryMap.TryGetValue((ic.ProductId!.Value, ic.WarehouseId.Value), out var lid)
                             || ic.SubmitDate!.Value > lid))
            .ToList();

        var conversionRepo = LazyServiceProvider.LazyGetRequiredService<IRepository<ProductUnitConversion, Guid>>();
        var conversionMap = (await conversionRepo.GetQueryableAsync())
            .Where(c => productIds.Contains(c.ProductId))
            .ToList()
            .ToDictionary(c => (c.ProductId, c.ConvertedUnitId));

        var consumptionByWarehouseLot = consumptionRaw
            .GroupBy(ic => (WarehouseId: ic.WarehouseId!.Value, ic.ProductId!.Value, LotNumber: ic.LotNumber ?? ""))
            .ToDictionary(
                g => g.Key,
                g => g.Sum(ic =>
                {
                    // PSB consumption dùng ActualQuantity (đã ở display qty, không cần convert)
                    if (ic.PreparedSolutionBatchId != null) return ic.ActualQuantity;
                    if (!productMap.TryGetValue(g.Key.Item2, out var prod)) return ic.ActualQuantity;
                    var prodSkus = skusByProduct.GetValueOrDefault(g.Key.Item2) ?? new List<ProductSku>();
                    int defaultVpu = prodSkus.FirstOrDefault(s => s.VolumePerUnit > 0)?.VolumePerUnit ?? prod.VolumePerUnit ?? 0;
                    return ConvertConsumptionToVolume(ic.ActualQuantity, ic.UnitId, prod, prodSkus, conversionMap, defaultVpu);
                }));

        var lotsByWarehouse = lotData.GroupBy(l => l.EffectiveWarehouseId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new Dictionary<Guid, List<WarehouseProductLotDto>>();
        foreach (var whId in warehouseIds)
        {
            var whLots = lotsByWarehouse.GetValueOrDefault(whId) ?? new();
            var dtos = new List<WarehouseProductLotDto>();

            foreach (var group in whLots.GroupBy(l => (l.ProductId, LotNumber: l.LotNumber ?? "")))
            {
                var productId = group.Key.ProductId;
                if (!productMap.TryGetValue(productId, out var product)) continue;

                var remaining = group.Sum(l => Math.Max(0, l.ReceivedBaseQuantity - l.ConsumedBaseQuantity));

                string? manufacturerName = null;
                int? volumePerUnit = null;
                Guid? volumeUnitId = null;
                Guid? packagingUnitId = null;

                var firstSkuId = group.FirstOrDefault(l => l.SkuId.HasValue)?.SkuId;
                if (firstSkuId.HasValue && skuById.TryGetValue(firstSkuId.Value, out var sku))
                {
                    manufacturers.TryGetValue(sku.ManufacturerId, out manufacturerName);
                    if (sku.VolumePerUnit > 0) volumePerUnit = sku.VolumePerUnit;
                    volumeUnitId = sku.VolumeUnitId;
                    packagingUnitId = sku.PackagingUnitId;
                }
                volumePerUnit ??= product.VolumePerUnit > 0 ? product.VolumePerUnit : null;
                volumeUnitId ??= product.VolumeUnitId;
                packagingUnitId ??= product.PackagingUnitId;

                var consumableQty = volumePerUnit is > 0
                    ? remaining * volumePerUnit.Value
                    : remaining;

                dtos.Add(new WarehouseProductLotDto
                {
                    ProductId = productId,
                    ProductName = product.Name,
                    ProductType = product.ProductType,
                    LotNumber = string.IsNullOrEmpty(group.Key.LotNumber) ? null : group.Key.LotNumber,
                    ManufacturerName = manufacturerName,
                    RemainingBaseQuantity = remaining,
                    VolumePerUnit = volumePerUnit,
                    VolumeUnitId = volumeUnitId,
                    BaseUnitId = product.BaseUnitId,
                    ConsumableQuantity = consumableQty,
                    TheoreticalConsumption = consumptionByWarehouseLot.GetValueOrDefault((whId, productId, group.Key.LotNumber)),
                    VolumeUnitName = volumeUnitId.HasValue && unitNames.TryGetValue(volumeUnitId.Value, out var vun) ? vun : null,
                    PackagingUnitName = packagingUnitId.HasValue && unitNames.TryGetValue(packagingUnitId.Value, out var pun) ? pun : null,
                    BaseUnitName = product.BaseUnitId.HasValue && unitNames.TryGetValue(product.BaseUnitId.Value, out var bun) ? bun : null,
                });
            }

            result[whId] = dtos.OrderBy(x => x.ProductName).ThenBy(x => x.LotNumber).ToList();
        }

        return result;
    }

    private static decimal ConvertConsumptionToVolume(
        decimal amount,
        Guid? consumeUnitId,
        Product product,
        List<ProductSku> skus,
        Dictionary<(Guid ProductId, Guid ConvertedUnitId), ProductUnitConversion> conversionMap,
        int defaultVpu)
    {
        if (!consumeUnitId.HasValue || defaultVpu <= 0)
            return amount;

        decimal? unitsPerStorage = null;

        if (consumeUnitId == product.PackagingUnitId)
            unitsPerStorage = 1;
        else if (consumeUnitId == product.VolumeUnitId && product.VolumePerUnit > 0)
            unitsPerStorage = product.VolumePerUnit.Value;
        else if (consumeUnitId == product.BaseUnitId && product.PackagingQuantity > 0)
            unitsPerStorage = 1m / product.PackagingQuantity.Value;

        if (unitsPerStorage == null)
        {
            foreach (var sku in skus)
            {
                if (consumeUnitId == sku.PackagingUnitId) { unitsPerStorage = 1; break; }
                if (consumeUnitId == sku.VolumeUnitId && sku.VolumePerUnit > 0) { unitsPerStorage = sku.VolumePerUnit.Value; break; }
                if (consumeUnitId == sku.BaseUnitId && sku.PackagingQuantity > 0) { unitsPerStorage = 1m / sku.PackagingQuantity.Value; break; }
            }
        }

        if (unitsPerStorage == null
            && conversionMap.TryGetValue((product.Id, consumeUnitId.Value), out var conv)
            && conv.ConvertedQuantity > 0)
        {
            unitsPerStorage = conv.ConvertedQuantity / conv.Quantity;
        }

        if (unitsPerStorage == null || unitsPerStorage.Value == 0)
            return amount;

        return (amount / unitsPerStorage.Value) * defaultVpu;
    }

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
        var importItemQ = await _importReceiptItemRepository.GetQueryableAsync();
        var importReceiptQ = await _importReceiptRepository.GetQueryableAsync();

        var itemWarehouseIds = inputItems
            .Where(x => x.WarehouseId.HasValue)
            .Select(x => x.WarehouseId!.Value)
            .Distinct()
            .ToList();
        var lotsByWarehouse = itemWarehouseIds.Count > 0
            ? await BuildLotsPerWarehouseAsync(itemWarehouseIds)
            : new Dictionary<Guid, List<WarehouseProductLotDto>>();

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

            WarehouseProductLotDto? lotDto = null;
            if (itemInput.WarehouseId.HasValue)
            {
                var inputLotKey = itemInput.LotNumber ?? "";
                lotDto = (lotsByWarehouse.GetValueOrDefault(itemInput.WarehouseId.Value) ?? new List<WarehouseProductLotDto>())
                    .FirstOrDefault(l => l.ProductId == product.Id && (l.LotNumber ?? "") == inputLotKey);
            }

            if (lotDto != null)
            {
                // Sổ sách + quy đổi theo dung tích SKU của chính lô (K30/K101)
                var lotActualBase = ConvertActualToBase(product, itemInput.ActualQuantity, itemInput.ActualUnitId,
                    lotDto.VolumePerUnit, lotDto.VolumeUnitId);

                await _itemRepository.InsertAsync(new StockAdjustmentItem(
                    GuidGenerator.Create(),
                    adjustmentId,
                    product.Id,
                    product.Name,
                    product.BaseUnitId ?? Guid.Empty,
                    unitName,
                    lotDto.RemainingBaseQuantity,
                    lotActualBase)
                {
                    WarehouseId = itemInput.WarehouseId,
                    WarehouseName = warehouseName,
                    ActualUnitId = itemInput.ActualUnitId,
                    ActualUnitName = actualUnitName,
                    ActualQuantity = itemInput.ActualQuantity,
                    Reason = itemInput.Reason,
                    ExpectedStock = lotDto.ConsumableQuantity - lotDto.TheoreticalConsumption,
                    ConsumableQuantityBefore = lotDto.ConsumableQuantity,
                    LotNumber = itemInput.LotNumber,
                    ManufacturerName = itemInput.ManufacturerName ?? lotDto.ManufacturerName,
                }, autoSave: true);
                continue;
            }

            var (quantityBefore, consumableQuantityBefore) = await ComputeWarehouseStockAsync(
                importItemQ, importReceiptQ, product, itemInput.WarehouseId);

            if (!string.IsNullOrEmpty(itemInput.LotNumber))
            {
                var lotQuery = from ii in importItemQ
                               join ir in importReceiptQ on ii.WarehouseImportReceiptId equals ir.Id
                               where ii.ProductId == product.Id
                                     && ir.Status == WarehouseImportReceiptStatus.Completed
                                     && ii.Status == WarehouseImportLineStatus.Checked
                                     && ii.LotNumber == itemInput.LotNumber
                               select new { ii, ir };

                if (itemInput.WarehouseId.HasValue)
                {
                    var whId = itemInput.WarehouseId.Value;
                    lotQuery = lotQuery.Where(x =>
                        (x.ii.StorageWarehouseId.HasValue && x.ii.StorageWarehouseId == whId)
                        || (!x.ii.StorageWarehouseId.HasValue && x.ir.WarehouseId.HasValue && x.ir.WarehouseId == whId));
                }

                var lotRows = lotQuery
                    .Select(x => new { x.ii.ReceivedBaseQuantity, x.ii.ConsumedBaseQuantity, x.ii.SkuId })
                    .ToList();

                if (lotRows.Count > 0)
                {
                    var lotBase = lotRows.Sum(l => Math.Max(0, l.ReceivedBaseQuantity - l.ConsumedBaseQuantity));

                    int? vpu = null;
                    var firstSkuId = lotRows.FirstOrDefault(l => l.SkuId.HasValue)?.SkuId;
                    if (firstSkuId.HasValue)
                    {
                        var sku = (await _skuRepository.GetQueryableAsync())
                            .FirstOrDefault(s => s.Id == firstSkuId.Value && s.VolumePerUnit.HasValue && s.VolumePerUnit > 0);
                        if (sku != null) vpu = sku.VolumePerUnit;
                    }
                    vpu ??= (product.VolumePerUnit > 0 ? product.VolumePerUnit : null);

                    quantityBefore = lotBase;
                    consumableQuantityBefore = vpu.HasValue && vpu.Value > 0
                        ? lotBase * vpu.Value
                        : lotBase;
                }
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
                quantityBefore,
                actualInBaseUnit)
            {
                WarehouseId = itemInput.WarehouseId,
                WarehouseName = warehouseName,
                ActualUnitId = itemInput.ActualUnitId,
                ActualUnitName = actualUnitName,
                ActualQuantity = itemInput.ActualQuantity,
                Reason = itemInput.Reason,
                ExpectedStock = itemInput.ExpectedStock,
                ConsumableQuantityBefore = consumableQuantityBefore,
                LotNumber = itemInput.LotNumber,
                ManufacturerName = itemInput.ManufacturerName,
            };

            await _itemRepository.InsertAsync(item, autoSave: true);
        }
    }

    private async Task<(decimal baseQty, decimal volumeQty)> ComputeWarehouseStockAsync(
        IQueryable<WarehouseImportReceiptItem> importItemQ,
        IQueryable<WarehouseImportReceipt> importReceiptQ,
        Product product,
        Guid? warehouseId)
    {
        var baseLotQuery = from ii in importItemQ
                           join ir in importReceiptQ on ii.WarehouseImportReceiptId equals ir.Id
                           where ii.ProductId == product.Id
                                 && ir.Status == WarehouseImportReceiptStatus.Completed
                                 && ii.Status == WarehouseImportLineStatus.Checked
                           select new { ii, ir };

        if (warehouseId.HasValue)
        {
            baseLotQuery = baseLotQuery.Where(x =>
                (x.ii.StorageWarehouseId.HasValue && x.ii.StorageWarehouseId == warehouseId.Value)
                || (!x.ii.StorageWarehouseId.HasValue && x.ir.WarehouseId.HasValue && x.ir.WarehouseId == warehouseId.Value));
        }

        var lotData = baseLotQuery
            .Select(x => new { x.ii.ReceivedBaseQuantity, x.ii.ConsumedBaseQuantity, x.ii.SkuId })
            .ToList();

        decimal baseQty = lotData.Sum(l => Math.Max(0, l.ReceivedBaseQuantity - l.ConsumedBaseQuantity));

        var skuIds = lotData.Where(l => l.SkuId.HasValue).Select(l => l.SkuId!.Value).Distinct().ToList();
        var skuVpuMap = skuIds.Count > 0
            ? (await _skuRepository.GetQueryableAsync())
                .Where(s => skuIds.Contains(s.Id) && s.VolumePerUnit.HasValue && s.VolumePerUnit > 0)
                .ToDictionary(s => s.Id, s => s.VolumePerUnit)
            : new Dictionary<Guid, int?>();

        decimal volumeQty = 0;
        foreach (var lot in lotData)
        {
            var remaining = Math.Max(0, lot.ReceivedBaseQuantity - lot.ConsumedBaseQuantity);
            int? vpu = null;
            if (lot.SkuId.HasValue && skuVpuMap.TryGetValue(lot.SkuId.Value, out var skuVpu) && skuVpu > 0)
                vpu = skuVpu;
            else if (product.VolumePerUnit > 0)
                vpu = product.VolumePerUnit;

            if (vpu.HasValue && vpu.Value > 0)
                volumeQty += remaining * vpu.Value;
        }

        return (baseQty, volumeQty);
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
            .ToList();

        var itemWarehouseIds = items
            .Where(i => i.WarehouseId.HasValue)
            .Select(i => i.WarehouseId!.Value)
            .Distinct()
            .ToList();
        var lotsByWarehouse = itemWarehouseIds.Count > 0
            ? await BuildLotsPerWarehouseAsync(itemWarehouseIds)
            : new Dictionary<Guid, List<WarehouseProductLotDto>>();
        WarehouseProductLotDto? FindLot(StockAdjustmentItem i) => i.WarehouseId.HasValue
            ? (lotsByWarehouse.GetValueOrDefault(i.WarehouseId.Value) ?? new List<WarehouseProductLotDto>())
                .FirstOrDefault(l => l.ProductId == i.ProductId && (l.LotNumber ?? "") == (i.LotNumber ?? ""))
            : null;
        var isDraft = adjustment.Status == StockAdjustmentStatus.Draft;
        var stockChanged = false;

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
                itemDto.ConsumableQuantityBefore = i.ConsumableQuantityBefore;

                var lot = FindLot(i);
                if (productLookup.TryGetValue(i.ProductId, out var prod))
                {
                    var volumePerUnit = prod.VolumePerUnit > 0 ? prod.VolumePerUnit : null;
                    var packagingUnitId = prod.PackagingUnitId;
                    var volumeUnitId = prod.VolumeUnitId;

                    if (skuLookup.TryGetValue(i.ProductId, out var sku))
                    {
                        volumePerUnit ??= sku.VolumePerUnit;
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

                    // Đơn vị theo SKU của chính lô (KK-08 thiếu "Chai" khi cấp vật tư không khai dung tích)
                    if (lot != null)
                    {
                        if (lot.VolumePerUnit is > 0) itemDto.VolumePerUnit = lot.VolumePerUnit;
                        itemDto.PackagingUnitName = lot.PackagingUnitName ?? itemDto.PackagingUnitName;
                        itemDto.VolumeUnitName = lot.VolumeUnitName ?? itemDto.VolumeUnitName;
                        itemDto.BaseUnitName = lot.BaseUnitName ?? itemDto.BaseUnitName;
                    }

                    // Phiếu nháp: hiển thị số liệu sổ sách hiện tại (BA Q18), báo nếu đã khác lúc lưu nháp
                    if (isDraft && lot != null)
                    {
                        var liveExpected = lot.ConsumableQuantity - lot.TheoreticalConsumption;
                        if (Math.Round(lot.RemainingBaseQuantity, 6) != Math.Round(i.QuantityBefore, 6)
                            || Math.Round(liveExpected, 6) != Math.Round(i.ExpectedStock ?? liveExpected, 6))
                            stockChanged = true;

                        itemDto.QuantityBefore = lot.RemainingBaseQuantity;
                        itemDto.ConsumableQuantityBefore = lot.ConsumableQuantity;
                        itemDto.ExpectedStock = liveExpected;
                        itemDto.AdjustmentQuantity = lot.RemainingBaseQuantity - ConvertActualToBase(
                            prod, i.ActualQuantity, i.ActualUnitId, lot.VolumePerUnit, lot.VolumeUnitId);
                    }
                }
                itemDto.LotNumber = i.LotNumber;
                itemDto.ManufacturerName = i.ManufacturerName;
                return itemDto;
            }).ToList(),
        };
        dto.StockChangedSinceSaved = stockChanged;

        await _userNameProvider.EnrichWithUserNamesAsync(
            new List<StockAdjustmentDto> { dto },
            _ => adjustment.CreatorId,
            (d, name) => d.CreatorName = name);

        return dto;
    }

    #endregion
}
