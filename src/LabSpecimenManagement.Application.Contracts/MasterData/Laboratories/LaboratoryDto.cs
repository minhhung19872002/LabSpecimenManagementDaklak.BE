using System;
using Volo.Abp.Application.Dtos;

namespace LabSpecimenManagement.MasterData;

public class LaboratoryDto : FullAuditedEntityDto<Guid>
{
    public string Code { get; set; }

    /// <summary>
    /// Name được compute từ Department.Name khi query
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Phòng ban liên kết - Department ID
    /// </summary>
    public Guid DepartmentId { get; set; }

    public Guid? TechnicalManagerId { get; set; }
    public string? TechnicalManagerName { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
}
