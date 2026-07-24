using System;
using System.Collections.Generic;
using LabSpecimenManagement.TestBatches;

namespace LabSpecimenManagement.Testing;

public class SubmitIndicatorWithConsumptionDto
{
    public Guid IndicatorId { get; set; }
    public DateTime TestDate { get; set; }
    public List<ConsumptionLineInputDto> ConsumptionLines { get; set; } = new();
}

public class SaveIndicatorConsumptionDraftDto
{
    public Guid IndicatorId { get; set; }
    public List<ConsumptionLineInputDto> ConsumptionLines { get; set; } = new();
}

public class SubmitIndicatorItemDto
{
    public Guid IndicatorId { get; set; }
    public DateTime TestDate { get; set; }
}

public class BulkSubmitIndicatorWithConsumptionDto
{
    public Guid SampleId { get; set; }
    public List<SubmitIndicatorItemDto> Indicators { get; set; } = new();
    public List<ConsumptionLineInputDto> ConsumptionLines { get; set; } = new();
}
