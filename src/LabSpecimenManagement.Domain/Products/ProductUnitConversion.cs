using System;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.Products;

/// <summary>
/// Đơn vị quy đổi sản phẩm
/// Ví dụ: 12 Chai = 1 Thùng
/// </summary>
public class ProductUnitConversion : Entity<Guid>
{
    public Guid ProductId { get; set; }

    /// <summary>
    /// Số lượng đơn vị chính (vd: 12)
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// ID đơn vị chính (vd: Chai)
    /// </summary>
    public Guid BaseUnitId { get; set; }

    /// <summary>
    /// Số lượng đơn vị quy đổi (vd: 1)
    /// </summary>
    public decimal ConvertedQuantity { get; set; }

    /// <summary>
    /// ID đơn vị quy đổi (vd: Thùng)
    /// </summary>
    public Guid ConvertedUnitId { get; set; }

    protected ProductUnitConversion()
    {
    }

    public ProductUnitConversion(Guid id, Guid productId, decimal quantity, Guid baseUnitId, decimal convertedQuantity, Guid convertedUnitId) : base(id)
    {
        ProductId = productId;
        Quantity = quantity;
        BaseUnitId = baseUnitId;
        ConvertedQuantity = convertedQuantity;
        ConvertedUnitId = convertedUnitId;
    }
}
