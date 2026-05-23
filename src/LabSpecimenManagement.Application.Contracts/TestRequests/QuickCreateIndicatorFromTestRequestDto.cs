using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LabSpecimenManagement.MasterData.Indicators;

namespace LabSpecimenManagement.TestRequests;

public class QuickCreateIndicatorsFromTestRequestDto
{
    [Required(ErrorMessage = "Vui lòng chọn nhóm chỉ tiêu")]
    public Guid IndicatorGroupId { get; set; }

    public Guid? TestRequestId { get; set; }

    [StringLength(64)]
    public string? ContractNumber { get; set; }

    [Required]
    [MinLength(1, ErrorMessage = "Vui lòng thêm ít nhất 1 chỉ tiêu")]
    public List<QuickCreateIndicatorItemDto> Indicators { get; set; } = new();
}

public class QuickCreateIndicatorItemDto
{
    [Required(ErrorMessage = "Vui lòng chọn nền mẫu")]
    public Guid SpecimenTypeId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập tên chỉ tiêu")]
    [StringLength(256, ErrorMessage = "Tên chỉ tiêu không được vượt quá 256 ký tự")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? FormattedName { get; set; }

    public decimal UnitPrice { get; set; }

    public List<QuickCreateSubIndicatorItemDto> SubIndicators { get; set; } = new();
}

public class QuickCreateSubIndicatorItemDto
{
    [Required(ErrorMessage = "Vui lòng nhập tên chỉ tiêu phụ")]
    [StringLength(256, ErrorMessage = "Tên chỉ tiêu không được vượt quá 256 ký tự")]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? FormattedName { get; set; }
}

public class QuickCreateIndicatorsFromTestRequestResultDto
{
    public List<QuickCreateIndicatorResultItemDto> Results { get; set; } = new();
}

public class QuickCreateIndicatorResultItemDto
{
    public IndicatorDto Indicator { get; set; } = new();
    public Guid SpecimenTypeId { get; set; }
    public string SpecimenTypeName { get; set; } = string.Empty;
    public Guid? TestMethodId { get; set; }
    public string? TestMethodName { get; set; }
    public decimal UnitPrice { get; set; }
    public List<QuickCreateIndicatorResultItemDto> SubIndicatorResults { get; set; } = new();
}
