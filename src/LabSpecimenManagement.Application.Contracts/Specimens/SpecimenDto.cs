using System;
using Volo.Abp.Application.Dtos;
using LabSpecimenManagement.TestRequests;

namespace LabSpecimenManagement.Specimens;

public class SpecimenDto : FullAuditedEntityDto<Guid>
{
    public Guid TestRequestId { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public string Type { get; set; }
    public DateTime ReceivedDate { get; set; }
    public SpecimenStatus Status { get; set; }
    public string? StorageLocation { get; set; }
    public string? Notes { get; set; }
    public string? Images { get; set; }
    public string? Deviation { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; }

    // Related data
    public string? TestRequestNumber { get; set; }
}
