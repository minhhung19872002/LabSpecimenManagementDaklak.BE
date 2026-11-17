using System;
using LabSpecimenManagement.Common;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// L3 · Định mức tiêu hao theo mẻ (MD_BaseNorm): lượng vật tư cho 1 mẻ chuẩn của 1 phương pháp.
/// Vật tư / số lượng không bắt buộc: dòng chỉ có phương pháp = phương pháp đã khai báo nhưng chưa đủ định mức
/// (khi chấp nhận TN sẽ hiện popup "Thiết lập tiêu hao").
/// Tiêu hao / mẫu = BatchQuantity ÷ dự toán số mẫu năm (L4) của dịch vụ.
/// </summary>
public class MethodBatchNorm : FullAuditedAggregateRoot<Guid>, IHasActive
{
    /// <summary>Mã định mức (DM0001...)</summary>
    public string Code { get; set; } = string.Empty;

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

    public MethodBatchNorm(Guid id, string code, Guid testMethodId, Guid? productId, Guid? unitId, decimal? batchQuantity)
        : base(id)
    {
        Code = code;
        TestMethodId = testMethodId;
        ProductId = productId;
        UnitId = unitId;
        BatchQuantity = batchQuantity;
    }
}
