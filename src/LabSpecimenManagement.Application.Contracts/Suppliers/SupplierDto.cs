using System;
using System.ComponentModel.DataAnnotations;
using LabSpecimenManagement.Common;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Suppliers;

/// <summary>
/// DTO cho Nhà cung cấp
/// </summary>
public class SupplierDto : FullAuditedEntityDto<Guid>
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string? TaxCode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? LogoFileId { get; set; }
    public string? LogoUrl { get; set; }
    public string? SupplierGroup { get; set; }
    public string? Note { get; set; }
    public decimal TotalPurchase { get; set; }
    public decimal TotalPurchaseAfterReturn { get; set; }
    public decimal OutstandingDebt { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// DTO tạo/cập nhật Nhà cung cấp
/// </summary>
public class CreateUpdateSupplierDto
{
    /// <summary>
    /// Mã NCC (để trống thì hệ thống tự tạo)
    /// </summary>
    [StringLength(50)]
    public string? Code { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên nhà cung cấp")]
    [StringLength(500)]
    public string Name { get; set; }

    [StringLength(50)]
    public string? TaxCode { get; set; }

    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(256)]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string? Email { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(50)]
    public string? LogoFileId { get; set; }

    [StringLength(256)]
    public string? SupplierGroup { get; set; }

    [StringLength(1000)]
    public string? Note { get; set; }

    public bool IsActive { get; set; } = true;
}

/// <summary>
/// DTO filter danh sách nhà cung cấp
/// </summary>
public class SupplierFilterDto : PagedRequestDto
{
    public string? Search { get; set; }
    public string? SupplierGroup { get; set; }
}
