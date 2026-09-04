using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LabSpecimenManagement.MasterData;
using LabSpecimenManagement.Permissions;
using LabSpecimenManagement.Common;
using LabSpecimenManagement.Products;
using LabSpecimenManagement.StockAdjustments;
using LabSpecimenManagement.Testing;
using LabSpecimenManagement.WarehouseImports;
using Microsoft.AspNetCore.Authorization;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.PreparedSolutions;

[Authorize(LabSpecimenManagementPermissions.PreparedSolutions.Default)]
public class PreparedSolutionBatchAppService : ApplicationService, IPreparedSolutionBatchAppService
{
    private readonly IRepository<PreparedSolutionBatch, Guid> _repository;
    private readonly IRepository<PreparedSolutionIngredient, Guid> _ingredientRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IRepository<Unit, Guid> _unitRepository;
    private readonly IRepository<StockMovementLog, Guid> _stockMovementLogRepository;
    private readonly IRepository<Warehouse, Guid> _warehouseRepository;
    private readonly IRepository<WarehouseImportReceipt, Guid> _importReceiptRepository;
    private readonly IRepository<WarehouseImportReceiptItem, Guid> _importReceiptItemRepository;
    private readonly IRepository<Manufacturer, Guid> _manufacturerRepository;
    private readonly IRepository<InventoryBarcode, Guid> _inventoryBarcodeRepository;
    private readonly IRepository<IndicatorConsumption, Guid> _indicatorConsumptionRepository;
    private readonly IRepository<ProductWarehouseInventory, Guid> _productWarehouseInventoryRepository;
    private readonly IFifoStockDeductionService _fifoStockService;
    private readonly InventoryBarcodeStatusService _barcodeStatusService;

    public PreparedSolutionBatchAppService(
        IRepository<PreparedSolutionBatch, Guid> repository,
        IRepository<PreparedSolutionIngredient, Guid> ingredientRepository,
        IRepository<Product, Guid> productRepository,
        IRepository<Unit, Guid> unitRepository,
        IRepository<StockMovementLog, Guid> stockMovementLogRepository,
        IRepository<Warehouse, Guid> warehouseRepository,
        IRepository<WarehouseImportReceipt, Guid> importReceiptRepository,
        IRepository<WarehouseImportReceiptItem, Guid> importReceiptItemRepository,
        IRepository<Manufacturer, Guid> manufacturerRepository,
        IRepository<InventoryBarcode, Guid> inventoryBarcodeRepository,
        IRepository<IndicatorConsumption, Guid> indicatorConsumptionRepository,
        IRepository<ProductWarehouseInventory, Guid> productWarehouseInventoryRepository,
        IFifoStockDeductionService fifoStockService,
        InventoryBarcodeStatusService barcodeStatusService)
    {
        _repository = repository;
        _ingredientRepository = ingredientRepository;
        _productRepository = productRepository;
        _unitRepository = unitRepository;
        _stockMovementLogRepository = stockMovementLogRepository;
        _warehouseRepository = warehouseRepository;
        _importReceiptRepository = importReceiptRepository;
        _importReceiptItemRepository = importReceiptItemRepository;
        _manufacturerRepository = manufacturerRepository;
        _inventoryBarcodeRepository = inventoryBarcodeRepository;
        _indicatorConsumptionRepository = indicatorConsumptionRepository;
        _productWarehouseInventoryRepository = productWarehouseInventoryRepository;
        _fifoStockService = fifoStockService;
        _barcodeStatusService = barcodeStatusService;
    }

    public async Task<PagedResultDto<PreparedSolutionBatchDto>> GetListAsync(GetPreparedSolutionBatchesInput input)
    {
        var now = DateTime.UtcNow;

        var query = await _repository.GetQueryableAsync();
        var ingredientQuery = await _ingredientRepository.GetQueryableAsync();

        if (!string.IsNullOrWhiteSpace(input.Search))
        {
            var s = input.Search.Trim().ToLower();
            query = query.Where(x =>
                x.SolutionProductName.ToLower().Contains(s) ||
                x.LotCode.ToLower().Contains(s) ||
                (x.IdentificationCode != null && x.IdentificationCode.ToLower().Contains(s)));
        }

        var batches = query.OrderByDescending(x => x.CreationTime).ToList();

        // Compute status in memory (status depends on DateTime.UtcNow — EF translation varies)
        var dtoList = batches.Select(b => MapToDto(b, now)).ToList();

        // Status filter
        if (input.IsHetOrHetHan == true)
        {
            dtoList = dtoList.Where(d =>
                d.ComputedStatus == PreparedSolutionStatus.Expired ||
                d.ComputedStatus == PreparedSolutionStatus.Empty).ToList();
        }
        else if (input.Status.HasValue)
        {
            dtoList = dtoList.Where(d => d.ComputedStatus == input.Status.Value).ToList();
        }

        var totalCount = dtoList.Count;
        var paged = dtoList.Skip(input.SkipCount).Take(input.MaxResultCount).ToList();

        // Load ingredients for paged items
        var batchIds = paged.Select(d => d.Id).ToList();
        var ingredients = ingredientQuery.Where(i => batchIds.Contains(i.PreparedSolutionBatchId)).ToList();

        foreach (var dto in paged)
        {
            dto.Ingredients = ingredients
                .Where(i => i.PreparedSolutionBatchId == dto.Id)
                .Select(MapIngredientToDto)
                .ToList();
        }

        return new PagedResultDto<PreparedSolutionBatchDto>(totalCount, paged);
    }

