using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.MasterData;

public class LaboratoryDto : FullAuditedEntityDto<Guid>
{
    public string Code { get; set; }
    public string Name { get; set; }
    public Guid? TechnicalManagerId { get; set; }
    public string? TechnicalManagerName { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}
