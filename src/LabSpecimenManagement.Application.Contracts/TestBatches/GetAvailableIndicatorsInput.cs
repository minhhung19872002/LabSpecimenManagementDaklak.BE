using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.TestBatches;

public class GetAvailableIndicatorsInput : PagedAndSortedResultRequestDto
{
    public Guid? IndicatorId { get; set; }

    public Guid? MethodId { get; set; }

    public Guid? SpecimenTypeId { get; set; }

    public string? Filter { get; set; }

    public Guid? TechnicianId { get; set; }
}

public class AvailableIndicatorDto : EntityDto<Guid>
{
    public Guid LaboratoryTaskId { get; set; }

    public string? ContractNumber { get; set; }

    public string? SampleCode { get; set; }

    public string? SampleName { get; set; }

    public string? TechnicianName { get; set; }

    public Guid? TechnicianId { get; set; }

    public Guid? IndicatorId { get; set; }

    public string? IndicatorName { get; set; }

    public Guid? MethodId { get; set; }

    public string? MethodName { get; set; }

    public Guid? SpecimenTypeId { get; set; }

    public string? SpecimenTypeName { get; set; }

    public DateTime? RequestCompletionDate { get; set; }

    public DateTime? SampleSentDate { get; set; }

    public DateTime? SampleReceivedDate { get; set; }

    public bool IsAlreadyInBatch { get; set; }

    public int TestingStatus { get; set; }
}
