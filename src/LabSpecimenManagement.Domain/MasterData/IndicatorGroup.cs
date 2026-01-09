using System;
using LabSpecimenManagement.Common;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Nhóm chỉ tiêu - Indicator Group
/// </summary>
public class IndicatorGroup : FullAuditedAggregateRoot<Guid>, IHasActive
{
    /// <summary>
    /// Tên nhóm chỉ tiêu
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Khoa phòng thực hiện (không bắt buộc) - để auto-fill khi gửi QLKT
    /// </summary>
    public Guid? LaboratoryId { get; set; }

    protected IndicatorGroup()
    {
    }

    public IndicatorGroup(Guid id, string name, Guid? laboratoryId = null) : base(id)
    {
        Name = name;
        LaboratoryId = laboratoryId;
        IsActive = true;
    }
}
