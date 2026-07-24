using System.Collections.Generic;

namespace LabSpecimenManagement.TestBatches;

public class SaveConsumptionDraftDto
{
    public List<ConsumptionLineInputDto> ConsumptionLines { get; set; } = new();
}
