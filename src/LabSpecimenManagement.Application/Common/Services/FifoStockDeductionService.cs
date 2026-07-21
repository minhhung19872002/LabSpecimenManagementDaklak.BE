using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Products;
using LabSpecimenManagement.WarehouseImports;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;
using Volo.Abp.Linq;

namespace LabSpecimenManagement.Common;

public class LotDeduction
{
    public Guid WarehouseImportReceiptItemId { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? LotNumber { get; set; }
    public decimal DeductedBaseQty { get; set; }
}

public interface IFifoStockDeductionService
{
    Task<List<LotDeduction>> DeductAsync(
        Product product,
        decimal baseQtyToDeduct,
        StockMovementType movementType,
        Guid? referenceId,
        string? referenceCode,
        string? notes,
        ICollection<Guid>? warehouseIds = null);

    Task ReverseAsync(
        Product product,
        Guid referenceId,
        string? reverseNotes);
}

public class FifoStockDeductionService : IFifoStockDeductionService, ITransientDependency
{
    private readonly IRepository<WarehouseImportReceiptItem, Guid> _importItemRepository;
    private readonly IRepository<WarehouseImportReceipt, Guid> _importReceiptRepository;
    private readonly IRepository<StockMovementLog, Guid> _stockMovementLogRepository;
    private readonly IRepository<Product, Guid> _productRepository;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IAsyncQueryableExecuter _asyncExecuter;

    public FifoStockDeductionService(
        IRepository<WarehouseImportReceiptItem, Guid> importItemRepository,
        IRepository<WarehouseImportReceipt, Guid> importReceiptRepository,
        IRepository<StockMovementLog, Guid> stockMovementLogRepository,
        IRepository<Product, Guid> productRepository,
        IGuidGenerator guidGenerator,
        IAsyncQueryableExecuter asyncExecuter)
    {
        _importItemRepository = importItemRepository;
        _importReceiptRepository = importReceiptRepository;
        _stockMovementLogRepository = stockMovementLogRepository;
        _productRepository = productRepository;
        _guidGenerator = guidGenerator;
        _asyncExecuter = asyncExecuter;
    }

    public async Task<List<LotDeduction>> DeductAsync(
        Product product,
        decimal baseQtyToDeduct,
        StockMovementType movementType,
        Guid? referenceId,
        string? referenceCode,
        string? notes,
        ICollection<Guid>? warehouseIds = null)
    {
        var deductions = new List<LotDeduction>();

        var itemQuery = await _importItemRepository.GetQueryableAsync();
        var receiptQuery = await _importReceiptRepository.GetQueryableAsync();

        var lotsQuery = from item in itemQuery
                        join receipt in receiptQuery on item.WarehouseImportReceiptId equals receipt.Id
                        where item.ProductId == product.Id
                              && receipt.Status == WarehouseImportReceiptStatus.Completed
                              && item.Status == WarehouseImportLineStatus.Checked
                              && (item.ReceivedBaseQuantity - item.ConsumedBaseQuantity) > 0
                        orderby item.LotExpiryDate == null ? 1 : 0,
                                item.LotExpiryDate,
                                receipt.ActualImportDate
                        select new
                        {
                            Item = item,
                            EffectiveWarehouseId = item.StorageWarehouseId ?? receipt.WarehouseId
                        };

        if (warehouseIds != null && warehouseIds.Count > 0)
        {
            lotsQuery = lotsQuery.Where(x =>
                x.EffectiveWarehouseId.HasValue && warehouseIds.Contains(x.EffectiveWarehouseId.Value));
        }

        var lots = await _asyncExecuter.ToListAsync(lotsQuery);

        var remaining = baseQtyToDeduct;

        foreach (var lot in lots)
        {
            if (remaining <= 0) break;

            var available = lot.Item.ReceivedBaseQuantity - lot.Item.ConsumedBaseQuantity;
            var deduct = Math.Min(available, remaining);

            lot.Item.ConsumedBaseQuantity += deduct;
            await _importItemRepository.UpdateAsync(lot.Item, autoSave: true);

            var qtyBefore = product.AvailableQuantity;
            product.AvailableQuantity -= deduct;

            await _stockMovementLogRepository.InsertAsync(new StockMovementLog(
                _guidGenerator.Create(),
                product.Id,
                product.Name,
                qtyBefore,
                product.AvailableQuantity,
                movementType,
                referenceId,
                referenceCode,
                notes,
                lot.EffectiveWarehouseId,
                lot.Item.LotNumber,
                lot.Item.Id), autoSave: true);

            deductions.Add(new LotDeduction
            {
                WarehouseImportReceiptItemId = lot.Item.Id,
                WarehouseId = lot.EffectiveWarehouseId,
                LotNumber = lot.Item.LotNumber,
                DeductedBaseQty = deduct
            });

            remaining -= deduct;
        }

        if (remaining > 0)
        {
            var qtyBefore = product.AvailableQuantity;
            product.AvailableQuantity -= remaining;

            await _stockMovementLogRepository.InsertAsync(new StockMovementLog(
                _guidGenerator.Create(),
                product.Id,
                product.Name,
                qtyBefore,
                product.AvailableQuantity,
                movementType,
                referenceId,
                referenceCode,
                notes), autoSave: true);
        }

        await _productRepository.UpdateAsync(product, autoSave: true);

        return deductions;
    }

    public async Task ReverseAsync(
        Product product,
        Guid referenceId,
        string? reverseNotes)
    {
        var logQuery = await _stockMovementLogRepository.GetQueryableAsync();
        var allLogs = await _asyncExecuter.ToListAsync(
            logQuery.Where(l => l.ReferenceId == referenceId && l.ProductId == product.Id));

        if (!allLogs.Any()) return;

        var netByLot = allLogs
            .GroupBy(l => l.WarehouseImportReceiptItemId)
            .Select(g => new
            {
                LotItemId = g.Key,
                NetChange = g.Sum(l => l.ChangeQuantity),
                Sample = g.First()
            })
            .Where(x => x.NetChange < 0)
            .ToList();

        if (!netByLot.Any()) return;

        var originalType = allLogs.First(l => l.ChangeQuantity < 0).MovementType;

        foreach (var entry in netByLot)
        {
            var reverseQty = Math.Abs(entry.NetChange);

            if (entry.LotItemId.HasValue)
            {
                var lotItem = await _importItemRepository.FindAsync(entry.LotItemId.Value);
                if (lotItem != null)
                {
                    lotItem.ConsumedBaseQuantity = Math.Max(0, lotItem.ConsumedBaseQuantity - reverseQty);
                    await _importItemRepository.UpdateAsync(lotItem, autoSave: true);
                }
            }

            var qtyBefore = product.AvailableQuantity;
            product.AvailableQuantity += reverseQty;

            await _stockMovementLogRepository.InsertAsync(new StockMovementLog(
                _guidGenerator.Create(),
                product.Id,
                product.Name,
                qtyBefore,
                product.AvailableQuantity,
                originalType,
                referenceId,
                null,
                reverseNotes,
                entry.Sample.WarehouseId,
                entry.Sample.LotNumber,
                entry.LotItemId), autoSave: true);
        }

        await _productRepository.UpdateAsync(product, autoSave: true);
    }
}
