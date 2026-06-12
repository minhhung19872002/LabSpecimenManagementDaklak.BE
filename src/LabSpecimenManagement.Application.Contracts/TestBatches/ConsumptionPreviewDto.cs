using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.TestBatches;

public class ConsumptionPreviewDto
{
    public bool IsFirstSubmit { get; set; }
    public List<ConsumptionPreviewLineDto> Lines { get; set; } = new();
}

public class ConsumptionPreviewLineDto
{
    public Guid? ConsumptionId { get; set; }
    public Guid? ProductId { get; set; }
    public string? ProductName { get; set; }
    public string? ProductCode { get; set; }
    public int? ProductType { get; set; }
    public decimal? AvailableQuantity { get; set; }
    public Guid? EquipmentId { get; set; }
    public string? EquipmentName { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitName { get; set; }
    public decimal RecommendedQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public bool IsDeducted { get; set; }
    public bool ExceedsStock { get; set; }
}
