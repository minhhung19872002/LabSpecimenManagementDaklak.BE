using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LabSpecimenManagement.MasterData.BatchNorms;

namespace LabSpecimenManagement.MasterData.YearlyForecasts;

// ───────────────────────── Phiếu tạo / sửa dự toán mẫu năm ─────────────────────────

public class GetForecastCreateOptionsInput
{
    [Required]
    public int Year { get; set; }
}

/// <summary>1 dòng vật tư trong định mức theo mẻ của bộ (lượng "cho 1 mẫu")</summary>
public class ForecastNormMaterialDto
{
    public Guid ProductId { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitName { get; set; }
    public decimal Quantity { get; set; }
}

public class ForecastExtraMaterialDto
{
    [Required]
    public Guid ProductId { get; set; }

    [Required]
    public Guid UnitId { get; set; }

    /// <summary>Lượng tiêu hao — theo <see cref="Mode"/> (null = chưa nhập)</summary>
    public decimal? Quantity { get; set; }

    public ForecastQuantityMode Mode { get; set; }
}

/// <summary>Cấu hình dự toán đã lưu của 1 bộ trong năm</summary>
public class ForecastConfigDto
{
    public int? EstimatedSamples { get; set; }

    /// <summary>Áp dụng tiêu hao theo bộ (dịch vụ) này</summary>
    public Guid? SourceTestMethodIndicatorId { get; set; }

    public List<ForecastExtraMaterialDto> ExtraMaterials { get; set; } = new();
    public List<Guid> ExtraEquipmentIds { get; set; } = new();
    public List<Guid> HiddenMaterialIds { get; set; } = new();
    public List<Guid> HiddenEquipmentIds { get; set; } = new();
}

/// <summary>1 bộ Nền mẫu × Chỉ tiêu × Phương pháp để chọn vào phiếu dự toán</summary>
public class ForecastComboDto : BatchNormComboDto
{
    /// <summary>Vật tư của định mức theo mẻ đang sử dụng (rỗng nếu chưa có định mức)</summary>
    public List<ForecastNormMaterialDto> NormMaterials { get; set; } = new();

    /// <summary>Thiết bị của định mức theo mẻ</summary>
    public List<Guid> NormEquipmentIds { get; set; } = new();

    /// <summary>Dự toán năm đang chọn (null = chưa từng lưu)</summary>
    public ForecastConfigDto? Forecast { get; set; }
}

public class ForecastCreateOptionsDto
{
    public int Year { get; set; }
    public List<ForecastComboDto> Combos { get; set; } = new();
    public List<BatchNormProductOptionDto> Materials { get; set; } = new();
    public List<BatchNormProductOptionDto> Equipments { get; set; } = new();
}

public class SaveForecastDetailItemInput : ForecastConfigDto
{
    [Required]
    public Guid TestMethodIndicatorId { get; set; }
}

public class SaveForecastDetailsInput
{
    [Required]
    public int Year { get; set; }

    /// <summary>true = phiếu tạo mới: chặn bộ đã có dự toán năm này</summary>
    public bool IsCreate { get; set; }

    [Required]
    public List<SaveForecastDetailItemInput> Items { get; set; } = new();
}

public class SaveForecastDetailsResultDto
{
    public int Count { get; set; }
    public int TotalSamples { get; set; }
}
