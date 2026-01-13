using System;
using LabSpecimenManagement.Common;
using Volo.Abp.Domain.Entities.Auditing;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Chỉ tiêu - Indicator
/// </summary>
public class Indicator : FullAuditedAggregateRoot<Guid>, IHasActive
{
    /// <summary>
    /// Mã chỉ tiêu (tự động sinh: CT-0001, CT-0002...)
    /// </summary>
    public string Code { get; set; }

    /// <summary>
    /// Tên chỉ tiêu (plain text)
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Tên chỉ tiêu có định dạng (HTML sanitized - bold, italic, underline, sub, sup)
    /// Nếu null thì sử dụng Name
    /// </summary>
    public string? FormattedName { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// ID nhóm chỉ tiêu
    /// </summary>
    public Guid IndicatorGroupId { get; set; }

    /// <summary>
    /// Thứ tự hiển thị
    /// </summary>
    public int SortOrder { get; set; }

    protected Indicator()
    {
    }

    public Indicator(Guid id, string code, string name, Guid indicatorGroupId) : base(id)
    {
        Code = code;
        Name = name;
        IndicatorGroupId = indicatorGroupId;
        IsActive = true;
        SortOrder = 0;
    }
}
