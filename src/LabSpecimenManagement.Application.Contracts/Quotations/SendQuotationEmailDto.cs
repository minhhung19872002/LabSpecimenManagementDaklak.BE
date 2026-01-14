using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.Quotations;

public class SendQuotationEmailDto
{
    [Required]
    public List<string> To { get; set; } = new();

    public List<string>? Cc { get; set; }

    [Required]
    public string Subject { get; set; } = string.Empty;

    [Required]
    public string Body { get; set; } = string.Empty;

    public string? CustomerName { get; set; }

    [Required]
    public List<QuotationEmailItemDto> Items { get; set; } = new();

    public decimal TotalPrice { get; set; }
}

public class QuotationEmailItemDto
{
    public int No { get; set; }
    public string Parameter { get; set; } = string.Empty;
    public string TestMethod { get; set; } = string.Empty;
    public int NumberOfSample { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Price { get; set; }
}
