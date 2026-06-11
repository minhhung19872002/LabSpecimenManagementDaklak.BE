using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;
using LabSpecimenManagement.Testing;

namespace LabSpecimenManagement.TestBatches;

public class TestBatchDto : FullAuditedEntityDto<Guid>
{
    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public Guid IndicatorId { get; set; }

    public string? IndicatorName { get; set; }

    public Guid MethodId { get; set; }

    public string? MethodName { get; set; }

    public Guid? SpecimenTypeId { get; set; }

    public string? SpecimenTypeName { get; set; }

    public TestBatchStatus Status { get; set; }

    public DateTime? TestDate { get; set; }

    public DateTime? CompletedDate { get; set; }

    public Guid? CompletedById { get; set; }

    public string? CompletedByName { get; set; }

    public string? Note { get; set; }

    public bool ConsumptionConfirmed { get; set; }

    public DateTime? ConsumptionConfirmedDate { get; set; }

    public List<TestBatchItemDto> Items { get; set; } = new();

    public List<TestBatchConsumptionDto> Consumptions { get; set; } = new();
}

public class TestBatchItemDto : EntityDto<Guid>
{
    public Guid TestBatchId { get; set; }

    public Guid LaboratoryTaskIndicatorId { get; set; }

    public Guid LaboratoryTaskId { get; set; }

    public string? ContractNumber { get; set; }

    public string? SampleCode { get; set; }

    public string? SampleName { get; set; }

    public string? TechnicianName { get; set; }

    public string? IndicatorName { get; set; }

    public string? MethodName { get; set; }

    public string? Unit { get; set; }

    public string? Result { get; set; }

    public DateTime? TestDate { get; set; }

    public DateTime? SubmitDate { get; set; }

    public string? RejectionReason { get; set; }

    public bool IsSubmitted { get; set; }

    public bool IsExternallyCompleted { get; set; }
}

public class TestBatchConsumptionDto : EntityDto<Guid>
{
    public Guid TestBatchId { get; set; }

    public Guid? ProductId { get; set; }

    public string? ProductName { get; set; }

    public Guid? EquipmentId { get; set; }

    public string? EquipmentName { get; set; }

    public Guid? UnitId { get; set; }

    public string? UnitName { get; set; }

    public decimal RecommendedQuantity { get; set; }

    public decimal ActualQuantity { get; set; }

    public bool IsDeducted { get; set; }
}
