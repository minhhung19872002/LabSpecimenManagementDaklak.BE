using System;

namespace LabSpecimenManagement.MasterData;

public class StandardIndicatorDto
{
    public Guid Id { get; set; }
    public Guid StandardId { get; set; }
    public Guid IndicatorId { get; set; }
    public string IndicatorCode { get; set; }
    public string IndicatorName { get; set; }
    public Guid TestMethodId { get; set; }
    public string TestMethodCode { get; set; }
    public string TestMethodName { get; set; }
    public string Unit { get; set; }
    public string? ComparisonOperator { get; set; }
    public string? AllowedLimit { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}
