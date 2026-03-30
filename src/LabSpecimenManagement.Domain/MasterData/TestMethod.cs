using System;
using LabSpecimenManagement.Common;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Phương pháp thử - Test Method
/// </summary>
public class TestMethod : FullAuditedAggregateRoot<Guid>, IHasActive
{
    /// <summary>
    /// Mã phương pháp - Auto generated: PPddmmyyyyxxx
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Tên phương pháp
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Tên phương pháp có định dạng (HTML)
    /// </summary>
    public string? FormattedName { get; set; }

    /// <summary>
    /// Đơn vị tính
    /// </summary>
    public string Unit { get; set; }

    /// <summary>
    /// Đơn vị tính có định dạng (HTML)
    /// </summary>
    public string? FormattedUnit { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Thứ tự hiển thị
    /// </summary>
    public int SortOrder { get; set; }

    protected TestMethod()
    {
    }

    public TestMethod(Guid id, string code, string name, string unit) : base(id)
    {
        Code = code;
        Name = name;
        Unit = unit;
        IsActive = true;
        SortOrder = 0;
    }
}
