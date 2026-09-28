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

// TODO: EXTERNAL-CONSUMPTION — remove after BA confirms
// Input cho endpoint test ẩn trên PROD: forward payload sang UAT theo yêu cầu.
public class ExternalConsumptionForwardDto
{
    /// <summary>URL API UAT, vd https://api-uat.tihelab.vn. Bỏ trống = dùng config ExternalConsumption:UatApiUrl.</summary>
    public string? TargetUrl { get; set; }

    public ExternalConsumptionRequestDto Request { get; set; } = new();
}

// TODO: EXTERNAL-CONSUMPTION — remove after BA confirms
public class ExternalConsumptionForwardResultDto
{
    public bool Success { get; set; }
    public string Url { get; set; } = string.Empty;
    public int? StatusCode { get; set; }
    public string? ResponseBody { get; set; }
    public string? Error { get; set; }
    public long ElapsedMs { get; set; }
    public string RequestJson { get; set; } = string.Empty;
}
