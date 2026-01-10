using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// DTO loại mẫu kèm danh sách chỉ tiêu chi tiết
/// </summary>
public class SpecimenTypeWithIndicatorsDto : FullAuditedEntityDto<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DefaultStorageCondition { get; set; }
    public int? MaxStorageDays { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }

    /// <summary>
    /// Số lượng chỉ tiêu
    /// </summary>
    public int IndicatorCount { get; set; }

    /// <summary>
    /// Danh sách chỉ tiêu chi tiết
    /// </summary>
    public List<SpecimenTypeIndicatorDto> Indicators { get; set; } = new();
}
