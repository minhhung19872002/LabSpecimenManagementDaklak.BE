using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// Lịch sử thay đổi tiền của phiếu yêu cầu thử nghiệm
/// </summary>
public class CostChangeHistory : CreationAuditedEntity<Guid>
{
    /// <summary>
    /// ID phiếu yêu cầu
    /// </summary>
    public Guid TestRequestId { get; set; }

    /// <summary>
    /// Loại thay đổi
    /// </summary>
    public CostChangeType ChangeType { get; set; }

    /// <summary>
    /// Số tiền trước thay đổi
    /// </summary>
    public decimal OldTotalCost { get; set; }

    /// <summary>
    /// Số tiền sau thay đổi
    /// </summary>
    public decimal NewTotalCost { get; set; }

    /// <summary>
    /// Chênh lệch (NewTotalCost - OldTotalCost)
    /// </summary>
    public decimal DifferenceAmount { get; set; }

    /// <summary>
    /// ID chỉ tiêu bị ảnh hưởng (LaboratoryTaskIndicator ID)
    /// </summary>
    public Guid? AffectedIndicatorId { get; set; }

    /// <summary>
    /// Tên chỉ tiêu bị ảnh hưởng (snapshot)
    /// </summary>
    [MaxLength(500)]
    public string? AffectedIndicatorName { get; set; }

    /// <summary>
    /// Giá của chỉ tiêu bị hủy
    /// </summary>
    public decimal? IndicatorUnitPrice { get; set; }

    /// <summary>
    /// Lý do thay đổi
    /// </summary>
    [MaxLength(1000)]
    public string? Reason { get; set; }

    /// <summary>
    /// Navigation property - Phiếu yêu cầu
    /// </summary>
    public virtual TestRequest TestRequest { get; set; } = null!;

    protected CostChangeHistory() { }

    public CostChangeHistory(
        Guid id,
        Guid testRequestId,
        CostChangeType changeType,
        decimal oldTotalCost,
        decimal newTotalCost,
        Guid? affectedIndicatorId = null,
        string? affectedIndicatorName = null,
        decimal? indicatorUnitPrice = null,
        string? reason = null) : base(id)
    {
        TestRequestId = testRequestId;
        ChangeType = changeType;
        OldTotalCost = oldTotalCost;
        NewTotalCost = newTotalCost;
        DifferenceAmount = newTotalCost - oldTotalCost;
        AffectedIndicatorId = affectedIndicatorId;
        AffectedIndicatorName = affectedIndicatorName;
        IndicatorUnitPrice = indicatorUnitPrice;
        Reason = reason;
    }
}

/// <summary>
/// Loại thay đổi tiền
/// </summary>
public enum CostChangeType
{
    /// <summary>
    /// KH hủy chỉ tiêu (không đồng ý đổi PP)
    /// </summary>
    IndicatorCancelled = 1,

    /// <summary>
    /// KH đồng ý đổi PP (giá có thể thay đổi nếu PP mới có giá khác)
    /// </summary>
    MethodChanged = 2,

    /// <summary>
    /// KH hủy cả mẫu
    /// </summary>
    SampleCancelled = 3,

    /// <summary>
    /// Điều chỉnh thủ công
    /// </summary>
    ManualAdjustment = 4
}
