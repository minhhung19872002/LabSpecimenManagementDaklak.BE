using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.Quotations;

public class GenerateQuotationPdfDto
{
    public string? CustomerName { get; set; }

    [Required]
    public List<QuotationPdfSampleDto> Samples { get; set; } = new();

    public decimal TotalPrice { get; set; }
}

/// <summary>
/// Sample data for PDF generation
/// </summary>
public class QuotationPdfSampleDto
{
    public string SampleName { get; set; } = string.Empty;

    public int Quantity { get; set; } = 1;

    public List<QuotationPdfIndicatorDto> Indicators { get; set; } = new();
}

/// <summary>
/// Indicator data for PDF generation
/// </summary>
public class QuotationPdfIndicatorDto
{
    public string IndicatorName { get; set; } = string.Empty;

    public int Quantity { get; set; } = 1;

    public decimal UnitPrice { get; set; }
}
