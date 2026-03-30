using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.MasterData;

public class TestMethodDto : FullAuditedEntityDto<Guid>
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string? FormattedName { get; set; }
    public string Unit { get; set; }
    public string? FormattedUnit { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public int IndicatorCount { get; set; }
}
