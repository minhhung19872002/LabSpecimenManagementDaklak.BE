using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.MasterData;

public class TestResultValueDto : EntityDto<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }

    /// <summary>
    /// Số lượng chỉ tiêu được gắn
    /// </summary>
    public int IndicatorCount { get; set; }

    /// <summary>
    /// Danh sách chỉ tiêu chi tiết (for with-indicators endpoint)
    /// </summary>
    public List<TestResultValueIndicatorDto>? Indicators { get; set; }
}
