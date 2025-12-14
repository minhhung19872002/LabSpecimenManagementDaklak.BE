using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.NCRs;

public class NCRDto : FullAuditedEntityDto<Guid>
{
    public string NCRNumber { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public NCRType Type { get; set; }
    public NCRSeverity Severity { get; set; }
    public NCRStatus Status { get; set; }
    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityCode { get; set; }
    public string? Department { get; set; }
    public Guid ReportedById { get; set; }
    public string? ReportedByName { get; set; }
    public DateTime ReportedAt { get; set; }
    public Guid? AssignedToId { get; set; }
    public string? AssignedToName { get; set; }
    public string? RootCause { get; set; }
    public string? CorrectiveAction { get; set; }
    public string? PreventiveAction { get; set; }
    public DateTime DueDate { get; set; }
    public Guid? ClosedById { get; set; }
    public string? ClosedByName { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? Notes { get; set; }
}
