using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.Testing;

// TODO: EXTERNAL-CONSUMPTION — remove after BA confirms
public class ExternalConsumptionRequestDto
{
    public string LaboratoryCode { get; set; } = string.Empty;
    public List<ExternalConsumptionIndicatorDto> Indicators { get; set; } = new();
}

// TODO: EXTERNAL-CONSUMPTION — remove after BA confirms
public class ExternalConsumptionIndicatorDto
{
    public string MethodName { get; set; } = string.Empty;
    public string IndicatorName { get; set; } = string.Empty;
    public string SpecimenTypeName { get; set; } = string.Empty;
    public DateTime TestDate { get; set; }
    public string? TechnicianName { get; set; }
}
