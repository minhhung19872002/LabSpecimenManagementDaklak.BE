using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.MasterData;

public class ManufacturerDto : FullAuditedEntityDto<Guid>
{
    public string Code { get; set; }
    public string Name { get; set; }
    public bool IsActive { get; set; }
}
