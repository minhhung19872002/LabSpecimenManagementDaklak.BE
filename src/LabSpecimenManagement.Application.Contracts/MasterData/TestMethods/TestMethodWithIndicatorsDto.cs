using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.MasterData;

public class TestMethodWithIndicatorsDto : FullAuditedEntityDto<Guid>
{
    public string Code { get; set; }
    public string Name { get; set; }
    /// <summary>
    /// Tên phương pháp có định dạng (HTML)
    /// </summary>
    public string? FormattedName { get; set; }
    public string Unit { get; set; }
    public string? FormattedUnit { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public int IndicatorCount { get; set; }
    public List<TestMethodIndicatorDto> Indicators { get; set; } = new();
}
