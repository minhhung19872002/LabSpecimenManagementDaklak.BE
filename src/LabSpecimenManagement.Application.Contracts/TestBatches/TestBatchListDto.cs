using System;
using Volo.Abp.Application.Dtos;
using LabSpecimenManagement.Testing;

namespace LabSpecimenManagement.TestBatches;

public class TestBatchListDto : EntityDto<Guid>
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

    public int ItemCount { get; set; }

    public int SubmittedItemCount { get; set; }

    public int ContractCount { get; set; }

    public DateTime? RequestCompletionDate { get; set; }

    public DateTime CreationTime { get; set; }

    public Guid? CreatorId { get; set; }

    public string? CreatorName { get; set; }
}
