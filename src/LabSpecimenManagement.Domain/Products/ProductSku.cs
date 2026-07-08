using System;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.Products;

/// <summary>
/// SKU sản phẩm theo hãng sản xuất
/// Mỗi SKU = 1 hãng sản xuất + quy cách đóng gói riêng
/// </summary>
public class ProductSku : Entity<Guid>, IHasCreationTime
{
    public Guid ProductId { get; set; }

    /// <summary>
    /// FK → Manufacturer
    /// </summary>
    public Guid ManufacturerId { get; set; }

    /// <summary>
    /// Đơn vị mua hàng (FK → Unit)
    /// </summary>
    public Guid? BaseUnitId { get; set; }

    /// <summary>
    /// Số lượng trong quy cách đóng gói
    /// </summary>
    public int? PackagingQuantity { get; set; }

    /// <summary>
    /// Đơn vị lưu kho (FK → Unit)
    /// </summary>
    public Guid? PackagingUnitId { get; set; }

    /// <summary>
    /// Dung tích mỗi đơn vị
    /// </summary>
    public int? VolumePerUnit { get; set; }

    /// <summary>
    /// Đơn vị tiêu hao (FK → Unit)
    /// </summary>
    public Guid? VolumeUnitId { get; set; }

    public DateTime CreationTime { get; set; }

    protected ProductSku()
    {
    }

    public ProductSku(
        Guid id,
        Guid productId,
        Guid manufacturerId,
        Guid? baseUnitId,
        int? packagingQuantity,
        Guid? packagingUnitId,
        int? volumePerUnit,
        Guid? volumeUnitId) : base(id)
    {
        ProductId = productId;
        ManufacturerId = manufacturerId;
        BaseUnitId = baseUnitId;
        PackagingQuantity = packagingQuantity;
        PackagingUnitId = packagingUnitId;
        VolumePerUnit = volumePerUnit;
        VolumeUnitId = volumeUnitId;
    }
}
