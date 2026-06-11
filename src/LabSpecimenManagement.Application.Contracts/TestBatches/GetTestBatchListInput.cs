using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;
using LabSpecimenManagement.Testing;

namespace LabSpecimenManagement.TestBatches;

public class GetTestBatchListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }

    public TestBatchStatus? Status { get; set; }

    public Guid? IndicatorId { get; set; }

    public List<Guid>? IndicatorIds { get; set; }

    public Guid? MethodId { get; set; }

    public Guid? SpecimenTypeId { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public List<Guid>? TechnicianIds { get; set; }
}
