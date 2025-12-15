using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.MasterData;

public class TestMethodDto : FullAuditedEntityDto<Guid>
{
    public string Code { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string? Standard { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public int? EstimatedDurationHours { get; set; }
    public decimal? Price { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}