    public async Task<PreparedSolutionBatchDto> GetAsync(Guid id)
    {
        var batch = await _repository.GetAsync(id);
        var ingredients = await _ingredientRepository.GetListAsync(i => i.PreparedSolutionBatchId == id);
        var dto = MapToDto(batch, DateTime.UtcNow);
        dto.Ingredients = ingredients.Select(MapIngredientToDto).ToList();
        if (batch.PreparedSolutionReceiptId.HasValue)
        {
            var receipt = await _importReceiptRepository.FindAsync(batch.PreparedSolutionReceiptId.Value);
            dto.PreparedSolutionReceiptId = receipt?.Id;
            dto.PreparedSolutionReceiptCode = receipt?.Code;

            var receiptItems = await _importReceiptItemRepository.GetListAsync(
                i => i.WarehouseImportReceiptId == batch.PreparedSolutionReceiptId.Value);
            dto.BarcodeValues = receiptItems
                .Where(i => !string.IsNullOrWhiteSpace(i.BarcodeValues))
                .SelectMany(i => JsonSerializer.Deserialize<List<string>>(i.BarcodeValues!) ?? new List<string>())
                .ToList();
        }
        return dto;
    }

    [Authorize(LabSpecimenManagementPermissions.PreparedSolutions.Create)]
    public async Task<PreparedSolutionBatchDto> CreateAsync(CreateUpdatePreparedSolutionBatchDto input)
    {
        if (!input.Ingredients.Any())
            throw new UserFriendlyException("Phải chọn ít nhất 1 nguyên liệu tiêu hao.");

        if (!input.StorageWarehouseId.HasValue)
            throw new UserFriendlyException("Vui lòng chọn vị trí lưu kho.");

        var productQuery = await _productRepository.WithDetailsAsync(p => p.Skus);
        var solutionProduct = productQuery.First(p => p.Id == input.SolutionProductId);

        var tuPhaSku = await FindTuPhaSkuAsync(solutionProduct);
        if (tuPhaSku == null || !tuPhaSku.VolumePerUnit.HasValue || tuPhaSku.VolumePerUnit.Value <= 0)
            throw new UserFriendlyException("Sản phẩm dung dịch pha chưa có SKU 'Tự pha' với dung tích hợp lệ.");

        string? storageWarehouseName = null;
        if (input.StorageWarehouseId.HasValue)
        {
            var w = await _warehouseRepository.FindAsync(input.StorageWarehouseId.Value);
            storageWarehouseName = w?.Name;
        }
        string? sourceWarehouseName = null;
        if (input.SourceWarehouseId.HasValue)
        {
            var w = await _warehouseRepository.FindAsync(input.SourceWarehouseId.Value);
            sourceWarehouseName = w?.Name;
        }

        var preparedByName = BuildDisplayName(CurrentUser.SurName, CurrentUser.Name) ?? CurrentUser.UserName;

        var volumeUnitName = solutionProduct.VolumeUnitId.HasValue
            ? (await _unitRepository.FindAsync(solutionProduct.VolumeUnitId.Value))?.Name
            : null;

        var batch = new PreparedSolutionBatch
        {
            SolutionProductId = input.SolutionProductId,
            SolutionProductName = solutionProduct.Name,
            IdentificationCode = solutionProduct.IdentificationCode,
            Concentration = solutionProduct.Concentration,
            LotCode = input.LotCode,
            PreparationDate = input.PreparationDate,
            ExpiryDate = input.ExpiryDate,
            TotalVolume = input.TotalVolume,
            RemainingVolume = input.TotalVolume,
            VolumeUnitId = solutionProduct.VolumeUnitId,
            VolumeUnitName = volumeUnitName,
            StorageWarehouseId = input.StorageWarehouseId,
            StorageWarehouseName = storageWarehouseName,
            SourceWarehouseId = input.SourceWarehouseId,
            SourceWarehouseName = sourceWarehouseName,
            StorageConditions = input.StorageConditions,
            PreparationMethod = input.PreparationMethod,
            IsProcessed = false,
            ProcessingImages = "[]",
            PreparedByUserId = CurrentUser.Id,
            PreparedByName = preparedByName,
        };

        await _repository.InsertAsync(batch, autoSave: true);

        // Convert TotalVolume (mL) → storage units via "Tự pha" SKU
        var storageQty = input.TotalVolume / tuPhaSku.VolumePerUnit.Value;

        // Add prepared solution to stock (in storage units)
        var solQtyBefore = solutionProduct.AvailableQuantity;
        solutionProduct.AvailableQuantity += storageQty;
        await _productRepository.UpdateAsync(solutionProduct, autoSave: true);
        await _stockMovementLogRepository.InsertAsync(new StockMovementLog(
            GuidGenerator.Create(),
            solutionProduct.Id,
            solutionProduct.Name,
            solQtyBefore,
            solutionProduct.AvailableQuantity,
            StockMovementType.PreparedSolutionProduction,
            batch.Id,
            batch.LotCode,
            "Pha dung dịch — nhập kho"), autoSave: true);

        // Create synthetic receipt so the solution appears in warehouse inventory
        await CreateSyntheticReceiptAsync(batch, solutionProduct, storageQty, tuPhaSku, storageWarehouseName);

        // Process ingredients and deduct stock
        await ProcessIngredientsAsync(batch, input.Ingredients, input.SourceWarehouseId);

        return await GetAsync(batch.Id);
    }

