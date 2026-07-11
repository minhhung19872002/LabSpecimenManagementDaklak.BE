using System;
using System.Collections.Generic;
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
    public Guid? BaseUnitId { get; set; }

    public int? PackagingQuantity { get; set; }
    public Guid? PackagingUnitId { get; set; }
    public int? VolumePerUnit { get; set; }
    public Guid? VolumeUnitId { get; set; }

    /// <summary>
    /// Quy cách đóng gói (auto-computed)
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
    /// Nồng độ / Hàm lượng có định dạng HTML (rich text, dùng cho Sinh phẩm)
    /// </summary>
    public string? FormattedConcentration { get; set; }

    /// <summary>
    /// Xuất xứ có định dạng HTML (rich text, dùng cho Sinh phẩm)
    /// </summary>
    public string? FormattedOrigin { get; set; }

    /// <summary>
    /// Mã nguy hiểm GHS/MSDS (dùng cho Sinh phẩm, Chất chuẩn)
    /// </summary>
    public string? GhsCode { get; set; }

    /// <summary>
    /// Loại chất chuẩn (Chuẩn chứng nhận, Chuẩn thường, Chuẩn làm việc)
    /// </summary>
    public StandardCertificationType? StandardCertificationType { get; set; }

    /// <summary>
    /// Công thức hóa học có định dạng HTML (rich text, dùng cho Chất chuẩn)
    /// </summary>
    public string? FormattedChemicalFormula { get; set; }

    /// <summary>
    /// Giá trị chứng nhận (plain text backup)
    /// </summary>
    public string? CertifiedValue { get; set; }

    /// <summary>
    /// Giá trị chứng nhận có định dạng HTML (rich text, dùng cho Chất chuẩn)
    /// </summary>
    public string? FormattedCertifiedValue { get; set; }

    /// <summary>
    /// Độ không đảm bảo đo (plain text backup)
    /// </summary>
    public string? MeasurementUncertainty { get; set; }

    /// <summary>
    /// Độ không đảm bảo đo có định dạng HTML (rich text, dùng cho Chất chuẩn)
    /// </summary>
    public string? FormattedMeasurementUncertainty { get; set; }

    /// <summary>
    /// Liên kết chất chuẩn (URL)
    /// </summary>
    public string? StandardLink { get; set; }

    /// <summary>
    /// Cấp độ phân tích
    /// </summary>
    public string? AnalysisGrade { get; set; }

    /// <summary>
    /// Thông số kích thước Dung tích có định dạng HTML (rich text, dùng cho Vật tư tiêu hao)
    /// </summary>
    public string? FormattedPackagingSpec { get; set; }

    /// <summary>
    /// Vật liệu cấu thành (plain text backup, dùng cho Vật tư tiêu hao)
    /// </summary>
    public string? MaterialComposition { get; set; }

    /// <summary>
    /// Vật liệu cấu thành có định dạng HTML (rich text, dùng cho Vật tư tiêu hao)
    /// </summary>
    public string? FormattedMaterialComposition { get; set; }

    /// <summary>
    /// Khả năng tái sử dụng (null=chưa chọn, true=Có, false=Không)
    /// </summary>
    public bool? IsReusable { get; set; }

    /// <summary>
    /// Số lần tái sử dụng (chỉ áp dụng khi IsReusable=true)
    /// </summary>
    public int? MaxReuseCount { get; set; }

    /// <summary>
    /// Trạng thái vô trùng (null=chưa chọn, true=Đã tiệt trùng, false=Chưa tiệt trùng)
    /// </summary>
    public bool? IsSterile { get; set; }

    /// <summary>
    /// Mục đích sử dụng (dùng cho Dung dịch pha)
    /// </summary>
    public string? UsagePurpose { get; set; }

    /// <summary>
    /// Hạn sử dụng tính theo ngày (dùng cho Dung dịch pha)
    /// </summary>
    public int? ShelfLifeDays { get; set; }

    /// <summary>
    /// ID chất nền (FK tự tham chiếu → Product, dùng cho Dung dịch pha)
    /// </summary>
    public Guid? BaseProductId { get; set; }

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

    /// <summary>
    /// Công thức hóa học (chỉ dùng cho Hóa chất)
    /// </summary>
    public string? ChemicalFormula { get; set; }

    /// <summary>
    /// Định mức tồn kho tối thiểu
    /// </summary>
    public decimal? MinStockLevel { get; set; }

    public virtual ICollection<ProductSku> Skus { get; set; } = new List<ProductSku>();

    protected Product()
    {
    }

    public Product(Guid id, string code, string name, ProductType productType, Guid? baseUnitId) : base(id)
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
