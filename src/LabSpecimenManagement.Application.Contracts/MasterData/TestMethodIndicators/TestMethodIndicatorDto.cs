using System;

namespace LabSpecimenManagement.MasterData;

public class TestMethodIndicatorDto
{
    public Guid Id { get; set; }
    public Guid TestMethodId { get; set; }
    public Guid IndicatorId { get; set; }
    public string IndicatorCode { get; set; }
    public string IndicatorName { get; set; }
    public string? IndicatorFormattedName { get; set; }
    public decimal UnitPrice { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }

    /// <summary>
    /// ID chỉ tiêu cha (null nếu là chỉ tiêu gốc)
    /// </summary>
    public Guid? ParentIndicatorId { get; set; }
}
