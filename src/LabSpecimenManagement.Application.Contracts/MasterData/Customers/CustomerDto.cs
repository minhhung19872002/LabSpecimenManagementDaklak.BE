using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.MasterData;

public class CustomerDto : FullAuditedEntityDto<Guid>
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? TaxCode { get; set; }
    public string? ContactPerson { get; set; }
    public string? ContactPhone { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }
}
