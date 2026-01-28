using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// DTO để thêm chỉ tiêu vào loại mẫu
/// </summary>
public class AddIndicatorsToSpecimenTypeDto
{
    /// <summary>
    /// Danh sách ID chỉ tiêu cần thêm
    /// </summary>
    [Required]
    public List<Guid> IndicatorIds { get; set; } = new();
}
