using System;
using Volo.Abp.Application.Dtos;
using LabSpecimenManagement.TestRequests;

namespace LabSpecimenManagement.Tests;

public class TestDto : FullAuditedEntityDto<Guid>
{
    public Guid SpecimenId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Method { get; set; }
    public string? Standard { get; set; }
    public TestStatus Status { get; set; }
    public Guid? AssigneeId { get; set; }
    public string? AssigneeName { get; set; }
    public DateTime? AssignmentDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? CompletionDate { get; set; }
    public decimal? EstimatedDuration { get; set; }
    public string? Notes { get; set; }

    // Navigation properties
    public string? SpecimenCode { get; set; }
    public string? SpecimenName { get; set; }
}
