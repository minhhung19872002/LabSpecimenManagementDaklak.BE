using System;
using LabSpecimenManagement.Common;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Đơn vị hàng hóa - Unit of measurement
/// </summary>
public class Unit : FullAuditedAggregateRoot<Guid>, IHasActive
{
    /// <summary>
    /// Mã đơn vị hàng hóa
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Tên đơn vị hàng hóa
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; }

    protected Unit()
    {
    }

    public Unit(Guid id, string code, string name) : base(id)
    {
        Code = code;
        Name = name;
        IsActive = true;
    }
}