    [Authorize(LabSpecimenManagementPermissions.PreparedSolutions.Edit)]
    public async Task<PreparedSolutionBatchDto> UpdateAsync(Guid id, CreateUpdatePreparedSolutionBatchDto input)
    {
        if (!input.Ingredients.Any())
            throw new UserFriendlyException("Phải chọn ít nhất 1 nguyên liệu tiêu hao.");

        if (!input.StorageWarehouseId.HasValue)
            throw new UserFriendlyException("Vui lòng chọn vị trí lưu kho.");

        var batch = await _repository.GetAsync(id);
        if (batch.IsProcessed)
            throw new UserFriendlyException("Không thể chỉnh sửa phiếu pha đã xử lý.");

        if ((DateTime.UtcNow - batch.PreparationDate).TotalDays > 1)
            throw new UserFriendlyException("Phiếu pha đã qua 1 ngày, không thể chỉnh sửa.");

        // Delete old synthetic receipt
        await DeleteSyntheticReceiptAsync(batch);

        // Reverse old stock deductions
        var oldIngredients = await _ingredientRepository.GetListAsync(i => i.PreparedSolutionBatchId == id);
        await ReverseStockDeductionsAsync(id, batch.LotCode, oldIngredients);
        await _ingredientRepository.DeleteManyAsync(oldIngredients);

        // Delete old usage log entries for this batch
        await _indicatorConsumptionRepository.DeleteAsync(
            c => c.PreparedSolutionBatchId == id, autoSave: true);

        // Reverse old solution product stock (convert old TotalVolume to storage units)
        var oldProductQuery = await _productRepository.WithDetailsAsync(p => p.Skus);
        var oldSolutionProduct = oldProductQuery.First(p => p.Id == batch.SolutionProductId);
        var oldTuPhaSku = await FindTuPhaSkuAsync(oldSolutionProduct);
        var oldStorageQty = oldTuPhaSku != null && oldTuPhaSku.VolumePerUnit > 0
            ? batch.TotalVolume / oldTuPhaSku.VolumePerUnit.Value
            : batch.TotalVolume;

        var oldSolQtyBefore = oldSolutionProduct.AvailableQuantity;
        oldSolutionProduct.AvailableQuantity -= oldStorageQty;
        await _productRepository.UpdateAsync(oldSolutionProduct, autoSave: true);
        await _stockMovementLogRepository.InsertAsync(new StockMovementLog(
            GuidGenerator.Create(),
            oldSolutionProduct.Id,
            oldSolutionProduct.Name,
            oldSolQtyBefore,
            oldSolutionProduct.AvailableQuantity,
            StockMovementType.PreparedSolutionProduction,
            id,
            batch.LotCode,
            "Hoàn lại dung dịch pha — chỉnh sửa phiếu pha"), autoSave: true);

        // Update batch metadata
        var newProductQuery = await _productRepository.WithDetailsAsync(p => p.Skus);
        var solutionProduct = newProductQuery.First(p => p.Id == input.SolutionProductId);

        var tuPhaSku = await FindTuPhaSkuAsync(solutionProduct);
        if (tuPhaSku == null || !tuPhaSku.VolumePerUnit.HasValue || tuPhaSku.VolumePerUnit.Value <= 0)
            throw new UserFriendlyException("Sản phẩm dung dịch pha chưa có SKU 'Tự pha' với dung tích hợp lệ.");

        string? storageWarehouseName = null;
        if (input.StorageWarehouseId.HasValue)
        {
            var w = await _warehouseRepository.FindAsync(input.StorageWarehouseId.Value);
            storageWarehouseName = w?.Name;
        }
        string? sourceWarehouseName = null;
        if (input.SourceWarehouseId.HasValue)
        {
            var w = await _warehouseRepository.FindAsync(input.SourceWarehouseId.Value);
            sourceWarehouseName = w?.Name;
        }

        batch.SolutionProductId = input.SolutionProductId;
        batch.SolutionProductName = solutionProduct.Name;
        batch.IdentificationCode = solutionProduct.IdentificationCode;
        batch.Concentration = solutionProduct.Concentration;
        batch.LotCode = input.LotCode;
        batch.PreparationDate = input.PreparationDate;
        batch.ExpiryDate = input.ExpiryDate;
        var usedVolume = batch.TotalVolume - batch.RemainingVolume;
        batch.TotalVolume = input.TotalVolume;
        batch.RemainingVolume = input.TotalVolume - usedVolume;
        batch.VolumeUnitId = solutionProduct.VolumeUnitId;
        batch.VolumeUnitName = solutionProduct.VolumeUnitId.HasValue
            ? (await _unitRepository.FindAsync(solutionProduct.VolumeUnitId.Value))?.Name
            : null;
        batch.StorageWarehouseId = input.StorageWarehouseId;
        batch.StorageWarehouseName = storageWarehouseName;
        batch.SourceWarehouseId = input.SourceWarehouseId;
        batch.SourceWarehouseName = sourceWarehouseName;
        batch.StorageConditions = input.StorageConditions;
        batch.PreparationMethod = input.PreparationMethod;

        await _repository.UpdateAsync(batch, autoSave: true);

        // Convert new TotalVolume (mL) → storage units
        var newStorageQty = input.TotalVolume / tuPhaSku.VolumePerUnit.Value;

        // Add new solution product to stock (in storage units)
        var newSolQtyBefore = solutionProduct.AvailableQuantity;
        solutionProduct.AvailableQuantity += newStorageQty;
        await _productRepository.UpdateAsync(solutionProduct, autoSave: true);
        await _stockMovementLogRepository.InsertAsync(new StockMovementLog(
            GuidGenerator.Create(),
            solutionProduct.Id,
            solutionProduct.Name,
            newSolQtyBefore,
            solutionProduct.AvailableQuantity,
            StockMovementType.PreparedSolutionProduction,
            batch.Id,
            batch.LotCode,
            "Pha dung dịch — nhập kho (cập nhật)"), autoSave: true);

        // Create new synthetic receipt
        await CreateSyntheticReceiptAsync(batch, solutionProduct, newStorageQty, tuPhaSku, storageWarehouseName);

        // Apply new stock deductions
        await ProcessIngredientsAsync(batch, input.Ingredients, input.SourceWarehouseId);

        return await GetAsync(batch.Id);
    }

    [Authorize(LabSpecimenManagementPermissions.PreparedSolutions.Process)]
    public async Task<PreparedSolutionBatchDto> ProcessAsync(Guid id, ProcessPreparedSolutionDto input)
    {
        var batch = await _repository.GetAsync(id);
        if (batch.IsProcessed)
            throw new UserFriendlyException("Phiếu pha này đã được xử lý trước đó.");

        batch.IsProcessed = true;
        batch.ProcessedAt = DateTime.UtcNow;
        batch.ProcessingNote = input.Note;
        batch.ProcessingImages = JsonSerializer.Serialize(input.ImageUrls ?? new List<string>());

        await _repository.UpdateAsync(batch, autoSave: true);
        return await GetAsync(batch.Id);
    }

