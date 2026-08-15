using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.MaterialReceipts;

public class GetMaterialReceiptListInput : PagedAndSortedResultRequestDto
{
    public string? Search { get; set; }
    public MaterialReceiptStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class MaterialReceiptListDto : EntityDto<Guid>
{
    public string Code { get; set; }
    public string ReceiverName { get; set; }
    public DateTime ReceiptDate { get; set; }
    public MaterialReceiptStatus Status { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreationTime { get; set; }
    public string? CreatorName { get; set; }
}

public class MaterialReceiptStatisticsDto
{
    public int DraftCount { get; set; }
    public int ReceivedCount { get; set; }
}

public class MaterialReceiptDto : EntityDto<Guid>
{
    public string Code { get; set; }
    public string ReceiverName { get; set; }
    public Guid? ReceiverId { get; set; }
    public DateTime ReceiptDate { get; set; }
    public MaterialReceiptStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime CreationTime { get; set; }
    public string? CreatorName { get; set; }

    public List<MaterialReceiptItemDto> Items { get; set; } = new();
}

public class MaterialReceiptItemDto : EntityDto<Guid>
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitName { get; set; }
    public decimal Quantity { get; set; }
    public decimal BaseQuantity { get; set; }
    public Guid? SkuId { get; set; }
    public Guid ReceivingWarehouseId { get; set; }
    public string ReceivingWarehouseName { get; set; }

    public string? IdentificationCode { get; set; }
    public string? TechnicalSpec { get; set; }
    public string? CASNumber { get; set; }
    public DateTime? NearestExpiryDate { get; set; }
    public string? LotNumber { get; set; }
    public string? ManufacturerName { get; set; }
    public int? ProductType { get; set; }
    public List<ProductUnitOptionDto> AvailableUnits { get; set; } = new();
}

public class CreateUpdateMaterialReceiptDto
{
    [Required]
    [StringLength(50)]
    public string Code { get; set; }

    public DateTime ReceiptDate { get; set; }
    public string? Notes { get; set; }

    [Required]
    public List<CreateUpdateMaterialReceiptItemDto> Items { get; set; } = new();
}

public class CreateUpdateMaterialReceiptItemDto
{
    public Guid? Id { get; set; }

    [Required]
    public Guid ProductId { get; set; }

    public Guid? UnitId { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public decimal Quantity { get; set; }

    public Guid? SkuId { get; set; }

    [Required]
    public Guid ReceivingWarehouseId { get; set; }

    public string? LotNumber { get; set; }
    public string? ManufacturerName { get; set; }
}

public class CentralWarehouseProductDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public string? FormattedName { get; set; }
    public string? IdentificationCode { get; set; }
    public string? TechnicalSpec { get; set; }
    public string? CASNumber { get; set; }
    public int ProductType { get; set; }
    public decimal CentralStockBaseQty { get; set; }
    public DateTime? NearestExpiryDate { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? LotExpiryDate { get; set; }
    public string? ManufacturerName { get; set; }

    public string? StorageUnitName { get; set; }
    public string? ConsumableUnitName { get; set; }
    public int? VolumePerUnit { get; set; }

    public List<ProductUnitOptionDto> AvailableUnits { get; set; } = new();
    public List<CentralWarehouseProductSkuDto> Skus { get; set; } = new();
}

public class ProductUnitOptionDto
{
    public Guid UnitId { get; set; }
    public string UnitName { get; set; }
    public bool IsConsumable { get; set; }
}

public class CentralWarehouseProductSkuDto
{
    public Guid SkuId { get; set; }
    public Guid? StorageUnitId { get; set; }
    public string? StorageUnitName { get; set; }
    public Guid? ConsumableUnitId { get; set; }
    public string? ConsumableUnitName { get; set; }
    public int? VolumePerUnit { get; set; }
    public int? PackagingQuantity { get; set; }
}

public class ReceivingWarehouseStockDto
{
    public Guid ProductId { get; set; }
    public Guid WarehouseId { get; set; }
    public decimal AvailableBaseQuantity { get; set; }
    public string? StorageUnitName { get; set; }
    public string? ConsumableUnitName { get; set; }
    public int? VolumePerUnit { get; set; }
}
