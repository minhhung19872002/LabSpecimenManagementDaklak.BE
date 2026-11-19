using System;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Thiết bị dùng trong định mức theo mẻ (Product loại Thiết bị phòng Lab) + công suất sử dụng cho 1 mẫu.
/// Chỉ để khai báo / dự toán — không tham gia trừ tiêu hao kho.
/// </summary>
public class ServiceBatchNormEquipment : FullAuditedEntity<Guid>
{
    public Guid ServiceBatchNormId { get; set; }

    public Guid ProductId { get; set; }

    /// <summary>Công suất sử dụng (cho 1 mẫu)</summary>
    public decimal Capacity { get; set; }

    /// <summary>Đơn vị công suất: phút/mẫu, giờ/mẫu, lần chạy/mẫu, kWh/mẫu, % công suất</summary>
    public string CapacityUnit { get; set; } = "";

    protected ServiceBatchNormEquipment()
    {
    }

    public ServiceBatchNormEquipment(Guid id, Guid serviceBatchNormId, Guid productId, decimal capacity, string capacityUnit)
        : base(id)
    {
        ServiceBatchNormId = serviceBatchNormId;
        ProductId = productId;
        Capacity = capacity;
        CapacityUnit = capacityUnit;
    }
}