    public async Task<List<PreparedSolutionBatchDto>> GetWarningItemsAsync()
    {
        var now = DateTime.UtcNow;
        var batches = await _repository.GetListAsync(b => !b.IsProcessed);
        var dtoList = batches.Select(b => MapToDto(b, now))
            .Where(d => d.ComputedStatus != PreparedSolutionStatus.Active &&
                        d.ComputedStatus != PreparedSolutionStatus.Processed)
            .ToList();

        // Sort per BA: Hết hạn → Dùng hết → Sắp hết hạn → Sắp hết
        var priority = new Dictionary<PreparedSolutionStatus, int>
        {
            [PreparedSolutionStatus.Expired] = 0,
            [PreparedSolutionStatus.Empty] = 1,
            [PreparedSolutionStatus.NearingExpiry] = 2,
            [PreparedSolutionStatus.NearlyEmpty] = 3,
        };

        return dtoList
            .OrderBy(d => priority.GetValueOrDefault(d.ComputedStatus, 99))
            .Take(5)
            .ToList();
    }

    public async Task<List<PreparedSolutionWarehouseProductDto>> GetWarehouseProductsAsync(Guid warehouseId, int? productType)
    {
        // BFS to collect all warehouse IDs in the subtree of warehouseId
        var allWarehouses = (await _warehouseRepository.GetQueryableAsync())
            .Select(w => new { w.Id, w.ParentWarehouseId })
            .ToList();

        var warehouseIds = new HashSet<Guid> { warehouseId };
        var queue = new Queue<Guid>();
        queue.Enqueue(warehouseId);
        while (queue.Count > 0)
        {
            var parentId = queue.Dequeue();
            foreach (var child in allWarehouses.Where(w => w.ParentWarehouseId == parentId))
            {
                if (warehouseIds.Add(child.Id))
                    queue.Enqueue(child.Id);
            }
        }

        // Find receipt items imported into those warehouses with per-item stock data
        var importReceiptQuery = await _importReceiptRepository.GetQueryableAsync();
        var importItemQuery = await _importReceiptItemRepository.GetQueryableAsync();

        var stockItems = (from item in importItemQuery
                          join receipt in importReceiptQuery on item.WarehouseImportReceiptId equals receipt.Id
                          where receipt.Status == WarehouseImportReceiptStatus.Completed
                                && item.Status == WarehouseImportLineStatus.Checked
                                && (
                                    (item.StorageWarehouseId.HasValue && warehouseIds.Contains(item.StorageWarehouseId.Value))
                                    || (!item.StorageWarehouseId.HasValue && receipt.WarehouseId.HasValue && warehouseIds.Contains(receipt.WarehouseId.Value))
                                )
                          select new
                          {
                              item.ProductId,
                              item.SkuId,
                              NetBase = item.ReceivedBaseQuantity - item.ConsumedBaseQuantity,
                          }).ToList();

        var productIds = stockItems.Select(x => x.ProductId).Distinct().ToList();

        if (!productIds.Any())
            return new List<PreparedSolutionWarehouseProductDto>();

        // Load products with SKUs
        var productQuery = await _productRepository.WithDetailsAsync(p => p.Skus);
        var products = productQuery
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .ToList();

        if (productType.HasValue)
            products = products.Where(p => (int)p.ProductType == productType.Value).ToList();

        // Build lookups for per-item volume calculation
        var skuMap = products.SelectMany(p => p.Skus).ToDictionary(s => s.Id);
        var productLookup = products.ToDictionary(p => p.Id);

        var availableVolumeByProduct = stockItems
            .Where(x => productLookup.ContainsKey(x.ProductId))
            .GroupBy(x => x.ProductId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(item =>
                {
                    int? vpu = item.SkuId.HasValue
                        && skuMap.TryGetValue(item.SkuId.Value, out var sku)
                        && sku.VolumePerUnit > 0
                            ? sku.VolumePerUnit
                            : ResolveVolumePerUnit(productLookup[g.Key]);
                    return vpu.HasValue && vpu.Value > 0
                        ? item.NetBase * vpu.Value
                        : item.NetBase;
                })
            );

        // Batch-load unit names
        var unitIds = products
            .Select(p => p.VolumeUnitId ?? p.BaseUnitId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        var unitMap = unitIds.Any()
            ? (await _unitRepository.GetQueryableAsync())
                .Where(u => unitIds.Contains(u.Id))
                .ToDictionary(u => u.Id, u => u.Name)
            : new Dictionary<Guid, string>();

        return products.Select(p =>
        {
            return new PreparedSolutionWarehouseProductDto
            {
                ProductId = p.Id,
                ProductName = p.Name,
                ProductType = (int)p.ProductType,
                ProductTypeName = GetProductTypeName(p.ProductType),
                UnitName = p.VolumeUnitId.HasValue && unitMap.ContainsKey(p.VolumeUnitId.Value)
                    ? unitMap[p.VolumeUnitId.Value]
                    : p.BaseUnitId.HasValue && unitMap.ContainsKey(p.BaseUnitId.Value)
                        ? unitMap[p.BaseUnitId.Value]
                        : null,
                AvailableQuantity = availableVolumeByProduct.GetValueOrDefault(p.Id, 0m),
            };
        }).OrderBy(p => p.ProductName).ToList();
    }

    #region Private Methods

    private async Task CreateSyntheticReceiptAsync(
        PreparedSolutionBatch batch,
        Product solutionProduct,
        decimal storageQty,
        ProductSku tuPhaSku,
        string? storageWarehouseName)
    {
        var storageUnitName = tuPhaSku.PackagingUnitId.HasValue
            ? (await _unitRepository.FindAsync(tuPhaSku.PackagingUnitId.Value))?.Name
            : null;

        var receipt = new WarehouseImportReceipt(
            GuidGenerator.Create(),
            $"PSB-{batch.LotCode}",
            WarehouseImportReceiptType.PreparedSolutionProduction)
        {
            ActualImportDate = batch.PreparationDate,
            WarehouseId = batch.StorageWarehouseId,
            WarehouseName = storageWarehouseName,
            Status = WarehouseImportReceiptStatus.Completed,
        };
        await _importReceiptRepository.InsertAsync(receipt, autoSave: true);

        var lotItem = new WarehouseImportReceiptItem(
            GuidGenerator.Create(), receipt.Id, solutionProduct.Id, solutionProduct.Name)
        {
            LotNumber = batch.LotCode,
            LotExpiryDate = batch.ExpiryDate,
            ReceivedBaseQuantity = storageQty,
            ConsumedBaseQuantity = 0,
            ActualReceivedQuantity = batch.TotalVolume,
            StorageWarehouseId = batch.StorageWarehouseId,
            StorageWarehouseName = storageWarehouseName,
            UnitId = tuPhaSku.PackagingUnitId ?? solutionProduct.BaseUnitId,
            UnitName = storageUnitName,
            SkuId = tuPhaSku.Id,
        };
        await _importReceiptItemRepository.InsertAsync(lotItem, autoSave: true);

        if (batch.StorageWarehouseId.HasValue)
        {
            var barcodeCount = Math.Max(1, (int)Math.Ceiling(storageQty));
            var barcodeCodes = new List<string>(barcodeCount);
            for (var sequence = 1; sequence <= barcodeCount; sequence++)
            {
                var code = InventoryBarcodeCodeBuilder.Build(
                    solutionProduct.IdentificationCode,
                    batch.PreparationDate,
                    batch.LotCode,
                    sequence);
                var uniqueCode = code;
                var suffix = 1;
                while (await _inventoryBarcodeRepository.AnyAsync(x => x.Code == uniqueCode))
                    uniqueCode = $"{code}-{suffix++:D2}";

                await _inventoryBarcodeRepository.InsertAsync(new InventoryBarcode(
                    GuidGenerator.Create(),
                    uniqueCode,
                    solutionProduct.Id,
                    lotItem.Id,
                    batch.StorageWarehouseId.Value)
                {
                    SkuId = tuPhaSku.Id
                }, autoSave: true);
                barcodeCodes.Add(uniqueCode);
            }
            lotItem.BarcodeValues = JsonSerializer.Serialize(barcodeCodes);
            await _importReceiptItemRepository.UpdateAsync(lotItem, autoSave: true);
        }

        batch.PreparedSolutionReceiptId = receipt.Id;
        await _repository.UpdateAsync(batch, autoSave: true);
    }

    private async Task DeleteSyntheticReceiptAsync(PreparedSolutionBatch batch)
    {
        if (!batch.PreparedSolutionReceiptId.HasValue) return;

        var oldItems = await _importReceiptItemRepository.GetListAsync(
            i => i.WarehouseImportReceiptId == batch.PreparedSolutionReceiptId.Value);
        if (oldItems.Any())
        {
            var oldItemIds = oldItems.Select(i => i.Id).ToList();
            await _inventoryBarcodeRepository.DeleteAsync(
                b => oldItemIds.Contains(b.OriginWarehouseImportReceiptItemId), autoSave: true);
            await _importReceiptItemRepository.DeleteManyAsync(oldItems, autoSave: true);
        }

        await _importReceiptRepository.DeleteAsync(batch.PreparedSolutionReceiptId.Value, autoSave: true);
        batch.PreparedSolutionReceiptId = null;
    }

    private async Task<ProductSku?> FindTuPhaSkuAsync(Product product)
    {
        var mfgQuery = await _manufacturerRepository.GetQueryableAsync();
        var systemMfgId = mfgQuery.Where(m => m.IsSystem).Select(m => m.Id).FirstOrDefault();
        if (systemMfgId == Guid.Empty) return null;
        return product.Skus?.FirstOrDefault(s => s.ManufacturerId == systemMfgId);
    }

    private static int? ResolveVolumePerUnit(Product p) =>
        p.VolumePerUnit
        ?? p.Skus?.FirstOrDefault(s => s.VolumePerUnit > 0)?.VolumePerUnit;

    private static decimal ToBaseQty(int? volumePerUnit, decimal displayQty) =>
        (volumePerUnit.HasValue && volumePerUnit.Value > 0)
            ? displayQty / volumePerUnit.Value
            : displayQty;

    private static decimal ToDisplayQty(int? volumePerUnit, decimal baseQty) =>
        (volumePerUnit.HasValue && volumePerUnit.Value > 0)
            ? baseQty * volumePerUnit.Value
            : baseQty;

    private async Task ProcessIngredientsAsync(
        PreparedSolutionBatch batch,
        List<CreateIngredientDto> inputIngredients,
        Guid? sourceWarehouseId)
    {
        // Exclude kho Level=0 và IsDefault — giống logic ResolveWarehouseForProductsAsync trong TestingAppService
        var warehouseQuery2 = await _warehouseRepository.GetQueryableAsync();
        var excludedWarehouseIds = warehouseQuery2
            .Where(w => w.Level == 0 || w.IsDefault)
            .Select(w => w.Id)
            .ToHashSet();

        HashSet<Guid>? sourceWarehouseIds = null;
        if (sourceWarehouseId.HasValue)
        {
            var subtree = await CollectWarehouseSubtreeAsync(sourceWarehouseId.Value);
            sourceWarehouseIds = subtree.Where(id => !excludedWarehouseIds.Contains(id)).ToHashSet();
        }
        else
        {
            var nonRootWarehouses = (await _warehouseRepository.GetQueryableAsync())
                .Where(w => w.IsActive && w.Level > 0 && !w.IsDefault)
                .Select(w => w.Id)
                .ToHashSet();
            sourceWarehouseIds = nonRootWarehouses;
        }

        var productIds = inputIngredients.Select(i => i.ProductId).Distinct().ToList();
        var productQuery = await _productRepository.WithDetailsAsync(p => p.Skus);
        var products = productQuery.Where(p => productIds.Contains(p.Id)).ToList();
        var productMap = products.ToDictionary(p => p.Id);

        // Resolve available lots per product (FEFO order) for usage log split
        var itemQuery = await _importReceiptItemRepository.GetQueryableAsync();
        var receiptQuery = await _importReceiptRepository.GetQueryableAsync();
        var rawLots = (
            from item in itemQuery
            join receipt in receiptQuery on item.WarehouseImportReceiptId equals receipt.Id
            where productIds.Contains(item.ProductId)
                  && receipt.Status == WarehouseImportReceiptStatus.Completed
                  && item.Status == WarehouseImportLineStatus.Checked
                  && (item.StorageWarehouseId.HasValue
                        ? sourceWarehouseIds.Contains(item.StorageWarehouseId.Value)
                        : receipt.WarehouseId.HasValue && sourceWarehouseIds.Contains(receipt.WarehouseId.Value))
            orderby item.LotExpiryDate == null ? 1 : 0, item.LotExpiryDate, receipt.ActualImportDate
            select new
            {
                item.ProductId,
                item.LotNumber,
                WarehouseId = item.StorageWarehouseId ?? receipt.WarehouseId,
                ReceivedBaseQuantity = item.ReceivedBaseQuantity,
                ConsumedBaseQuantity = item.ConsumedBaseQuantity,
                item.LotExpiryDate,
            })
            .ToList();

        // Trừ thêm PSB consumption đã ghi trước đó cho từng lô (hệ thống không trừ thật ConsumedBaseQuantity)
        // Chỉ tính PSB consumption SAU lần kiểm kê kho gần nhất (giống cách ProductAppService lọc tiêu hao)
        var pwiQuery = await _productWarehouseInventoryRepository.GetQueryableAsync();
        var lastInventoryMap = pwiQuery
            .Where(pwi => productIds.Contains(pwi.ProductId))
            .Select(pwi => new { pwi.ProductId, pwi.WarehouseId, pwi.LastInventoryDate })
            .ToList()
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.WarehouseId, x => x.LastInventoryDate));

