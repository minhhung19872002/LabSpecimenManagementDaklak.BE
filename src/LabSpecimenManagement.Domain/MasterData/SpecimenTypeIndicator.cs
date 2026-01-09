using System;
using LabSpecimenManagement.Common;
using Volo.Abp.Domain.Entities;

namespace LabSpecimenManagement.MasterData;

/// <summary>
/// Bảng trung gian liên kết Loại mẫu và Chỉ tiêu (Many-to-Many)
/// </summary>
public class SpecimenTypeIndicator : Entity<Guid>, IHasActive
{
    /// <summary>
    /// ID Loại mẫu
    /// </summary>
    public Guid SpecimenTypeId { get; set; }

    /// <summary>
    /// ID Chỉ tiêu
    /// </summary>
    public Guid IndicatorId { get; set; }

    /// <summary>
    /// Trạng thái hoạt động
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Thứ tự hiển thị
    /// </summary>
    public int SortOrder { get; set; }

    protected SpecimenTypeIndicator()
    {
    }

    public SpecimenTypeIndicator(Guid id, Guid specimenTypeId, Guid indicatorId) : base(id)
    {
        SpecimenTypeId = specimenTypeId;
        IndicatorId = indicatorId;
        IsActive = true;
        SortOrder = 0;
    }
}
