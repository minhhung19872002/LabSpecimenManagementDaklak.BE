using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.TestRequests;

/// <summary>
/// Input DTO for getting all samples across test requests
/// </summary>
public class GetAllSamplesInput : PagedAndSortedResultRequestDto
{
    /// <summary>
    /// Search by sample code or sample name
    /// </summary>
    public string? Search { get; set; }
}

/// <summary>
/// DTO representing a sample from an existing test request (for reuse)
/// </summary>
public class ExistingSampleDto
{
    public Guid SampleId { get; set; }
    public string SampleCode { get; set; } = string.Empty;
    public string SampleName { get; set; } = string.Empty;
    public string? SampleQuantity { get; set; }
    public Guid? SpecimenTypeId { get; set; }
    public string? SpecimenTypeName { get; set; }
    public SampleStatus Status { get; set; }
    public string? StatusText { get; set; }

    /// <summary>
    /// Contract number of the test request containing this sample
    /// </summary>
    public string? ContractNumber { get; set; }

    /// <summary>
    /// Indicators associated with this sample
    /// </summary>
    public List<ExistingSampleIndicatorDto> Indicators { get; set; } = new();
}

/// <summary>
/// DTO for an indicator within an existing sample
/// </summary>
public class ExistingSampleIndicatorDto
{
    public Guid IndicatorId { get; set; }
    public string? IndicatorName { get; set; }
    public string? IndicatorFormattedName { get; set; }
    public Guid? TestMethodId { get; set; }
    public string? TestMethodName { get; set; }
    public string? TestMethodFormattedName { get; set; }
    public Guid? SpecimenTypeId { get; set; }
    public string? SpecimenTypeName { get; set; }
    public decimal UnitPrice { get; set; }
}
