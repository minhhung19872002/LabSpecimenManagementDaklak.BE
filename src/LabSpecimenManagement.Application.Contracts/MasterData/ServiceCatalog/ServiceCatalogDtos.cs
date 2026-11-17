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

    public Guid? SpecimenTypeId { get; set; }
    public string? SpecimenTypeCode { get; set; }
    public string? SpecimenTypeName { get; set; }

    public string? Unit { get; set; }
    public string? FormattedUnit { get; set; }
    public bool IsActive { get; set; }
    public bool IsSubcontract { get; set; }

    /// <summary>Giá đang áp dụng hôm nay (đơn giá dùng khi lập phiếu / báo giá)</summary>
    public decimal UnitPrice { get; set; }
    public int? TurnaroundDays { get; set; }

    /// <summary>Giá / TAT tại ngày đang xem (bộ lọc "Giá tại ngày")</summary>
    public decimal PriceAtDate { get; set; }
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

public class BulkCreateServiceRowInput
{
    [Required]
    public Guid IndicatorId { get; set; }

    /// <summary>Mỗi nền mẫu = 1 dịch vụ</summary>
    [Required]
    public List<Guid> SpecimenTypeIds { get; set; } = new();
}

public class BulkCreateServicesInput
{
    [Required]
    public Guid TestMethodId { get; set; }

    [Required]
    public List<BulkCreateServiceRowInput> Rows { get; set; } = new();

    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }

    [Range(0, 3650)]
    public int TurnaroundDays { get; set; }

    [Required]
    public DateTime EffectiveDate { get; set; }

    public bool IsSubcontract { get; set; }
}

public class BulkCreateServicesResultDto
{
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
