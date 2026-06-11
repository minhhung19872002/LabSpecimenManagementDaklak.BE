using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.Testing;

public class TestBatch : FullAuditedAggregateRoot<Guid>
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public Guid IndicatorId { get; set; }

    public Guid MethodId { get; set; }

    public Guid? SpecimenTypeId { get; set; }

    public TestBatchStatus Status { get; set; }

    public DateTime? TestDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    public Guid? CompletedById { get; set; }

    public string? Note { get; set; }

    public bool ConsumptionConfirmed { get; set; }

    public DateTime? ConsumptionConfirmedDate { get; set; }

    public virtual ICollection<TestBatchItem> Items { get; set; }

    public virtual ICollection<TestBatchConsumption> Consumptions { get; set; }

    protected TestBatch()
    {
        Items = new List<TestBatchItem>();
        Consumptions = new List<TestBatchConsumption>();
    }

    public TestBatch(
        Guid id,
        string code,
        string name,
        Guid indicatorId,
        Guid methodId,
        Guid? specimenTypeId) : base(id)
    {
        Code = code;
        Name = name;
        IndicatorId = indicatorId;
        MethodId = methodId;
        SpecimenTypeId = specimenTypeId;
        Status = TestBatchStatus.Draft;
        Items = new List<TestBatchItem>();
        Consumptions = new List<TestBatchConsumption>();
    }
}
