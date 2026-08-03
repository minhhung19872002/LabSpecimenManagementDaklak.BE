using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.WarehouseImports;

public class DeliveryDocumentFileDto
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public long? FileSize { get; set; }
}

public class WarehouseImportReceiptListDto : EntityDto<Guid>
{
    public string Code { get; set; }
    public Guid? GoodsReceiptId { get; set; }
    public string? GoodsReceiptCode { get; set; }
    public DateTime ActualImportDate { get; set; }
    public string? WarehouseKeeperName { get; set; }
    public string? WarehouseName { get; set; }
    public string? SupplierName { get; set; }
    public string? ContactPersonName { get; set; }
    public WarehouseImportReceiptStatus Status { get; set; }
    public bool HasDefectiveItem { get; set; }
    public string? CreatorName { get; set; }
    public DateTime CreationTime { get; set; }
}

public enum WarehouseImportReceiptDisplayStatusFilter
{
    All = 0,
    InProgress = 1,
    Completed = 2,
    Defective = 3
}

public class GetWarehouseImportReceiptListInput : PagedAndSortedResultRequestDto
{
    public string? Search { get; set; }
    public WarehouseImportReceiptDisplayStatusFilter? DisplayStatus { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public Guid? SupplierId { get; set; }
}

public class WarehouseImportReceiptStatisticsDto
{
    public int TotalReceipts { get; set; }
    public int DefectiveLots { get; set; }
}

public class WarehouseImportReceiptDto : EntityDto<Guid>
{
    public string Code { get; set; }
    public Guid? GoodsReceiptId { get; set; }
    public string? GoodsReceiptCode { get; set; }
    public DateTime ActualImportDate { get; set; }
    public Guid? WarehouseKeeperId { get; set; }
    public string? WarehouseKeeperName { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public string? DeliveryDocumentFileId { get; set; }
    public string? DeliveryDocumentFileName { get; set; }
    public string? DeliveryDocumentFilePath { get; set; }
    public long? DeliveryDocumentFileSize { get; set; }
    public List<DeliveryDocumentFileDto> DeliveryDocumentFiles { get; set; } = new();
    public string? Notes { get; set; }
    public WarehouseImportReceiptStatus Status { get; set; }
    public bool HasDefectiveItem { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? ContactPersonName { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? CreatorName { get; set; }
    public DateTime CreationTime { get; set; }

    public List<WarehouseImportReceiptItemDto> Items { get; set; } = new();
}

public class WarehouseImportReceiptItemDto : EntityDto<Guid>
{
    public Guid WarehouseImportReceiptId { get; set; }
    public Guid GoodsReceiptItemId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public decimal OrderedQuantity { get; set; }
    public string? PurchaseUnitName { get; set; }
    public decimal ActualReceivedQuantity { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitName { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? LotExpiryDate { get; set; }
    public WarehouseImportLineStatus Status { get; set; }
    public string? QualityDocumentFileId { get; set; }
    public string? QualityDocumentFileName { get; set; }
    public string? QualityDocumentFilePath { get; set; }
    public long? QualityDocumentFileSize { get; set; }
    public string? Notes { get; set; }
    public Guid? SkuId { get; set; }
    public string? ManufacturerName { get; set; }
    public Guid? StorageWarehouseId { get; set; }
    public string? StorageWarehouseName { get; set; }

    // Pricing fields
    public decimal? UnitPrice { get; set; }
    public int? VatPercent { get; set; }
    public decimal? TotalLineAmount { get; set; }

    // Equipment fields (Nhóm 2)
    public bool IsEquipment { get; set; }
    public string? AssetCode { get; set; }
    public string? SerialNumber { get; set; }
    public Guid? ManufacturerId { get; set; }
    public EquipmentReceiveStatus EquipmentReceiveStatus { get; set; }
    public int? DepreciationPeriod { get; set; }
    public decimal? OriginalPrice { get; set; }
    public string? Location { get; set; }
    public int EquipmentIndex { get; set; }
}

public class CreateUpdateWarehouseImportReceiptDto
{
    [Required]
    public Guid GoodsReceiptId { get; set; }

    [Required]
    public DateTime ActualImportDate { get; set; }

    public Guid? WarehouseKeeperId { get; set; }

    [StringLength(200)]
    public string? WarehouseKeeperName { get; set; }

    public Guid? WarehouseId { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

    public Guid? SupplierId { get; set; }

    [StringLength(500)]
    public string? SupplierName { get; set; }

    public decimal? TotalAmount { get; set; }

    public List<DeliveryDocumentFileDto> DeliveryDocumentFiles { get; set; } = new();

    public List<CreateUpdateWarehouseImportReceiptItemDto> Items { get; set; } = new();
}

public class CreateUpdateWarehouseImportReceiptItemDto
{
    public Guid? Id { get; set; }

    [Required]
    public Guid GoodsReceiptItemId { get; set; }

    [Required]
    public decimal ActualReceivedQuantity { get; set; }

    [StringLength(100)]
    public string? LotNumber { get; set; }

    public DateTime? LotExpiryDate { get; set; }

    public WarehouseImportLineStatus Status { get; set; } = WarehouseImportLineStatus.Checked;

    [StringLength(50)]
    public string? QualityDocumentFileId { get; set; }

    [StringLength(500)]
    public string? QualityDocumentFileName { get; set; }

    [StringLength(500)]
    public string? QualityDocumentFilePath { get; set; }

    public long? QualityDocumentFileSize { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
    public Guid? SkuId { get; set; }
    public Guid? SelectedUnitId { get; set; }
    [StringLength(100)]
    public string? SelectedUnitName { get; set; }
    public Guid? StorageWarehouseId { get; set; }
    [StringLength(500)]
    public string? StorageWarehouseName { get; set; }

    // Pricing fields
    public decimal? UnitPrice { get; set; }
    public int? VatPercent { get; set; }
    public decimal? TotalLineAmount { get; set; }

    // Equipment fields (Nhóm 2)
    public bool IsEquipment { get; set; }
    [StringLength(100)]
    public string? AssetCode { get; set; }
    [StringLength(200)]
    public string? SerialNumber { get; set; }
    public Guid? ManufacturerId { get; set; }
    [StringLength(500)]
    public string? ManufacturerName { get; set; }
    public EquipmentReceiveStatus EquipmentReceiveStatus { get; set; }
    public int? DepreciationPeriod { get; set; }
    public decimal? OriginalPrice { get; set; }
    [StringLength(256)]
    public string? Location { get; set; }
    public int EquipmentIndex { get; set; }
}

public class ProductSkuForImportDto
{
    public Guid SkuId { get; set; }
    public Guid ManufacturerId { get; set; }
    public string ManufacturerName { get; set; } = string.Empty;
    /// <summary>SKU.BaseUnitId = Đơn vị mua hàng</summary>
    public Guid? PurchaseUnitId { get; set; }
    public string? PurchaseUnitName { get; set; }
    public int? PackagingQuantity { get; set; }
    /// <summary>SKU.PackagingUnitId = Đơn vị lưu kho</summary>
    public Guid? StorageUnitId { get; set; }
    public string? StorageUnitName { get; set; }
    public int? VolumePerUnit { get; set; }
    /// <summary>SKU.VolumeUnitId = Đơn vị tiêu hao</summary>
    public Guid? ConsumableUnitId { get; set; }
    public string? ConsumableUnitName { get; set; }
}

public class EligibleGoodsReceiptForImportDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public DateTime? ExpectedDeliveryDate { get; set; }
}

public class GoodsReceiptItemForImportDto
{
    public Guid GoodsReceiptItemId { get; set; }
    public Guid ProductId { get; set; }
    public string ProductName { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitName { get; set; }
    public decimal OrderedQuantity { get; set; }
    public decimal AlreadyReceivedQuantity { get; set; }
    public decimal RemainingOrderableQuantity { get; set; }
    /// <summary>Đơn vị lưu kho (base unit), chỉ set khi khác với đơn vị mua hàng (UnitId/UnitName).</summary>
    public string? BaseUnitName { get; set; }
    public decimal? BaseOrderedQuantity { get; set; }
    /// <summary>Các dòng đã nhập ở những phiếu nhập kho Hoàn thành trước đó, hiển thị read-only.</summary>
    public List<PreviousReceivedLotDto> PreviousLots { get; set; } = new();
    public List<ProductSkuForImportDto> Skus { get; set; } = new();
    public string? CASNumber { get; set; }
    public string? PackagingSpec { get; set; }

    // Equipment fields
    public bool IsEquipment { get; set; }
    public string? InstallationLocation { get; set; }
    public int? ProductType { get; set; }
    public Guid? ManufacturerId { get; set; }
    public string? ManufacturerName { get; set; }
}

public class DefectiveHistoryItemDto
{
    public Guid ReceiptId { get; set; }
    public string ReceiptCode { get; set; }
    public string? ProductCode { get; set; }
    public string? CasNumber { get; set; }
    public string? Manufacturer { get; set; }
    public string ProductName { get; set; }
    public decimal ActualReceivedQuantity { get; set; }
    public string? UnitName { get; set; }
    public string? Notes { get; set; }
    public string? CreatorName { get; set; }
    public DateTime ReceiptCreationTime { get; set; }
}

public class PreviousReceivedLotDto
{
    public string? LotNumber { get; set; }
    public DateTime? LotExpiryDate { get; set; }
    public decimal ActualReceivedQuantity { get; set; }
    public Guid? SkuId { get; set; }
    public string? UnitName { get; set; }
    public string? StorageWarehouseName { get; set; }
    public WarehouseImportLineStatus Status { get; set; }
    public string? QualityDocumentFileId { get; set; }
    public string? QualityDocumentFileName { get; set; }
    public long? QualityDocumentFileSize { get; set; }

    // Equipment fields
    public bool IsEquipment { get; set; }
    public int EquipmentIndex { get; set; }
    public string? AssetCode { get; set; }
    public string? SerialNumber { get; set; }
    public Guid? ManufacturerId { get; set; }
    public string? ManufacturerName { get; set; }
    public int EquipmentReceiveStatus { get; set; }
    public int? DepreciationPeriod { get; set; }
    public decimal? OriginalPrice { get; set; }
    public string? Location { get; set; }
}
