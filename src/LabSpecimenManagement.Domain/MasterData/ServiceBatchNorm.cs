using System;
using LabSpecimenManagement.Common;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// L3 · Định mức theo mẻ của 1 dịch vụ (Nền mẫu × Chỉ tiêu × Phương pháp = TestMethodIndicator).
/// Danh sách vật tư tiêu hao nằm ở <see cref="MethodBatchNorm"/> (ServiceBatchNormId).
/// IsActive = "Trạng thái sử dụng": ngừng → không trừ tiêu hao.
/// </summary>
public class ServiceBatchNorm : FullAuditedAggregateRoot<Guid>, IHasActive
{
    /// <summary>Mã định mức DM-{năm}-{0001}, tăng dần theo năm tạo</summary>
    public string Code { get; set; } = "";

    public Guid TestMethodIndicatorId { get; set; }

    public bool IsActive { get; set; } = true;

    protected ServiceBatchNorm()
    {
    }

    public ServiceBatchNorm(Guid id, string code, Guid testMethodIndicatorId)
        : base(id)
    {
        Code = code;
        TestMethodIndicatorId = testMethodIndicatorId;
        IsActive = true;
    }
}
