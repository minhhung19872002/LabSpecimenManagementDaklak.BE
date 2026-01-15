using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.MasterData.SampleTypeCategories;

/// <summary>
/// DTO cho Danh mục loại mẫu
/// </summary>
public class SampleTypeCategoryDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int SpecimenTypeCount { get; set; }
}

/// <summary>
/// DTO tạo/cập nhật Danh mục loại mẫu
/// </summary>
public class CreateUpdateSampleTypeCategoryDto
{
    [Required(ErrorMessage = "Vui lòng nhập tên danh mục loại mẫu")]
    [StringLength(256, ErrorMessage = "Tên danh mục không được vượt quá 256 ký tự")]
    public string Name { get; set; }

    [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
    public string? Description { get; set; }

    public bool IsActive { get; set; } = true;
}
