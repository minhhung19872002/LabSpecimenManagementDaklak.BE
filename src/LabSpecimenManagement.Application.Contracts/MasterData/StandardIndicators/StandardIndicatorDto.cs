using System;

namespace LabSpecimenManagement.MasterData;

public class StandardIndicatorDto
{
    public Guid Id { get; set; }
    public Guid StandardId { get; set; }
    public Guid IndicatorId { get; set; }
    public string IndicatorCode { get; set; }
    public string IndicatorName { get; set; }
    public string? IndicatorFormattedName { get; set; }
    public Guid? ParentIndicatorId { get; set; }
    public Guid TestMethodId { get; set; }
    public string TestMethodCode { get; set; }
    public string TestMethodName { get; set; }
    public string? TestMethodFormattedName { get; set; }
    public string Unit { get; set; }
    public string? ComparisonOperator { get; set; }
    public string? AllowedLimit { get; set; }
    public string? LowerOperator { get; set; }
    public string? LowerLimit { get; set; }
    public string? UpperOperator { get; set; }
    public string? UpperLimit { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}
