using System;

namespace LabSpecimenManagement.PreparedSolutions;

public class PreparedSolutionWarehouseProductDto
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int ProductType { get; set; }
    public string? ProductTypeName { get; set; }
    public string? UnitName { get; set; }
    public decimal AvailableQuantity { get; set; }
}
