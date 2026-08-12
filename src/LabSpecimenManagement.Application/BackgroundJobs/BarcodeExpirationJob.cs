using System;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Products;
using LabSpecimenManagement.WarehouseImports;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;
using Volo.Abp.Uow;

namespace LabSpecimenManagement.BackgroundJobs;

public class BarcodeExpirationJob : ITransientDependency
{
    private readonly IRepository<InventoryBarcode, Guid> _barcodeRepository;
    private readonly IRepository<WarehouseImportReceiptItem, Guid> _importItemRepository;
    private readonly IAsyncQueryableExecuter _asyncExecuter;
    private readonly ILogger<BarcodeExpirationJob> _logger;

    public BarcodeExpirationJob(
        IRepository<InventoryBarcode, Guid> barcodeRepository,
        IRepository<WarehouseImportReceiptItem, Guid> importItemRepository,
        IAsyncQueryableExecuter asyncExecuter,
        ILogger<BarcodeExpirationJob> logger)
    {
        _barcodeRepository = barcodeRepository;
        _importItemRepository = importItemRepository;
        _asyncExecuter = asyncExecuter;
        _logger = logger;
    }

    [UnitOfWork]
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting barcode expiration check...");

        try
        {
            var today = DateTime.UtcNow.AddHours(7).Date;

            var barcodeQuery = await _barcodeRepository.GetQueryableAsync();
            var itemQuery = await _importItemRepository.GetQueryableAsync();

            var expiredBarcodes = await _asyncExecuter.ToListAsync(
                from barcode in barcodeQuery
                join item in itemQuery
                    on barcode.OriginWarehouseImportReceiptItemId equals item.Id
                where item.LotExpiryDate != null
                    && item.LotExpiryDate < today
                    && barcode.Status != InventoryBarcodeStatus.UsedUp
                    && barcode.Status != InventoryBarcodeStatus.Expired
                select barcode);

            if (expiredBarcodes.Count == 0)
            {
                _logger.LogInformation("No expired barcodes found.");
                return;
            }

            foreach (var barcode in expiredBarcodes)
            {
                barcode.Status = InventoryBarcodeStatus.Expired;
            }

            await _barcodeRepository.UpdateManyAsync(expiredBarcodes, autoSave: true);

            _logger.LogInformation("Marked {Count} barcodes as Expired.", expiredBarcodes.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during barcode expiration check.");
            throw;
        }
    }
}
