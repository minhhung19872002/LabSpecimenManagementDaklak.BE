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

    /// <summary>
    /// "Áp dụng tiêu hao" theo dự toán khác cùng năm: dùng danh sách vật tư / thiết bị của nguồn
    /// (chỉ 1 cấp — nguồn không được tự áp dụng theo bộ khác). Số mẫu vẫn nhập riêng.
    /// </summary>
    public Guid? SourceForecastId { get; set; }

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