        var icQuery = await _indicatorConsumptionRepository.GetQueryableAsync();
        var psbConsumedByLot = icQuery
            .Where(c => productIds.Contains(c.ProductId ?? Guid.Empty)
                && c.PreparedSolutionBatchId != null
                && c.LotNumber != null)
            .Select(c => new { c.ProductId, c.LotNumber, c.ActualQuantity, c.UnitId, c.WarehouseId, c.ConsumptionDate })
            .ToList()
            .Where(c =>
            {
                // Filter: chỉ tính consumption sau lastInventoryDate của (product, warehouse) tương ứng
                if (c.ProductId == null || c.WarehouseId == null) return true;
                if (!lastInventoryMap.TryGetValue(c.ProductId.Value, out var wMap)) return true;
                if (!wMap.TryGetValue(c.WarehouseId.Value, out var lastDate)) return true;
                return c.ConsumptionDate.HasValue && c.ConsumptionDate.Value > lastDate;
            })
            .ToList();

        var lotsByProduct = rawLots
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g =>
            {
                return g.Select(item =>
                {
                    var product2 = productMap.TryGetValue(item.ProductId, out var p) ? p : null;
                    var vpu2 = product2 != null ? ResolveVolumePerUnit(product2) : null;
                    var psbUsedDisplay = psbConsumedByLot
                        .Where(c => c.ProductId == item.ProductId
                            && c.LotNumber == item.LotNumber
                            && c.WarehouseId == item.WarehouseId)
                        .Sum(c => c.ActualQuantity);
                    var psbUsedBase = vpu2.HasValue && vpu2.Value > 0
                        ? psbUsedDisplay / vpu2.Value
                        : psbUsedDisplay;
                    var effectiveRemaining = item.ReceivedBaseQuantity - item.ConsumedBaseQuantity - psbUsedBase;
                    return new
                    {
                        item.ProductId,
                        item.LotNumber,
                        item.WarehouseId,
                        item.LotExpiryDate,
                        RemainingBaseQuantity = effectiveRemaining,
                    };
                })
                // Đảm bảo thứ tự FEFO sau khi tính effectiveRemaining
                .OrderBy(x => x.LotExpiryDate == null ? 1 : 0)
                .ThenBy(x => x.LotExpiryDate)
                .ToList();
            });

        foreach (var inp in inputIngredients)
        {
            var product = productMap[inp.ProductId];
            var isEquipment = inp.ProductType == (int)ProductType.Equipment;
            var vpu = ResolveVolumePerUnit(product);
            var stockBefore = ToDisplayQty(vpu, product.AvailableQuantity);

            var ingredient = new PreparedSolutionIngredient
            {
                PreparedSolutionBatchId = batch.Id,
                ProductType = inp.ProductType,
                ProductTypeName = GetProductTypeName((ProductType)inp.ProductType),
                ProductId = inp.ProductId,
                ProductName = product.Name,
                UnitName = await ResolveProductUnitNameAsync(product),
                QuantityUsed = inp.QuantityUsed,
                WarehouseQuantitySnapshot = stockBefore,
                IsEquipment = isEquipment,
            };

            await _ingredientRepository.InsertAsync(ingredient, autoSave: true);

            if (!isEquipment)
            {
                // Split across lots FEFO: ghi nhiều IndicatorConsumption nếu qty vượt quá tồn lô đầu
                var lots = lotsByProduct.TryGetValue(inp.ProductId, out var l) ? l : new();
                var remaining = inp.QuantityUsed; // display qty (mL)

                if (lots.Count == 0)
                {
                    // Không tìm thấy lô nào — ghi 1 row không có LotNumber
                    await _indicatorConsumptionRepository.InsertAsync(new IndicatorConsumption(
                        GuidGenerator.Create(), Guid.Empty, inp.ProductId, null, product.VolumeUnitId, inp.QuantityUsed)
                    {
                        PreparedSolutionBatchId = batch.Id,
                        TechnicianName = batch.PreparedByName,
                        ConsumptionDate = batch.PreparationDate,
                        SubmitDate = batch.PreparationDate,
                        ConsumableStockBefore = stockBefore,
                        IsDeducted = false,
                    }, autoSave: true);
                }
                else
                {
                    // Collect splits theo FEFO, chỉ lô còn tồn > 0
                    var splits = new List<(string? LotNumber, Guid? WarehouseId, decimal TakeQty, decimal StockBefore)>();
                    foreach (var lotInfo in lots.Where(l => l.RemainingBaseQuantity > 0))
                    {
                        if (remaining <= 0) break;
                        var lotRemainingDisplay = ToDisplayQty(vpu, lotInfo.RemainingBaseQuantity);
                        var takeQty = Math.Min(remaining, lotRemainingDisplay);
                        splits.Add((lotInfo.LotNumber, lotInfo.WarehouseId, takeQty, lotRemainingDisplay));
                        remaining -= takeQty;
                    }

                    // Phần dư (vượt tồn hoặc tất cả lô đã hết) — cộng vào lô cuối đã split hoặc lô FEFO đầu tiên
                    if (remaining > 0)
                    {
                        if (splits.Count > 0)
                        {
                            var last = splits[^1];
                            splits[^1] = (last.LotNumber, last.WarehouseId, last.TakeQty + remaining, last.StockBefore);
                        }
                        else
                        {
                            // Tất cả lô đã hết — ghi vào lô cuối cùng (FEFO last)
                            var lastLot = lots[^1];
                            var lotRemainingDisplay = ToDisplayQty(vpu, lastLot.RemainingBaseQuantity);
                            splits.Add((lastLot.LotNumber, lastLot.WarehouseId, remaining, lotRemainingDisplay));
                        }
                        remaining = 0;
                    }

                    foreach (var (lotNumber, warehouseId, takeQty, stockBefore2) in splits)
                    {
                        await _indicatorConsumptionRepository.InsertAsync(new IndicatorConsumption(
                            GuidGenerator.Create(), Guid.Empty, inp.ProductId, null, product.VolumeUnitId, takeQty)
                        {
                            ActualQuantity = takeQty,
                            PreparedSolutionBatchId = batch.Id,
                            TechnicianName = batch.PreparedByName,
                            ConsumptionDate = batch.PreparationDate,
                            SubmitDate = batch.PreparationDate,
                            ConsumableStockBefore = stockBefore2,
                            LotNumber = lotNumber,
                            WarehouseId = warehouseId,
                            IsDeducted = false,
                        }, autoSave: true);
                    }

                    // Không có lô nào khớp sau filter — ghi 1 row không có lô
                    if (remaining > 0)
                    {
                        await _indicatorConsumptionRepository.InsertAsync(new IndicatorConsumption(
                            GuidGenerator.Create(), Guid.Empty, inp.ProductId, null, product.VolumeUnitId, remaining)
                        {
                            ActualQuantity = remaining,
                            PreparedSolutionBatchId = batch.Id,
                            TechnicianName = batch.PreparedByName,
                            ConsumptionDate = batch.PreparationDate,
                            SubmitDate = batch.PreparationDate,
                            ConsumableStockBefore = 0,
                            IsDeducted = false,
                        }, autoSave: true);
                    }
                }
            }
        }

        // Recalculate barcode status for all affected product+warehouse pairs
        var barcodeTargets = inputIngredients
            .Where(i => i.ProductType != (int)ProductType.Equipment)
            .SelectMany(i =>
            {
                if (lotsByProduct.TryGetValue(i.ProductId, out var lots))
                    return lots.Where(l => l.WarehouseId.HasValue)
                               .Select(l => (i.ProductId, l.WarehouseId!.Value));
                return [];
            })
            .Distinct();
        await _barcodeStatusService.RecalculateForProductsAsync(barcodeTargets);
    }

    private async Task<HashSet<Guid>> CollectWarehouseSubtreeAsync(Guid rootId)
    {
        var allWarehouses = (await _warehouseRepository.GetQueryableAsync())
            .Select(w => new { w.Id, w.ParentWarehouseId })
            .ToList();

        var result = new HashSet<Guid> { rootId };
        var queue = new Queue<Guid>();
        queue.Enqueue(rootId);
        while (queue.Count > 0)
        {
            var parentId = queue.Dequeue();
            foreach (var child in allWarehouses.Where(w => w.ParentWarehouseId == parentId))
            {
                if (result.Add(child.Id))
                    queue.Enqueue(child.Id);
            }
        }
        return result;
    }

    private async Task UpdateSourceBatchRemainingVolumeAsync(
        Product product, int? vpu, List<LotDeduction> deductions)
    {
        var receiptItemIds = deductions.Select(d => d.WarehouseImportReceiptItemId).ToList();
        var receiptItems = await _importReceiptItemRepository.GetListAsync(
            i => receiptItemIds.Contains(i.Id));
        var receiptIds = receiptItems.Select(i => i.WarehouseImportReceiptId).Distinct().ToList();

        var batches = await _repository.GetListAsync(
            b => b.PreparedSolutionReceiptId.HasValue && receiptIds.Contains(b.PreparedSolutionReceiptId.Value));
        if (!batches.Any()) return;

        var batchByReceiptId = batches.ToDictionary(b => b.PreparedSolutionReceiptId!.Value);

        foreach (var deduction in deductions)
        {
            var receiptItem = receiptItems.FirstOrDefault(i => i.Id == deduction.WarehouseImportReceiptItemId);
            if (receiptItem == null) continue;

            if (!batchByReceiptId.TryGetValue(receiptItem.WarehouseImportReceiptId, out var batch))
                continue;

            var deductedVolume = vpu is > 0
                ? deduction.DeductedBaseQty * vpu.Value
                : deduction.DeductedBaseQty;

            batch.RemainingVolume = Math.Max(0m, batch.RemainingVolume - deductedVolume);
            await _repository.UpdateAsync(batch, autoSave: true);
        }
    }


    private async Task ReverseStockDeductionsAsync(
        Guid batchId,
        string lotCode,
        List<PreparedSolutionIngredient> oldIngredients)
    {
        var distinctProductIds = oldIngredients
            .Where(i => !i.IsEquipment)
            .Select(i => i.ProductId)
            .Distinct()
            .ToList();

        var productQuery = await _productRepository.WithDetailsAsync(p => p.Skus);

        foreach (var productId in distinctProductIds)
        {
            var product = productQuery.FirstOrDefault(p => p.Id == productId);
            if (product == null) continue;

            await _fifoStockService.ReverseAsync(product, batchId, "Hoàn lại — chỉnh sửa phiếu pha");

            if (product.ProductType == ProductType.PreparedSolution)
            {
                await RecalcSourceBatchRemainingVolumeAsync(product);
            }
        }
    }

    private async Task RecalcSourceBatchRemainingVolumeAsync(Product product)
    {
        var sourceBatches = await _repository.GetListAsync(
            b => b.SolutionProductId == product.Id && b.PreparedSolutionReceiptId.HasValue);
        if (!sourceBatches.Any()) return;

        var receiptIds = sourceBatches.Select(b => b.PreparedSolutionReceiptId!.Value).ToList();
        var receiptItems = await _importReceiptItemRepository.GetListAsync(
            i => receiptIds.Contains(i.WarehouseImportReceiptId));

        var vpu = ResolveVolumePerUnit(product);

        foreach (var batch in sourceBatches)
        {
            var lotItem = receiptItems.FirstOrDefault(
                i => i.WarehouseImportReceiptId == batch.PreparedSolutionReceiptId!.Value);
            if (lotItem == null) continue;

            var remainingStorage = lotItem.ReceivedBaseQuantity - lotItem.ConsumedBaseQuantity;
            batch.RemainingVolume = vpu is > 0
                ? Math.Max(0m, remainingStorage * vpu.Value)
                : Math.Max(0m, remainingStorage);
            await _repository.UpdateAsync(batch, autoSave: true);
        }
    }

    private PreparedSolutionBatchDto MapToDto(PreparedSolutionBatch batch, DateTime now)
    {
        var status = ComputeStatus(batch, now);
        var daysRemaining = batch.ExpiryDate.HasValue
            ? (int)Math.Floor((batch.ExpiryDate.Value - now).TotalDays)
            : int.MaxValue;

        List<string> imageUrls;
        try { imageUrls = JsonSerializer.Deserialize<List<string>>(batch.ProcessingImages) ?? new(); }
        catch { imageUrls = new(); }

        return new PreparedSolutionBatchDto
        {
            Id = batch.Id,
            CreationTime = batch.CreationTime,
            LastModificationTime = batch.LastModificationTime,
            SolutionProductId = batch.SolutionProductId,
            SolutionProductName = batch.SolutionProductName,
            IdentificationCode = batch.IdentificationCode,
            Concentration = batch.Concentration,
            LotCode = batch.LotCode,
            PreparationDate = batch.PreparationDate,
            ExpiryDate = batch.ExpiryDate,
            TotalVolume = batch.TotalVolume,
            RemainingVolume = batch.RemainingVolume,
            VolumeUnitId = batch.VolumeUnitId,
            VolumeUnitName = batch.VolumeUnitName,
            StorageWarehouseId = batch.StorageWarehouseId,
            StorageWarehouseName = batch.StorageWarehouseName,
            SourceWarehouseId = batch.SourceWarehouseId,
            SourceWarehouseName = batch.SourceWarehouseName,
            StorageConditions = batch.StorageConditions,
            PreparationMethod = batch.PreparationMethod,
            IsProcessed = batch.IsProcessed,
            ProcessingNote = batch.ProcessingNote,
            ProcessedAt = batch.ProcessedAt,
            ProcessingImageUrls = imageUrls,
            PreparedByUserId = batch.PreparedByUserId,
            PreparedByName = batch.PreparedByName,
            ComputedStatus = status,
            DaysRemaining = daysRemaining,
            Ingredients = new List<PreparedSolutionIngredientDto>(),
        };
    }

    private static PreparedSolutionIngredientDto MapIngredientToDto(PreparedSolutionIngredient i) =>
        new()
        {
            Id = i.Id,
            PreparedSolutionBatchId = i.PreparedSolutionBatchId,
            ProductType = i.ProductType,
            ProductTypeName = i.ProductTypeName,
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            UnitName = i.UnitName,
            QuantityUsed = i.QuantityUsed,
            WarehouseQuantitySnapshot = i.WarehouseQuantitySnapshot,
            IsEquipment = i.IsEquipment,
        };

    private static PreparedSolutionStatus ComputeStatus(PreparedSolutionBatch batch, DateTime now)
    {
        if (batch.IsProcessed) return PreparedSolutionStatus.Processed;
        if (batch.ExpiryDate.HasValue && now >= batch.ExpiryDate.Value) return PreparedSolutionStatus.Expired;
        if (batch.RemainingVolume <= 0) return PreparedSolutionStatus.Empty;
        if (batch.ExpiryDate.HasValue && (batch.ExpiryDate.Value - now).TotalDays < 14) return PreparedSolutionStatus.NearingExpiry;
        if (batch.TotalVolume > 0 && batch.RemainingVolume <= 0.05m * batch.TotalVolume) return PreparedSolutionStatus.NearlyEmpty;
        return PreparedSolutionStatus.Active;
    }

    private static string? BuildDisplayName(string? surName, string? name)
    {
        var full = $"{surName} {name}".Trim();
        return string.IsNullOrWhiteSpace(full) ? null : full;
    }

    private async Task<string?> ResolveProductUnitNameAsync(Product product)
    {
        var unitId = product.VolumeUnitId ?? product.BaseUnitId;
        if (!unitId.HasValue) return null;
        var unit = await _unitRepository.FindAsync(unitId.Value);
        return unit?.Name;
    }

    private static string GetProductTypeName(ProductType type) => type switch
    {
        ProductType.Chemical => "Hóa chất",
        ProductType.Biological => "Sinh phẩm",
        ProductType.Supply => "Vật tư tiêu hao",
        ProductType.Standard => "Chất chuẩn",
        ProductType.Solvent => "Dung môi",
        ProductType.Equipment => "Thiết bị",
        ProductType.PreparedSolution => "Dung dịch pha",
        _ => type.ToString(),
    };

    #endregion
}
