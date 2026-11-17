using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// L4 · Dự toán số mẫu năm (OP_YearlyForecast) cho 1 dịch vụ (TestMethodIndicator).
/// Unique [TestMethodIndicatorId + Year]. Năm đã qua chỉ được xem (BR-07).
/// </summary>
public class ServiceYearlyForecast : FullAuditedAggregateRoot<Guid>
{
    public Guid TestMethodIndicatorId { get; set; }

    public int Year { get; set; }

    /// <summary>Số mẫu dự toán trong năm. Null = chưa thiết lập.</summary>
    public int? EstimatedSamples { get; set; }

    protected ServiceYearlyForecast()
    {
    }

    public ServiceYearlyForecast(Guid id, Guid testMethodIndicatorId, int year, int? estimatedSamples)
        : base(id)
    {
        TestMethodIndicatorId = testMethodIndicatorId;
        Year = year;
        EstimatedSamples = estimatedSamples;
    }
}
