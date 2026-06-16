using System;
using LabSpecimenManagement.Common;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.Products;

/// <summary>
/// Vật phẩm / Hàng hóa phòng thí nghiệm
/// </summary>
public class Product : FullAuditedAggregateRoot<Guid>, IHasActive
{
    /// <summary>
    /// Mã hàng hóa (auto-generated: CHE-20260330-0001)
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Tên hàng hóa
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Tên hàng hóa có định dạng HTML (rich text)
    /// </summary>
    public string? FormattedName { get; set; }

    /// <summary>
    /// Mã nhận dạng
    /// </summary>
    public string? IdentificationCode { get; set; }

    /// <summary>
    /// Loại sản phẩm
    /// </summary>
    public ProductType ProductType { get; set; }

    /// <summary>
    /// ID đơn vị tính cơ bản (FK → Unit)
    /// </summary>
    public Guid BaseUnitId { get; set; }

    /// <summary>
    /// Quy cách đóng gói (Size)
    /// </summary>
    public string? PackagingSpec { get; set; }

    /// <summary>
    /// Số CAS (định danh hóa chất)
    /// </summary>
    public string? CASNumber { get; set; }

    /// <summary>
    /// Nhà sản xuất / Thương hiệu
    /// </summary>
    public string? Manufacturer { get; set; }

    /// <summary>
    /// Xuất xứ (nước)
    /// </summary>
    public string? Origin { get; set; }

    /// <summary>
    /// Nồng độ / Hàm lượng
    /// </summary>
    public string? Concentration { get; set; }

    /// <summary>
    /// Điều kiện bảo quản (plain text)
    /// </summary>
    public string? StorageConditions { get; set; }

    /// <summary>
    /// Điều kiện bảo quản có định dạng HTML (rich text)
    /// </summary>
    public string? FormattedStorageConditions { get; set; }

    /// <summary>
    /// Nhóm / Phân loại (JSON array tags)
    /// </summary>
    public string? Categories { get; set; }

    /// <summary>
    /// Mức tồn kho tối thiểu
    /// </summary>
    public decimal? MinStockLevel { get; set; }

    /// <summary>
    /// Mức tồn kho tối đa
    /// </summary>
    public decimal? MaxStockLevel { get; set; }

    /// <summary>
    /// Số lượng khả dụng (mới tạo = 0, tăng khi nhập hàng)
    /// </summary>
    public decimal AvailableQuantity { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Trạng thái nháp (chưa hoàn thiện)
    /// </summary>
    public bool IsDraft { get; set; }

    protected Product()
    {
    }

    public Product(Guid id, string code, string name, ProductType productType, Guid baseUnitId) : base(id)
    {
        Code = code;
        Name = name;
        ProductType = productType;
        BaseUnitId = baseUnitId;
        AvailableQuantity = 0;
        IsActive = true;
        IsDraft = false;
    }
}
