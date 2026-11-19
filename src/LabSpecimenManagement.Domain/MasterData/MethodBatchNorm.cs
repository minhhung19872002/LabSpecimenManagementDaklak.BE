using System;
using LabSpecimenManagement.Common;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// L3 · 1 dòng vật tư trong định mức theo mẻ của 1 dịch vụ (<see cref="ServiceBatchNorm"/>).
/// Vật tư / số lượng không bắt buộc: thiếu → khi chấp nhận TN hiện popup "Thiết lập tiêu hao".
/// Tiêu hao / mẫu = BatchQuantity ÷ dự toán số mẫu năm (L4) của dịch vụ.
/// TestMethodId = phương pháp của dịch vụ (denormalize).
/// </summary>
public class MethodBatchNorm : FullAuditedAggregateRoot<Guid>, IHasActive
{
    /// <summary>Mã định mức (DM0001...)</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Định mức dịch vụ chứa dòng này (null = dữ liệu cũ theo phương pháp, đã bỏ)</summary>
    public Guid? ServiceBatchNormId { get; set; }

    public Guid TestMethodId { get; set; }

    /// <summary>Vật tư (hóa chất / vật tư tiêu hao)</summary>
    public Guid? ProductId { get; set; }

    /// <summary>Đơn vị tính của BatchQuantity (đơn vị thể tích của vật tư, nếu không có thì đơn vị cơ bản)</summary>
    public Guid? UnitId { get; set; }

    /// <summary>Tổng tiêu hao cho 1 mẻ chuẩn</summary>
    public decimal? BatchQuantity { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>Đủ thông tin để tính tiêu hao</summary>
    public bool IsComplete => ProductId.HasValue && BatchQuantity is > 0;

    protected MethodBatchNorm()
    {
    }

    public MethodBatchNorm(Guid id, string code, Guid serviceBatchNormId, Guid testMethodId, Guid? productId, Guid? unitId, decimal? batchQuantity)
        : base(id)
    {
        Code = code;
        ServiceBatchNormId = serviceBatchNormId;
        TestMethodId = testMethodId;
        ProductId = productId;
        UnitId = unitId;
        BatchQuantity = batchQuantity;
    }
}
