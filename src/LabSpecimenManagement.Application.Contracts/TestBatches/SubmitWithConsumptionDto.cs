using System;
using System.Collections.Generic;

namespace LabSpecimenManagement.TestBatches;

public class SubmitWithConsumptionDto
{
    public List<Guid> ItemIds { get; set; } = new();
    public List<ConsumptionLineInputDto> ConsumptionLines { get; set; } = new();
}

public class ConsumptionLineInputDto
{
    public Guid? ConsumptionId { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? EquipmentId { get; set; }
    public Guid? UnitId { get; set; }
    public decimal RecommendedQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
}
