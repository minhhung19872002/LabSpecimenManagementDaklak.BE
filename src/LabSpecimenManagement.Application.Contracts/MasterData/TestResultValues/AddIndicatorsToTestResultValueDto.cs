using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// DTO để thêm chỉ tiêu vào kết quả thử nghiệm
/// </summary>
public class AddIndicatorsToTestResultValueDto
{
    /// <summary>
    /// Danh sách chỉ tiêu + nền mẫu cần thêm
    /// </summary>
    [Required]
    public List<IndicatorSpecimenTypeItemDto> Indicators { get; set; } = new();
}

/// <summary>
/// DTO cho một item chỉ tiêu + nền mẫu
/// </summary>
public class IndicatorSpecimenTypeItemDto
{
    /// <summary>
    /// ID Chỉ tiêu
    /// </summary>
    [Required]
    public Guid IndicatorId { get; set; }

    /// <summary>
    /// ID Nền mẫu
    /// </summary>
    [Required]
    public Guid SpecimenTypeId { get; set; }
}

/// <summary>
/// DTO cho batch remove indicators (với composite key)
/// </summary>
public class BatchRemoveIndicatorsDto
{
    /// <summary>
    /// Danh sách composite keys cần xóa
    /// </summary>
    [Required]
    public List<IndicatorSpecimenTypeItemDto> Keys { get; set; } = new();
}
