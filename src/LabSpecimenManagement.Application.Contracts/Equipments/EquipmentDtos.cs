using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LabSpecimenManagement.Common;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Equipments;

public class EquipmentListDto : EntityDto<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public EquipmentStatus Status { get; set; }
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }
    public string? Origin { get; set; }
    public string? SupplierName { get; set; }
    public string? GoodsReceiptCode { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public int UsageCount { get; set; }
    public int MaintenanceCount { get; set; }
    public string? CreatorName { get; set; }
    public DateTime CreationTime { get; set; }
    public List<EquipmentAttachmentDto> Attachments { get; set; } = new();
}

public class EquipmentDto : EntityDto<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public EquipmentStatus Status { get; set; }
    public Guid? GoodsReceiptId { get; set; }
    public string? GoodsReceiptCode { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }
    public string? Origin { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitName { get; set; }
    public decimal Price { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? LotExpiryDate { get; set; }
    public DateTime? WarrantyExpiryDate { get; set; }
    public int UsageCount { get; set; }
    public int MaintenanceCount { get; set; }
    public string? Notes { get; set; }
    public string? CreatorName { get; set; }
    public DateTime CreationTime { get; set; }
    public List<EquipmentAttachmentDto> Attachments { get; set; } = new();
}

public class CreateUpdateEquipmentDto
{
    [Required, StringLength(500)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string SerialNumber { get; set; } = string.Empty;

    [StringLength(256)]
    public string? Model { get; set; }

    [StringLength(256)]
    public string? Manufacturer { get; set; }

    [StringLength(256)]
    public string? Origin { get; set; }

    public DateTime? WarrantyExpiryDate { get; set; }

    public Guid? UnitId { get; set; }

    public decimal Price { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    public List<CreateEquipmentAttachmentDto>? Attachments { get; set; }
}

public class UpdateEquipmentStatusDto
{
    [Required]
    public EquipmentStatus Status { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }
}

public class GetEquipmentListInput : PagedRequestDto
{
    public string? Search { get; set; }
    public EquipmentStatus? Status { get; set; }
    public Guid? SupplierId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class EquipmentAttachmentDto
{
    public Guid Id { get; set; }
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? FileType { get; set; }
    public string Url { get; set; } = string.Empty;
    public DateTime CreationTime { get; set; }
}

public class CreateEquipmentAttachmentDto
{
    [Required]
    public string FileId { get; set; } = string.Empty;

    [Required]
    public string FileName { get; set; } = string.Empty;

    public long FileSize { get; set; }
    public string? FileType { get; set; }
}
