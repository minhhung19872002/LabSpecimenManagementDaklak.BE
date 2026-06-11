using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace LabSpecimenManagement.TestBatches;

public class SaveTestBatchResultsDto
{
    public DateTime? TestDate { get; set; }

    [Required]
    public List<TestBatchItemResultDto> Items { get; set; } = new();
}

public class TestBatchItemResultDto
{
    [Required]
    public Guid ItemId { get; set; }

    [StringLength(500)]
    public string? Result { get; set; }

    public DateTime? TestDate { get; set; }
}
