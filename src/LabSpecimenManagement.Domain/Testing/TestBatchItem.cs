using System;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.Testing;

public class TestBatchItem : Entity<Guid>
{
    public Guid TestBatchId { get; set; }

    public Guid LaboratoryTaskIndicatorId { get; set; }

    public Guid LaboratoryTaskId { get; set; }

    public string? ContractNumber { get; set; }

    public string? SampleCode { get; set; }

    public string? Result { get; set; }

    public DateTime? TestDate { get; set; }

    public bool IsSubmitted { get; set; }

    /// <summary>
    /// True nếu chỉ tiêu đã được submit qua flow cũ (ngoài batch)
    /// </summary>
    public bool IsExternallyCompleted { get; set; }

    protected TestBatchItem() { }

    public TestBatchItem(
        Guid id,
        Guid testBatchId,
        Guid laboratoryTaskIndicatorId,
        Guid laboratoryTaskId,
        string? contractNumber,
        string? sampleCode) : base(id)
    {
        TestBatchId = testBatchId;
        LaboratoryTaskIndicatorId = laboratoryTaskIndicatorId;
        LaboratoryTaskId = laboratoryTaskId;
        ContractNumber = contractNumber;
        SampleCode = sampleCode;
    }
}
