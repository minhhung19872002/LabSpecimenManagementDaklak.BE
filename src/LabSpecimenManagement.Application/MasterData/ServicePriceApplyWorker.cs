using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;
using Volo.Abp.Uow;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Mỗi giờ áp các phiên bản giá dịch vụ đã đến ngày hiệu lực vào TestMethodIndicator.UnitPrice
/// (giá "Sắp áp dụng" → "Đang áp dụng" mà không cần ai mở màn Bảng giá).
/// </summary>
public class ServicePriceApplyWorker : AsyncPeriodicBackgroundWorkerBase
{
    public ServicePriceApplyWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory)
        : base(timer, serviceScopeFactory)
    {
        Timer.Period = (int)TimeSpan.FromHours(1).TotalMilliseconds;
        Timer.RunOnStart = true;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        var uowManager = workerContext.ServiceProvider.GetRequiredService<IUnitOfWorkManager>();
        var priceManager = workerContext.ServiceProvider.GetRequiredService<ServicePriceManager>();

        using var uow = uowManager.Begin(requiresNew: true, isTransactional: true);
        var changed = await priceManager.ApplyDuePricesAsync();
        await uow.CompleteAsync();

        if (changed > 0)
        {
            Logger.LogInformation("ServicePriceApplyWorker: đã áp giá mới cho {Count} dịch vụ", changed);
        }
    }
}
