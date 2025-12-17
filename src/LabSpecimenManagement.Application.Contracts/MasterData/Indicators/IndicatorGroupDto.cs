using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.MasterData.Indicators;

/// <summary>
/// DTO cho Nhóm chỉ tiêu
/// </summary>
public class IndicatorGroupDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public int IndicatorCount { get; set; }
}

/// <summary>
/// DTO tạo/cập nhật Nhóm chỉ tiêu
/// </summary>
public class CreateUpdateIndicatorGroupDto
{
    [Required(ErrorMessage = "Vui lòng nhập tên nhóm chỉ tiêu")]
    [StringLength(256, ErrorMessage = "Tên nhóm chỉ tiêu không được vượt quá 256 ký tự")]
    public string Name { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; } = 0;
}

/// <summary>
/// Kết quả import nhóm chỉ tiêu
/// </summary>
public class ImportIndicatorGroupsResultDto
{
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public List<ImportIndicatorGroupErrorDto> Errors { get; set; } = new();
}

/// <summary>
/// Lỗi import nhóm chỉ tiêu
/// </summary>
public class ImportIndicatorGroupErrorDto
{
    public int RowNumber { get; set; }
    public string GroupName { get; set; }
    public string ErrorMessage { get; set; }
}
