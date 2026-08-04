using System;

namespace LabSpecimenManagement.Products;

public class GetProductUsageLogInput
{
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int SkipCount { get; set; } = 0;
    public int MaxResultCount { get; set; } = 20;
}

public class ProductUsageLogItemDto
{
    public string SampleCode { get; set; } = string.Empty;
    public string SampleName { get; set; } = string.Empty;
    public string? SampleMatrix { get; set; }
    public string IndicatorName { get; set; } = string.Empty;
    public string TestMethodName { get; set; } = string.Empty;
    public string? TechnicianName { get; set; }
    public string? Result { get; set; }
    public DateTime? ResultSubmissionDate { get; set; }
    public decimal? ConsumptionAmount { get; set; }
    public string? ConsumptionUnitName { get; set; }
}
