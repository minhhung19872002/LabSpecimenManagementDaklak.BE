using System;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.MasterData.Units;

/// <summary>
/// DTO cho Đơn vị hàng hóa
/// </summary>
public class UnitDto : FullAuditedEntityDto<Guid>
{
    public string Code { get; set; }
    public string Name { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO tạo/cập nhật Đơn vị hàng hóa
/// </summary>
public class CreateUpdateUnitDto
{
    [Required(ErrorMessage = "Vui lòng nhập mã đơn vị hàng hóa")]
    [StringLength(50, ErrorMessage = "Mã đơn vị không được vượt quá 50 ký tự")]
    public string Code { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên đơn vị hàng hóa")]
    [StringLength(256, ErrorMessage = "Tên đơn vị không được vượt quá 256 ký tự")]
    public string Name { get; set; }

    public bool IsActive { get; set; } = true;
}
