using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.MasterData;

public class StandardWithIndicatorsDto : FullAuditedEntityDto<Guid>
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string? LimitDescription { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public int IndicatorCount { get; set; }
    public List<StandardIndicatorDto> Indicators { get; set; } = new();
}
