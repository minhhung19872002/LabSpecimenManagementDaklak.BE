using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.TestBatches;

public class ConfirmConsumptionDto
{
    [Required]
    public List<ConsumptionItemDto> Items { get; set; } = new();
}

public class ConsumptionItemDto
{
    [Required]
    public Guid ConsumptionId { get; set; }

    [Range(0, double.MaxValue)]
    public decimal ActualQuantity { get; set; }
}
