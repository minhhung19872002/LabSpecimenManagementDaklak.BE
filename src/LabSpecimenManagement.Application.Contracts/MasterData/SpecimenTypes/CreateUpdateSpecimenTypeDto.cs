using System;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.MasterData;

public class CreateUpdateSpecimenTypeDto
{
    [StringLength(20)]
    public string? Code { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    [StringLength(200)]
    public string? DefaultStorageCondition { get; set; }

    public int? MaxStorageDays { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; } = 0;

    /// <summary>
    /// ID danh mục loại mẫu
    /// </summary>
    public Guid? SampleTypeCategoryId { get; set; }
}
