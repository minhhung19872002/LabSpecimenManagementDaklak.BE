using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.MasterData;

public class SpecimenTypeDto : FullAuditedEntityDto<Guid>
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? DefaultStorageCondition { get; set; }
    public int? MaxStorageDays { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }

    /// <summary>
    /// Số lượng chỉ tiêu thuộc loại mẫu này
    /// </summary>
    public int IndicatorCount { get; set; }
}
