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
    public string? FormattedConcentration { get; set; }
    public string? FormattedOrigin { get; set; }
    public string? GhsCode { get; set; }
    public int? StandardCertificationType { get; set; }
    public string? FormattedChemicalFormula { get; set; }
    public string? CertifiedValue { get; set; }
    public string? FormattedCertifiedValue { get; set; }
    public string? MeasurementUncertainty { get; set; }
    public string? FormattedMeasurementUncertainty { get; set; }
    public string? StandardLink { get; set; }
    public string? AnalysisGrade { get; set; }
    public string? FormattedPackagingSpec { get; set; }
    public string? MaterialComposition { get; set; }
    public string? FormattedMaterialComposition { get; set; }
    public bool? IsReusable { get; set; }
    public int? MaxReuseCount { get; set; }
    public bool? IsSterile { get; set; }
    public string? UsagePurpose { get; set; }
    public int? ShelfLifeDays { get; set; }
    public Guid? BaseProductId { get; set; }
    public string? StorageConditions { get; set; }
    public string? FormattedStorageConditions { get; set; }
    public string? Categories { get; set; }
    public string? ChemicalFormula { get; set; }
    public decimal? MinStockLevel { get; set; }
    public string? FormattedIdentificationCode { get; set; }
    public string? EquipmentModel { get; set; }
    public string? SerialNumber { get; set; }
    public int? ManufactureYear { get; set; }
    public string? MainTechnicalSpecs { get; set; }
    public string? InstallationLocation { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal StorageAvailableQuantity { get; set; }
    public decimal ConsumableImportedQuantity { get; set; }
    public bool IsActive { get; set; }
    public bool IsDraft { get; set; }
    public int SkuCount { get; set; }
    public string? WarehouseName { get; set; }

    public DateTime? EarliestReceivedDate { get; set; }
    public string? EarliestLotNumber { get; set; }
    public DateTime? EarliestLotExpiryDate { get; set; }
    public string? EarliestLotNotes { get; set; }

    public decimal TotalConsumedQuantity { get; set; }
    public decimal StorageTotalConsumedQuantity { get; set; }
    public List<ProductAttachmentDto> Attachments { get; set; } = new();
    public List<ProductUnitConversionDto> UnitConversions { get; set; } = new();
    public List<ProductSkuDto> Skus { get; set; } = new();
}

/// <summary>
/// DTO cho SKU sản phẩm
/// </summary>
public class ProductSkuDto
{
    public Guid Id { get; set; }
    public Guid ManufacturerId { get; set; }
    public string? ManufacturerCode { get; set; }
    public string? ManufacturerName { get; set; }
    public Guid? BaseUnitId { get; set; }
    public string? BaseUnitName { get; set; }
    public int? PackagingQuantity { get; set; }
    public Guid? PackagingUnitId { get; set; }
    public string? PackagingUnitName { get; set; }
    public int? VolumePerUnit { get; set; }
    public Guid? VolumeUnitId { get; set; }
    public string? VolumeUnitName { get; set; }
    public DateTime CreationTime { get; set; }
}

/// <summary>
/// DTO tạo/cập nhật SKU
/// </summary>
public class CreateUpdateProductSkuDto
{
    public Guid? Id { get; set; }
    public Guid ManufacturerId { get; set; }
    public Guid? BaseUnitId { get; set; }
    public int? PackagingQuantity { get; set; }
    public Guid? PackagingUnitId { get; set; }
    public int? VolumePerUnit { get; set; }
    public Guid? VolumeUnitId { get; set; }
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

    public string? PackagingSpec { get; set; }

    [StringLength(100)]
    public string? CASNumber { get; set; }

    [StringLength(256)]
    public string? Manufacturer { get; set; }

    [StringLength(256)]
    public string? Origin { get; set; }

    [StringLength(256)]
    public string? Concentration { get; set; }

    public string? FormattedConcentration { get; set; }

    public string? FormattedOrigin { get; set; }

    [StringLength(128)]
    public string? GhsCode { get; set; }

    public int? StandardCertificationType { get; set; }

    public string? FormattedChemicalFormula { get; set; }

    public string? CertifiedValue { get; set; }

    public string? FormattedCertifiedValue { get; set; }

    public string? MeasurementUncertainty { get; set; }

    public string? FormattedMeasurementUncertainty { get; set; }

    [StringLength(500)]
    public string? StandardLink { get; set; }

    [StringLength(256)]
    public string? AnalysisGrade { get; set; }

    public string? FormattedPackagingSpec { get; set; }

    public string? MaterialComposition { get; set; }

    public string? FormattedMaterialComposition { get; set; }

    public bool? IsReusable { get; set; }

    public int? MaxReuseCount { get; set; }

    public bool? IsSterile { get; set; }

    public string? UsagePurpose { get; set; }

    public int? ShelfLifeDays { get; set; }

    public Guid? BaseProductId { get; set; }

    public string? StorageConditions { get; set; }

    public string? FormattedStorageConditions { get; set; }

    public string? Categories { get; set; }

    [StringLength(256)]
    public string? ChemicalFormula { get; set; }

    public decimal? MinStockLevel { get; set; }

    public string? FormattedIdentificationCode { get; set; }

    [StringLength(256)]
    public string? EquipmentModel { get; set; }

    [StringLength(256)]
    public string? SerialNumber { get; set; }

    public int? ManufactureYear { get; set; }

    public string? MainTechnicalSpecs { get; set; }

    [StringLength(256)]
    public string? InstallationLocation { get; set; }

    public bool IsActive { get; set; } = true;
    public bool IsDraft { get; set; } = false;

    public List<CreateProductAttachmentDto>? Attachments { get; set; }
    public List<CreateUpdateProductUnitConversionDto>? UnitConversions { get; set; }
    public List<CreateUpdateProductSkuDto>? Skus { get; set; }
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

public class ProductDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? FormattedName { get; set; }
    public string? IdentificationCode { get; set; }
    public string? CASNumber { get; set; }
    public string? StorageConditions { get; set; }
    public string? FormattedStorageConditions { get; set; }
    public string? MainTechnicalSpecs { get; set; }
    public ProductType ProductType { get; set; }
    public decimal WarehouseAvailableQuantity { get; set; }
    public string? BaseUnitName { get; set; }
    public string? PackagingUnitName { get; set; }
    public int? PackagingQuantity { get; set; }
}

public class ProductImportHistoryDto
{
    public Guid WarehouseImportReceiptId { get; set; }
    public string ReceiptCode { get; set; } = null!;
    public DateTime? ActualImportDate { get; set; }
    public string? SupplierName { get; set; }
    public decimal OrderedQuantity { get; set; }
    public string? PurchaseUnitName { get; set; }
    public decimal ActualReceivedQuantity { get; set; }
    public string? UnitName { get; set; }
    public string? ManufacturerName { get; set; }
    public string? LotNumber { get; set; }
    public DateTime? LotExpiryDate { get; set; }
    public decimal? TotalAmount { get; set; }
    public string? QualityDocumentFileId { get; set; }
    public string? QualityDocumentFileName { get; set; }
    public string? QualityDocumentFilePath { get; set; }
    public List<WarehouseImports.DeliveryDocumentFileDto>? DeliveryDocumentFiles { get; set; }
}

public class GetProductImportHistoryInput : PagedRequestDto
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public Guid? WarehouseId { get; set; }
}
