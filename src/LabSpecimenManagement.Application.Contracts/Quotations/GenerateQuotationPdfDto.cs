using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.Quotations;

public class GenerateQuotationPdfDto
{
    public string? CustomerName { get; set; }

    [Required]
    public List<QuotationEmailItemDto> Items { get; set; } = new();

    public decimal TotalPrice { get; set; }
}
