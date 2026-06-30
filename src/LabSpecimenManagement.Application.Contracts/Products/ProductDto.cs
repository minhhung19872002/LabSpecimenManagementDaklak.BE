using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LabSpecimenManagement.Common;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.Products;

/// <summary>
/// DTO cho Vật phẩm
/// </summary>
public class ProductDto : FullAuditedEntityDto<Guid>
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string? FormattedName { get; set; }
    public string? IdentificationCode { get; set; }
    public ProductType ProductType { get; set; }
    public Guid? BaseUnitId { get; set; }
    public string? BaseUnitName { get; set; }
    public int? PackagingQuantity { get; set; }
    public Guid? PackagingUnitId { get; set; }
    public string? PackagingUnitName { get; set; }
    public int? VolumePerUnit { get; set; }
    public Guid? VolumeUnitId { get; set; }
    public string? VolumeUnitName { get; set; }
    public string? PackagingSpec { get; set; }
    public string? CASNumber { get; set; }
    public string? Manufacturer { get; set; }
    public string? Origin { get; set; }
    public string? Concentration { get; set; }
    public string? StorageConditions { get; set; }
    public string? FormattedStorageConditions { get; set; }
    public string? Categories { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal StorageAvailableQuantity { get; set; }
    public bool IsActive { get; set; }
    public bool IsDraft { get; set; }
    public string? WarehouseName { get; set; }

    public DateTime? EarliestReceivedDate { get; set; }
    public string? EarliestLotNumber { get; set; }
    public DateTime? EarliestLotExpiryDate { get; set; }
    public string? EarliestLotNotes { get; set; }

    public decimal TotalConsumedQuantity { get; set; }
    public decimal StorageTotalConsumedQuantity { get; set; }
    public List<ProductAttachmentDto> Attachments { get; set; } = new();
    public List<ProductUnitConversionDto> UnitConversions { get; set; } = new();
}

/// <summary>
/// DTO tạo/cập nhật Vật phẩm
/// </summary>
public class CreateUpdateProductDto
{
    [StringLength(500, ErrorMessage = "Tên hàng hóa không được vượt quá 500 ký tự")]
    public string? Name { get; set; }

    public string? FormattedName { get; set; }

    [StringLength(256)]
    public string? IdentificationCode { get; set; }

    public ProductType ProductType { get; set; }

    public Guid? BaseUnitId { get; set; }

    public int? PackagingQuantity { get; set; }
    public Guid? PackagingUnitId { get; set; }
    public int? VolumePerUnit { get; set; }
    public Guid? VolumeUnitId { get; set; }

    [StringLength(256)]
    public string? PackagingSpec { get; set; }

    [StringLength(100)]
    public string? CASNumber { get; set; }

    [StringLength(256)]
    public string? Manufacturer { get; set; }

    [StringLength(256)]
    public string? Origin { get; set; }

    [StringLength(256)]
    public string? Concentration { get; set; }

    public string? StorageConditions { get; set; }

    public string? FormattedStorageConditions { get; set; }

    public string? Categories { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDraft { get; set; } = false;

    public List<CreateProductAttachmentDto>? Attachments { get; set; }
    public List<CreateUpdateProductUnitConversionDto>? UnitConversions { get; set; }
}

/// <summary>
/// DTO tài liệu đính kèm
/// </summary>
public class ProductAttachmentDto
{
    public Guid Id { get; set; }
    public string FileId { get; set; }
    public string FileName { get; set; }
    public long FileSize { get; set; }
    public string? FileType { get; set; }
    public string Url { get; set; }
    public DateTime CreationTime { get; set; }
}

public class CreateProductAttachmentDto
{
    [Required]
    public string FileId { get; set; }

    [Required]
    public string FileName { get; set; }

    public long FileSize { get; set; }
    public string? FileType { get; set; }
}

/// <summary>
/// DTO đơn vị quy đổi
/// </summary>
public class ProductUnitConversionDto
{
    public Guid Id { get; set; }
    public decimal Quantity { get; set; }
    public Guid BaseUnitId { get; set; }
    public string? BaseUnitName { get; set; }
    public decimal ConvertedQuantity { get; set; }
    public Guid ConvertedUnitId { get; set; }
    public string? ConvertedUnitName { get; set; }
}

public class CreateUpdateProductUnitConversionDto
{
    public Guid? Id { get; set; }
    public decimal Quantity { get; set; }
    public Guid BaseUnitId { get; set; }
    public decimal ConvertedQuantity { get; set; }
    public Guid ConvertedUnitId { get; set; }
}

/// <summary>
/// DTO filter danh sách sản phẩm
/// </summary>
public class ProductFilterDto : PagedRequestDto
{
    public string? Search { get; set; }
    public ProductType? ProductType { get; set; }
    public bool? IsDraft { get; set; }
    public Guid? WarehouseId { get; set; }
}

/// <summary>
/// DTO nguồn gốc nhập hàng của sản phẩm (group by NCC)
/// </summary>
public class ProductSourceDto
{
    public Guid SupplierId { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public decimal TotalImported { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal? LatestUnitPrice { get; set; }
}

public class GetProductSourcesInput : PagedRequestDto
{
    public string? Search { get; set; }
}
