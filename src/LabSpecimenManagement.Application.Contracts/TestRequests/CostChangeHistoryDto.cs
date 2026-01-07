using System;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// DTO lịch sử thay đổi tiền
/// </summary>
public class CostChangeHistoryDto
{
    public Guid Id { get; set; }
    public Guid TestRequestId { get; set; }
    public int ChangeType { get; set; }
    public string ChangeTypeLabel { get; set; } = null!;
    public decimal OldTotalCost { get; set; }
    public decimal NewTotalCost { get; set; }
    public decimal DifferenceAmount { get; set; }
    public Guid? AffectedIndicatorId { get; set; }
    public string? AffectedIndicatorName { get; set; }
    public decimal? IndicatorUnitPrice { get; set; }
    public string? Reason { get; set; }
    public DateTime CreationTime { get; set; }
    public string? CreatorName { get; set; }
}
