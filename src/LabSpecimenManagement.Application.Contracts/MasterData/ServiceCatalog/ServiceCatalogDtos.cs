using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.MasterData.ServiceCatalog;

/// <summary>
/// 1 dịch vụ = Phương pháp + Chỉ tiêu + Nền mẫu (bản ghi TestMethodIndicator)
/// </summary>
public class ServiceCatalogItemDto
{
    public Guid Id { get; set; }
    public string? ServiceCode { get; set; }

    public Guid TestMethodId { get; set; }
    public string TestMethodCode { get; set; } = "";
    public string TestMethodName { get; set; } = "";
    public string? TestMethodFormattedName { get; set; }

    public Guid IndicatorId { get; set; }
    public string IndicatorCode { get; set; } = "";
    public string IndicatorName { get; set; } = "";
    public string? IndicatorFormattedName { get; set; }
    public Guid? ParentIndicatorId { get; set; }
    public Guid? IndicatorGroupId { get; set; }
    public string? IndicatorGroupName { get; set; }

    /// <summary>Dịch vụ cấp 1. null = dịch vụ cũ "Chưa thuộc dịch vụ"</summary>
    public Guid? ServiceGroupId { get; set; }
    public string? ServiceGroupCode { get; set; }
    public string? ServiceGroupName { get; set; }

    public Guid? SpecimenTypeId { get; set; }
    public string? SpecimenTypeCode { get; set; }
    public string? SpecimenTypeName { get; set; }

    public string? Unit { get; set; }
    public string? FormattedUnit { get; set; }
    public bool IsActive { get; set; }
    public bool IsSubcontract { get; set; }
    public Guid? SubcontractorId { get; set; }
    public string? SubcontractorName { get; set; }

    /// <summary>Giá đang áp dụng hôm nay (đơn giá dùng khi lập phiếu / báo giá). null = Chờ định giá</summary>
    public decimal? UnitPrice { get; set; }
    public int? TurnaroundDays { get; set; }

    /// <summary>Giá / TAT tại ngày đang xem (bộ lọc "Giá tại ngày"). null = Chờ định giá</summary>
    public decimal? PriceAtDate { get; set; }
    public int? TurnaroundDaysAtDate { get; set; }
    public DateTime? EffectiveDateAtDate { get; set; }

    /// <summary>Phiên bản giá sắp áp dụng gần nhất (sau ngày đang xem)</summary>
    public decimal? UpcomingPrice { get; set; }
    public DateTime? UpcomingEffectiveDate { get; set; }

    public int VersionCount { get; set; }
}

public class GetServiceCatalogInput
{
    public Guid? TestMethodId { get; set; }
    public Guid? IndicatorGroupId { get; set; }
    public string? Keyword { get; set; }

    /// <summary>Ngày xem giá (mặc định hôm nay)</summary>
    public DateTime? PriceDate { get; set; }

    /// <summary>null = tất cả, true = đang hoạt động, false = ngừng</summary>
    public bool? IsActive { get; set; }
}

public enum ServicePriceVersionStatus
{
    /// <summary>Đang áp dụng</summary>
    Current = 0,
    /// <summary>Sắp áp dụng</summary>
    Upcoming = 1,
    /// <summary>Đã thay thế</summary>
    Replaced = 2,
}

public class ServicePriceVersionDto
{
    public Guid Id { get; set; }
    public Guid TestMethodIndicatorId { get; set; }
    public decimal UnitPrice { get; set; }
    public int? TurnaroundDays { get; set; }

    /// <summary>null = giá gốc (trước khi có bảng giá theo phiên bản)</summary>
    public DateTime? EffectiveDate { get; set; }
    public string? Note { get; set; }
    public ServicePriceVersionStatus Status { get; set; }
    public DateTime CreationTime { get; set; }
    public string? CreatorName { get; set; }
}

/// <summary>1 ô của Phiếu khai báo dịch vụ = 1 dịch vụ (chỉ tiêu × nền mẫu)</summary>
public class BulkCreateServiceItemInput
{
    [Required]
    public Guid IndicatorId { get; set; }

    /// <summary>null = Mọi nền mẫu</summary>
    public Guid? SpecimenTypeId { get; set; }

    /// <summary>null = Chờ định giá</summary>
    [Range(0, double.MaxValue)]
    public decimal? UnitPrice { get; set; }

    /// <summary>ĐVT (tiêu hao) — trống = theo đơn vị của phương pháp</summary>
    [StringLength(100)]
    public string? Unit { get; set; }

    public bool IsSubcontract { get; set; }

    /// <summary>Đơn vị nhận thầu — không bắt buộc, chỉ lưu khi IsSubcontract</summary>
    public Guid? SubcontractorId { get; set; }
}

/// <summary>
/// Phiếu khai báo dịch vụ: giá có hiệu lực ngay (hôm nay), không có TAT — sửa sau trên Ma trận giá.
/// </summary>
public class BulkCreateServicesInput
{
    [Required]
    public Guid TestMethodId { get; set; }

