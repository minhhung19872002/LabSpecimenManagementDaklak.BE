using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.MasterData.Organizations;

public class OrganizationDto : FullAuditedEntityDto<Guid>
{
    public string Name { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}
