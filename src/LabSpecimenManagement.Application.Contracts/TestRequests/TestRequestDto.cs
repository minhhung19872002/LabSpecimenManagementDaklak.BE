using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.TestRequests;

public class TestRequestDto : FullAuditedEntityDto<Guid>
{
    public string RequestNumber { get; set; }
    public DateTime RequestDate { get; set; }
    public string CustomerName { get; set; }
    public string? CustomerInfo { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public string? Notes { get; set; }
    public TestRequestStatus Status { get; set; }
    public DateTime? ExpectedCompletionDate { get; set; }
    public string? Subcontractor { get; set; }
}
