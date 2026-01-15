using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.MasterData.SampleConditions;

/// <summary>
/// DTO cho Tình trạng mẫu
/// </summary>
public class SampleConditionDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// DTO tạo/cập nhật Tình trạng mẫu
/// </summary>
public class CreateUpdateSampleConditionDto
{
    [Required(ErrorMessage = "Vui lòng nhập tên tình trạng mẫu")]
    [StringLength(256, ErrorMessage = "Tên tình trạng không được vượt quá 256 ký tự")]
    public string Name { get; set; }

    [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; } = 0;
}
