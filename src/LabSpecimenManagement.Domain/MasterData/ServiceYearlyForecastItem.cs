using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Cấu hình vật tư / thiết bị riêng của 1 dự toán năm (phiếu tạo dự toán):
/// - IsHidden = true: bỏ 1 dòng của định mức theo mẻ khỏi dự toán.
/// - IsHidden = false: vật tư / thiết bị bổ sung ngoài định mức.
/// Chỉ để hiển thị / ước tính — KHÔNG tham gia tính tiêu hao / mẫu khi trừ kho.
/// Xóa cứng: lưu phiếu = thay toàn bộ cấu hình của dự toán.
/// </summary>
public class ServiceYearlyForecastItem : CreationAuditedEntity<Guid>
{
    public Guid ForecastId { get; set; }

    public Guid ProductId { get; set; }

    public ForecastItemKind Kind { get; set; }

    public bool IsHidden { get; set; }

    /// <summary>Đơn vị tiêu hao (vật tư bổ sung)</summary>
    public Guid? UnitId { get; set; }

    /// <summary>Lượng tiêu hao (vật tư bổ sung) — theo <see cref="Mode"/></summary>
    public decimal? Quantity { get; set; }

    public ForecastQuantityMode? Mode { get; set; }

    protected ServiceYearlyForecastItem()
    {
    }

    public ServiceYearlyForecastItem(Guid id, Guid forecastId, Guid productId, ForecastItemKind kind, bool isHidden,
        Guid? unitId = null, decimal? quantity = null, ForecastQuantityMode? mode = null)
        : base(id)
    {
        ForecastId = forecastId;
        ProductId = productId;
        Kind = kind;
        IsHidden = isHidden;
        UnitId = unitId;
        Quantity = quantity;
        Mode = mode;
    }
}
