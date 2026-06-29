using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.WarehouseImports;

public class WarehouseImportReceiptListDto : EntityDto<Guid>
{
    public string Code { get; set; }
    public Guid GoodsReceiptId { get; set; }
    public string GoodsReceiptCode { get; set; }
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
}

public class WarehouseImportReceiptStatisticsDto
{
    public int TotalReceipts { get; set; }
    public int DefectiveLots { get; set; }
}

public class WarehouseImportReceiptDto : EntityDto<Guid>
{
    public string Code { get; set; }
    public Guid GoodsReceiptId { get; set; }
    public string GoodsReceiptCode { get; set; }
    public DateTime ActualImportDate { get; set; }
    public Guid? WarehouseKeeperId { get; set; }
    public string? WarehouseKeeperName { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public string? DeliveryDocumentFileId { get; set; }
    public string? DeliveryDocumentFileName { get; set; }
    public string? DeliveryDocumentFilePath { get; set; }
    public long? DeliveryDocumentFileSize { get; set; }
    public string? Notes { get; set; }
    public WarehouseImportReceiptStatus Status { get; set; }
    public bool HasDefectiveItem { get; set; }
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

    [StringLength(50)]
    public string? DeliveryDocumentFileId { get; set; }

    [StringLength(500)]
    public string? DeliveryDocumentFileName { get; set; }

    [StringLength(500)]
    public string? DeliveryDocumentFilePath { get; set; }

    public long? DeliveryDocumentFileSize { get; set; }

    [StringLength(2000)]
    public string? Notes { get; set; }

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
}

public class EligibleGoodsReceiptForImportDto
{
    public Guid Id { get; set; }
    public string Code { get; set; }
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
}

public class DefectiveHistoryItemDto
{
    public Guid ReceiptId { get; set; }
    public string ReceiptCode { get; set; }
    public string? ProductCode { get; set; }
    public string ProductName { get; set; }
    public decimal ActualReceivedQuantity { get; set; }
    public string? Notes { get; set; }
    public string? CreatorName { get; set; }
    public DateTime ReceiptCreationTime { get; set; }
}

public class PreviousReceivedLotDto
{
    public string? LotNumber { get; set; }
    public DateTime? LotExpiryDate { get; set; }
    public decimal ActualReceivedQuantity { get; set; }
    public WarehouseImportLineStatus Status { get; set; }
    public string? QualityDocumentFileId { get; set; }
    public string? QualityDocumentFileName { get; set; }
    public long? QualityDocumentFileSize { get; set; }
}