    /// <summary>Dịch vụ có sẵn. Bỏ trống thì phải nhập ServiceGroupName</summary>
    public Guid? ServiceGroupId { get; set; }

    /// <summary>Tên dịch vụ mới — tạo dịch vụ mới (trùng tên dịch vụ có sẵn thì dùng lại dịch vụ đó)</summary>
    [StringLength(256)]
    public string? ServiceGroupName { get; set; }

    [Required]
    public List<BulkCreateServiceItemInput> Items { get; set; } = new();
}

/// <summary>Dịch vụ cấp 1 (vd "Kiểm nghiệm thực phẩm")</summary>
public class ServiceGroupDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
}

public class CreateServiceGroupInput
{
    [Required]
    [StringLength(256)]
    public string Name { get; set; } = "";
}

/// <summary>Chuyển dịch vụ (dòng PP × chỉ tiêu × nền mẫu) sang tên dịch vụ khác và/hoặc đổi HĐ phụ</summary>
public class AssignServicesInput
{
    [Required]
    [MinLength(1)]
    public List<Guid> ServiceIds { get; set; } = new();

    /// <summary>null = "Chưa thuộc dịch vụ"</summary>
    public Guid? ServiceGroupId { get; set; }

    /// <summary>Đổi nhóm dịch vụ (false = giữ nguyên nhóm, chỉ đổi HĐ phụ)</summary>
    public bool ChangeServiceGroup { get; set; } = true;

    /// <summary>null = giữ nguyên HĐ phụ</summary>
    public bool? IsSubcontract { get; set; }
}

public class BulkCreateServicesResultDto
{
    public Guid? ServiceGroupId { get; set; }

    public int CreatedCount { get; set; }

    /// <summary>Tổ hợp đã có dịch vụ → bỏ qua</summary>
    public int SkippedCount { get; set; }
}

public class SetServicePriceInput
{
    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [Range(0, 3650)]
    public int? TurnaroundDays { get; set; }

    /// <summary>Ngày hiệu lực của mức giá (mặc định hôm nay). Trùng ngày với version có sẵn → cập nhật version đó.</summary>
    public DateTime? EffectiveDate { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }
}

public class UpdateServiceInput
{
    public string? Unit { get; set; }
    public string? FormattedUnit { get; set; }
    public bool IsSubcontract { get; set; }

    /// <summary>Đơn vị nhận thầu — bị xóa khi bỏ tích HĐ phụ</summary>
    public Guid? SubcontractorId { get; set; }
}

public enum BulkPriceAdjustMode
{
    /// <summary>Theo %</summary>
    Percent = 0,
    /// <summary>Theo số tiền</summary>
    Amount = 1,
}

public class BulkUpdatePriceInput
{
    [Required]
    public List<Guid> ServiceIds { get; set; } = new();

    public BulkPriceAdjustMode Mode { get; set; }

    /// <summary>true = tăng, false = giảm</summary>
    public bool Increase { get; set; } = true;

    [Range(0, double.MaxValue)]
    public decimal Value { get; set; }

    [Required]
    public DateTime EffectiveDate { get; set; }

    /// <summary>Giữ TAT hiện tại nếu null</summary>
    public int? TurnaroundDays { get; set; }
}

public class BulkUpdatePriceResultDto
{
    public int UpdatedCount { get; set; }
}

public class ImportServicePriceRowInput
{
    /// <summary>Số dòng trong file Excel (để báo lỗi)</summary>
    public int Line { get; set; }
    public string? ServiceCode { get; set; }
    public decimal? UnitPrice { get; set; }
    public int? TurnaroundDays { get; set; }
    public DateTime? EffectiveDate { get; set; }
}

public class ImportServicePricesInput
{
    [Required]
    public List<ImportServicePriceRowInput> Rows { get; set; } = new();

    /// <summary>true = chỉ kiểm tra, không ghi</summary>
    public bool DryRun { get; set; } = true;
}

public enum ImportServicePriceRowResult
{
    Update = 0,
    Same = 1,
    Error = 2,
}

public class ImportServicePriceRowDto
{
    public int Line { get; set; }
    public string? ServiceCode { get; set; }
    public string? ServiceName { get; set; }
    public decimal? OldPrice { get; set; }
    public decimal? NewPrice { get; set; }
    public int? OldTurnaroundDays { get; set; }
    public int? NewTurnaroundDays { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public ImportServicePriceRowResult Result { get; set; }
    public string? Message { get; set; }
}

public class ImportServicePricesResultDto
{
    public List<ImportServicePriceRowDto> Rows { get; set; } = new();
    public int UpdateCount { get; set; }
    public int SameCount { get; set; }
    public int ErrorCount { get; set; }
    public bool Applied { get; set; }
}
