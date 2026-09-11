using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.MasterData;

public class WarehouseDto : FullAuditedEntityDto<Guid>
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string? PhysicalLocation { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }
    public Guid? ParentWarehouseId { get; set; }
    public string? ParentWarehouseName { get; set; }
    public int Level { get; set; }
    public Guid? LaboratoryId { get; set; }
    public string? LaboratoryName { get; set; }
}
