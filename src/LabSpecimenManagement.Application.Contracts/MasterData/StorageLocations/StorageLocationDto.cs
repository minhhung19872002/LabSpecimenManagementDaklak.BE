using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.MasterData;

public class StorageLocationDto : FullAuditedEntityDto<Guid>
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? Temperature { get; set; }
    public string? Humidity { get; set; }
    public int? Capacity { get; set; }
    public int CurrentCount { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}
