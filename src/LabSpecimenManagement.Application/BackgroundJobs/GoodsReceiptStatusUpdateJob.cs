using System;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.GoodsReceipts;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;

namespace LabSpecimenManagement.BackgroundJobs;

public class GoodsReceiptStatusUpdateJob : ITransientDependency
{
    private readonly IRepository<GoodsReceipt, Guid> _goodsReceiptRepository;
    private readonly ILogger<GoodsReceiptStatusUpdateJob> _logger;

    public GoodsReceiptStatusUpdateJob(
        IRepository<GoodsReceipt, Guid> goodsReceiptRepository,
        ILogger<GoodsReceiptStatusUpdateJob> logger)
    {
        _goodsReceiptRepository = goodsReceiptRepository;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting goods receipt status update job...");

        var today = DateTime.Today;
        var query = await _goodsReceiptRepository.GetQueryableAsync();

        var receiptsToUpdate = query
            .Where(x => x.OrderStatus == GoodsReceiptOrderStatus.Ordering
                && x.ExpectedDeliveryDate.HasValue
                && x.ExpectedDeliveryDate.Value.Date <= today)
            .ToList();

        if (receiptsToUpdate.Count == 0)
        {
            _logger.LogInformation("No goods receipts to update.");
            return;
        }

        foreach (var receipt in receiptsToUpdate)
        {
            receipt.OrderStatus = GoodsReceiptOrderStatus.WaitingReceipt;
            await _goodsReceiptRepository.UpdateAsync(receipt, autoSave: true);
        }

        _logger.LogInformation("Updated {Count} goods receipt(s) to WaitingReceipt status.", receiptsToUpdate.Count);
    }
}
