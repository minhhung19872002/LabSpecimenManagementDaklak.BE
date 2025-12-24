using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.MasterData.Organizations;

public class OrganizationDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; }
    public string ProvinceCode { get; set; }
    public string WardCode { get; set; }
    public string Address { get; set; }
    public string TaxCode { get; set; }
    public string ContactPerson { get; set; }
    public string Phone { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}
