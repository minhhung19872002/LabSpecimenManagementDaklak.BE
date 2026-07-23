using System;
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
    /// <summary>
    /// Indicator ID (for parent-child grouping)
    /// </summary>
    public Guid? IndicatorId { get; set; }

    /// <summary>
    /// Parent indicator ID (null if root indicator)
    /// </summary>
    public Guid? ParentIndicatorId { get; set; }

    public string IndicatorName { get; set; } = string.Empty;

    /// <summary>
    /// Formatted name with HTML (bold, italic, sub, sup)
    /// </summary>
    public string? IndicatorFormattedName { get; set; }

    public int Quantity { get; set; } = 1;

    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Ghi chú cho chỉ tiêu
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Combo group key (e.g. "hm_specimenTypeId" for heavy metal, "ec_specimenTypeId" for E.coli+Coliform).
    /// Indicators with the same non-null ComboGroup get their Note cells merged in the PDF.
    /// </summary>
    public string? ComboGroup { get; set; }
}
