using System;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.Testing;

public class IndicatorConsumption : Entity<Guid>
{
    public Guid LaboratoryTaskIndicatorId { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? EquipmentId { get; set; }
    public Guid? UnitId { get; set; }
    public decimal RecommendedQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public bool IsDeducted { get; set; }
    public Guid? WarehouseId { get; set; }
    public DateTime? SubmitDate { get; set; }
    public decimal? ConsumableStockBefore { get; set; }
    public string? LotNumber { get; set; }
    public Guid? PreparedSolutionBatchId { get; set; }
    public string? TechnicianName { get; set; }
    public DateTime? ConsumptionDate { get; set; }
    public string? BarcodeCode { get; set; }
    public Guid? WarehouseImportReceiptItemId { get; set; }

    // Snapshot định mức tại thời điểm ghi tiêu hao (đối soát khi master data L3/L4 thay đổi)
    /// <summary>Định mức theo mẻ (L3) đã dùng</summary>
    public Guid? MethodBatchNormId { get; set; }
    /// <summary>Tổng tiêu hao 1 mẻ tại thời điểm ghi</summary>
    public decimal? NormBatchQuantity { get; set; }
    /// <summary>Năm dự toán đã dùng (năm của ngày gửi kết quả)</summary>
    public int? NormForecastYear { get; set; }
    /// <summary>Số mẫu dự toán năm tại thời điểm ghi</summary>
    public int? NormEstimatedSamples { get; set; }

    protected IndicatorConsumption() { }

    public IndicatorConsumption(
        Guid id,
        Guid laboratoryTaskIndicatorId,
        Guid? productId,
        Guid? equipmentId,
        Guid? unitId,
        decimal recommendedQuantity) : base(id)
    {
        LaboratoryTaskIndicatorId = laboratoryTaskIndicatorId;
        ProductId = productId;
        EquipmentId = equipmentId;
        UnitId = unitId;
        RecommendedQuantity = recommendedQuantity;
        ActualQuantity = recommendedQuantity;
    }
}
