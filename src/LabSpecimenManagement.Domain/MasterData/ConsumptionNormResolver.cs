using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabSpecimenManagement.Testing;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Domain.Services;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// 1 dòng tiêu hao / phép thử tính từ định mức theo mẻ (L3) ÷ dự toán mẫu năm (L4).
/// </summary>
public class ResolvedNormLine
{
    public Guid MethodBatchNormId { get; init; }
    public Guid? ProductId { get; init; }
    public Guid? UnitId { get; init; }

    /// <summary>Thiết bị không còn khai báo trong định mức — giữ để code trừ kho cũ không đổi</summary>
    public Guid? EquipmentId => null;

    public decimal BatchQuantity { get; init; }
    public int ForecastYear { get; init; }
    public int EstimatedSamples { get; init; }

    /// <summary>BR-01/03: BatchQuantity ÷ EstimatedSamples, làm tròn 4 chữ số thập phân</summary>
    public decimal? AvgConsumptionPerSample { get; init; }

    /// <summary>Lượng tiêu hao cho 1 phép thử (= AvgConsumptionPerSample)</summary>
    public decimal Quantity => AvgConsumptionPerSample ?? 0;

    /// <summary>Tạo bản ghi tiêu hao kèm snapshot định mức đã dùng để tính</summary>
    public IndicatorConsumption NewConsumption(Guid id, Guid laboratoryTaskIndicatorId, decimal recommendedQuantity)
    {
        return new IndicatorConsumption(id, laboratoryTaskIndicatorId, ProductId, null, UnitId, recommendedQuantity)
        {
            MethodBatchNormId = MethodBatchNormId,
            NormBatchQuantity = BatchQuantity,
            NormForecastYear = ForecastYear,
            NormEstimatedSamples = EstimatedSamples,
        };
    }
}

/// <summary>
/// Tính tiêu hao trung bình / mẫu cho dịch vụ (Phương pháp + Chỉ tiêu + Nền mẫu) theo năm.
/// Luôn đọc master data hiện tại → sửa định mức/dự toán áp dụng ngay cho lần submit sau (kể cả submit lại).
/// </summary>
public class ConsumptionNormResolver : DomainService
{
    public const int Decimals = 4;

    private readonly IRepository<ServiceBatchNorm, Guid> _serviceNormRepository;
    private readonly IRepository<MethodBatchNorm, Guid> _normRepository;
    private readonly IRepository<ServiceYearlyForecast, Guid> _forecastRepository;

    public ConsumptionNormResolver(
        IRepository<ServiceBatchNorm, Guid> serviceNormRepository,
        IRepository<MethodBatchNorm, Guid> normRepository,
        IRepository<ServiceYearlyForecast, Guid> forecastRepository)
    {
        _serviceNormRepository = serviceNormRepository;
        _normRepository = normRepository;
        _forecastRepository = forecastRepository;
    }

    public static decimal? ComputePerSample(decimal? batchQuantity, int? estimatedSamples)
    {
        if (batchQuantity is not > 0 || estimatedSamples is not > 0)
        {
            return null;
        }

        return Math.Round(batchQuantity.Value / estimatedSamples.Value, Decimals, MidpointRounding.AwayFromZero);
    }

    public async Task<List<ResolvedNormLine>> ResolveAsync(TestMethodIndicator service, int year)
    {
        var map = await ResolveManyAsync(new[] { service }, year);
        return map.GetValueOrDefault(service.Id) ?? new List<ResolvedNormLine>();
    }

    /// <summary>
    /// Key = TestMethodIndicatorId. Dịch vụ thiếu định mức hoặc dự toán năm → danh sách rỗng (không trừ kho).
    /// </summary>
    public async Task<Dictionary<Guid, List<ResolvedNormLine>>> ResolveManyAsync(IEnumerable<TestMethodIndicator> services, int year)
    {
        var serviceList = services.DistinctBy(s => s.Id).ToList();
        var result = serviceList.ToDictionary(s => s.Id, _ => new List<ResolvedNormLine>());
        if (serviceList.Count == 0)
        {
            return result;
        }

        var serviceIds = serviceList.Select(s => s.Id).ToList();

        // Định mức gắn theo dịch vụ (nền mẫu × chỉ tiêu × PP); ngừng sử dụng → không trừ tiêu hao
        var headers = (await _serviceNormRepository.GetListAsync(h => serviceIds.Contains(h.TestMethodIndicatorId) && h.IsActive))
            .ToDictionary(h => h.Id, h => h.TestMethodIndicatorId);
        if (headers.Count == 0)
        {
            return result;
        }

        var headerIds = headers.Keys.ToList();
        var norms = await _normRepository.GetListAsync(n =>
            n.ServiceBatchNormId != null && headerIds.Contains(n.ServiceBatchNormId.Value)
            && n.IsActive && n.ProductId != null && n.BatchQuantity > 0);
        if (norms.Count == 0)
        {
            return result;
        }

        var forecasts = (await _forecastRepository.GetListAsync(f =>
                serviceIds.Contains(f.TestMethodIndicatorId) && f.Year == year && f.EstimatedSamples > 0))
            .ToDictionary(f => f.TestMethodIndicatorId, f => f.EstimatedSamples!.Value);

        var normsByService = norms.GroupBy(n => headers[n.ServiceBatchNormId!.Value]).ToDictionary(g => g.Key, g => g.ToList());

        foreach (var service in serviceList)
        {
            if (!forecasts.TryGetValue(service.Id, out var estimated)
                || !normsByService.TryGetValue(service.Id, out var serviceNorms))
            {
                continue;
            }

            foreach (var norm in serviceNorms)
            {
                var perSample = ComputePerSample(norm.BatchQuantity, estimated);
                if (perSample is not > 0)
                {
                    continue;
                }

                result[service.Id].Add(new ResolvedNormLine
                {
                    MethodBatchNormId = norm.Id,
                    ProductId = norm.ProductId,
                    UnitId = norm.UnitId,
                    BatchQuantity = norm.BatchQuantity!.Value,
                    ForecastYear = year,
                    EstimatedSamples = estimated,
                    AvgConsumptionPerSample = perSample,
                });
            }
        }

        return result;
    }
}
