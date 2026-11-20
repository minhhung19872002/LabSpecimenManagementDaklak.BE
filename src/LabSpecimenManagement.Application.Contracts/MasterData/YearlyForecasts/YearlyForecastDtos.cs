using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace LabSpecimenManagement.MasterData.YearlyForecasts;

public class GetYearlyForecastsInput
{
    [Required]
    public int Year { get; set; }

    public Guid? TestMethodId { get; set; }
    public string? Keyword { get; set; }

    /// <summary>Chỉ hiện dòng thiếu dự toán</summary>
    public bool OnlyMissing { get; set; }
}

/// <summary>
/// "Xem tính toán": 1 vật tư trong định mức của phương pháp ÷ số mẫu dự toán
/// </summary>
public class ForecastNormLineDto
{
    public Guid MethodBatchNormId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public string? UnitName { get; set; }
    public decimal BatchQuantity { get; set; }
    public decimal? PerSample { get; set; }
}

public class YearlyForecastRowDto
{
    /// <summary>Id dịch vụ (TestMethodIndicator)</summary>
    public Guid TestMethodIndicatorId { get; set; }
    public string? ServiceCode { get; set; }

    public Guid TestMethodId { get; set; }
    public string TestMethodCode { get; set; } = "";
    public string TestMethodName { get; set; } = "";

    public Guid IndicatorId { get; set; }
    public string IndicatorCode { get; set; } = "";
    public string IndicatorName { get; set; } = "";
    public string? IndicatorFormattedName { get; set; }

    public Guid? SpecimenTypeId { get; set; }
    public string? SpecimenTypeName { get; set; }
    public int SpecimenTypeSortOrder { get; set; }

    /// <summary>Nhóm nền mẫu (thẻ cấp 1 trên L4)</summary>
    public Guid? SampleTypeCategoryId { get; set; }
    public string? SampleTypeCategoryName { get; set; }

    public int? PreviousYearSamples { get; set; }
    public int? EstimatedSamples { get; set; }

    public List<ForecastNormLineDto> NormLines { get; set; } = new();
}

public class YearlyForecastListDto
{
    public int Year { get; set; }

    /// <summary>Năm đã qua → chỉ xem (BR-07)</summary>
    public bool IsLocked { get; set; }

    /// <summary>Năm trước có dữ liệu → hiện nút "Clone từ năm {Year-1}"</summary>
    public bool CanClone { get; set; }

    public int TotalCount { get; set; }
    public int MissingCount { get; set; }

    /// <summary>Tổng số mẫu dự toán năm trước (mọi dịch vụ) — KPI "So với năm trước"</summary>
    public int PreviousYearTotal { get; set; }

    public List<YearlyForecastRowDto> Items { get; set; } = new();
}

public class SaveYearlyForecastItemInput
{
    [Required]
    public Guid TestMethodIndicatorId { get; set; }

    /// <summary>null = xóa dự toán (chưa thiết lập)</summary>
    [Range(0, int.MaxValue)]
    public int? EstimatedSamples { get; set; }
}

public class SaveYearlyForecastsInput
{
    [Required]
    public int Year { get; set; }

    [Required]
    public List<SaveYearlyForecastItemInput> Items { get; set; } = new();
}

public class CloneYearlyForecastInput
{
    /// <summary>Năm đích — copy từ Year-1 vào các dòng còn trống</summary>
    [Required]
    public int Year { get; set; }
}

public class CloneYearlyForecastResultDto
{
    public int ClonedCount { get; set; }
}

public interface IYearlyForecastAppService : IApplicationService
{
    Task<YearlyForecastListDto> GetListAsync(GetYearlyForecastsInput input);

    Task<int> SaveAsync(SaveYearlyForecastsInput input);

    Task<CloneYearlyForecastResultDto> CloneFromPreviousYearAsync(CloneYearlyForecastInput input);
}
