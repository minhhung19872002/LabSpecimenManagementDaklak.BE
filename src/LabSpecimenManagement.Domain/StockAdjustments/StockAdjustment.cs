using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.StockAdjustments;

public class StockAdjustment : FullAuditedAggregateRoot<Guid>
{
    public string Code { get; set; }
    public StockAdjustmentStatus Status { get; set; }
    public string? Notes { get; set; }

    public ICollection<StockAdjustmentItem> Items { get; set; }

    protected StockAdjustment()
    {
        Items = new List<StockAdjustmentItem>();
    }

    public StockAdjustment(Guid id, string code) : base(id)
    {
        Code = code;
        Status = StockAdjustmentStatus.Draft;
        Items = new List<StockAdjustmentItem>();
    }
}
