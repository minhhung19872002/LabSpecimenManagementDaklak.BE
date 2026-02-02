using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.MasterData;

public class AddIndicatorsToTestMethodDto
{
    [Required]
    public List<IndicatorWithPriceDto> Indicators { get; set; } = new();
}

public class IndicatorWithPriceDto
{
    [Required]
    public Guid IndicatorId { get; set; }

    /// <summary>
    /// Nền mẫu (Loại mẫu) - bắt buộc, cho phép cùng indicator thêm nhiều lần với nền mẫu khác nhau
    /// </summary>
    [Required(ErrorMessage = "Vui lòng chọn nền mẫu")]
    public Guid SpecimenTypeId { get; set; }

    [Required]
    [Range(0, double.MaxValue)]
    public decimal UnitPrice { get; set; }
}
